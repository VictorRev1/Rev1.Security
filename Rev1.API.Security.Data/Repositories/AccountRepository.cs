using Microsoft.EntityFrameworkCore;
using Rev1.API.Security.Data.Contract;
using Rev1.API.Security.Data.Entities;
using Rev1.API.Security.Utils.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rev1.API.Security.Data.Repositories
{
    public class AccountRepository : BaseRepository<Account>, IAccountRepository
    {
        public AccountRepository(ConnectionStrings connectionStrings, DataContext dataContext) : base(connectionStrings, dataContext)
        {

        }

        public void Add(Account account)
        {
            DataContext.Accounts.Add(account);
        }

        public bool Any(string email)
        {
            return DataContext.Accounts.Where(x => x.Email == email).Any();
        }

        public int Count()
        {
            return DataContext.Accounts.Count();
        }

        public Account Get(string verificationToken)
        {
            return DataContext.Accounts.AsNoTracking().SingleOrDefault(x => x.VerificationToken == verificationToken);
        }

        public Account GetByEmail(string email)
        {
            return DataContext.Accounts.AsNoTracking().SingleOrDefault(x => x.Email == email);
        }

        public Account GetByVerificationToken(string verificationToken)
        {
            return DataContext.Accounts.AsNoTracking().SingleOrDefault(x => x.VerificationToken == verificationToken);
        }

        public Account GetByResetToken(string resetToken)
        {
            return DataContext.Accounts.AsNoTracking().SingleOrDefault(x => x.ResetToken == resetToken && x.ResetTokenExpires > DateTime.UtcNow);
        }

        public void SaveChanges()
        {
            DataContext.SaveChanges();
        }

        public void Update(Account account)
        {
            DataContext.Accounts.Update(account);
        }

        public Account GetById(int id)
        {
            return DataContext.Accounts.Find(id);
        }

        public void Delete(int id)
        {
            var toDelete = DataContext.Accounts.Find(id);
            DataContext.Accounts.Remove(toDelete);
        }

        public Account GetByRefreshToken(string refreshToken)
        {
            return DataContext.Accounts.AsNoTracking().SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == refreshToken));
        }

        public List<Account> GetAll()
        {            
           return DataContext.Accounts.ToList();           
        }
    }
}
