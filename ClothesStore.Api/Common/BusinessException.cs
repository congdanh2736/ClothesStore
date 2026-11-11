namespace ClothesStore.Api.Common
{
    /*
     * Ngoại lệ nghiệp vụ: dùng khi một thao tác vi phạm quy tắc nghiệp vụ
     * (hết hàng, đơn hàng không ở trạng thái hợp lệ, xóa dữ liệu đang được tham chiếu, ...)
     * ExceptionMiddleware sẽ bắt ngoại lệ này và trả về HTTP 400 kèm message thay vì 500.
     */
    public class BusinessException : Exception
    {
        public int StatusCode { get; }

        public BusinessException(string message, int statusCode = StatusCodes.Status400BadRequest)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    // Ngoại lệ khi không tìm thấy dữ liệu -> HTTP 404
    public class NotFoundException : BusinessException
    {
        public NotFoundException(string message) : base(message, StatusCodes.Status404NotFound) { }
    }

    // Ngoại lệ khi không có quyền truy cập -> HTTP 403
    public class ForbiddenException : BusinessException
    {
        public ForbiddenException(string message = "Bạn không có quyền thực hiện thao tác này.")
            : base(message, StatusCodes.Status403Forbidden) { }
    }
}

