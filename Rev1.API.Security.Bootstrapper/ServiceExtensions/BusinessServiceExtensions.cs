using Microsoft.Extensions.DependencyInjection;
using Rev1.API.Security.Business;
using Rev1.API.Security.Business.Contract;

namespace Rev1.API.Security.Bootstrapper.ServiceExtensions
{
    public static class BusinessServiceExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddTransient<IHrUserService, HrUserService>();
            services.AddTransient<IEmailService, EmailService>();

            return services;
        }
    }
}
