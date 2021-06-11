using System;
using System.ComponentModel.DataAnnotations;

namespace Rev1.API.Security.Data.Entities
{
    public class HrRole
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastmodifyDate { get; set; }
        
    }
}
