# Cơ sở dữ liệu hệ thống thương mại điện tử thời trang

Dự án này mô phỏng cấu trúc cơ sở dữ liệu (Database) cho hệ thống thương mại điện tử của một chuỗi thời trang, lấy ý tưởng từ mô hình kinh doanh thực tế. Cấu trúc được thiết kế tối ưu với 22 bảng (Tables/Entities), phân chia thành 5 nhóm quản lý chính nhằm đáp ứng các yêu cầu thực tế của một hệ thống bán lẻ đa kênh (Omnichannel).

---

## Danh sách các nhóm và bảng

### Nhóm 1: Khách hàng và Phân quyền (4 bảng)

1. `Customer` (Khách hàng): Lưu thông tin người dùng mua hàng.
2. `Address` (Sổ địa chỉ): Khách hàng có thể lưu nhiều địa chỉ (nhà riêng, công ty).
3. `Membership_Tier` (Hạng thành viên): Chính sách tích lũy điểm lên hạng (Đồng, Bạc, Vàng, VIP).
4. `Employee` (Nhân viên): Tài khoản cho nhân viên quản trị website hoặc nhân viên trực tại cửa hàng vật lý.

### Nhóm 2: Sản phẩm và Phân loại (6 bảng)

5. `Category` (Danh mục): Phân cấp danh mục (Quần áo, Áo thun, Phụ kiện...).
6. `Collection_Tech` (Bộ sưu tập và Công nghệ): Quản lý các dòng vải (AirDry, FlexFit) hoặc bộ sưu tập (Minimalist).
7. `Product` (Sản phẩm chung): Thông tin gốc của sản phẩm.
8. `Product_Collection` (Bảng trung gian): Liên kết nhiều-nhiều giữa `Product` và `Collection_Tech`.
9. `Product_Variant` (Phiên bản - SKU): Cụ thể hóa màu sắc và kích cỡ (ví dụ: áo đen size M, áo trắng size L).
10. `Product_Image` (Hình ảnh sản phẩm): Quản lý hình ảnh đa góc độ của từng sản phẩm.

### Nhóm 3: Tương tác mua sắm (4 bảng)

11. `Cart` (Giỏ hàng): Lưu phiên giỏ hàng của người dùng đang thao tác.
12. `Cart_Item` (Chi tiết giỏ): Các sản phẩm (variant) và số lượng đang nằm trong giỏ hàng.
13. `Wishlist` (Sản phẩm yêu thích): Người dùng lưu lại các mẫu sản phẩm ưng ý để mua sau.
14. `Review` (Đánh giá): Khách hàng phản hồi, chấm điểm (1-5 sao) kèm bình luận sau khi mua.

### Nhóm 4: Đơn hàng và Thanh toán (4 bảng)

15. `Promotion` (Khuyến mãi/Voucher): Quản lý mã giảm giá (ví dụ: SALE10K, FREESHIP), hạn mức, ngày hiệu lực.
16. `Order` (Đơn hàng): Thông tin hóa đơn tổng.
17. `Order_Item` (Chi tiết đơn hàng): Các món hàng cụ thể trong đơn.
18. `Payment_Transaction` (Giao dịch thanh toán): Lưu log từ cổng thanh toán (Momo, VNPay, COD...) gồm mã giao dịch đối soát và trạng thái.

### Nhóm 5: Chuỗi cửa hàng và Tồn kho (4 bảng)

19. `Store` (Cửa hàng): Thông tin các chi nhánh của chuỗi cửa hàng.
20. `Store_Stock` (Tồn kho theo cửa hàng): Quản lý số lượng tồn kho thực tế của từng `Product_Variant` tại từng `Store` (hỗ trợ mô hình Click & Collect).
21. `Store_Daily_Stat` (Thống kê tổng quan cửa hàng theo ngày): Tổng hợp số liệu đơn hàng và doanh thu theo từng ngày cho mỗi cửa hàng.
22. `Store_Item_Stat` (Thống kê mặt hàng theo cửa hàng): Tổng hợp số liệu bán hàng và hoàn trả theo từng variant tại mỗi cửa hàng.

---

## Sơ đồ ERD (Mermaid)

Sơ đồ dưới đây có thể được xem trực quan trên GitHub hoặc bằng cách dán đoạn mã vào [Mermaid Live Editor](https://mermaid.live/).

```mermaid
erDiagram
    MEMBERSHIP_TIER ||--o{ CUSTOMER : "has"
    CUSTOMER ||--o{ ADDRESS : "has"
    CUSTOMER ||--o| CART : "owns"
    CUSTOMER ||--o{ WISHLIST : "saves"
    CUSTOMER ||--o{ REVIEW : "writes"
    CUSTOMER ||--o{ ORDER : "places"
    CUSTOMER ||--o{ LOYALTY_TRANSACTION : "earns_spends"

    STORE ||--o{ EMPLOYEE : "employs"
    STORE ||--o{ STORE_STOCK : "holds"
    STORE ||--o{ STORE_DAILY_STAT : "generates_daily_stat"
    STORE ||--o{ STORE_ITEM_STAT : "generates_item_stat"

    CATEGORY ||--o{ PRODUCT : "contains"
    CATEGORY ||--o{ CATEGORY : "parent_of"
    CATEGORY ||--o{ SIZE_CHART : "defines"

    PRODUCT ||--|{ PRODUCT_VARIANT : "has_skus"
    PRODUCT ||--o{ PRODUCT_IMAGE : "has_images"
    PRODUCT ||--o{ PRODUCT_COLLECTION : "linked_via"
    COLLECTION_TECH ||--o{ PRODUCT_COLLECTION : "linked_via"
    PRODUCT ||--o{ WISHLIST : "added_to"
    PRODUCT ||--o{ REVIEW : "receives"

    PRODUCT_VARIANT ||--o{ STORE_STOCK : "stocked_as"
    PRODUCT_VARIANT ||--o{ CART_ITEM : "in_cart"
    PRODUCT_VARIANT ||--o{ ORDER_ITEM : "in_order"
    PRODUCT_VARIANT ||--o{ STORE_ITEM_STAT : "tracked_in_stat"

    CART ||--o{ CART_ITEM : "contains"

    PROMOTION ||--o{ ORDER : "applied_to"
    ADDRESS ||--o{ ORDER : "ships_to"

    ORDER ||--|{ ORDER_ITEM : "contains"
    ORDER ||--o| PAYMENT_TRANSACTION : "paid_via"
    ORDER ||--o| SHIPMENT : "shipped_via"
    ORDER ||--o{ RETURN_REQUEST : "has"

    RETURN_REQUEST ||--|{ RETURN_ITEM : "contains"
    ORDER_ITEM ||--o| RETURN_ITEM : "refers_to"

    %% Attributes

    MEMBERSHIP_TIER { int tier_id PK string name }
    CUSTOMER { int customer_id PK int tier_id FK string full_name }
    ADDRESS { int address_id PK int customer_id FK string full_address }
    EMPLOYEE { int employee_id PK int store_id FK string full_name }

    CATEGORY { int category_id PK int parent_id FK string name }
    COLLECTION_TECH { int collection_id PK string type string name }
    PRODUCT { int product_id PK int category_id FK string name }
    PRODUCT_COLLECTION { int product_id PK int collection_id PK }
    PRODUCT_IMAGE { int image_id PK int product_id FK string image_url }
    PRODUCT_VARIANT { int variant_id PK int product_id FK string color string size decimal price }

    STORE { int store_id PK string name string location }
    STORE_STOCK { int stock_id PK int store_id FK int variant_id FK int quantity }

    STORE_DAILY_STAT {
        int stat_id PK
        int store_id FK
        date stat_date
        int total_orders
        int completed_orders
        int canceled_orders
        decimal total_revenue
        int total_items_sold
    }
    STORE_ITEM_STAT {
        int stat_id PK
        int store_id FK
        int variant_id FK
        date stat_date
        int quantity_sold
        decimal total_revenue
        int return_quantity
    }

    CART { int cart_id PK int customer_id FK }
    CART_ITEM { int cart_item_id PK int cart_id FK int variant_id FK int quantity }
    WISHLIST { int wishlist_id PK int customer_id FK int product_id FK }
    REVIEW { int review_id PK int customer_id FK int product_id FK int rating }

    PROMOTION { int promo_id PK string code decimal discount_value }
    ORDER { int order_id PK int customer_id FK int address_id FK int promo_id FK decimal total_amount }
    ORDER_ITEM { int order_item_id PK int order_id FK int variant_id FK int quantity decimal price }
    PAYMENT_TRANSACTION { int transaction_id PK int order_id FK string payment_method string status }

    %% New tables

    RETURN_REQUEST {
        int return_id PK
        int order_id FK
        date request_date
        string reason
        string status
    }
    RETURN_ITEM {
        int return_item_id PK
        int return_id FK
        int order_item_id FK
        int quantity
        string condition
        decimal refund_amount
    }

    SHIPMENT {
        int shipment_id PK
        int order_id FK
        string carrier
        string tracking_number
        date shipped_date
        date delivered_date
        string status
    }

    SIZE_CHART {
        int size_chart_id PK
        int category_id FK
        string size_label
        string measurements
    }

    LOYALTY_TRANSACTION {
        int transaction_id PK
        int customer_id FK
        date txn_date
        int points_change
        string reason
    }
```
