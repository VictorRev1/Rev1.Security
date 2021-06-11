using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rev1.API.Security.Utils.Configuration;

namespace Rev1.API.Security.Bootstrapper.ServiceExtensions
{
    public static class ConfigurationServiceExtension
    {    
        public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration
            , out ConnectionStrings connectionStrings)
        {
            connectionStrings = new ConnectionStrings();
            var appSettings = new AppSettings();

            configuration.GetSection("ConnectionStrings").Bind(connectionStrings);
            configuration.GetSection("AppSettings").Bind(appSettings);

            services.AddSingleton(connectionStrings);
            services.AddSingleton(appSettings);

            return services;
        }           
    }
}
