using AutoMapper;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Auth;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using ClothesStore.Api.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClothesStore.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICustomerRepository _customerRepository;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IJwtService _jwtService;

        public AuthService(
            UserManager<ApplicationUser> userManager, 
            ICustomerRepository customerRepository, 
            ApplicationDbContext context, 
            IJwtService jwtService,
            IMapper mapper
            )
        {
            _userManager = userManager;
            _customerRepository = customerRepository;
            _context = context;
            _mapper = mapper;
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
                };
                
                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return (false, errors, null);
                }

                await _userManager.AddToRoleAsync(user, nameof(AppRole.Customer));

                var customer = new Customer
                {
                    ApplicationUserId = user.Id,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
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

            // Trả về kết quả đăng nhập thành công với token, email và danh sách vai trò
            return (true, null, new LoginResultDto
            {
                Token = token,
                Email = user.Email!,
                Roles = roles.ToList()
            });
        }
    }
}
