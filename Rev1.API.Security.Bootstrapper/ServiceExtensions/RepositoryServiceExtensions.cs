using Microsoft.Extensions.DependencyInjection;
using Rev1.API.Security.Data;
using Rev1.API.Security.Data.Contract;
using Rev1.API.Security.Data.Repositories;
using Rev1.API.Security.Utils.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Rev1.API.Security.Bootstrapper.ServiceExtensions
{
    public static class RepositoryServiceExtensions
    {
        public static IServiceCollection AddRepositoryServices(this IServiceCollection services, ConnectionStrings connectionStrings)
        {
            services.AddTransient<IDataContext, DataContext>();

            services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddTransient<IHrUserRepository, HrUserRepository>();

            services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(connectionStrings.DefaultConnection));

            return services;
        }
    }
}
