/*
 * ============================================================================
 *  config.js  -  "SỔ GHI CHÚ CHUNG" CỦA CẢ FRONTEND
 * ============================================================================
 *
 *  File này KHÔNG làm gì cả, nó chỉ CẤT GIỮ các giá trị cố định (hằng số)
 *  mà nhiều file khác cần dùng chung. Ví dụ: địa chỉ API, tên các vai trò,
 *  tên các trạng thái đơn hàng...
 *
 *  Tại sao phải gom vào một chỗ?
 *    - Nếu mỗi file tự gõ "Pending", "Admin"... thì lỡ gõ sai 1 chữ
 *      (ví dụ "pending" viết thường) là chương trình chạy sai mà rất khó tìm lỗi.
 *    - Khi cần đổi (ví dụ đổi đường dẫn trang đăng nhập) thì chỉ sửa ở ĐÂY,
 *      mọi file khác tự động dùng giá trị mới.
 *
 *  Cách dùng ở file khác:
 *      import { API_BASE, ROLES, ORDER_STATUS } from "../core/config.js";
 *
 *  Chữ "export" phía trước mỗi biến nghĩa là: "cho phép file khác lấy biến này".
 *  "Object.freeze(...)" nghĩa là "đóng băng": không ai sửa được nội dung bên trong,
 *  tránh trường hợp có đoạn code vô tình ghi đè làm hỏng giá trị.
 *
 *  LƯU Ý: Các chữ như "Pending", "Admin", "COD"... phải GIỐNG HỆT bên backend
 *  (thư mục Common của project ClothesStore.Api). Nếu backend đổi thì sửa ở đây theo.
 * ============================================================================
 */

/* ----------------------------------------------------------------------------
 * 1. ĐỊA CHỈ GỐC CỦA API
 * ----------------------------------------------------------------------------
 * Frontend nằm trong wwwroot nên chạy CHUNG một địa chỉ với backend
 * (ví dụ cùng là https://localhost:7001). Vì vậy chỉ cần ghi "/api" là đủ,
 * trình duyệt sẽ tự hiểu là https://localhost:7001/api.
 *
 * Ví dụ: muốn gọi danh sách sản phẩm thì đường dẫn đầy đủ là API_BASE + "/Product"
 *        => "/api/Product"
 */
export const API_BASE = "/api";

/* ----------------------------------------------------------------------------
 * 2. CÁC "NGĂN KÉO" LƯU THÔNG TIN ĐĂNG NHẬP TRONG TRÌNH DUYỆT
 * ----------------------------------------------------------------------------
 * Sau khi đăng nhập, ta cất token và thông tin người dùng vào localStorage
 * (localStorage giống một cái tủ nhỏ trong trình duyệt: tắt trang mở lại vẫn còn).
 * Mỗi món đồ cất vào tủ cần một cái "nhãn" (key). Đây là danh sách các nhãn đó.
 *
 * Thêm chữ "cs_" (ClothesStore) phía trước để không trùng với trang web khác
 * chạy cùng địa chỉ localhost trên máy bạn.
 */
export const STORAGE_KEYS = Object.freeze({
    TOKEN: "cs_token",        // chuỗi JWT - "thẻ ra vào" để gọi API
    USER: "cs_user"           // thông tin người dùng: email, họ tên, vai trò, id...
});

/* ----------------------------------------------------------------------------
 * 3. VAI TRÒ (ROLE) CỦA NGƯỜI DÙNG
 * ----------------------------------------------------------------------------
 * Backend có 3 vai trò. Admin = quản lý, Employee = nhân viên, Customer = khách hàng.
 * STAFF_ROLES là "nhóm nhân sự": những ai được vào trang admin.
 */
export const ROLES = Object.freeze({
    ADMIN: "Admin",
    EMPLOYEE: "Employee",
    CUSTOMER: "Customer"
});

export const STAFF_ROLES = Object.freeze([ROLES.ADMIN, ROLES.EMPLOYEE]);

/* ----------------------------------------------------------------------------
 * 4. ĐƯỜNG DẪN CÁC TRANG QUAN TRỌNG
 * ----------------------------------------------------------------------------
 * Dùng khi cần "chuyển trang" tự động. Ví dụ:
 *   - chưa đăng nhập mà vào trang admin   -> đẩy về ADMIN_LOGIN
 *   - Admin/Employee đăng nhập xong        -> đưa tới ADMIN_HOME
 *   - Customer đăng nhập xong              -> đưa tới SHOP_HOME
 * Đường dẫn bắt đầu bằng "/" nghĩa là tính từ gốc của wwwroot.
 */
export const PAGES = Object.freeze({
    ADMIN_LOGIN: "/admin/login.html",
    ADMIN_HOME: "/admin/dashboard.html",
    SHOP_LOGIN: "/shop/login.html",
    SHOP_HOME: "/shop/index.html"
});

/* ----------------------------------------------------------------------------
 * 5. TRẠNG THÁI ĐƠN HÀNG
 * ----------------------------------------------------------------------------
 * Vòng đời bình thường của một đơn hàng:
 *   Pending (chờ xác nhận) -> Confirmed (đã xác nhận) -> Shipping (đang giao)
 *   -> Completed (hoàn tất)
 * Nhánh khác: Cancelled (đã hủy), Returned (khách đã trả lại toàn bộ hàng).
 */
export const ORDER_STATUS = Object.freeze({
    PENDING: "Pending",
    CONFIRMED: "Confirmed",
    SHIPPING: "Shipping",
    COMPLETED: "Completed",
    CANCELLED: "Cancelled",
    RETURNED: "Returned"
});

/* ----------------------------------------------------------------------------
 * 6. TRẠNG THÁI THANH TOÁN
 * ----------------------------------------------------------------------------
 * Pending = chưa trả tiền, Paid = đã trả, Failed = trả lỗi, Cancelled = đã hủy,
 * Refunded = đã hoàn tiền toàn bộ, PartiallyRefunded = mới hoàn một phần.
 */
export const PAYMENT_STATUS = Object.freeze({
    PENDING: "Pending",
    PAID: "Paid",
    FAILED: "Failed",
    CANCELLED: "Cancelled",
    REFUNDED: "Refunded",
    PARTIALLY_REFUNDED: "PartiallyRefunded"
});

/* ----------------------------------------------------------------------------
 * 7. PHƯƠNG THỨC THANH TOÁN
 * ----------------------------------------------------------------------------
 * Dùng để đổ vào ô chọn (<select>) khi khách chọn cách trả tiền.
 */
export const PAYMENT_METHODS = Object.freeze({
    COD: "COD",                   // trả tiền mặt khi nhận hàng
    MOMO: "Momo",
    VNPAY: "VNPay",
    BANK_TRANSFER: "BankTransfer", // chuyển khoản ngân hàng
    CASH: "Cash"                  // trả tiền mặt tại cửa hàng
});

/* ----------------------------------------------------------------------------
 * 8. TRẠNG THÁI VẬN CHUYỂN (vận đơn)
 * ---------------------------------------------------------------------------- */
export const SHIPMENT_STATUS = Object.freeze({
    SHIPPING: "Shipping",   // đang giao
    DELIVERED: "Delivered", // giao thành công
    FAILED: "Failed"        // giao thất bại
});

/* ----------------------------------------------------------------------------
 * 9. TRẠNG THÁI YÊU CẦU ĐỔI / TRẢ HÀNG
 * ----------------------------------------------------------------------------
 * Pending (chờ duyệt) -> Approved (đã duyệt) -> Completed (đã nhận lại hàng, hoàn tiền)
 * hoặc Rejected (bị từ chối).
 */
export const RETURN_STATUS = Object.freeze({
    PENDING: "Pending",
    APPROVED: "Approved",
    REJECTED: "Rejected",
    COMPLETED: "Completed"
});

/* ----------------------------------------------------------------------------
 * 10. CHỮ TIẾNG VIỆT ĐỂ HIỂN THỊ
 * ----------------------------------------------------------------------------
 * Backend trả về chữ tiếng Anh ("Pending"), nhưng người dùng nên thấy tiếng Việt.
 * Bảng dưới đây giống cuốn từ điển nhỏ: tra chữ tiếng Anh -> ra chữ tiếng Việt
 * và một "màu" (tên class CSS) để tô nhãn cho đẹp.
 *
 *   color: "warning" = vàng (đang chờ), "info" = xanh dương (đang xử lý),
 *          "success" = xanh lá (xong), "danger" = đỏ (hủy/lỗi), "secondary" = xám.
 *   Nhóm làm CSS (components.css) sẽ tạo các class: .badge-warning, .badge-success...
 *
 * Hàm statusBadge() trong utils.js sẽ dùng các bảng này.
 */
export const ORDER_STATUS_LABELS = Object.freeze({
    Pending: { text: "Chờ xác nhận", color: "warning" },
    Confirmed: { text: "Đã xác nhận", color: "info" },
    Shipping: { text: "Đang giao", color: "info" },
    Completed: { text: "Hoàn tất", color: "success" },
    Cancelled: { text: "Đã hủy", color: "danger" },
    Returned: { text: "Đã trả hàng", color: "secondary" }
});

export const PAYMENT_STATUS_LABELS = Object.freeze({
    Pending: { text: "Chưa thanh toán", color: "warning" },
    Paid: { text: "Đã thanh toán", color: "success" },
    Failed: { text: "Thanh toán lỗi", color: "danger" },
    Cancelled: { text: "Đã hủy", color: "danger" },
    Refunded: { text: "Đã hoàn tiền", color: "secondary" },
    PartiallyRefunded: { text: "Hoàn tiền một phần", color: "secondary" }
});

export const SHIPMENT_STATUS_LABELS = Object.freeze({
    Shipping: { text: "Đang giao", color: "info" },
    Delivered: { text: "Đã giao", color: "success" },
    Failed: { text: "Giao thất bại", color: "danger" }
});

export const RETURN_STATUS_LABELS = Object.freeze({
    Pending: { text: "Chờ duyệt", color: "warning" },
    Approved: { text: "Đã duyệt", color: "info" },
    Rejected: { text: "Từ chối", color: "danger" },
    Completed: { text: "Hoàn tất", color: "success" }
});

export const PAYMENT_METHOD_LABELS = Object.freeze({
    COD: "Thanh toán khi nhận hàng (COD)",
    Momo: "Ví MoMo",
    VNPay: "VNPay",
    BankTransfer: "Chuyển khoản ngân hàng",
    Cash: "Tiền mặt tại cửa hàng"
});

export const ROLE_LABELS = Object.freeze({
    Admin: "Quản lý",
    Employee: "Nhân viên",
    Customer: "Khách hàng"
});

/* ----------------------------------------------------------------------------
 * 11. MỘT VÀI CON SỐ DÙNG CHUNG
 * ---------------------------------------------------------------------------- */

// Tích điểm: cứ 10.000đ thì được 1 điểm (giống quy tắc LoyaltyRules ở backend).
// Chỉ dùng để HIỂN THỊ ước tính cho người dùng xem, số điểm thật do backend tính.
export const MONEY_PER_POINT = 10000;

// Số dòng hiển thị trên một trang của bảng (phân trang).
export const PAGE_SIZE = 10;
