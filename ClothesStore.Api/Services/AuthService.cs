using ClothesStore.Api.Common; // [SỬA] chỉ đổi vị trí dòng using
// [XÓA] using AutoMapper + field IMapper: không dùng tới
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Auth;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClothesStore.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICustomerRepository _customerRepository;
        private readonly IEmployeeRepository _employeeRepository;     // [THÊM] tìm hồ sơ nhân viên khi đăng nhập
        private readonly IMembershipTierRepository _tierRepository;  // [THÊM] gán hạng mặc định khi đăng ký
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthService(
            UserManager<ApplicationUser> userManager, 
            ICustomerRepository customerRepository, 
            IEmployeeRepository employeeRepository,     // [THÊM]
            IMembershipTierRepository tierRepository,   // [THÊM]
            ApplicationDbContext context, 
            IJwtService jwtService
            )
        {
            _userManager = userManager;
            _customerRepository = customerRepository;
            _employeeRepository = employeeRepository; // [THÊM]
            _tierRepository = tierRepository;         // [THÊM]
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<(bool Success, string? Error, RegisterResultDto? Data)> RegisterAsync(RegisterDto dto)
        {
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.FirstName, // [THÊM] lưu họ tên vào tài khoản luôn
                    LastName = dto.LastName    // [THÊM]
                };
                
                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return (false, errors, null);
                }

                // [SỬA] Kiểm tra kết quả gán role (trước đây bỏ qua, gán lỗi vẫn tạo khách hàng)
                var roleResult = await _userManager.AddToRoleAsync(user, nameof(AppRole.Customer));
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return (false, string.Join(", ", roleResult.Errors.Select(e => e.Description)), null);
                }

                // [THÊM] Khách mới: gán hạng thấp nhất (0 điểm) + 1 giỏ hàng rỗng
                var defaultTier = await _tierRepository.GetTierForPointsAsync(0);
                var customer = new Customer
                {
                    ApplicationUserId = user.Id,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    MembershipTierId = defaultTier?.Id, // [THÊM]
                    Cart = new Cart(),
                };

                await _customerRepository.AddAsync(customer);

                await transaction.CommitAsync();

                return (true, null, new RegisterResultDto
                {
                    Id = customer.Id,
                    Email = user.Email,
                    FullName = $"{customer.FirstName} {customer.LastName}"
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool Success, string? Error, LoginResultDto? Data)> LoginAsync(LoginDto dto)
        {
            // Tìm người dùng có tồn tại không bằng cách kiểm tra email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return (false, "Email hoặc mật khẩu không đúng.", null);

            // Kiểm tra mật khẩu có đúng không khi đăng nhập
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordValid)
                return (false, "Email hoặc mật khẩu không đúng.", null);

            // Tạo token JWT cho người dùng
            var token = await _jwtService.GenerateTokenAsync(user);
            // Lấy danh sách vai trò của người dùng
            var roles = await _userManager.GetRolesAsync(user);

            // [THÊM] Trả kèm Id hồ sơ khách hàng / nhân viên để client gọi API giỏ hàng, đơn hàng...
            //        (trước đây login chỉ trả token, client không biết CustomerId của mình)
            var customer = await _customerRepository.GetByApplicationUserIdAsync(user.Id);
            var employee = customer is null ? await _employeeRepository.GetByApplicationUserIdAsync(user.Id) : null;

            var fullName = customer is not null ? $"{customer.FirstName} {customer.LastName}"
                : employee is not null ? $"{employee.FirstName} {employee.LastName}"
                : $"{user.FirstName} {user.LastName}".Trim();

            // Trả về kết quả đăng nhập thành công với token, email và danh sách vai trò
            return (true, null, new LoginResultDto
            {
                Token = token,
                Email = user.Email!,
                Roles = roles.ToList(),
                CustomerId = customer?.Id,  // [THÊM]
                EmployeeId = employee?.Id,  // [THÊM]
                FullName = fullName         // [THÊM]
            });
        }
    }
}
