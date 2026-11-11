using AutoMapper;
using ClothesStore.Api.Common;
using ClothesStore.Api.DTOs.ReturnRequest;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Mappings;
using ClothesStore.Api.Models;
using ClothesStore.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ClothesStore.Tests
{
    public class ReturnRequestServiceTests
    {
        private readonly Mock<IReturnRequestRepository> _repoMock;
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<ICustomerRepository> _customerRepoMock;
        private readonly Mock<IInventoryService> _inventoryMock;
        private readonly Mock<IStoreStatisticsService> _statsMock;
        private readonly Mock<ILoyaltyTransactionService> _loyaltyMock;
        private readonly IMapper _mapper;

        public ReturnRequestServiceTests()
        {
            _repoMock = new Mock<IReturnRequestRepository>();
            _orderRepoMock = new Mock<IOrderRepository>();
            _customerRepoMock = new Mock<ICustomerRepository>();
            _inventoryMock = new Mock<IInventoryService>();
            _statsMock = new Mock<IStoreStatisticsService>();
            _loyaltyMock = new Mock<ILoyaltyTransactionService>();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<ReturnRequestProfile>());
            var provider = services.BuildServiceProvider();
            _mapper = provider.GetRequiredService<IMapper>();
        }

        private ReturnRequestService CreateService()
        {
            return new ReturnRequestService(
                _repoMock.Object,
                _orderRepoMock.Object,
                _customerRepoMock.Object,
                _inventoryMock.Object,
                _statsMock.Object,
                _loyaltyMock.Object,
                context: null!,
                _mapper);
        }

        #region RETURN-01: Kiểm tra quyền sở hữu đơn hàng khi tạo yêu cầu trả hàng

        [Fact]
        public async Task CreateAsync_WhenUserIsOrderOwner_ShouldSucceed()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-123";
            var customer = new Customer { Id = 1, ApplicationUserId = userId };
            var order = new Order
            {
                Id = 10,
                CustomerId = 1, // Same as customer.Id
                Status = OrderStatus.Completed,
                OrderDate = DateTime.UtcNow.AddDays(-2),
                TotalAmount = 500000m,
                DiscountAmount = 0m,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { Id = 101, Quantity = 2, Price = 250000m, VariantId = 1 }
                }
            };

            var dto = new CreateReturnRequestDto
            {
                OrderId = 10,
                Reason = "Hàng bị lỗi đường chỉ may",
                Items = new List<CreateReturnItemDto>
                {
                    new CreateReturnItemDto { OrderItemId = 101, Quantity = 1, Condition = "Nguyên tem mác" }
                }
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _orderRepoMock.Setup(o => o.GetByIdWithDetailsAsync(10)).ReturnsAsync(order);
            _repoMock.Setup(r => r.GetReturnedQuantitiesAsync(10, null)).ReturnsAsync(new Dictionary<int, int>());

            ReturnRequest? savedEntity = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<ReturnRequest>()))
                .Callback<ReturnRequest>(rr =>
                {
                    rr.Id = 99;
                    savedEntity = rr;
                })
                .Returns(Task.CompletedTask);

            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(99))
                .ReturnsAsync(() => new ReturnRequest
                {
                    Id = 99,
                    OrderId = 10,
                    Order = order,
                    Reason = dto.Reason,
                    Status = ReturnStatus.Pending,
                    RequestDate = DateTime.UtcNow,
                    ReturnItems = new List<ReturnItem>
                    {
                        new ReturnItem
                        {
                            Id = 201,
                            OrderItemId = 101,
                            Quantity = 1,
                            RefundAmount = 250000m,
                            OrderItem = order.OrderItems.First()
                        }
                    }
                });

            // Act
            var result = await service.CreateAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(99, result.Id);
            Assert.Equal(10, result.OrderId);
            Assert.Equal(1, result.CustomerId);
            Assert.Equal(ReturnStatus.Pending, result.Status);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<ReturnRequest>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenUserIsNotOrderOwner_ShouldThrowForbiddenException()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-attacker";
            var customer = new Customer { Id = 2, ApplicationUserId = userId };
            var order = new Order
            {
                Id = 10,
                CustomerId = 1, // Owned by Customer #1, not Customer #2
                Status = OrderStatus.Completed,
                OrderDate = DateTime.UtcNow.AddDays(-2)
            };

            var dto = new CreateReturnRequestDto
            {
                OrderId = 10,
                Reason = "Cố tình tạo trả hàng cho đơn người khác",
                Items = new List<CreateReturnItemDto>
                {
                    new CreateReturnItemDto { OrderItemId = 101, Quantity = 1 }
                }
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _orderRepoMock.Setup(o => o.GetByIdWithDetailsAsync(10)).ReturnsAsync(order);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(dto, userId));
            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
            Assert.Contains("Bạn không có quyền", ex.Message);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<ReturnRequest>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenUserHasNoCustomerProfile_ShouldThrowForbiddenException()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-no-customer";

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId))
                .ReturnsAsync((Customer?)null);

            var dto = new CreateReturnRequestDto
            {
                OrderId = 10,
                Reason = "Lỗi",
                Items = new List<CreateReturnItemDto> { new CreateReturnItemDto { OrderItemId = 1, Quantity = 1 } }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(dto, userId));
            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
            Assert.Contains("Tài khoản chưa được liên kết với hồ sơ khách hàng", ex.Message);
        }

        #endregion

        #region RETURN-02: Quyền tra cứu và xóa yêu cầu trả hàng

        [Fact]
        public async Task GetByIdAsync_WhenCustomerIsOwner_ShouldReturnDto()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-123";
            var customer = new Customer { Id = 1, ApplicationUserId = userId };
            var order = new Order { Id = 10, CustomerId = 1 };
            var returnRequest = new ReturnRequest
            {
                Id = 5,
                OrderId = 10,
                Order = order,
                Status = ReturnStatus.Pending
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(returnRequest);

            // Act
            var result = await service.GetByIdAsync(5, userId, isStaff: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
            Assert.Equal(1, result.CustomerId);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCustomerIsNotOwner_ShouldThrowForbiddenException()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-attacker";
            var customer = new Customer { Id = 2, ApplicationUserId = userId };
            var order = new Order { Id = 10, CustomerId = 1 }; // Customer #1
            var returnRequest = new ReturnRequest
            {
                Id = 5,
                OrderId = 10,
                Order = order,
                Status = ReturnStatus.Pending
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(returnRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(5, userId, isStaff: false));
            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
            Assert.Contains("Bạn không có quyền", ex.Message);
        }

        [Fact]
        public async Task GetByIdAsync_WhenStaff_ShouldReturnAnyRequest()
        {
            // Arrange
            var service = CreateService();
            var order = new Order { Id = 10, CustomerId = 1 };
            var returnRequest = new ReturnRequest
            {
                Id = 5,
                OrderId = 10,
                Order = order,
                Status = ReturnStatus.Pending
            };

            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(returnRequest);

            // Act
            var result = await service.GetByIdAsync(5, "staff-user", isStaff: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
        }

        [Fact]
        public async Task GetByOrderIdAsync_WhenCustomerIsOwner_ShouldReturnList()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-123";
            var customer = new Customer { Id = 1, ApplicationUserId = userId };
            var order = new Order { Id = 10, CustomerId = 1 };
            var returnRequests = new List<ReturnRequest>
            {
                new ReturnRequest { Id = 1, OrderId = 10, Order = order, Status = ReturnStatus.Pending }
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _orderRepoMock.Setup(o => o.GetByIdAsync(10)).ReturnsAsync(order);
            _repoMock.Setup(r => r.GetByOrderIdAndCustomerIdAsync(10, 1)).ReturnsAsync(returnRequests);

            // Act
            var result = await service.GetByOrderIdAsync(10, userId, isStaff: false);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetByOrderIdAsync_WhenCustomerIsNotOwner_ShouldThrowForbiddenException()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-attacker";
            var customer = new Customer { Id = 2, ApplicationUserId = userId };
            var order = new Order { Id = 10, CustomerId = 1 }; // Customer #1

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _orderRepoMock.Setup(o => o.GetByIdAsync(10)).ReturnsAsync(order);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByOrderIdAsync(10, userId, isStaff: false));
            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
            Assert.Contains("Bạn không có quyền", ex.Message);
        }

        [Fact]
        public async Task GetByCustomerIdAsync_WhenCustomerQueriesOwnId_ShouldSucceed()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-123";
            var customer = new Customer { Id = 1, ApplicationUserId = userId };
            var returnRequests = new List<ReturnRequest>
            {
                new ReturnRequest { Id = 1, OrderId = 10, Order = new Order { CustomerId = 1 }, Status = ReturnStatus.Pending }
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _repoMock.Setup(r => r.GetByCustomerIdAsync(1)).ReturnsAsync(returnRequests);

            // Act
            var result = await service.GetByCustomerIdAsync(1, userId, isStaff: false);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetByCustomerIdAsync_WhenCustomerQueriesOtherCustomerId_ShouldThrowForbiddenException()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-123";
            var customer = new Customer { Id = 1, ApplicationUserId = userId };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);

            // Act & Assert: Customer #1 trying to query Customer #2's requests
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByCustomerIdAsync(2, userId, isStaff: false));
            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
            Assert.Contains("Bạn không có quyền xem yêu cầu trả hàng của khách hàng khác", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_WhenCustomerIsOwnerAndStatusIsPending_ShouldSucceed()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-123";
            var customer = new Customer { Id = 1, ApplicationUserId = userId };
            var order = new Order { Id = 10, CustomerId = 1 };
            var returnRequest = new ReturnRequest
            {
                Id = 7,
                OrderId = 10,
                Order = order,
                Status = ReturnStatus.Pending
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(7)).ReturnsAsync(returnRequest);
            _repoMock.Setup(r => r.DeleteAsync(returnRequest)).Returns(Task.CompletedTask);

            // Act
            var result = await service.DeleteAsync(7, userId, isStaff: false);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(returnRequest), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenCustomerIsNotOwner_ShouldThrowForbiddenException()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-attacker";
            var customer = new Customer { Id = 2, ApplicationUserId = userId };
            var order = new Order { Id = 10, CustomerId = 1 };
            var returnRequest = new ReturnRequest
            {
                Id = 7,
                OrderId = 10,
                Order = order,
                Status = ReturnStatus.Pending
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(7)).ReturnsAsync(returnRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(7, userId, isStaff: false));
            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
            Assert.Contains("Bạn không có quyền", ex.Message);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<ReturnRequest>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenStatusIsApproved_ShouldThrowBusinessException()
        {
            // Arrange
            var service = CreateService();
            var userId = "user-123";
            var customer = new Customer { Id = 1, ApplicationUserId = userId };
            var order = new Order { Id = 10, CustomerId = 1 };
            var returnRequest = new ReturnRequest
            {
                Id = 7,
                OrderId = 10,
                Order = order,
                Status = ReturnStatus.Approved // Cannot delete approved request
            };

            _customerRepoMock.Setup(c => c.GetByApplicationUserIdAsync(userId)).ReturnsAsync(customer);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(7)).ReturnsAsync(returnRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(7, userId, isStaff: false));
            Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
            Assert.Contains("Chỉ được xóa yêu cầu đang chờ xử lý hoặc đã bị từ chối", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_WhenStaffAndStatusIsPending_ShouldSucceedEvenWithoutCustomerProfile()
        {
            // Arrange
            var service = CreateService();
            var order = new Order { Id = 10, CustomerId = 1 };
            var returnRequest = new ReturnRequest
            {
                Id = 7,
                OrderId = 10,
                Order = order,
                Status = ReturnStatus.Pending
            };

            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(7)).ReturnsAsync(returnRequest);
            _repoMock.Setup(r => r.DeleteAsync(returnRequest)).Returns(Task.CompletedTask);

            // Act
            var result = await service.DeleteAsync(7, "staff-user-id", isStaff: true);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(returnRequest), Times.Once);
        }

        #endregion
    }
}
