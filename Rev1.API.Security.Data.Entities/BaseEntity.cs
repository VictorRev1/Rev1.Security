using System;
using System.ComponentModel.DataAnnotations;

namespace Rev1.API.Security.Data.Entities
{
    [Serializable]
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
