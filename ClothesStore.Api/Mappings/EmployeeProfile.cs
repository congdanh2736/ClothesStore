using AutoMapper;
using ClothesStore.Api.DTOs.Employee;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.StoreName,
                    opt => opt.MapFrom(src => src.Store != null ? src.Store.Name : null))
                // [THÊM] Email lấy từ tài khoản đăng nhập (FirstName, LastName, StoreId tự map vì trùng tên)
                .ForMember(dest => dest.Email,
                    opt => opt.MapFrom(src => src.ApplicationUser != null ? src.ApplicationUser.Email : null));

            CreateMap<CreateEmployeeDto, Employee>();
        }
    }
}
