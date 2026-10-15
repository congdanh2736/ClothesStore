// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Service vận chuyển: tạo vận đơn, cập nhật trạng thái, giao thành công -> hoàn tất đơn
using AutoMapper;
using ClothesStore.Api.Common;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Shipment;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    /*
     * Vận chuyển (bảng Shipment):
     * - Tạo vận đơn : đơn phải Confirmed -> đơn chuyển Shipping
     * - Delivered   : gọi OrderService.CompleteAsync (thanh toán COD, cộng điểm, thống kê)
     * - Failed      : giao thất bại -> có thể giao lại (Shipping) hoặc hủy đơn (hoàn kho)
     */
    public class ShipmentService : IShipmentService
    {
        private readonly IShipmentRepository _repository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ShipmentService(
            IShipmentRepository repository,
            IOrderRepository orderRepository,
            IOrderService orderService,
            ApplicationDbContext context,
            IMapper mapper)
        {
            _repository = repository;
            _orderRepository = orderRepository;
            _orderService = orderService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ShipmentDto>> GetAllAsync()
            => _mapper.Map<IEnumerable<ShipmentDto>>(await _repository.GetAllAsync());

        public async Task<ShipmentDto?> GetByIdAsync(int id)
        {
            var shipment = await _repository.GetByIdWithDetailsAsync(id);
            return shipment is null ? null : _mapper.Map<ShipmentDto>(shipment);
        }

        public async Task<ShipmentDto?> GetByOrderIdAsync(int orderId)
        {
            var shipment = await _repository.GetByOrderIdAsync(orderId);
            return shipment is null ? null : _mapper.Map<ShipmentDto>(shipment);
        }

        public async Task<ShipmentDto> CreateAsync(CreateShipmentDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(dto.OrderId)
                ?? throw new NotFoundException("Không tìm thấy đơn hàng.");

            if (order.Status != OrderStatus.Confirmed)
                throw new BusinessException("Chỉ tạo vận đơn cho đơn hàng đã được xác nhận (Confirmed).");

            if (await _repository.GetByOrderIdAsync(dto.OrderId) is not null)
                throw new BusinessException("Đơn hàng này đã có vận đơn.");

            var shipment = new Shipment
            {
                OrderId = order.Id,
                Carrier = dto.Carrier.Trim(),
                TrackingNumber = dto.TrackingNumber?.Trim(),
                ShippedDate = DateTime.UtcNow,
                Status = ShipmentStatus.Shipping
            };

            await _context.InTransactionAsync(async () =>
            {
                order.Status = OrderStatus.Shipping;
                await _repository.AddAsync(shipment);   // SaveChanges lưu luôn trạng thái đơn
            });

            return (await GetByIdAsync(shipment.Id))!;
        }

        public async Task<ShipmentDto> UpdateAsync(int id, UpdateShipmentDto dto)
        {
            var shipment = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Không tìm thấy vận đơn.");

            if (shipment.Status == ShipmentStatus.Delivered)
                throw new BusinessException("Vận đơn đã giao thành công, không thể sửa.");

            shipment.Carrier = dto.Carrier.Trim();
            shipment.TrackingNumber = dto.TrackingNumber?.Trim();
            await _repository.UpdateAsync(shipment);

            return (await GetByIdAsync(id))!;
        }

        public async Task<ShipmentDto> UpdateStatusAsync(int id, UpdateShipmentStatusDto dto)
        {
            var shipment = await _repository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException("Không tìm thấy vận đơn.");

            if (shipment.Status == ShipmentStatus.Delivered)
                throw new BusinessException("Vận đơn đã giao thành công, không thể đổi trạng thái.");

            if (shipment.Order is null || shipment.Order.Status != OrderStatus.Shipping)
                throw new BusinessException("Đơn hàng không ở trạng thái đang giao.");

            if (dto.Status == shipment.Status)
                return _mapper.Map<ShipmentDto>(shipment);

            await _context.InTransactionAsync(async () =>
            {
                shipment.Status = dto.Status;

                if (dto.Status == ShipmentStatus.Delivered)
                {
                    shipment.DeliveredDate = DateTime.UtcNow;
                    await _repository.UpdateAsync(shipment);

                    // Giao thành công -> hoàn tất đơn (thanh toán COD, cộng điểm, thống kê cửa hàng)
                    await _orderService.CompleteAsync(shipment.OrderId);
                }
                else
                {
                    // Shipping (giao lại) hoặc Failed (giao thất bại)
                    if (dto.Status == ShipmentStatus.Shipping)
                        shipment.ShippedDate = DateTime.UtcNow;

                    await _repository.UpdateAsync(shipment);
                }
            });

            return (await GetByIdAsync(id))!;
        }
    }
}
