using Rev1.API.Security.Data.Entities;
using System.Collections.Generic;

namespace Rev1.API.Security.Data.Contract
{
    public interface IAccountRepository : IBaseRepository<Account> 
    {
        bool Any(string email);
        int Count();
        void Add(Account account);
        void SaveChanges();
        Account GetByVerificationToken(string verificationToken);
        Account GetByResetToken(string resetToken);
        Account GetByRefreshToken(string refreshToken);
        Account GetByEmail(string email);
        void Update(Account account);
        Account GetById(int id);
        void Delete(int id);
        List<Account> GetAll();       
    }
}
