using System.ComponentModel.DataAnnotations;

namespace Rev1.API.Security.Business.Entities
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
