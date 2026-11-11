using System.Security.Claims;
using ClothesStore.Api.Common;
using ClothesStore.Api.Controllers;
using ClothesStore.Api.DTOs.ReturnRequest;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClothesStore.Tests
{
    public class ReturnRequestControllerTests
    {
        private readonly Mock<IReturnRequestService> _serviceMock;
        private readonly ReturnRequestController _controller;

        public ReturnRequestControllerTests()
        {
            _serviceMock = new Mock<IReturnRequestService>();
            _controller = new ReturnRequestController(_serviceMock.Object);
        }

        private void SetUserContext(string userId, params string[] roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task Create_ShouldPassUserIdFromJwtClaimToService()
        {
            // Arrange
            var expectedUserId = "user-jwt-123";
            SetUserContext(expectedUserId, AppRole.Customer.ToString());

            var dto = new CreateReturnRequestDto
            {
                OrderId = 10,
                Reason = "Hàng lỗi",
                Items = new List<CreateReturnItemDto>
                {
                    new CreateReturnItemDto { OrderItemId = 1, Quantity = 1 }
                }
            };

            var returnedDto = new ReturnRequestDto
            {
                Id = 15,
                OrderId = 10,
                CustomerId = 1,
                Status = ReturnStatus.Pending
            };

            _serviceMock.Setup(s => s.CreateAsync(dto, expectedUserId))
                .ReturnsAsync(returnedDto);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var model = Assert.IsType<ReturnRequestDto>(actionResult.Value);
            Assert.Equal(15, model.Id);
            _serviceMock.Verify(s => s.CreateAsync(dto, expectedUserId), Times.Once);
        }

        [Fact]
        public async Task GetById_Customer_ShouldPassCustomerCredentials()
        {
            // Arrange
            var userId = "customer-user";
            SetUserContext(userId, AppRole.Customer.ToString());

            var returnedDto = new ReturnRequestDto { Id = 5, OrderId = 10, CustomerId = 1 };
            _serviceMock.Setup(s => s.GetByIdAsync(5, userId, false))
                .ReturnsAsync(returnedDto);

            // Act
            var result = await _controller.GetById(5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var model = Assert.IsType<ReturnRequestDto>(okResult.Value);
            Assert.Equal(5, model.Id);
            _serviceMock.Verify(s => s.GetByIdAsync(5, userId, false), Times.Once);
        }

        [Fact]
        public async Task GetById_Staff_ShouldPassIsStaffTrue()
        {
            // Arrange
            var userId = "staff-user";
            SetUserContext(userId, AppRole.Employee.ToString());

            var returnedDto = new ReturnRequestDto { Id = 5, OrderId = 10, CustomerId = 1 };
            _serviceMock.Setup(s => s.GetByIdAsync(5, userId, true))
                .ReturnsAsync(returnedDto);

            // Act
            var result = await _controller.GetById(5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
            _serviceMock.Verify(s => s.GetByIdAsync(5, userId, true), Times.Once);
        }

        [Fact]
        public async Task Delete_Customer_ShouldPassUserIdAndNotStaff()
        {
            // Arrange
            var userId = "customer-user";
            SetUserContext(userId, AppRole.Customer.ToString());

            _serviceMock.Setup(s => s.DeleteAsync(7, userId, false))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(7);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _serviceMock.Verify(s => s.DeleteAsync(7, userId, false), Times.Once);
        }
    }
}
