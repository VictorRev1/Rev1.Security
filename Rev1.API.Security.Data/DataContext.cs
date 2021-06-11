using Microsoft.EntityFrameworkCore;
using Rev1.API.Security.Data.Contract;
using Rev1.API.Security.Data.Entities;
using Rev1.API.Security.Utils.Configuration;

namespace Rev1.API.Security.Data
{
    public class DataContext : DbContext, IDataContext
    {
        private readonly ConnectionStrings _connectionStrings;
              
        public DataContext(ConnectionStrings connectionStrings) => _connectionStrings = connectionStrings;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionStrings.DefaultConnection);
        }

        public void EnsureDbCreated()
        {
            Database.EnsureCreated();
            //Database.Migrate();   //TODO:        
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            
            //Map entity to table
            modelBuilder.Entity<HrUser>().ToTable("tbl_hrUser", "dbo");
            modelBuilder.Entity<RefreshToken>().ToTable("tbl_refreshToken", "dbo");
            modelBuilder.Entity<HrUserRole>().ToTable("tbl_userRole", "dbo");
            modelBuilder.Entity<HrRole>().ToTable("tbl_hrRole", "dbo");            
        }        

        public DbSet<HrUser> HrUser { get; set; }

    }
}
