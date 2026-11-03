// [THÊM MỚI] Chatbot AI - interface service
using ClothesStore.Api.DTOs.Chatbot;

namespace ClothesStore.Api.Interface.Services
{
    public interface IChatbotService
    {
        /// <summary>
        /// Trả lời 1 câu hỏi của khách.
        /// applicationUserId: Id tài khoản lấy từ token (null nếu khách chưa đăng nhập) - dùng để tra cứu đơn hàng của chính khách.
        /// </summary>
        Task<ChatResponseDto> AskAsync(ChatRequestDto request, string? applicationUserId);
    }
}
