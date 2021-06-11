using System.ComponentModel.DataAnnotations;

namespace Rev1.API.Security.Data.Entities
{
    public class HrUserRole
    {
        [Key]
        public int Id { get; set; }
        public HrRole HrRole { get; set; }
        public HrUser HrUser { get; set; }
        public string ApplicationId { get; set; }
    }
}
