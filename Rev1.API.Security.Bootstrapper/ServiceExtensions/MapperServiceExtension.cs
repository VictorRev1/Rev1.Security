using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Rev1.API.Security.Bootstrapper.Mapper;

namespace Rev1.API.Security.Bootstrapper.ServiceExtensions
{
    public static class MapperServiceExtension
    {
        public static IServiceCollection AddAutoMapperService(this IServiceCollection services)
        {
            // Auto Mapper Configurations
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new AutoMapperProfile());
            });

            IMapper mapper = mapperConfig.CreateMapper();
            services.AddSingleton(mapper);
            
            return services;
        }
    }
}
