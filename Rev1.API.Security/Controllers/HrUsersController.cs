using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rev1.API.Security.Business.Contract;
using Rev1.API.Security.Business.Entities;
using Rev1.API.Security.Utils.Authentication;
using System;
using System.Collections.Generic;

namespace Rev1.API.Security.Controllers
{
    [Route("api/[controller]")]
    [ApiController]    
    public class HrUsersController : BaseController
    {
        private readonly IHrUserService _hrUserService;

        public HrUsersController([FromServices] IHrUserService hrUserService)
        {
            _hrUserService = hrUserService;
        }

        [HttpPost("authenticate")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            var response = _hrUserService.Authenticate(model, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public ActionResult<AuthenticateResponse> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _hrUserService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public ActionResult RevokeToken(RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            // users can revoke their own tokens and admins can revoke any tokens
            if (!HrUser.OwnsToken(token) && HrUser.HrRole != HrRole.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            _hrUserService.RevokeToken(token, ipAddress());
            return Ok(new { message = "Token revoked" });
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest model)
        {
            _hrUserService.Register(model, Request.Headers["origin"]);
            return Ok(new { message = "Registration successful, please check your email for verification instructions" });
        }

        [HttpPost("verify-email")]
        public IActionResult VerifyEmail(VerifyEmailRequest model)
        {
            _hrUserService.VerifyEmail(model.Token);
            return Ok(new { message = "Verification successful, you can now login" });
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model)
        {
            _hrUserService.ForgotPassword(model, Request.Headers["origin"]);
            return Ok(new { message = "Please check your email for password reset instructions" });
        }

        [HttpPost("validate-reset-token")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
        {
            _hrUserService.ValidateResetToken(model);
            return Ok(new { message = "Token is valid" });
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordRequest model)
        {
            _hrUserService.ResetPassword(model);
            return Ok(new { message = "Password reset successful, you can now login" });
        }

        [Authorize(HrRole.Admin)]       
        [HttpGet]
        public ActionResult<IEnumerable<HrUserResponse>> GetAll()
        {
            var hrUsers = _hrUserService.GetAll();
            return Ok(hrUsers);
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public ActionResult<HrUserResponse> GetById(int id)
        {
            // hrUsers can get their own user and admins can get any user
            if (id != HrUser.Id && HrUser.HrRole != HrRole.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            var hrUser = _hrUserService.GetById(id);
            return Ok(hrUser);
        }

        [Authorize(HrRole.Admin)]        
        [HttpPost]
        public ActionResult<HrUserResponse> Create(CreateRequest model)
        {
            var hrUser = _hrUserService.Create(model);
            return Ok(hrUser);
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public ActionResult<HrUserResponse> Update(int id, UpdateRequest model)
        {
            // hrUsers can update their own user and admins can update any user
            if (id != HrUser.Id && HrUser.HrRole != HrRole.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            // only admins can update role
            if (HrUser.HrRole != HrRole.Admin)
                model.HrRole = null;

            var hrUser = _hrUserService.Update(id, model);
            return Ok(hrUser);
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            // hrUsers can delete their own account and admins can delete any user
            if (id != HrUser.Id && HrUser.HrRole != HrRole.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            _hrUserService.Delete(id);
            return Ok(new { message = "User deleted successfully" });
        }

        #region helper methods

        private void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

        #endregion
    }
}
