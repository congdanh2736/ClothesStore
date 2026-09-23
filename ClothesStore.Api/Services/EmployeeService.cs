using AutoMapper;
using ClothesStore.Api.Common;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Employee;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using ClothesStore.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClothesStore.Api.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeService(IEmployeeRepository repository, IMapper mapper, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
        {
            var employees = await _repository.GetAllAsync();
            return _mapper.Map<List<EmployeeDto>>(employees);
        }


        public async Task<EmployeeDto?> GetByIdAsync(int id)
        { 
            var employee = await _repository.GetByIdAsync(id);
            return employee is null ? null : _mapper.Map<EmployeeDto>(employee);
        }
        public async Task<(bool Success, string? Error, EmployeeDto? Data)> CreateAsync(CreateEmployeeDto dto)
        {
            if (!await _repository.StoreExistsAsync(dto.StoreId))
                return (false, "Cửa hàng không tồn tại.", null);

            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email
                };

                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return (false, errors, null);
                }

                await _userManager.AddToRoleAsync(user, nameof(AppRole.Employee));

                var employee = new Employee
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    StoreId = dto.StoreId,
                    ApplicationUserId = user.Id
                };

                await _repository.AddAsync(employee);

                await transaction.CommitAsync();

                var created = await _repository.GetByIdAsync(employee.Id);
                return (true, null, _mapper.Map<EmployeeDto>(created));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateEmployeeDto dto)
        {
            var employee = await _repository.GetByIdAsync(id);
            if (employee == null) return (false, "Employee is not exist!");

            if (dto.StoreId.HasValue && !await _repository.StoreExistsAsync(dto.StoreId.Value))
            {
                return (false, "Store is not exist!");
            }

            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.StoreId = dto.StoreId;

            await _repository.UpdateAsync(employee);
            return (true, null);
        } 

        public async Task<bool> DeleteAsync(int id)
        {
            var employee = await _repository.GetByIdAsync(id);
            if (employee == null) return false;
            await _repository.DeleteAsync(employee);
            return true;
        }
    }
}
