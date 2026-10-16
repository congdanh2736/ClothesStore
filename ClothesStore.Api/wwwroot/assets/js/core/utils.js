/*
 * ============================================================================
 *  utils.js  -  "HỘP DỤNG CỤ" DÙNG CHUNG
 * ============================================================================
 *
 *  Gồm các hàm nhỏ, việc gì hay lặp đi lặp lại ở nhiều trang thì đưa vào đây:
 *    - Hiển thị tiền:        450000           -> "450.000 ₫"
 *    - Hiển thị ngày giờ:    "2026-10-13T..." -> "13/10/2026 14:30"
 *    - Đọc id trên thanh địa chỉ: customer-detail.html?id=5 -> 5
 *    - Chống chèn mã độc khi hiển thị chữ người dùng nhập (escapeHtml)
 *    - Tạo nhãn trạng thái có màu ("Chờ xác nhận" màu vàng...)
 *    - Đọc dữ liệu từ form, chờ người dùng gõ xong mới tìm kiếm...
 *
 *  Cách dùng:
 *      import { formatMoney, formatDateTime, getIdFromUrl } from "../core/utils.js";
 * ============================================================================
 */

import {
    ORDER_STATUS_LABELS,
    PAYMENT_STATUS_LABELS,
    SHIPMENT_STATUS_LABELS,
    RETURN_STATUS_LABELS,
    PAYMENT_METHOD_LABELS,
    ROLE_LABELS,
    MONEY_PER_POINT
} from "./config.js";

/* ============================================================================
 *  PHẦN 1: HIỂN THỊ SỐ VÀ TIỀN
 * ========================================================================== */

/**
 * Định dạng tiền Việt Nam.
 *   formatMoney(450000)    -> "450.000 ₫"
 *   formatMoney("120000")  -> "120.000 ₫"  (chữ số cũng được)
 *   formatMoney(null)      -> "0 ₫"
 *
 * Intl.NumberFormat là công cụ có sẵn của trình duyệt, tự thêm dấu chấm
 * ngăn cách hàng nghìn theo kiểu Việt Nam ("vi-VN") và ký hiệu ₫.
 * maximumFractionDigits: 0 = không hiện số lẻ sau dấu phẩy (tiền VNĐ không có xu).
 */
export function formatMoney(value) {
    const number = Number(value) || 0; // không đổi được thành số (null, "abc") thì lấy 0
    return new Intl.NumberFormat("vi-VN", {
        style: "currency",
        currency: "VND",
        maximumFractionDigits: 0
    }).format(number);
}

/**
 * Định dạng số thường (không có ₫), ví dụ số lượng tồn kho, điểm tích lũy.
 *   formatNumber(15000) -> "15.000"
 */
export function formatNumber(value) {
    return new Intl.NumberFormat("vi-VN").format(Number(value) || 0);
}

/**
 * Ước tính số điểm tích lũy từ số tiền (10.000đ = 1 điểm, làm tròn xuống).
 *   estimatePoints(255000) -> 25
 * Chỉ để hiển thị cho người dùng xem trước, điểm thật do backend tính.
 */
export function estimatePoints(amount) {
    return Math.floor((Number(amount) || 0) / MONEY_PER_POINT);
}

/* ============================================================================
 *  PHẦN 2: HIỂN THỊ NGÀY GIỜ
 * ========================================================================== */

/**
 * Biến giá trị ngày (chữ hoặc Date) thành đối tượng Date của JavaScript.
 *
 * Chuyện múi giờ (đọc để hiểu vì sao có đoạn thêm chữ "Z"):
 *   Backend lưu giờ theo giờ quốc tế UTC (DateTime.UtcNow), tức là CHẬM hơn
 *   giờ Việt Nam 7 tiếng. Nhưng khi gửi về, chuỗi ngày thường KHÔNG ghi kèm
 *   múi giờ, ví dụ "2026-10-13T07:30:00". Trình duyệt sẽ tưởng đó là giờ
 *   Việt Nam -> hiển thị sai lệch 7 tiếng.
 *   Thêm chữ "Z" vào cuối ("...07:30:00Z") để nói rõ "đây là giờ UTC",
 *   trình duyệt sẽ tự cộng thêm 7 tiếng -> ra đúng 14:30 giờ Việt Nam.
 *
 * Trả về null nếu giá trị rỗng hoặc không phải ngày hợp lệ.
 */
export function toDate(value) {
    if (!value) return null;
    if (value instanceof Date) return value;

    let text = String(value);
    // Có chữ "T" (có phần giờ) nhưng chưa có thông tin múi giờ (Z hoặc +07:00...) -> thêm "Z"
    const hasTime = text.includes("T");
    const hasTimezone = /([zZ]|[+-]\d{2}:?\d{2})$/.test(text);
    if (hasTime && !hasTimezone) {
        text += "Z";
    }

    const date = new Date(text);
    return isNaN(date.getTime()) ? null : date; // ngày không hợp lệ thì getTime() ra NaN
}

/**
 * Chỉ hiện ngày.      formatDate("2026-10-13T07:30:00") -> "13/10/2026"
 * Giá trị rỗng -> "—" (gạch ngang dài, cho bảng nhìn gọn).
 */
export function formatDate(value) {
    const date = toDate(value);
    if (!date) return "—";
    return date.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric" });
}

/**
 * Hiện cả ngày và giờ. formatDateTime("2026-10-13T07:30:00") -> "13/10/2026 14:30"
 */
export function formatDateTime(value) {
    const date = toDate(value);
    if (!date) return "—";
    const day = formatDate(date);
    const time = date.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit", hour12: false });
    return `${day} ${time}`;
}

/**
 * Đổi ngày thành dạng "yyyy-MM-dd" để điền sẵn vào ô <input type="date">.
 * (Ô chọn ngày của trình duyệt chỉ hiểu đúng định dạng này.)
 *   toDateInputValue("2026-10-13T07:30:00") -> "2026-10-13"
 *
 * padStart(2, "0"): thêm số 0 phía trước cho đủ 2 chữ số, ví dụ 5 -> "05".
 */
export function toDateInputValue(value) {
    const date = toDate(value);
    if (!date) return "";
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, "0"); // tháng trong JS đếm từ 0 nên phải +1
    const d = String(date.getDate()).padStart(2, "0");
    return `${y}-${m}-${d}`;
}

/* ============================================================================
 *  PHẦN 3: ĐỌC THÔNG TIN TRÊN THANH ĐỊA CHỈ (URL)
 * ========================================================================== */

/**
 * Đọc một tham số trên URL.
 * Ví dụ đang ở trang: customer-detail.html?id=5&tab=orders
 *   getQueryParam("tab") -> "orders"
 *   getQueryParam("abc") -> null (không có)
 */
export function getQueryParam(name) {
    return new URLSearchParams(window.location.search).get(name);
}

/**
 * Đọc "id" trên URL và đổi thành SỐ.
 * Dùng cho các trang chi tiết: customer-detail.html?id=5 -> 5
 * Nếu không có id hoặc id không phải số nguyên dương -> trả về null,
 * khi đó trang nên báo "Không tìm thấy" hoặc quay về trang danh sách.
 *
 *   const id = getIdFromUrl();
 *   if (!id) { alert("Thiếu mã khách hàng"); history.back(); return; }
 */
export function getIdFromUrl(name = "id") {
    const value = Number(getQueryParam(name));
    return Number.isInteger(value) && value > 0 ? value : null;
}

/* ============================================================================
 *  PHẦN 4: AN TOÀN KHI HIỂN THỊ CHỮ
 * ========================================================================== */

/**
 * "Vô hiệu hóa" các ký tự đặc biệt của HTML trước khi chèn chữ vào trang.
 *
 * Tại sao cần? Nếu một khách hàng cố tình đặt tên là
 *     <img src=x onerror="alert('hack')">
 * và ta chèn thẳng vào trang bằng innerHTML, trình duyệt sẽ CHẠY đoạn đó
 * như mã thật (gọi là tấn công XSS). escapeHtml đổi các dấu < > & " '
 * thành dạng "chỉ để hiển thị", nên nó chỉ hiện ra như chữ bình thường.
 *
 * QUY TẮC: dữ liệu nào lấy từ server / người dùng gõ mà chèn bằng innerHTML
 * thì đều bọc qua escapeHtml(). Ví dụ:
 *     row.innerHTML = `<td>${escapeHtml(c.fullName)}</td>`;
 * (Nếu dùng element.textContent = ... thì không cần, vì textContent vốn an toàn.)
 */
export function escapeHtml(value) {
    if (value === null || value === undefined) return "";
    return String(value)
        .replace(/&/g, "&amp;")   // & phải đổi ĐẦU TIÊN, nếu không sẽ đổi nhầm các chữ vừa tạo
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

/* ============================================================================
 *  PHẦN 5: NHÃN TRẠNG THÁI (chữ tiếng Việt + màu)
 * ========================================================================== */

/**
 * Tạo đoạn HTML cho một nhãn màu, ví dụ:
 *     <span class="badge badge-warning">Chờ xác nhận</span>
 *
 * @param {string} status - chữ trạng thái từ backend, ví dụ "Pending"
 * @param {object} labels - bảng tra trong config.js (ORDER_STATUS_LABELS...)
 * Nếu trạng thái lạ (không có trong bảng) thì hiện nguyên chữ gốc, màu xám.
 */
export function statusBadge(status, labels) {
    const info = labels?.[status] ?? { text: status || "—", color: "secondary" };
    return `<span class="badge badge-${info.color}">${escapeHtml(info.text)}</span>`;
}

// Các hàm gọi tắt cho từng loại trạng thái, đỡ phải truyền bảng tra mỗi lần:
export const orderStatusBadge = status => statusBadge(status, ORDER_STATUS_LABELS);
export const paymentStatusBadge = status => statusBadge(status, PAYMENT_STATUS_LABELS);
export const shipmentStatusBadge = status => statusBadge(status, SHIPMENT_STATUS_LABELS);
export const returnStatusBadge = status => statusBadge(status, RETURN_STATUS_LABELS);

/**
 * Chỉ lấy CHỮ tiếng Việt của trạng thái (không có màu), dùng cho <select>, tiêu đề...
 *   statusText("Pending", ORDER_STATUS_LABELS) -> "Chờ xác nhận"
 */
export function statusText(status, labels) {
    return labels?.[status]?.text ?? status ?? "";
}

/** "COD" -> "Thanh toán khi nhận hàng (COD)" */
export function paymentMethodText(method) {
    return PAYMENT_METHOD_LABELS[method] ?? method ?? "";
}

/** ["Admin"] -> "Quản lý";  ["Admin","Employee"] -> "Quản lý, Nhân viên" */
export function roleText(roles) {
    const list = Array.isArray(roles) ? roles : [roles];
    return list.filter(Boolean).map(r => ROLE_LABELS[r] ?? r).join(", ");
}

/* ============================================================================
 *  PHẦN 6: LÀM VIỆC VỚI GIAO DIỆN (form, ô tìm kiếm, nút bấm)
 * ========================================================================== */

/**
 * Gom dữ liệu trong một <form> thành object để gửi lên API.
 * Mỗi ô nhập cần có thuộc tính name, ví dụ <input name="firstName">.
 *
 *   <form id="employeeForm">
 *       <input name="firstName" value="An">
 *       <input name="storeId" value="2">
 *   </form>
 *   formToObject(document.getElementById("employeeForm"))
 *       -> { firstName: "An", storeId: "2" }
 *
 * Lưu ý: giá trị lấy từ form LUÔN là chữ. Nếu API cần số thì tự đổi:
 *       data.storeId = data.storeId ? Number(data.storeId) : null;
 * Ô trống thì trả về chuỗi rỗng ""; xóa khoảng trắng thừa ở hai đầu bằng trim().
 */
export function formToObject(form) {
    const data = {};
    const formData = new FormData(form); // FormData tự đọc tất cả ô có name trong form
    for (const [key, value] of formData.entries()) {
        data[key] = typeof value === "string" ? value.trim() : value;
    }
    return data;
}

/**
 * "Chờ người dùng ngừng gõ rồi mới chạy".
 *
 * Ví dụ ô tìm kiếm: nếu mỗi lần gõ một chữ là gọi API một lần, gõ "nguyen"
 * sẽ gọi 6 lần. debounce giúp đợi người dùng dừng gõ khoảng 400ms rồi mới
 * gọi đúng 1 lần.
 *
 *   const search = debounce(keyword => loadCustomers(keyword), 400);
 *   input.addEventListener("input", e => search(e.target.value));
 *
 * Cách hoạt động: mỗi lần được gọi, nó hủy hẹn giờ cũ (clearTimeout) và hẹn
 * giờ mới (setTimeout). Chỉ khi không bị gọi nữa đủ lâu thì hẹn giờ mới "nổ".
 */
export function debounce(fn, delay = 400) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => fn(...args), delay);
    };
}

/**
 * Khóa / mở khóa nút bấm trong lúc đang gửi yêu cầu, để người dùng không
 * bấm "Lưu" 2-3 lần liên tiếp tạo ra dữ liệu trùng.
 *
 *   setLoading(btnSave, true);           // nút mờ đi, chữ thành "Đang xử lý..."
 *   try { await post(...); }
 *   finally { setLoading(btnSave, false); } // luôn mở lại, dù thành công hay lỗi
 *
 * button.dataset.originalText: cất tạm chữ cũ của nút vào chính cái nút,
 * để lúc mở khóa còn biết mà trả lại.
 */
export function setLoading(button, isLoading, loadingText = "Đang xử lý...") {
    if (!button) return;
    if (isLoading) {
        button.dataset.originalText = button.textContent;
        button.textContent = loadingText;
        button.disabled = true;
    } else {
        button.textContent = button.dataset.originalText ?? button.textContent;
        button.disabled = false;
    }
}

/* ============================================================================
 *  PHẦN 7: LINH TINH
 * ========================================================================== */

/**
 * Ghép họ và tên. Backend tách firstName / lastName riêng.
 * [SỬA] Theo quy ước backend của nhóm: firstName = HỌ (và tên đệm), lastName = TÊN
 *       (giống backend ghép FullName = FirstName + " " + LastName).
 *   fullName({ firstName: "Nguyễn Văn", lastName: "An" }) -> "Nguyễn Văn An"
 */
export function fullName(person) {
    if (!person) return "";
    return [person.firstName, person.lastName].filter(Boolean).join(" ");
}

/**
 * Hỏi lại trước khi làm việc nguy hiểm (xóa...). Trả về true nếu người dùng bấm OK.
 *   if (!confirmAction("Xóa nhân viên này?")) return;
 * Khi Nhóm 3 làm xong components/modal.js có thể thay bằng hộp thoại đẹp hơn.
 */
export function confirmAction(message = "Bạn có chắc chắn muốn thực hiện?") {
    return window.confirm(message);
}
