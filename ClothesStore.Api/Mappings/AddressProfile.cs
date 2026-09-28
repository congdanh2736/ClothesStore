using AutoMapper;
using ClothesStore.Api.DTOs.Address;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class AddressProfile : Profile
    {
        public AddressProfile()
        {
            CreateMap<Address, AddressDto>()

                .ForMember(dest => dest.OrderCount, opt 
                    => opt.MapFrom(src => src.Orders.Count));
        }
    }
}
