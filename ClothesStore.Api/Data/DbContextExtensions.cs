using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Data
{
    public static class DbContextExtensions
    {
        /*
         * Chạy một nhóm thao tác trong 1 transaction.
         * Nếu đang ở trong transaction khác (ví dụ ShipmentService gọi OrderService.CompleteAsync)
         * thì dùng luôn transaction hiện tại, tránh lỗi "connection is already in a transaction".
         */
        public static async Task<T> InTransactionAsync<T>(this ApplicationDbContext context, Func<Task<T>> action)
        {
            if (context.Database.CurrentTransaction is not null)
                return await action();

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var result = await action();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public static Task InTransactionAsync(this ApplicationDbContext context, Func<Task> action)
            => context.InTransactionAsync(async () =>
            {
                await action();
                return true;
            });
    }
}
