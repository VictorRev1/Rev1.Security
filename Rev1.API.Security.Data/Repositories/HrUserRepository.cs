using Microsoft.EntityFrameworkCore;
using Rev1.API.Security.Data.Contract;
using Rev1.API.Security.Data.Entities;
using Rev1.API.Security.Utils.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rev1.API.Security.Data.Repositories
{
    public class HrUserRepository : BaseRepository<HrUser>, IHrUserRepository
    {
        public HrUserRepository(ConnectionStrings connectionStrings, DataContext dataContext) : base(connectionStrings, dataContext)
        {

        }

        public void Add(HrUser hruser)
        {
            DataContext.HrUser.Add(hruser);
        }

        public bool Any(string email)
        {
            return DataContext.HrUser.Where(x => x.Email == email).Any();
        }

        public int Count()
        {
            return DataContext.HrUser.Count();
        }

        public HrUser Get(string verificationToken)
        {
            return DataContext.HrUser.AsNoTracking().SingleOrDefault(x => x.VerificationToken == verificationToken);
        }

        public HrUser GetByEmail(string email)
        {
            return DataContext.HrUser.AsNoTracking().SingleOrDefault(x => x.Email == email);
        }

        public HrUser GetByVerificationToken(string verificationToken)
        {
            return DataContext.HrUser.AsNoTracking().SingleOrDefault(x => x.VerificationToken == verificationToken);
        }

        public HrUser GetByResetToken(string resetToken)
        {
            return DataContext.HrUser.AsNoTracking().SingleOrDefault(x => x.ResetToken == resetToken && x.ResetTokenExpires > DateTime.UtcNow);
        }

        public void SaveChanges()
        {
            DataContext.SaveChanges();
        }

        public void Update(HrUser account)
        {
            DataContext.HrUser.Update(account);
        }

        public HrUser GetById(int id)
        {
            return DataContext.HrUser.Find(id);
        }

        public void Delete(int id)
        {
            var toDelete = DataContext.HrUser.Find(id);
            DataContext.HrUser.Remove(toDelete);
        }

        public HrUser GetByRefreshToken(string refreshToken)
        {
            return DataContext.HrUser.AsNoTracking().SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == refreshToken));
        }

        public List<HrUser> GetAll()
        {            
           return DataContext.HrUser.ToList();           
        }
    }
}
