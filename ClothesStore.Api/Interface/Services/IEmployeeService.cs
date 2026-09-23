using ClothesStore.Api.DTOs.Employee;
using ClothesStore.Api.Interface.Services.Base;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Interface.Services
{
    public interface IEmployeeService : IService<EmployeeDto, CreateEmployeeDto, UpdateEmployeeDto>
    {

    }
}
