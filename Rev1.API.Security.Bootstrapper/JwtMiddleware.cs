using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rev1.API.Security.Business.Entities;
using Rev1.API.Security.Data;
using Rev1.API.Security.Utils.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rev1.API.Security.Bootstrapper
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppSettings _appSettings;
        private readonly IMapper _mapper;

        public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings, IMapper mapper)
        {
            _next = next;
            _appSettings = appSettings.Value;
            _mapper = mapper;
        }

        public async Task Invoke(HttpContext context, DataContext dataContext)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                await attachHrUserToContext(context, dataContext, token);

            await _next(context);
        }

        private async Task attachHrUserToContext(HttpContext context, DataContext dataContext, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                //var key = Encoding.ASCII.GetBytes(_appSettings.Secret); // TODO:

                var key = Encoding.ASCII.GetBytes("THIS IS USED TO SIGN AND VERIFY JWT TOKENS, REPLACE IT WITH YOUR OWN SECRET, IT CAN BE ANY STRING");

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var hrUserId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                // attach hrUser to context on successful jwt validation
                var hrUserDto = await dataContext.HrUser.FindAsync(hrUserId);

                // map to business object
                context.Items["HrUser"] = _mapper.Map<HrUser>(hrUserDto);                

            }
            catch (Exception ex)
            {
                // do nothing if jwt validation fails
                // account is not attached to context so request won't have access to secure routes
            }            
        }
    }
}
