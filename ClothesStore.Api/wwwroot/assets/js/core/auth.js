/*
 * ============================================================================
 *  auth.js  -  QUẢN LÝ "AI ĐANG ĐĂNG NHẬP"
 * ============================================================================
 *
 *  Hình dung: khi đăng nhập thành công, backend đưa cho ta một "thẻ ra vào"
 *  (gọi là token, dạng JWT - một chuỗi ký tự rất dài). Mỗi lần gọi API cần
 *  quyền, ta phải trình thẻ này ra. File này lo các việc xoay quanh cái thẻ đó:
 *
 *    1. CẤT thẻ và thông tin người dùng vào tủ localStorage   -> saveLogin()
 *    2. LẤY thẻ / thông tin ra khi cần                        -> getToken(), getUser()...
 *    3. KIỂM TRA: đã đăng nhập chưa? thẻ còn hạn không?
 *       có phải Admin không?                                  -> isLoggedIn(), isAdmin()...
 *    4. CHẶN CỬA: trang nào chỉ cho nhân sự / Admin vào       -> requireStaff(), requireAdmin()...
 *    5. ĐĂNG XUẤT: vứt thẻ đi, quay về trang đăng nhập        -> logout()
 *
 *  File này KHÔNG tự gọi API đăng nhập. Việc gửi email + mật khẩu lên server
 *  nằm ở api/authApi.js. Sau khi server trả kết quả, trang login sẽ gọi
 *  saveLogin(ketQua) ở file này để cất lại. (Tách ra như vậy để http.js và
 *  auth.js không phải gọi qua gọi lại lẫn nhau, đỡ rối.)
 *
 *  Ví dụ dùng ở đầu một trang admin:
 *      import { requireStaff } from "../assets/js/core/auth.js";
 *      requireStaff();   // chưa đăng nhập hoặc là Customer -> bị đẩy ra ngoài
 *
 *  LƯU Ý BẢO MẬT (nói đơn giản): việc chặn ở frontend chỉ để GIAO DIỆN gọn gàng
 *  (ẩn trang, ẩn nút). Chặn thật sự là ở backend ([Authorize] trong Controller).
 *  Người dùng có thể sửa code JS trên máy họ, nhưng không sửa được backend.
 * ============================================================================
 */

import { STORAGE_KEYS, ROLES, STAFF_ROLES, PAGES } from "./config.js";

/* ============================================================================
 *  PHẦN 1: CẤT VÀ LẤY THÔNG TIN ĐĂNG NHẬP
 * ========================================================================== */

/**
 * Cất kết quả đăng nhập vào localStorage.
 *
 * Backend (POST /api/Auth/login) trả về dạng:
 *   {
 *     token: "eyJhbGciOi...",      <- thẻ ra vào
 *     email: "a@gmail.com",
 *     roles: ["Admin"],            <- danh sách vai trò (là MẢNG, có thể nhiều vai trò)
 *     customerId: null,            <- có số nếu là khách hàng
 *     employeeId: 3,               <- có số nếu là nhân viên
 *     fullName: "Nguyễn Văn A"
 *   }
 *
 * localStorage chỉ cất được CHỮ, không cất được object. Nên ta dùng
 * JSON.stringify() để "đóng gói" object thành chữ trước khi cất.
 *
 * @param {object} result - đúng cái object backend trả về ở trên
 */
export function saveLogin(result) {
    if (!result || !result.token) {
        // Không có token thì coi như đăng nhập không thành công, không cất gì cả
        throw new Error("Dữ liệu đăng nhập không hợp lệ (thiếu token).");
    }

    // Cất thẻ riêng một ngăn, vì http.js cần lấy nó rất thường xuyên
    localStorage.setItem(STORAGE_KEYS.TOKEN, result.token);

    // Phần thông tin còn lại gom chung vào một ngăn
    const user = {
        email: result.email ?? "",
        fullName: result.fullName ?? "",
        roles: Array.isArray(result.roles) ? result.roles : [], // chắc chắn luôn là mảng
        customerId: result.customerId ?? null,
        employeeId: result.employeeId ?? null
    };
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user));
}

/**
 * Lấy cái thẻ (token) ra. Chưa đăng nhập thì trả về null (nghĩa là "không có gì").
 */
export function getToken() {
    return localStorage.getItem(STORAGE_KEYS.TOKEN);
}

/**
 * Lấy thông tin người dùng đã cất. Chưa đăng nhập thì trả về null.
 *
 * JSON.parse() là bước "mở gói": biến chữ trở lại thành object.
 * Bọc trong try...catch vì nếu dữ liệu trong tủ bị hỏng (ai đó sửa tay)
 * thì JSON.parse sẽ báo lỗi -> ta bắt lỗi đó và coi như chưa đăng nhập,
 * thay vì để cả trang bị "đứng hình".
 */
export function getUser() {
    const raw = localStorage.getItem(STORAGE_KEYS.USER);
    if (!raw) return null;

    try {
        return JSON.parse(raw);
    } catch {
        return null;
    }
}

/** Danh sách vai trò của người đang đăng nhập, ví dụ ["Admin"]. Chưa đăng nhập -> []. */
export function getRoles() {
    return getUser()?.roles ?? [];
    // Dấu "?." nghĩa là: nếu getUser() ra null thì dừng lại, không đọc tiếp .roles (tránh lỗi).
    // Dấu "??" nghĩa là: nếu bên trái là null/undefined thì lấy giá trị bên phải.
}

/** Họ tên để hiện lên góc trên màn hình (topbar). Không có họ tên thì hiện email. */
export function getDisplayName() {
    const user = getUser();
    if (!user) return "";
    return user.fullName || user.email;
}

/** Id khách hàng (dùng cho trang tài khoản, giỏ hàng...). Không phải khách -> null. */
export function getCustomerId() {
    return getUser()?.customerId ?? null;
}

/** Id nhân viên. Không phải nhân viên -> null. */
export function getEmployeeId() {
    return getUser()?.employeeId ?? null;
}

/**
 * Xóa sạch thông tin đăng nhập khỏi tủ (nhưng KHÔNG chuyển trang).
 * http.js dùng hàm này khi server báo thẻ hết hạn.
 */
export function clearLogin() {
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    localStorage.removeItem(STORAGE_KEYS.USER);
}

/* ============================================================================
 *  PHẦN 2: KIỂM TRA TÌNH TRẠNG ĐĂNG NHẬP
 * ========================================================================== */

/**
 * Xem cái thẻ còn hạn hay không.
 *
 * Token JWT gồm 3 khúc nối nhau bằng dấu chấm:  khúc1.khúc2.khúc3
 *   - khúc 2 chứa thông tin, trong đó có "exp" = thời điểm hết hạn
 *     (tính bằng SỐ GIÂY kể từ ngày 1/1/1970 - cách máy tính hay dùng để ghi giờ).
 *   - khúc 2 được mã hóa kiểu base64 (một cách viết lại chữ cho an toàn khi gửi đi),
 *     nên phải "giải mã" bằng atob() mới đọc được.
 *
 * Backend đang đặt thẻ sống 60 phút (Jwt:ExpiresInMinutes = 60).
 * Kiểm tra trước ở đây giúp người dùng bị đưa về trang đăng nhập ngay,
 * thay vì bấm nút rồi mới thấy lỗi.
 *
 * @returns {boolean} true = đã hết hạn (hoặc thẻ hỏng), false = còn dùng được
 */
export function isTokenExpired(token = getToken()) {
    if (!token) return true;

    try {
        const parts = token.split(".");          // cắt thành 3 khúc
        if (parts.length !== 3) return true;     // không đủ 3 khúc -> thẻ hỏng

        // base64 trong JWT dùng "-" và "_" thay cho "+" và "/", nên đổi lại cho atob hiểu
        const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
        const payload = JSON.parse(atob(base64)); // giải mã rồi "mở gói" thành object

        if (!payload.exp) return false;          // thẻ không ghi hạn -> coi như còn dùng được

        const nowInSeconds = Date.now() / 1000;  // Date.now() ra mili-giây, chia 1000 thành giây
        return payload.exp < nowInSeconds;       // hạn nhỏ hơn bây giờ -> đã hết hạn
    } catch {
        return true; // giải mã lỗi -> coi như thẻ hỏng
    }
}

/**
 * Đã đăng nhập (và thẻ còn hạn) chưa?
 * Nếu thẻ đã hết hạn thì tiện tay dọn luôn khỏi tủ cho sạch.
 */
export function isLoggedIn() {
    const token = getToken();
    if (!token) return false;

    if (isTokenExpired(token)) {
        clearLogin();
        return false;
    }
    return true;
}

/**
 * Người dùng có MỘT TRONG các vai trò được liệt kê hay không.
 * Ví dụ: hasRole("Admin")              -> có phải Admin?
 *        hasRole("Admin", "Employee")  -> là Admin HOẶC Employee?
 *
 * "...roles" (ba dấu chấm) cho phép truyền vào bao nhiêu tham số cũng được,
 * tất cả sẽ được gom vào một mảng tên là roles.
 */
export function hasRole(...roles) {
    const myRoles = getRoles();
    return roles.some(role => myRoles.includes(role));
    // .some(...) = "có ít nhất một phần tử thỏa điều kiện không?"
}

/** Có phải Quản lý (Admin)? */
export function isAdmin() {
    return isLoggedIn() && hasRole(ROLES.ADMIN);
}

/** Có phải nhân sự (Admin hoặc Employee)? - người được vào trang admin */
export function isStaff() {
    return isLoggedIn() && hasRole(...STAFF_ROLES);
}

/** Có phải khách hàng? */
export function isCustomer() {
    return isLoggedIn() && hasRole(ROLES.CUSTOMER);
}

/* ============================================================================
 *  PHẦN 3: CHUYỂN TRANG
 * ========================================================================== */

/**
 * Lấy địa chỉ trang hiện tại (phần sau tên miền), ví dụ "/admin/customers/customers.html?page=2".
 * Dùng để ghi nhớ "đang ở đâu" trước khi bị đẩy ra trang đăng nhập,
 * đăng nhập xong thì quay lại đúng chỗ đó.
 */
function currentPath() {
    return window.location.pathname + window.location.search;
}

/**
 * Đưa người dùng tới trang đăng nhập phù hợp.
 *  - Đang ở trong khu /admin/ -> trang đăng nhập admin
 *  - Ở chỗ khác               -> trang đăng nhập của shop
 * Kèm theo "?returnUrl=..." để đăng nhập xong biết đường quay lại.
 *
 * encodeURIComponent(): "gói" địa chỉ lại cho an toàn khi nhét vào URL
 * (vì địa chỉ có thể chứa ký tự đặc biệt như ? & =).
 *
 * window.location.replace(): chuyển trang và KHÔNG lưu trang cũ vào lịch sử,
 * nên bấm nút "Back" sẽ không quay lại trang bị chặn.
 */
export function redirectToLogin() {
    const isAdminArea = window.location.pathname.startsWith("/admin");
    const loginPage = isAdminArea ? PAGES.ADMIN_LOGIN : PAGES.SHOP_LOGIN;

    // Nếu đang đứng ngay trang đăng nhập rồi thì thôi, khỏi chuyển (tránh lặp vô hạn)
    if (window.location.pathname === loginPage) return;

    window.location.replace(`${loginPage}?returnUrl=${encodeURIComponent(currentPath())}`);
}

/**
 * Gọi ngay sau khi đăng nhập thành công: đưa người dùng tới đúng trang.
 *  - Nếu URL đang có ?returnUrl=... (bị đẩy ra từ trang nào) -> quay lại trang đó
 *  - Nếu không: Admin/Employee -> dashboard, Customer -> trang chủ shop
 *
 * Kiểm tra returnUrl phải bắt đầu bằng "/" (và không phải "//") để chắc chắn
 * nó là trang TRONG web mình, tránh bị lừa chuyển sang trang web lạ.
 * Customer cũng không được quay lại trang /admin.
 */
export function redirectAfterLogin() {
    const params = new URLSearchParams(window.location.search);
    const returnUrl = params.get("returnUrl");

    const isSafeUrl = returnUrl && returnUrl.startsWith("/") && !returnUrl.startsWith("//");
    const goesToAdmin = returnUrl && returnUrl.startsWith("/admin");

    if (isSafeUrl && (!goesToAdmin || isStaff())) {
        window.location.replace(returnUrl);
        return;
    }

    window.location.replace(isStaff() ? PAGES.ADMIN_HOME : PAGES.SHOP_HOME);
}

/**
 * Đăng xuất: vứt thẻ và về trang đăng nhập.
 * Gắn vào nút "Đăng xuất" trên topbar:
 *     document.getElementById("btnLogout").addEventListener("click", logout);
 *
 * (Backend dùng JWT nên không cần gọi API đăng xuất, chỉ cần xóa thẻ ở trình duyệt.)
 */
export function logout() {
    const isAdminArea = window.location.pathname.startsWith("/admin");
    clearLogin();
    window.location.replace(isAdminArea ? PAGES.ADMIN_LOGIN : PAGES.SHOP_LOGIN);
}

/* ============================================================================
 *  PHẦN 4: "BẢO VỆ" CHẶN CỬA CÁC TRANG
 *  Gọi các hàm này ở ĐẦU file JS của mỗi trang. Nếu không đủ quyền thì bị
 *  chuyển trang ngay. Hàm trả về true/false để trang biết có nên chạy tiếp không:
 *
 *      if (!requireAdmin()) return;   // (đặt trong hàm khởi tạo của trang)
 * ========================================================================== */

/**
 * Trang chỉ cần đăng nhập (vai trò nào cũng được). Ví dụ: trang tài khoản của khách.
 */
export function requireLogin() {
    if (!isLoggedIn()) {
        redirectToLogin();
        return false;
    }
    return true;
}

/**
 * Trang dành cho NHÂN SỰ (Admin + Employee). Dùng cho hầu hết trang trong /admin/.
 *  - Chưa đăng nhập             -> đưa về trang đăng nhập admin
 *  - Đã đăng nhập nhưng là khách -> đưa về trang chủ shop (khách không có việc gì ở đây)
 */
export function requireStaff() {
    if (!requireLogin()) return false;

    if (!isStaff()) {
        window.location.replace(PAGES.SHOP_HOME);
        return false;
    }
    return true;
}

/**
 * Trang CHỈ dành cho Admin. Ví dụ của Nhóm 1: quản lý nhân viên, hạng thành viên.
 *  - Employee vào -> báo không có quyền rồi đưa về dashboard.
 *
 * alert() hiện một hộp thông báo đơn giản của trình duyệt. Khi Nhóm 3 làm xong
 * components/toast.js thì có thể thay bằng thông báo đẹp hơn.
 */
export function requireAdmin() {
    if (!requireStaff()) return false;

    if (!isAdmin()) {
        alert("Bạn không có quyền truy cập trang này (chỉ dành cho Quản lý).");
        window.location.replace(PAGES.ADMIN_HOME);
        return false;
    }
    return true;
}

/**
 * Trang chỉ dành cho KHÁCH HÀNG (giỏ hàng, đặt hàng, đơn của tôi...).
 *  - Nhân sự lỡ vào -> đưa về dashboard admin.
 */
export function requireCustomer() {
    if (!requireLogin()) return false;

    if (!isCustomer()) {
        window.location.replace(PAGES.ADMIN_HOME);
        return false;
    }
    return true;
}

/**
 * Dành cho trang đăng nhập: nếu đã đăng nhập rồi thì khỏi bắt đăng nhập lại,
 * chuyển thẳng vào trong luôn.
 *     // ở đầu admin/login.html
 *     redirectIfLoggedIn();
 */
export function redirectIfLoggedIn() {
    if (isLoggedIn()) {
        redirectAfterLogin();
        return true;
    }
    return false;
}
