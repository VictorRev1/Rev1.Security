using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rev1.API.Security.Business.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rev1.API.Security.Utils.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly IList<HrRole> _hrRoles;

        public AuthorizeAttribute(params HrRole[] hrRoles)
        {
            _hrRoles = hrRoles ?? new HrRole[] { };
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var hrUser = (HrUser)context.HttpContext.Items["HrUser"];
            if (hrUser == null || _hrRoles.Any() && !_hrRoles.Contains(hrUser.HrRole))
            {
                // not logged in or role not authorized
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }
    }
}
