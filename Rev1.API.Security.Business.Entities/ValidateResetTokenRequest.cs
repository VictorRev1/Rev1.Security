using System.ComponentModel.DataAnnotations;

namespace Rev1.API.Security.Business.Entities
{
    public class ValidateResetTokenRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
