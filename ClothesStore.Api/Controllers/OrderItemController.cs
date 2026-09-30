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
    public class OrderItemController : ControllerBase
    {
        private readonly IOrderItemRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public OrderItemController(IOrderItemRepository repository, ApplicationDbContext context, IMapper mapper)
        {
            _repository = repository;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<OrderItemDTO>>(await _repository.GetAllAsync()));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderItemDTO>> GetById(int id)
        {
            var item = await _repository.GetByIdWithDetailsAsync(id);
            return item is null ? NotFound() : Ok(_mapper.Map<OrderItemDTO>(item));
        }

        [HttpPost("orders/{orderId:int}")]
        public async Task<ActionResult<OrderItemDTO>> Create(int orderId, CreateOrderItemRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest(new { message = "Số lượng phải lớn hơn 0." });

            if (!await _context.Orders.AnyAsync(order => order.Id == orderId))
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            var variant = await _context.ProductVariants.FindAsync(request.VariantId);
            if (variant is null)
                return BadRequest(new { message = "Phiên bản sản phẩm không tồn tại." });

            var item = new OrderItem
            {
                OrderId = orderId,
                VariantId = request.VariantId,
                Quantity = request.Quantity,
                Price = (decimal)variant.Price
            };

            await _repository.AddAsync(item);
            await RecalculateOrderTotalAsync(orderId);
            var createdItem = await _repository.GetByIdWithDetailsAsync(item.Id);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, _mapper.Map<OrderItemDTO>(createdItem));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateOrderItemRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest(new { message = "Số lượng phải lớn hơn 0." });

            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return NotFound();

            var variant = await _context.ProductVariants.FindAsync(request.VariantId);
            if (variant is null)
                return BadRequest(new { message = "Phiên bản sản phẩm không tồn tại." });

            item.VariantId = request.VariantId;
            item.Quantity = request.Quantity;
            item.Price = (decimal)variant.Price;
            await _repository.UpdateAsync(item);
            await RecalculateOrderTotalAsync(item.OrderId);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null)
                return NotFound();

            var orderId = item.OrderId;
            await _repository.DeleteAsync(item);
            await RecalculateOrderTotalAsync(orderId);
            return NoContent();
        }

        private async Task RecalculateOrderTotalAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order is null)
                return;

            order.TotalAmount = await _context.OrderItems
                .Where(item => item.OrderId == orderId)
                .SumAsync(item => (decimal?)(item.Price * item.Quantity)) ?? 0m;
            await _context.SaveChangesAsync();
        }
    }
}
