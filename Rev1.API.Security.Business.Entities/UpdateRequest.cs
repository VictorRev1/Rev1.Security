using System.ComponentModel.DataAnnotations;

namespace Rev1.API.Security.Business.Entities
{
    public class UpdateRequest
    {
        private string _password;
        private string _confirmPassword;
        private string _hrRole;
        private string _email;

        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [EnumDataType(typeof(HrRole))]
        public string HrRole
        {
            get => _hrRole;
            set => _hrRole = replaceEmptyWithNull(value);
        }

        [EmailAddress]
        public string Email
        {
            get => _email;
            set => _email = replaceEmptyWithNull(value);
        }

        [MinLength(6)]
        public string Password
        {
            get => _password;
            set => _password = replaceEmptyWithNull(value);
        }

        [Compare("Password")]
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => _confirmPassword = replaceEmptyWithNull(value);
        }        

        #region helpers 

        private string replaceEmptyWithNull(string value)
        {
            // replace empty string with null to make field optional
            return string.IsNullOrEmpty(value) ? null : value;
        }

        #endregion
    }
}
