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

        public AuthService(UserManager<ApplicationUser> userManager, ICustomerRepository customerRepository, ApplicationDbContext context, IMapper mapper)
        {
            _userManager = userManager;
            _customerRepository = customerRepository;
            _context = context;
            _mapper = mapper;
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
    }
}
