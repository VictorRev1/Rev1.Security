using Rev1.API.Security.Data.Entities;
using System.Collections.Generic;

namespace Rev1.API.Security.Data.Contract
{
    public interface IHrUserRepository : IBaseRepository<HrUser> 
    {
        bool Any(string email);
        int Count();
        void Add(HrUser hrUser);
        void SaveChanges();
        HrUser GetByVerificationToken(string verificationToken);
        HrUser GetByResetToken(string resetToken);
        HrUser GetByRefreshToken(string refreshToken);
        HrUser GetByEmail(string email);
        void Update(HrUser account);
        HrUser GetById(int id);
        void Delete(int id);
        List<HrUser> GetAll();       
    }
}
