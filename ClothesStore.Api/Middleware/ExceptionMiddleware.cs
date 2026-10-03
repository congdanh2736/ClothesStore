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
            // Nếu có ngoại lệ xảy ra, ghi log lỗi và trả về phản hồi lỗi cho client
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn xảy ra");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var result = JsonSerializer.Serialize(new
                {
                    message = "Đã có lỗi xảy ra ở server, vui lòng thử lại sau."
                });

                await context.Response.WriteAsync(result);
            }
        }
    }
}
