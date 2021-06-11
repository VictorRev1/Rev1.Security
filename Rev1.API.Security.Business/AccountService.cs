using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rev1.API.Security.Business.Contract;
using Rev1.API.Security.Business.Entities;
using BC = BCrypt.Net.BCrypt;
using Rev1.API.Security.Utils.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Rev1.API.Security.Data.Contract;
using System.Threading.Tasks;

namespace Rev1.API.Security.Business
{
    public class AccountService : IAccountService
    {       
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;
        private readonly IEmailService _emailService;        

        public AccountService(           
            IAccountRepository accountRepository,
            IMapper mapper,
            IOptions<AppSettings> appSettings,
            IEmailService emailService)
        {            
            _accountRepository = accountRepository;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _emailService = emailService;
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            //var account = _context.Accounts.SingleOrDefault(x => x.Email == model.Email);
            var accountDto = _accountRepository.GetByEmail(model.Email);

            var account = _mapper.Map<Account>(accountDto);            

            if (account == null || !account.IsVerified || !BC.Verify(model.Password, account.PasswordHash))
                throw new AppException("Email or password is incorrect");

            // authentication successful so generate jwt and refresh tokens
            var jwtToken = generateJwtToken(account);
            var refreshToken = generateRefreshToken(ipAddress);
            account.RefreshTokens.Add(refreshToken);

            // remove old refresh tokens from account
            removeOldRefreshTokens(account);

            // save changes to db
            //_context.Update(account);
            //_context.SaveChanges();

            //_accountRepository.AddAsync(accountDto);
            //_accountRepository.CommitAsync();

            accountDto = _mapper.Map<Data.Entities.Account>(account);

            _accountRepository.Update(accountDto);
            _accountRepository.SaveChanges();

            var response = _mapper.Map<AuthenticateResponse>(account);
            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;
            return response;
        }

        public AuthenticateResponse RefreshToken(string token, string ipAddress)
        {
            var (refreshToken, account) = getRefreshToken(token);

            // replace old refresh token with a new one and save
            var newRefreshToken = generateRefreshToken(ipAddress);
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            account.RefreshTokens.Add(newRefreshToken);

            removeOldRefreshTokens(account);

            //_context.Update(account);
            //_context.SaveChanges();
            var accountDto = _mapper.Map<Data.Entities.Account>(account);           

            _accountRepository.Update(accountDto);
            _accountRepository.SaveChanges();

            //_accountRepository.AddAsync(accountDto);
            //_accountRepository.CommitAsync();           

            // generate new jwt
            var jwtToken = generateJwtToken(account);

            var response = _mapper.Map<AuthenticateResponse>(account);
            response.JwtToken = jwtToken;
            response.RefreshToken = newRefreshToken.Token;
            return response;
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var (refreshToken, account) = getRefreshToken(token);

            // revoke token and save
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            //_context.Update(account);
            //_context.SaveChanges();
            var accountDto = _mapper.Map<Data.Entities.Account>(account);

            _accountRepository.Update(accountDto);
            _accountRepository.SaveChanges();

            //await _accountRepository.AddAsync(accountDto);
            //await _accountRepository.CommitAsync();           
        }

        public void Register(RegisterRequest model, string origin)
        {
            // validate     
            var isAccountRegistered = _accountRepository.Any(model.Email);
            if (isAccountRegistered == true)
            {
                // send already registered error in email to prevent account enumeration
                sendAlreadyRegisteredEmail(model.Email, origin);
                return;
            }

            // map model to new account object
            var account = _mapper.Map<Account>(model);

            // first registered account is an admin           
            var isFirstAccount = _accountRepository.Count() == 0;  
            
            account.Role = isFirstAccount ? Role.Admin : Role.User;
            account.Created = DateTime.UtcNow;
            account.VerificationToken = randomTokenString();

            // hash password
            account.PasswordHash = BC.HashPassword(model.Password);

            // save account
            //_context.Accounts.Add(account);
            //_context.SaveChanges();

            //await _accountRepository.AddAsync(accountDto);
            //await _accountRepository.CommitAsync();

            var accountDto = _mapper.Map<Data.Entities.Account>(account);        
            _accountRepository.Add(accountDto);
            _accountRepository.SaveChanges();

            // send email
            sendVerificationEmail(account, origin);
        }

        public void VerifyEmail(string token)
        {
            //var account = _context.Accounts.SingleOrDefault(x => x.VerificationToken == token);
            var accountDto = _accountRepository.GetByVerificationToken(token);

            // map model to new account object
            var account = _mapper.Map<Account>(accountDto);

            if (account == null) throw new AppException("Verification failed");

            account.Verified = DateTime.UtcNow;
            account.VerificationToken = null;

            accountDto = _mapper.Map<Data.Entities.Account>(account);

            //_context.Accounts.Update(account);
            //_context.SaveChanges();

            //await _accountRepository.UpdateAsync(accountDto);
            //await _accountRepository.CommitAsync(); 

            _accountRepository.Update(accountDto);
            _accountRepository.SaveChanges();
        }

        public void ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            //var account = _context.Accounts.SingleOrDefault(x => x.Email == model.Email);
            var accountDto = _accountRepository.GetByEmail(model.Email);

            // map model to new account object
            var account = _mapper.Map<Account>(accountDto);

            // always return ok response to prevent email enumeration
            if (account == null) return;

            // create reset token that expires after 1 day
            account.ResetToken = randomTokenString();
            account.ResetTokenExpires = DateTime.UtcNow.AddDays(1);

            //_context.Accounts.Update(account);
            //_context.SaveChanges();

            accountDto = _mapper.Map<Data.Entities.Account>(account);
            //await _accountRepository.UpdateAsync(accountDto);
            //await _accountRepository.CommitAsync();

            _accountRepository.Update(accountDto);
            _accountRepository.SaveChanges();

            // send email
            sendPasswordResetEmail(account, origin);
        }

        public void ValidateResetToken(ValidateResetTokenRequest model)
        {
            //var account = _context.Accounts.SingleOrDefault(x =>
            //    x.ResetToken == model.Token &&
            //    x.ResetTokenExpires > DateTime.UtcNow);

            //accountDto = await _accountRepository.FirstOrDefaultAsync(x =>
            //  x.ResetToken == model.Token &&
            //  x.ResetTokenExpires > DateTime.UtcNow);

            var accountDto = _accountRepository.GetByResetToken(model.Token);
            var account = _mapper.Map<Account>(accountDto);

            if (account == null)
                throw new AppException("Invalid token");
        }

        public void ResetPassword(ResetPasswordRequest model)
        {
            //var account = _context.Accounts.SingleOrDefault(x =>
            //    x.ResetToken == model.Token &&
            //    x.ResetTokenExpires > DateTime.UtcNow);

            //accountDto = await _accountRepository.FirstOrDefaultAsync(x =>
            //    x.ResetToken == model.Token &&
            //    x.ResetTokenExpires > DateTime.UtcNow);

            var accountDto = _accountRepository.GetByResetToken(model.Token);

            var account = _mapper.Map<Account>(accountDto);

            if (account == null)
                throw new AppException("Invalid token");

            // update password and remove reset token
            account.PasswordHash = BC.HashPassword(model.Password);
            account.PasswordReset = DateTime.UtcNow;
            account.ResetToken = null;
            account.ResetTokenExpires = null;

            accountDto = _mapper.Map<Data.Entities.Account>(account);

            //_context.Accounts.Update(account);
            //_context.SaveChanges();

            //await _accountRepository.UpdateAsync(accountDto);
            //await _accountRepository.CommitAsync();

            _accountRepository.Update(accountDto);
            _accountRepository.SaveChanges();
        }

        public IEnumerable<AccountResponse> GetAllAsync()
        {
            //var accounts = _context.Accounts;
            var accountsDto = _accountRepository.GetAll();

            var accounts = _mapper.Map<List<Account>>(accountsDto);
            return _mapper.Map<List<AccountResponse>>(accounts);
        }

        public Task<AccountResponse> GetById(int id)
        {
            var account = getAccount(id);
            return _mapper.Map<Task<AccountResponse>>(account);
        }

        public Task<AccountResponse> Create(CreateRequest model)
        {
            // validate
            //if (_context.Accounts.Any(x => x.Email == model.Email))
            //    throw new AppException($"Email '{model.Email}' is already registered");

            //if (await _accountRepository.AnyAsync(x => x.Email == model.Email))
            //    throw new AppException($"Email '{model.Email}' is already registered");

            if(_accountRepository.Any(model.Email))
                throw new AppException($"Email '{model.Email}' is already registered");

            // map model to new account object
            var account = _mapper.Map<Account>(model);
            account.Created = DateTime.UtcNow;
            account.Verified = DateTime.UtcNow;

            // hash password
            account.PasswordHash = BC.HashPassword(model.Password);

            // save account
            //_context.Accounts.Add(account);
            //_context.SaveChanges();

            //await _accountRepository.AddAsync(accountDto);
            //await _accountRepository.CommitAsync();

            var accountDto = _mapper.Map<Data.Entities.Account>(account);
            _accountRepository.Add(accountDto);
            _accountRepository.SaveChanges();

            return _mapper.Map<Task<AccountResponse>>(account);
        }

        public Task<AccountResponse> Update(int id, UpdateRequest model)
        {
            var account = getAccount(id);

            // validate
            //if (account.Email != model.Email && _context.Accounts.Any(x => x.Email == model.Email))
            //    throw new AppException($"Email '{model.Email}' is already taken");

            // validate
            if (account.Email != model.Email && _accountRepository.Any(model.Email))
                throw new AppException($"Email '{model.Email}' is already taken");

            // hash password if it was entered
            if (!string.IsNullOrEmpty(model.Password))
                account.PasswordHash = BC.HashPassword(model.Password);

            // copy model to account and save
            _mapper.Map(model, account);
            account.Updated = DateTime.UtcNow;

            //_context.Accounts.Update(account);
            //_context.SaveChanges();

            var accountDto = _mapper.Map<Data.Entities.Account>(account);
            //await _accountRepository.UpdateAsync(accountDto);
            //await _accountRepository.CommitAsync();

            _accountRepository.Update(accountDto);
            _accountRepository.SaveChanges();

            return _mapper.Map<Task<AccountResponse>>(account);
        }

        public void Delete(int id)
        {
            //_context.Accounts.Remove(account);
            //_context.SaveChanges();

            //await _accountRepository.RemoveAsync(accountDto);
            //await _accountRepository.CommitAsync();   

            _accountRepository.Delete(id);
            _accountRepository.SaveChanges();
        }

        // helper methods

        private Account getAccount(int id)
        {
            //var account = _context.Accounts.Find(id);
            var accountDto = _accountRepository.GetById(id);

            var account = _mapper.Map<Account>(accountDto);

            if (account == null) throw new KeyNotFoundException("Account not found");
            return account;
        }

        private (RefreshToken, Account) getRefreshToken(string token)
        {
            //var account = _context.Accounts.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
            //accountDto = await _accountRepository.FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

            var accountDto = _accountRepository.GetByRefreshToken(token);

            var account = _mapper.Map<Account>(accountDto);

            if (account == null) throw new AppException("Invalid token");
            var refreshToken = account.RefreshTokens.Single(x => x.Token == token);
            if (!refreshToken.IsActive) throw new AppException("Invalid token");
            return (refreshToken, account);
        }

        private string generateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            //var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var key = Encoding.ASCII.GetBytes(new Guid().ToString());
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken generateRefreshToken(string ipAddress)
        {
            return new RefreshToken
            {
                Token = randomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        private void removeOldRefreshTokens(Account account)
        {
            account.RefreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.Created.AddDays(_appSettings.RefreshTokenTTL) <= DateTime.UtcNow);
        }

        private string randomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        private void sendVerificationEmail(Account account, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var verifyUrl = $"{origin}/account/verify-email?token={account.VerificationToken}";
                message = $@"<p>Please click the below link to verify your email address:</p>
                             <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to verify your email address with the <code>/accounts/verify-email</code> api route:</p>
                             <p><code>{account.VerificationToken}</code></p>";
            }

            _emailService.Send(
                to: account.Email,
                subject: "Sign-up Verification API - Verify Email",
                html: $@"<h4>Verify Email</h4>
                         <p>Thanks for registering!</p>
                         {message}"
            );
        }

        private void sendAlreadyRegisteredEmail(string email, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
                message = $@"<p>If you don't know your password please visit the <a href=""{origin}/account/forgot-password"">forgot password</a> page.</p>";
            else
                message = "<p>If you don't know your password you can reset it via the <code>/accounts/forgot-password</code> api route.</p>";

            _emailService.Send(
                to: email,
                subject: "Sign-up Verification API - Email Already Registered",
                html: $@"<h4>Email Already Registered</h4>
                         <p>Your email <strong>{email}</strong> is already registered.</p>
                         {message}"
            );
        }

        private void sendPasswordResetEmail(Account account, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var resetUrl = $"{origin}/account/reset-password?token={account.ResetToken}";
                message = $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                             <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to reset your password with the <code>/accounts/reset-password</code> api route:</p>
                             <p><code>{account.ResetToken}</code></p>";
            }

            _emailService.Send(
                to: account.Email,
                subject: "Sign-up Verification API - Reset Password",
                html: $@"<h4>Reset Password Email</h4>
                         {message}"
            );
        }
    }
}
