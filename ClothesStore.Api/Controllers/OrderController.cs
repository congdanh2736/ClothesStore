using ClothesStore.Api.DTOs.Order;
using AutoMapper;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.OrderItem;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public OrderController(IOrderRepository repository, ApplicationDbContext context, IMapper mapper)
        {
            _repository = repository;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDetailDTO>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<OrderDetailDTO>>(await _repository.GetAllAsync()));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderDetailDTO>> GetById(int id)
        {
            var order = await _repository.GetByIdWithDetailsAsync(id);
            return order is null ? NotFound() : Ok(_mapper.Map<OrderDetailDTO>(order));
        }

        [HttpPost("customers/{customerId:int}")]
        public async Task<ActionResult<OrderDetailDTO>> Create(int customerId, CreateOrderRequest request)
        {
            if (request.Items is not { Count: > 0 })
                return BadRequest(new { message = "Đơn hàng phải có ít nhất một sản phẩm." });

            if (!await _context.Customers.AnyAsync(customer => customer.Id == customerId))
                return NotFound(new { message = "Không tìm thấy khách hàng." });

            if (!await _context.Addresses.AnyAsync(address => address.Id == request.AddressId && address.CustomerId == customerId))
                return BadRequest(new { message = "Địa chỉ không thuộc về khách hàng này." });

            var promotion = await GetActivePromotionAsync(request.PromotionId);
            if (promotion is null)
                return BadRequest(new { message = "Khuyến mãi không tồn tại hoặc không còn hiệu lực." });

            var items = await BuildItemsAsync(request.Items);
            if (items is null)
                return BadRequest(new { message = "Sản phẩm hoặc số lượng trong đơn hàng không hợp lệ." });

            var order = new Order
            {
                CustomerId = customerId,
                AddressId = request.AddressId,
                PromotionId = request.PromotionId,
                Status = "Pending",
                TotalAmount = items.Sum(item => item.Price * item.Quantity),
                OrderItems = items,
                PaymentTransaction = new PaymentTransaction
                {
                    PaymentMethod = request.PaymentMethod?.Trim() ?? "COD",
                    Status = "Pending"
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var createdOrder = await _repository.GetByIdWithDetailsAsync(order.Id);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, _mapper.Map<OrderDetailDTO>(createdOrder));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateOrderRequest request)
        {
            var order = await _repository.GetByIdWithDetailsAsync(id);
            if (order is null)
                return NotFound();

            if (!await _context.Addresses.AnyAsync(address => address.Id == request.AddressId && address.CustomerId == order.CustomerId))
                return BadRequest(new { message = "Địa chỉ không thuộc về khách hàng của đơn hàng." });

            var promotion = await GetActivePromotionAsync(request.PromotionId);
            if (promotion is null)
                return BadRequest(new { message = "Khuyến mãi không tồn tại hoặc không còn hiệu lực." });

            order.AddressId = request.AddressId;
            order.PromotionId = request.PromotionId;

            if (request.Items is not null)
            {
                if (request.Items.Count == 0)
                    return BadRequest(new { message = "Đơn hàng phải có ít nhất một sản phẩm." });

                var items = await BuildItemsAsync(request.Items.Select(item => new CreateOrderItemRequest
                {
                    VariantId = item.VariantId,
                    Quantity = item.Quantity
                }));

                if (items is null)
                    return BadRequest(new { message = "Sản phẩm hoặc số lượng trong đơn hàng không hợp lệ." });

                _context.OrderItems.RemoveRange(order.OrderItems);
                order.OrderItems = items;
                order.TotalAmount = items.Sum(item => item.Price * item.Quantity);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _repository.GetByIdAsync(id);
            if (order is null)
                return NotFound();

            await _repository.DeleteAsync(order);
            return NoContent();
        }

        private async Task<Promotion?> GetActivePromotionAsync(int promotionId)
        {
            var now = DateTime.UtcNow;
            return await _context.Promotions.FirstOrDefaultAsync(promotion =>
                promotion.Id == promotionId && promotion.StartDate <= now && promotion.EndDate >= now);
        }

        private async Task<List<OrderItem>?> BuildItemsAsync(IEnumerable<CreateOrderItemRequest> requests)
        {
            var requestedItems = requests.ToList();
            if (requestedItems.Any(item => item.Quantity <= 0))
                return null;

            var variantIds = requestedItems.Select(item => item.VariantId).Distinct().ToList();
            var variants = await _context.ProductVariants
                .Where(variant => variantIds.Contains(variant.VariantId))
                .ToDictionaryAsync(variant => variant.VariantId);

            if (variants.Count != variantIds.Count)
                return null;

            return requestedItems.Select(item => new OrderItem
            {
                VariantId = item.VariantId,
                Quantity = item.Quantity,
                Price = (decimal)variants[item.VariantId].Price
            }).ToList();
        }

    }
}
