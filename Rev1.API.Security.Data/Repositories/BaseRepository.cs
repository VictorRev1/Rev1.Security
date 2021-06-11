using Rev1.API.Security.Data.Contract;
using Rev1.API.Security.Data.Entities;
using Rev1.API.Security.Utils.Configuration;

namespace Rev1.API.Security.Data.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
    {
        private DataContext _dataContext;
        protected readonly ConnectionStrings _connectionStrings;
        public DataContext DataContext
        {
            get
            {
                if (_dataContext is null)
                    _dataContext = new DataContext(_connectionStrings);

                return _dataContext;
            }
        }

        public BaseRepository(ConnectionStrings connectionStrings, DataContext dataContext)
        {
            _connectionStrings = connectionStrings;
            _dataContext = dataContext;
        }        
    }
}
