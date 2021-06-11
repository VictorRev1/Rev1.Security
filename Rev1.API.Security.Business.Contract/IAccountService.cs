using Rev1.API.Security.Business.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rev1.API.Security.Business.Contract
{
    public interface IAccountService
    {
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        AuthenticateResponse RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        void Register(RegisterRequest model, string origin);
        void VerifyEmail(string token);
        void ForgotPassword(ForgotPasswordRequest model, string origin);
        void ValidateResetToken(ValidateResetTokenRequest model);
        void ResetPassword(ResetPasswordRequest model);
        IEnumerable<AccountResponse> GetAllAsync();
        Task<AccountResponse> GetById(int id);
        Task<AccountResponse> Create(CreateRequest model);
        Task<AccountResponse> Update(int id, UpdateRequest model);
        void Delete(int id);
    }
}
