using System;
using System.ComponentModel.DataAnnotations;

namespace Rev1.API.Security.Business.Entities
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public HrUser HrUser { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public DateTime Created { get; set; }
        public string CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string RevokedByIp { get; set; }
        public string ReplacedByToken { get; set; }
        public bool IsActive => Revoked == null && !IsExpired;
    }
}
