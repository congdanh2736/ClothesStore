using ClothesStore.Api.Common;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace ClothesStore.Api.Middleware
{
    // Lớp middleware để xử lý ngoại lệ trong ứng dụng ASP.NET Core
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Thử thực hiện middleware tiếp theo trong pipeline, nếu có ngoại lệ xảy ra thì sẽ được bắt và xử lý ở đây
            try
            {
                await _next(context);
            }
            // Lỗi nghiệp vụ (hết hàng, sai trạng thái, ...) -> trả về 400/404 kèm message rõ ràng
            catch (BusinessException ex)
            {
                _logger.LogWarning("Lỗi nghiệp vụ: {Message}", ex.Message);
                await WriteErrorAsync(context, ex.StatusCode, ex.Message);
            }
            // Lỗi ràng buộc dữ liệu (khóa ngoại, unique index, ...) -> 409 Conflict thay vì 500
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi ràng buộc dữ liệu");
                await WriteErrorAsync(context, (int)HttpStatusCode.Conflict,
                    "Dữ liệu vi phạm ràng buộc (đang được tham chiếu hoặc bị trùng). Vui lòng kiểm tra lại.");
            }
            // Nếu có ngoại lệ khác xảy ra, ghi log lỗi và trả về phản hồi lỗi cho client
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn xảy ra");
                await WriteErrorAsync(context, (int)HttpStatusCode.InternalServerError,
                    "Đã có lỗi xảy ra ở server, vui lòng thử lại sau.");
            }
        }

        private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted) return;

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var result = JsonSerializer.Serialize(new { message });
            await context.Response.WriteAsync(result);
        }
    }
}
