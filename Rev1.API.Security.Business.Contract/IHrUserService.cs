using Rev1.API.Security.Business.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rev1.API.Security.Business.Contract
{
    public interface IHrUserService
    {
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);        
        AuthenticateResponse RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        void Register(RegisterRequest model, string origin);
        void VerifyEmail(string token);
        void ForgotPassword(ForgotPasswordRequest model, string origin);
        void ValidateResetToken(ValidateResetTokenRequest model);
        void ResetPassword(ResetPasswordRequest model);
        IEnumerable<HrUserResponse> GetAll();
        Task<HrUserResponse> GetById(int id);
        Task<HrUserResponse> Create(CreateRequest model);
        Task<HrUserResponse> Update(int id, UpdateRequest model);
        void Delete(int id);
    }
}

