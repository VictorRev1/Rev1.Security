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
    public class HrUserService : IHrUserService
    {
        private readonly IHrUserRepository _hrUserRepository;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;
        private readonly IEmailService _emailService;

        public HrUserService(
            IHrUserRepository hrUserRepository,
            IMapper mapper,
            IOptions<AppSettings> appSettings,
            IEmailService emailService)
        {
            _hrUserRepository = hrUserRepository;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _emailService = emailService;
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var hrUserDto = _hrUserRepository.GetByEmail(model.Email);

            var hrUser = _mapper.Map<HrUser>(hrUserDto);

            if (hrUser == null || !hrUser.IsVerified || !BC.Verify(model.Password, hrUser.PasswordHash))
                throw new AppException("Email or password is incorrect");

            // authentication successful so generate jwt and refresh tokens
            var jwtToken = generateJwtToken(hrUser);
            var refreshToken = generateRefreshToken(ipAddress);
            hrUser.RefreshTokens.Add(refreshToken);

            // remove old refresh tokens from account
            removeOldRefreshTokens(hrUser);

            hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);

            _hrUserRepository.Update(hrUserDto);
            _hrUserRepository.SaveChanges();

            var response = _mapper.Map<AuthenticateResponse>(hrUser);
            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;
            return response;
        }

        public AuthenticateResponse RefreshToken(string token, string ipAddress)
        {
            var (refreshToken, hrUser) = getRefreshToken(token);

            // replace old refresh token with a new one and save
            var newRefreshToken = generateRefreshToken(ipAddress);
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            hrUser.RefreshTokens.Add(newRefreshToken);

            removeOldRefreshTokens(hrUser);

            var hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);

            _hrUserRepository.Update(hrUserDto);
            _hrUserRepository.SaveChanges();                  

            // generate new jwt
            var jwtToken = generateJwtToken(hrUser);

            var response = _mapper.Map<AuthenticateResponse>(hrUser);
            response.JwtToken = jwtToken;
            response.RefreshToken = newRefreshToken.Token;
            return response;
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var (refreshToken, hrUser) = getRefreshToken(token);

            // revoke token and save
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            
            var hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);

            _hrUserRepository.Update(hrUserDto);
            _hrUserRepository.SaveChanges();
        }

        public void Register(RegisterRequest model, string origin)
        {
            // validate     

            // TODO: add error handling...

            var isHrUserRegistered = _hrUserRepository.Any(model.Email);
            if (isHrUserRegistered == true)
            {
                // send already registered error in email to prevent account enumeration
                sendAlreadyRegisteredEmail(model.Email, origin);
                return;
            }

            // map model to new account object
            var hrUser = _mapper.Map<HrUser>(model);

            // first registered hrUser is an admin           
            var isFirstHrUser = _hrUserRepository.Count() == 0;

            hrUser.HrRole = isFirstHrUser ? HrRole.Admin : HrRole.User;
            hrUser.Created = DateTime.UtcNow;
            hrUser.VerificationToken = randomTokenString();

            // hash password
            hrUser.PasswordHash = BC.HashPassword(model.Password);

            var hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);
            _hrUserRepository.Add(hrUserDto);
            _hrUserRepository.SaveChanges();

            // send email
            sendVerificationEmail(hrUser, origin);
        }

        public Task<HrUserResponse> Create(CreateRequest model)
        {
            if (_hrUserRepository.Any(model.Email))
                throw new AppException($"Email '{model.Email}' is already registered");

            // map model to new HRUser object
            var hrUser = _mapper.Map<HrUser>(model);
            hrUser.Created = DateTime.UtcNow;
            hrUser.Verified = DateTime.UtcNow;

            // hash password
            hrUser.PasswordHash = BC.HashPassword(model.Password);

            var hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);
            _hrUserRepository.Add(hrUserDto);
            _hrUserRepository.SaveChanges();

            return _mapper.Map<Task<HrUserResponse>>(hrUser);
        }

        public void Delete(int id)
        {
            _hrUserRepository.Delete(id);
            _hrUserRepository.SaveChanges();
        }

        public void ForgotPassword(ForgotPasswordRequest model, string origin)
        {            
            var hrUserDto = _hrUserRepository.GetByEmail(model.Email);

            // map model to new account object
            var hrUser = _mapper.Map<HrUser>(hrUserDto);

            // always return ok response to prevent email enumeration
            if (hrUser == null) return;

            // create reset token that expires after 1 day
            hrUser.ResetToken = randomTokenString();
            hrUser.ResetTokenExpires = DateTime.UtcNow.AddDays(1);

            hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);

            _hrUserRepository.Update(hrUserDto);
            _hrUserRepository.SaveChanges();

            // send email
            sendPasswordResetEmail(hrUser, origin);
        }

        public void ValidateResetToken(ValidateResetTokenRequest model)
        {
            var hrUserDto = _hrUserRepository.GetByResetToken(model.Token);
            var hrUser = _mapper.Map<HrUser>(hrUserDto);

            if (hrUser == null)
                throw new AppException("Invalid token");
        }

        public void ResetPassword(ResetPasswordRequest model)
        {
            var hrUserDto = _hrUserRepository.GetByResetToken(model.Token);

            var hrUser  = _mapper.Map<HrUser>(hrUserDto);

            if (hrUser == null)
                throw new AppException("Invalid token");

            // update password and remove reset token
            hrUser.PasswordHash = BC.HashPassword(model.Password);
            hrUser.PasswordReset = DateTime.UtcNow;
            hrUser.ResetToken = null;
            hrUser.ResetTokenExpires = null;

            hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);

            _hrUserRepository.Update(hrUserDto);
            _hrUserRepository.SaveChanges();
        }

        public IEnumerable<HrUserResponse> GetAll()
        {
            var hrUserDto = _hrUserRepository.GetAll();

            var hrUsers = _mapper.Map<List<HrUser>>(hrUserDto);
            return _mapper.Map<List<HrUserResponse>>(hrUsers);
        }

        public Task<HrUserResponse> GetById(int id)
        {
            var hrUser = gethrUser(id);
            return _mapper.Map<Task<HrUserResponse>>(hrUser);
        }           

        public Task<HrUserResponse> Update(int id, UpdateRequest model)
        {
            var hrUser = gethrUser(id);

            // validate
            if (hrUser.Email != model.Email && _hrUserRepository.Any(model.Email))
                throw new AppException($"Email '{model.Email}' is already taken");

            // hash password if it was entered
            if (!string.IsNullOrEmpty(model.Password))
                hrUser.PasswordHash = BC.HashPassword(model.Password);

            // copy model to account and save
            _mapper.Map(model, hrUser);
            hrUser.Updated = DateTime.UtcNow;           

            var hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);
           
            _hrUserRepository.Update(hrUserDto);
            _hrUserRepository.SaveChanges();

            return _mapper.Map<Task<HrUserResponse>>(hrUser);
        }

       

        public void VerifyEmail(string token)
        {           
            var hrUserDto = _hrUserRepository.GetByVerificationToken(token);

            // map model to new account object
            var hrUser = _mapper.Map<HrUser>(hrUserDto);

            if (hrUser == null) throw new AppException("Verification failed");

            hrUser.Verified = DateTime.UtcNow;
            hrUser.VerificationToken = null;

            hrUserDto = _mapper.Map<Data.Entities.HrUser>(hrUser);
            
            _hrUserRepository.Update(hrUserDto);
            _hrUserRepository.SaveChanges();
        }

        private HrUser gethrUser(int id)
        {
            var hrUserDto = _hrUserRepository.GetById(id);

            var hrUser = _mapper.Map<HrUser>(hrUserDto);

            if (hrUser == null) throw new KeyNotFoundException("HR User not found");
            return hrUser;
        }

        private (RefreshToken, HrUser) getRefreshToken(string token)
        {
            var hrUserDto = _hrUserRepository.GetByRefreshToken(token);

            var hrUser = _mapper.Map<HrUser>(hrUserDto);

            if (hrUser == null) throw new AppException("Invalid token");
            var refreshToken = hrUser.RefreshTokens.Single(x => x.Token == token);
            if (!refreshToken.IsActive) throw new AppException("Invalid token");
            return (refreshToken, hrUser);
        }

        private string generateJwtToken(HrUser hrUser)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            //var key = Encoding.ASCII.GetBytes(_appSettings.Secret); // TODO:

            var key = Encoding.ASCII.GetBytes("THIS IS USED TO SIGN AND VERIFY JWT TOKENS, REPLACE IT WITH YOUR OWN SECRET, IT CAN BE ANY STRING");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", hrUser.Id.ToString()) }),
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

        private void removeOldRefreshTokens(HrUser hrUser)
        {
            hrUser.RefreshTokens.RemoveAll(x =>
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

        private void sendVerificationEmail(HrUser hrUser, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var verifyUrl = $"{origin}/account/verify-email?token={hrUser.VerificationToken}";
                message = $@"<p>Please click the below link to verify your email address:</p>
                             <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to verify your email address with the <code>/accounts/verify-email</code> api route:</p>
                             <p><code>{hrUser.VerificationToken}</code></p>";
            }

            _emailService.Send(
                to: hrUser.Email,
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

        private void sendPasswordResetEmail(HrUser hrUser, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var resetUrl = $"{origin}/account/reset-password?token={hrUser.ResetToken}";
                message = $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                             <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to reset your password with the <code>/accounts/reset-password</code> api route:</p>
                             <p><code>{hrUser.ResetToken}</code></p>";
            }

            _emailService.Send(
                to: hrUser.Email,
                subject: "Sign-up Verification API - Reset Password",
                html: $@"<h4>Reset Password Email</h4>
                         {message}"
            );
        }
    }
}
