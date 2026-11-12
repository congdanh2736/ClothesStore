namespace ClothesStore.Api.DTOs.Wishlist
{
    // [THÊM MỚI] DTO cập nhật wishlist (đổi sản phẩm yêu thích) - cần để IWishlistService kế thừa IService<...>
    public class UpdateWishlistDto
    {
        public int ProductId { get; set; }
    }
}
