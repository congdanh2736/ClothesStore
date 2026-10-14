/*
 * ============================================================================
 *  layout.js  -  DỰNG "KHUNG" CHUNG CHO MỌI TRANG ADMIN
 * ============================================================================
 *
 *  Mỗi trang admin đều cần: kiểm tra đăng nhập, menu bên trái, thanh trên
 *  cùng có tên người dùng + nút đăng xuất. File này làm hết những việc đó,
 *  mỗi trang chỉ cần gọi MỘT hàm initAdminLayout() ở đầu.
 *
 *  initAdminLayout() làm lần lượt:
 *    1. Kiểm tra quyền: chưa đăng nhập / là khách hàng -> đẩy ra ngoài.
 *       Trang chỉ dành cho Admin mà Employee vào -> đẩy về dashboard.
 *    2. Đọc 2 file partials/sidebar.html, partials/topbar.html rồi chèn vào trang.
 *    3. Ẩn các mục menu có data-role="admin" nếu người dùng không phải Admin.
 *    4. Tô màu mục menu đang mở.
 *    5. Điền tên, vai trò người dùng; gắn nút Đăng xuất; gắn nút ☰ (điện thoại).
 *    6. Đặt tiêu đề trang, rồi mới cho khung hiện lên.
 *
 *  Cách dùng trong một trang (xem admin/_template.html):
 *
 *      <script type="module">
 *          import { initAdminLayout } from "/admin/js/layout.js";
 *
 *          // trang Admin + Employee đều vào được:
 *          if (await initAdminLayout({ title: "Khách hàng" })) {
 *              loadData();     // chỉ chạy tiếp khi đủ quyền
 *          }
 *
 *          // trang CHỈ Admin:
 *          if (await initAdminLayout({ title: "Nhân viên", adminOnly: true })) { ... }
 *
 *          // trang chi tiết muốn menu "Đơn hàng" sáng lên:
 *          await initAdminLayout({ title: "Chi tiết đơn", activeMenu: "/admin/sales/orders.html" });
 *      </script>
 *
 *  Vì sao phải "if (await ...)": nếu không đủ quyền, trang đang bị chuyển đi,
 *  ta không nên gọi API nữa (gọi cũng chỉ bị lỗi 401/403).
 * ============================================================================
 */

import { requireStaff, requireAdmin, isAdmin, getUser, getDisplayName, logout } from "../../assets/js/core/auth.js";
import { ROLES, ROLE_LABELS } from "../../assets/js/core/config.js";
import { confirmDialog } from "../../assets/js/components/modal.js";

// Đường dẫn tới 2 file "mảnh" HTML
const SIDEBAR_URL = "/admin/partials/sidebar.html";
const TOPBAR_URL = "/admin/partials/topbar.html";

// Tên hiện trên tab trình duyệt: "Khách hàng | ClothesStore Admin"
const APP_NAME = "ClothesStore Admin";

/**
 * Đọc nội dung một file HTML trên server (dạng chữ).
 * Dùng fetch trực tiếp (không qua http.js) vì đây là file tĩnh, không phải API.
 */
async function loadPartial(url) {
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error(`Không tải được ${url} (mã ${response.status})`);
    }
    return response.text();
}

/**
 * Dựng khung trang admin. Trả về true nếu đủ quyền và dựng xong, false nếu không.
 *
 * @param {object} [options]
 * @param {string} [options.title]       - tên trang, hiện ở topbar và tab trình duyệt
 * @param {boolean} [options.adminOnly]  - true = chỉ Admin được vào
 * @param {string|null} [options.activeMenu] - đường dẫn mục menu cần tô sáng
 *                                         (mặc định null = chính trang đang mở)
 *
 * (Ghi chú: các dòng @param ở trên không chỉ để người đọc, VS Code cũng đọc
 *  chúng để biết kiểu dữ liệu. "string|null" = "có thể là chữ, hoặc null".
 *  Dấu [ ] quanh tên = tham số không bắt buộc.)
 */
export async function initAdminLayout({ title = "", adminOnly = false, activeMenu = null } = {}) {
    // ---- 1. Kiểm tra quyền ----
    // requireAdmin / requireStaff tự chuyển trang nếu không đủ quyền và trả về false
    const allowed = adminOnly ? requireAdmin() : requireStaff();
    if (!allowed) return false;

    const sidebar = document.getElementById("sidebar");
    const topbar = document.getElementById("topbar");

    // ---- 2. Tải và chèn sidebar + topbar ----
    // Promise.all: tải CẢ HAI file cùng lúc (nhanh hơn tải lần lượt từng file)
    try {
        const [sidebarHtml, topbarHtml] = await Promise.all([
            loadPartial(SIDEBAR_URL),
            loadPartial(TOPBAR_URL)
        ]);
        if (sidebar) sidebar.innerHTML = sidebarHtml;
        if (topbar) topbar.innerHTML = topbarHtml;
    } catch (err) {
        // Tải lỗi thì vẫn cho trang chạy, chỉ báo ra Console (F12) để dễ tìm lỗi
        console.error("[layout] ", err);
    }

    // ---- 3. Ẩn các mục chỉ dành cho Admin ----
    if (!isAdmin()) {
        document.querySelectorAll('[data-role="admin"]').forEach(el => el.remove());
    }

    // ---- 4. Tô sáng mục menu đang mở ----
    highlightActiveMenu(activeMenu ?? window.location.pathname);

    // ---- 5. Thông tin người dùng + các nút ----
    fillUserInfo();
    setupLogoutButton();
    setupSidebarToggle();

    // ---- 6. Tiêu đề, rồi cho hiện khung lên ----
    setPageTitle(title);
    document.querySelector(".admin-shell")?.classList.add("is-ready");

    return true;
}

/**
 * Đổi tiêu đề trang (topbar + tab trình duyệt).
 * Trang chi tiết có thể gọi lại sau khi tải xong dữ liệu, ví dụ:
 *     setPageTitle(`Đơn hàng #${order.id}`);
 */
export function setPageTitle(title) {
    const titleElement = document.querySelector("[data-page-title]");
    if (titleElement) titleElement.textContent = title;
    document.title = title ? `${title} | ${APP_NAME}` : APP_NAME;
}

/* ---------------------------- Các hàm phụ ---------------------------- */

/**
 * Tìm mục menu có đường dẫn trùng với trang hiện tại và thêm class "is-active".
 * new URL(...).pathname: lấy riêng phần đường dẫn (bỏ tên miền, bỏ ?id=...)
 * để so sánh cho chính xác.
 *
 * Dùng link.getAttribute("href") thay vì link.href: VS Code chỉ biết link là
 * một "phần tử" chung chung (chưa chắc là thẻ <a>) nên báo lỗi khi đọc .href.
 * getAttribute thì phần tử nào cũng có. "?? """ = không có href thì lấy chuỗi rỗng.
 *
 * @param {string} path
 */
function highlightActiveMenu(path) {
    document.querySelectorAll("#sidebar .nav-link").forEach(link => {
        const linkPath = new URL(link.getAttribute("href") ?? "", window.location.origin).pathname;
        const active = linkPath === path;
        link.classList.toggle("is-active", active);
        if (active) link.setAttribute("aria-current", "page");
    });
}

/** Điền họ tên, vai trò và chữ cái đầu vào topbar. */
function fillUserInfo() {
    const user = getUser();
    const name = getDisplayName() || "Người dùng";

    // Vai trò: có Admin thì hiện "Quản lý", không thì "Nhân viên"
    const roleText = user?.roles?.includes(ROLES.ADMIN) ? ROLE_LABELS.Admin : ROLE_LABELS.Employee;

    // Chữ cái đầu: người Việt gọi bằng tên (từ CUỐI cùng), ví dụ "Nguyễn Văn An" -> "A"
    const words = name.trim().split(/\s+/);
    const initial = (words[words.length - 1] || "?").charAt(0).toUpperCase();

    setText("[data-user-name]", name);
    setText("[data-user-role]", roleText);
    setText("[data-user-avatar]", initial);
}

/** Gán chữ vào phần tử (textContent nên luôn an toàn, không lo chèn mã). */
function setText(selector, text) {
    const element = document.querySelector(selector);
    if (element) element.textContent = text;
}

/** Nút Đăng xuất: hỏi lại cho chắc rồi mới đăng xuất. */
function setupLogoutButton() {
    const button = document.querySelector("[data-logout]");
    if (!button) return;

    button.addEventListener("click", async () => {
        const ok = await confirmDialog({
            title: "Đăng xuất",
            message: "Bạn có muốn đăng xuất khỏi trang quản trị?",
            confirmText: "Đăng xuất"
        });
        if (ok) logout(); // logout() trong auth.js: xóa thẻ và về trang đăng nhập
    });
}

/**
 * Nút ☰ trên điện thoại: bấm để mở/đóng menu.
 * Mở menu = thêm class "sidebar-open" vào <body> (CSS trong admin.css lo phần hiển thị).
 * Bấm ra vùng tối bên ngoài hoặc bấm chọn một mục menu thì đóng lại.
 */
function setupSidebarToggle() {
    // Tạo lớp nền tối phía sau menu (chỉ hiện trên điện thoại khi menu mở)
    let overlay = document.querySelector(".sidebar-overlay");
    if (!overlay) {
        overlay = document.createElement("div");
        overlay.className = "sidebar-overlay";
        document.body.appendChild(overlay);
    }

    const close = () => document.body.classList.remove("sidebar-open");

    document.querySelector("[data-sidebar-toggle]")?.addEventListener("click", () => {
        document.body.classList.toggle("sidebar-open"); // toggle = đang có thì bỏ, chưa có thì thêm
    });
    overlay.addEventListener("click", close);
    document.querySelectorAll("#sidebar .nav-link").forEach(link => link.addEventListener("click", close));
}
