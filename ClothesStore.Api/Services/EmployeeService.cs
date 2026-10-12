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
            // [SỬA] Lấy kèm Store để map StoreName (trước đây dùng GetByIdAsync nên StoreName luôn rỗng)
            var employee = await _repository.GetByIdWithDetailsAsync(id);
            return employee is null ? null : _mapper.Map<EmployeeDto>(employee);
        }
        public async Task<(bool Success, string? Error, EmployeeDto? Data)> CreateAsync(CreateEmployeeDto dto)
        {
            // Kiểm tra xem cửa hàng có tồn tại không dựa vào StoreId trong dto
            // [SỬA] StoreId không bắt buộc (nhân viên quản trị website) nhưng nếu có thì cửa hàng phải tồn tại
            if (dto.StoreId.HasValue && !await _repository.StoreExistsAsync(dto.StoreId))
                return (false, "Cửa hàng không tồn tại.", null);

            /* 
             * Dùng transaction để đảm bảo rằng cả việc tạo ApplicationUser và Employee đều thành công hoặc không có gì thay đổi
             * Nếu không thành công trong việc tạo ApplicationUser hoặc Employee, transaction sẽ rollback và không có gì được lưu vào cơ sở dữ liệu
             */
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Tạo một user mới
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email
                };

                // Tạo user trong Identity
                var createResult = await _userManager.CreateAsync(user, dto.Password);

                // Kiểm tra xem việc tạo user có thành công không, nếu không thì trả về lỗi
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return (false, errors, null);
                }

                /* 
                 * Gán role "Employee" cho user vừa tạo
                 * Việc tạo Employee ở đây phải là Admin mới tạo được, nên mặc định role là Employee, không cần phải truyền vào dto
                 */
                await _userManager.AddToRoleAsync(user, nameof(AppRole.Employee));

                // Tạo một đối tượng Employee mới và gán các thông tin từ dto và user vừa tạo
                var employee = new Employee
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    StoreId = dto.StoreId,
                    ApplicationUserId = user.Id
                };

                // Thêm employee vào cơ sở dữ liệu
                await _repository.AddAsync(employee);

                // Commit transaction nếu tất cả các bước trên đều thành công
                await transaction.CommitAsync();

                // Lấy employee vừa tạo để trả về
                // [SỬA] GetByIdAsync -> GetByIdWithDetailsAsync để DTO trả về có StoreName
                var created = await _repository.GetByIdWithDetailsAsync(employee.Id);

                /* 
                 * Trả về kết quả thành công cùng với dữ liệu employee vừa tạo 
                 * Map employee sang EmployeeDto để trả về cho client
                 */
                return (true, null, _mapper.Map<EmployeeDto>(created));
            }
            catch
            {
                // Nếu có lỗi xảy ra trong quá trình tạo user hoặc employee, rollback transaction để không có gì được lưu vào cơ sở dữ liệu
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateEmployeeDto dto)
        {
            var employee = await _repository.GetByIdAsync(id);
            // [SỬA] đổi thông báo cho khớp với EmployeeController (trước đây không khớp nên trả 400 thay vì 404)
            if (employee == null) return (false, "Không tìm thấy nhân viên.");

            if (dto.StoreId.HasValue && !await _repository.StoreExistsAsync(dto.StoreId.Value))
            {
                return (false, "Cửa hàng không tồn tại."); // [SỬA] thông báo tiếng Việt
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

            /*
             * [THÊM] Xóa luôn tài khoản đăng nhập của nhân viên.
             * Trước đây chỉ xóa Employee nên tài khoản (role Employee) vẫn đăng nhập và gọi được API dành cho nhân viên.
             */
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var applicationUserId = employee.ApplicationUserId; // [THÊM] nhớ Id tài khoản trước khi xóa
                await _repository.DeleteAsync(employee);

                // [THÊM] xóa tài khoản đăng nhập (AspNetUsers) của nhân viên
                if (!string.IsNullOrEmpty(applicationUserId))
                {
                    var user = await _userManager.FindByIdAsync(applicationUserId);
                    if (user is not null)
                        await _userManager.DeleteAsync(user);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return true;
        }
    }
}
