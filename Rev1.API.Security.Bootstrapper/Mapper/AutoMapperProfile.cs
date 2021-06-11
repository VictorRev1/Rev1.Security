using AutoMapper;
using Rev1.API.Security.Business.Entities;

namespace Rev1.API.Security.Bootstrapper.Mapper
{
    public class AutoMapperProfile : Profile
    {
        // mappings between model and entity objects
        public AutoMapperProfile()
        {
            CreateMap<RegisterRequest, HrUser>();

            CreateMap<HrUser, Data.Entities.HrUser>();

            CreateMap<HrUser, HrUserResponse>();             

            CreateMap<Data.Entities.HrUser, HrUser>();

            CreateMap<Data.Entities.RefreshToken, RefreshToken>();

            CreateMap<RefreshToken, Data.Entities.RefreshToken>();

            CreateMap<Data.Entities.HrRole, HrRole>();

            CreateMap<HrRole, Data.Entities.HrRole>();

            CreateMap<HrUser, AuthenticateResponse>();            

            CreateMap<CreateRequest, HrUser>();

            CreateMap<UpdateRequest, HrUser>()
                .ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        // ignore null & empty string properties
                        if (prop == null) return false;
                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

                        // ignore null role
                        if (x.DestinationMember.Name == "Role" && src.HrRole == null) return false;

                        return true;
                    }
                ));
        }
    }
}
