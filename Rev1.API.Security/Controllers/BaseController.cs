using Microsoft.AspNetCore.Mvc;
using Rev1.API.Security.Business.Entities;

namespace Rev1.API.Security.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        // returns the current authenticated HrUser (null if not logged in)
        public HrUser HrUser => (HrUser)HttpContext.Items["HrUser"];        
    }
}
