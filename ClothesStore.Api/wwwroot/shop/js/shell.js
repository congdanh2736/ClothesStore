/*
 * ============================================================================
 *  shell.js  -  KHUNG DÙNG CHUNG CHO MỌI TRANG SHOP
 * ============================================================================
 *
 *  Mỗi trang chỉ cần:
 *      <header id="siteHeader"></header>  ... nội dung ...  <footer id="siteFooter"></footer>
 *      import { initShop } from "./js/shell.js";
 *      await initShop({ active: "products" });            // trang ai cũng xem được
 *      await initShop({ active: "account", needLogin: true }); // trang phải đăng nhập
 *
 *  File này lo:
 *    - Thanh menu (danh mục lấy từ API), ô tìm kiếm, giỏ hàng, tài khoản
 *    - Nút ĐỔI SÁNG / TỐI (lưu lựa chọn vào trình duyệt)
 *    - Chân trang
 *    - Các tiện ích: toast (thông báo nhỏ), hộp thoại (modal), thẻ sản phẩm, sao đánh giá...
 * ============================================================================
 */

import { shopApi, USE_MOCK } from "./shopApi.js";
import { isLoggedIn, getUser, logout, requireCustomer } from "../../assets/js/core/auth.js";
import { escapeHtml, formatMoney, toDate } from "../../assets/js/core/utils.js";

/* ===================================================== ICON (SVG) ===================================================== */
const svg = (/** @type {string} */ d) => `<svg class="ico" viewBox="0 0 24 24" aria-hidden="true">${d}</svg>`;
export const ICONS = {
    search: svg('<circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/>'),
    heart: svg('<path d="M19 14c1.5-1.5 3-3.3 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.8 0-3 .5-4.5 2-1.5-1.5-2.7-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4 3 5.5l7 7Z"/>'),
    bag: svg('<path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/><path d="M3 6h18"/><path d="M16 10a4 4 0 0 1-8 0"/>'),
    user: svg('<circle cx="12" cy="8" r="4"/><path d="M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1"/>'),
    sun: svg('<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>'),
    moon: svg('<path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z"/>'),
    menu: svg('<path d="M4 6h16M4 12h16M4 18h16"/>'),
    close: svg('<path d="M18 6 6 18M6 6l12 12"/>'),
    chevron: svg('<path d="m6 9 6 6 6-6"/>'),
    arrow: svg('<path d="M5 12h14M13 5l7 7-7 7"/>'),
    pin: svg('<path d="M12 22s7-6.2 7-12a7 7 0 0 0-14 0c0 5.8 7 12 7 12Z"/><circle cx="12" cy="10" r="2.5"/>'),
    store: svg('<path d="M3 9 4.5 4h15L21 9"/><path d="M4 9v11h16V9"/><path d="M3 9h18M9 20v-6h6v6"/>'),
    truck: svg('<path d="M1 4h14v12H1z"/><path d="M15 9h4l3 3v4h-7"/><circle cx="6" cy="18" r="2"/><circle cx="18" cy="18" r="2"/>'),
    box: svg('<path d="M21 8 12 3 3 8v8l9 5 9-5Z"/><path d="m3 8 9 5 9-5M12 13v8"/>'),
    ret: svg('<path d="M9 14 4 9l5-5"/><path d="M4 9h11a5 5 0 0 1 0 10h-3"/>'),
    star: '<svg class="ico solid" viewBox="0 0 24 24" aria-hidden="true"><path d="m12 2 3.1 6.3 6.9 1-5 4.9 1.2 6.8L12 17.8 5.8 21l1.2-6.8-5-4.9 6.9-1Z"/></svg>',
    tag: svg('<path d="M20.6 13.4 13.4 20.6a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8Z"/><circle cx="7.5" cy="7.5" r="1.5"/>'),
    ticket: svg('<path d="M3 7a2 2 0 0 0 2-2h14a2 2 0 0 0 2 2v3a2 2 0 0 0 0 4v3a2 2 0 0 0-2 2H5a2 2 0 0 0-2-2v-3a2 2 0 0 0 0-4Z"/><path d="M13 5v2M13 17v2M13 11v2"/>'),
    shield: svg('<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z"/><path d="m9 12 2 2 4-4"/>'),
    check: svg('<path d="M20 6 9 17l-5-5"/>'),
    plus: svg('<path d="M12 5v14M5 12h14"/>'),
    edit: svg('<path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/>'),
    trash: svg('<path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6"/>'),
    ruler: svg('<path d="M21.3 8.7 8.7 21.3a1 1 0 0 1-1.4 0l-4.6-4.6a1 1 0 0 1 0-1.4L15.3 2.7a1 1 0 0 1 1.4 0l4.6 4.6a1 1 0 0 1 0 1.4Z"/><path d="m7.5 10.5 2 2M10.5 7.5l2 2M13.5 4.5l2 2M4.5 13.5l2 2"/>'),
    gift: svg('<path d="M20 12v10H4V12M2 7h20v5H2zM12 22V7"/><path d="M12 7H7.5a2.5 2.5 0 0 1 0-5C11 2 12 7 12 7ZM12 7h4.5a2.5 2.5 0 0 0 0-5C13 2 12 7 12 7Z"/>'),
    coin: svg('<circle cx="12" cy="12" r="9"/><path d="M14.5 9a2.5 2.5 0 0 0-2.5-1.5c-1.4 0-2.5.8-2.5 2s1 1.7 2.5 2 2.5.8 2.5 2-1.1 2-2.5 2A2.5 2.5 0 0 1 9.5 15M12 6v1.5M12 16.5V18"/>'),
    clock: svg('<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>'),
    logout: svg('<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9"/>'),
    list: svg('<path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/>'),
    chat: svg('<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2Z"/>'),
    layers: svg('<path d="m12 2 10 5-10 5L2 7Z"/><path d="m2 17 10 5 10-5M2 12l10 5 10-5"/>'),
    drop: svg('<path d="M12 2.7S5 10 5 14.5a7 7 0 0 0 14 0C19 10 12 2.7 12 2.7Z"/>'),
    wind: svg('<path d="M3 8h11a3 3 0 1 0-3-3M3 16h15a3 3 0 1 1-3 3M3 12h18"/>'),
    copy: svg('<rect x="9" y="9" width="12" height="12" rx="2"/><path d="M5 15V5a2 2 0 0 1 2-2h8"/>'),
    camera: svg('<path d="M4 7h3l2-3h6l2 3h3a1 1 0 0 1 1 1v11a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V8a1 1 0 0 1 1-1Z"/><circle cx="12" cy="13" r="4"/>')
};

/** Logo chữ A cách điệu */
const LOGO = `<svg class="brand-mark" viewBox="0 0 32 32" aria-hidden="true"><path fill="currentColor" d="M16 3 3 29h7l6-12.5L22 29h7Z"/><path fill="currentColor" opacity=".55" d="M13 23h6l-3-6Z"/></svg>`;

/* ===================================================== SÁNG / TỐI ===================================================== */
const THEME_KEY = "aura-theme";

/** Giao diện hiện tại: "dark" | "light" */
export function getTheme() {
    return document.documentElement.dataset.theme === "light" ? "light" : "dark";
}

/** Đổi giao diện và nhớ lựa chọn cho lần sau. @param {"dark"|"light"} theme */
export function setTheme(theme) {
    document.documentElement.dataset.theme = theme;
    try { localStorage.setItem(THEME_KEY, theme); } catch { /* trình duyệt chặn lưu: bỏ qua */ }
}

/* ===================================================== TIỆN ÍCH ===================================================== */

/** Mã màu cho chấm màu sản phẩm (tên màu trong dữ liệu -> mã màu) */
const COLOR_HEX = {
    "Đen": "#1f1f1f", "Trắng": "#f4f4f4", "Xám": "#9e9e9e", "Xám Đậm": "#4a4a4a", "Xanh Navy": "#1f2f4f",
    "Xanh Rêu": "#4b5a3a", "Xanh Dương": "#2f6fb5", "Xanh Nhạt": "#a9c8e8", "Be": "#d9c7a7", "Kem": "#efe6d2",
    "Nâu": "#6b4a32", "Nâu Đất": "#8a5a3c", "Đỏ Đô": "#6e1f2a", "Đỏ": "#c62f2f", "Vàng Mù Tạt": "#c9a227",
    "Cam Đất": "#c0672f", "Hồng Pastel": "#f2c4cf", "Xanh Đậm": "#1f3a5f", "Xanh Wash": "#6f8fb5",
    "Đen Wash": "#3a3a3a", "Xám Khói": "#6b6b6b", "Bạc": "#c0c0c0", "Vàng Đồng": "#b08d57", "Nguyên bản": "#2b2b33"
};
/** @param {string} name */
export const colorHex = name => /** @type {Record<string,string>} */ (COLOR_HEX)[name] ?? "#888888";

/** Lấy tham số trên địa chỉ trang, ví dụ ?id=5 -> qs("id") = "5" */
/**
 * Đổi ngày giờ backend trả về thành số mili-giây để so sánh / sắp xếp.
 * Backend lưu giờ UTC nhưng chuỗi JSON không có chữ "Z" -> phải dùng toDate() (tự thêm "Z"),
 * dùng new Date() trực tiếp sẽ bị lệch 7 tiếng.
 * @param {unknown} value
 */
export const ts = value => toDate(/** @type {any} */ (value))?.getTime() ?? 0;

export const qs = (/** @type {string} */ name) => new URLSearchParams(location.search).get(name);

/** Lấy thẻ theo id (có ghi chú kiểu để VS Code không báo "possibly null") */
export const $ = (/** @type {string} */ id) => /** @type {HTMLElement} */ (document.getElementById(id));

/** Sao đánh giá, ví dụ stars(4.5) -> 4 sao sáng + 1 sao mờ. @param {number} value */
export function stars(value) {
    let html = "";
    for (let i = 1; i <= 5; i++) html += i <= Math.round(value) ? ICONS.star : ICONS.star.replace("<svg", '<svg class="off"');
    return `<span class="stars" aria-label="${value.toFixed(1)} trên 5 sao">${html}</span>`;
}

/** Tiền ngắn gọn: 1.890.000 ₫ */
export const money = (/** @type {number} */ v) => formatMoney(v);

/**
 * HTML 1 thẻ sản phẩm trong lưới.
 * @param {any} p - ProductDto { id, name, categoryName, mainProductImageUrl, minPrice }
 * @param {Set<number>} [favs] - id các sản phẩm đang yêu thích
 * @param {string} [tag] - nhãn góc trên (ví dụ "Mới")
 */
export function productCard(p, favs, tag) {
    const on = favs?.has(p.id);
    return `
        <article class="product-card">
            <div class="product-media">
                <a href="/shop/product.html?id=${p.id}" aria-label="${escapeHtml(p.name)}">
                    <img src="${escapeHtml(p.mainProductImageUrl)}" alt="" loading="lazy">
                </a>
                ${tag ? `<span class="badge badge-solid tag">${escapeHtml(tag)}</span>` : ""}
                <button class="fav ${on ? "on" : ""}" data-fav="${p.id}" aria-pressed="${on ? "true" : "false"}" aria-label="Yêu thích">${ICONS.heart}</button>
            </div>
            <div class="product-info">
                <span class="cat">${escapeHtml(p.categoryName ?? "")}</span>
                <a class="name" href="/shop/product.html?id=${p.id}">${escapeHtml(p.name)}</a>
                <div class="bottom">
                    <span><span class="from">Từ </span><span class="price">${money(p.minPrice)}</span></span>
                </div>
            </div>
        </article>`;
}

/**
 * Gắn sự kiện cho các nút tim trong một vùng (dùng sau khi vẽ thẻ sản phẩm).
 * Chưa đăng nhập -> chuyển tới trang đăng nhập.
 * @param {HTMLElement} root
 */
export function bindFavButtons(root) {
    root.addEventListener("click", async e => {
        const btn = /** @type {HTMLElement | null} */ ((/** @type {HTMLElement} */ (e.target)).closest("[data-fav]"));
        if (!btn) return;
        e.preventDefault();
        if (!isLoggedIn()) { goLogin(); return; }
        try {
            const on = await shopApi.wishlist.toggle(Number(btn.dataset.fav));
            btn.classList.toggle("on", on);
            btn.setAttribute("aria-pressed", on ? "true" : "false");
            toast(on ? "Đã thêm vào danh sách yêu thích." : "Đã bỏ khỏi danh sách yêu thích.", "success");
            refreshCounts();
        } catch (err) { toast(errMsg(err), "error"); }
    });
}

/** Id các sản phẩm đang yêu thích (chưa đăng nhập -> rỗng) */
export async function favoriteIds() {
    if (!isLoggedIn()) return new Set();
    try { return new Set((await shopApi.wishlist.getMine()).map((/** @type {any} */ w) => w.productId)); } catch { return new Set(); }
}

/** Chuyển tới trang đăng nhập, đăng nhập xong quay lại trang này */
export function goLogin() {
    location.href = `/shop/login.html?returnUrl=${encodeURIComponent(location.pathname + location.search)}`;
}

/** Lấy câu thông báo từ lỗi. @param {unknown} err */
export const errMsg = err => (err instanceof Error ? err.message : String(err));

/* ---------- Thông báo nhỏ góc màn hình ---------- */
/** @param {string} message @param {"info"|"success"|"error"|"warning"} [type] */
export function toast(message, type = "info") {
    let box = document.querySelector(".toasts");
    if (!box) { box = document.createElement("div"); box.className = "toasts"; box.setAttribute("role", "status"); document.body.append(box); }
    const el = document.createElement("div");
    el.className = `toast ${type}`;
    el.textContent = message;
    box.append(el);
    setTimeout(() => el.remove(), 3800);
}

/* ---------- Hộp thoại ---------- */
/**
 * Mở hộp thoại. Trả về { el, close }.
 * @param {{ title: string, body: string, footer?: string, size?: "lg" }} opts
 */
export function openModal({ title, body, footer = "", size }) {
    const backdrop = document.createElement("div");
    backdrop.className = "modal-backdrop";
    backdrop.innerHTML = `
        <div class="modal ${size ?? ""}" role="dialog" aria-modal="true" aria-label="${escapeHtml(title)}">
            <div class="modal-head"><h3>${escapeHtml(title)}</h3><button class="icon-btn" data-close aria-label="Đóng">${ICONS.close}</button></div>
            <div class="modal-body">${body}</div>
            ${footer ? `<div class="modal-foot">${footer}</div>` : ""}
        </div>`;
    const close = () => { backdrop.remove(); document.removeEventListener("keydown", onKey); };
    const onKey = (/** @type {KeyboardEvent} */ e) => { if (e.key === "Escape") close(); };
    backdrop.addEventListener("click", e => {
        const t = /** @type {HTMLElement} */ (e.target);
        if (t === backdrop || t.closest("[data-close]")) close();
    });
    document.addEventListener("keydown", onKey);
    document.body.append(backdrop);
    /** @type {HTMLElement | null} */ (backdrop.querySelector("input, select, textarea, button.btn"))?.focus();
    return { el: backdrop, close };
}

/** Hộp xác nhận Có / Không. @param {string} message @param {string} [okText] */
export function confirmBox(message, okText = "Đồng ý") {
    return new Promise(resolve => {
        const { el, close } = openModal({
            title: "Xác nhận",
            body: `<p>${escapeHtml(message)}</p>`,
            footer: `<button class="btn btn-ghost" data-close>Không</button><button class="btn btn-primary" data-ok>${escapeHtml(okText)}</button>`
        });
        el.addEventListener("click", e => {
            const t = /** @type {HTMLElement} */ (e.target);
            if (t.closest("[data-ok]")) { close(); resolve(true); }
            else if (t === el || t.closest("[data-close]")) resolve(false);
        });
    });
}

/* ===================================================== HEADER / FOOTER ===================================================== */

/**
 * Vẽ menu danh mục từ danh sách Category (cây cha - con)
 * @param {any[]} categories
 */
function megaMenu(categories) {
    const roots = categories.filter(c => !c.parentCategoryId);
    const children = (/** @type {number} */ id) => categories.filter(c => c.parentCategoryId === id);
    const link = (/** @type {any} */ c, cls = "sub") => `<a class="${cls}" href="/shop/products.html?category=${c.id}">${escapeHtml(c.name)}</a>`;
    return roots.map(r => `
        <div>
            <h4>${link(r, "")}</h4>
            ${children(r.id).map(c => children(c.id).length
                ? `${link(c)}${children(c.id).map(g => `<a class="sub" style="padding-left:12px" href="/shop/products.html?category=${g.id}">· ${escapeHtml(g.name)}</a>`).join("")}`
                : link(c)).join("")}
        </div>`).join("");
}

/** @param {string} active */
function headerHtml(active, categories) {
    const user = isLoggedIn() ? getUser() : null;
    const initial = (user?.fullName || user?.email || "?").trim().split(" ").pop()?.charAt(0).toUpperCase();
    const navLink = (/** @type {string} */ key, /** @type {string} */ href, /** @type {string} */ text, cls = "") =>
        `<a class="nav-link ${cls} ${active === key ? "active" : ""}" href="${href}">${text}</a>`;
    return `
        <div class="announce"><span class="dot"></span>Đặt online - giao toàn quốc &nbsp;|&nbsp; <b>Tích 1 điểm cho mỗi 10.000đ</b> khi đơn hoàn tất${USE_MOCK ? " &nbsp;|&nbsp; Đang xem dữ liệu mẫu" : ""}</div>
        <div class="site-header">
            <div class="container header-inner">
                <button class="icon-btn menu-toggle" id="menuToggle" aria-label="Mở menu">${ICONS.menu}</button>
                <a class="brand" href="/shop/index.html" aria-label="AURA Techwear - trang chủ">${LOGO}<span class="brand-text"><b>AURA</b><small>TECHWEAR</small></span></a>
                <nav class="main-nav" aria-label="Menu chính">
                    ${navLink("new", "/shop/products.html?sort=new", "Hàng mới")}
                    <div class="nav-drop" id="navDrop">
                        <button type="button" aria-haspopup="true">Sản phẩm ${ICONS.chevron}</button>
                        <div class="mega">${megaMenu(categories)}</div>
                    </div>
                    ${navLink("promotions", "/shop/promotions.html", "Ưu đãi", "hot")}
                    ${navLink("stores", "/shop/stores.html", "Cửa hàng")}
                </nav>
                <div class="header-actions">
                    <form class="search-box" action="/shop/products.html" role="search">
                        ${ICONS.search}<input name="q" type="search" placeholder="Tìm sản phẩm..." aria-label="Tìm sản phẩm">
                    </form>
                    <button class="icon-btn theme-toggle" id="themeToggle" aria-label="Đổi giao diện sáng / tối" title="Đổi sáng / tối">
                        <span class="moon">${ICONS.moon}</span><span class="sun">${ICONS.sun}</span>
                    </button>
                    <a class="icon-btn" href="/shop/account/wishlist.html" aria-label="Yêu thích">${ICONS.heart}<span class="count hidden" id="wishCount"></span></a>
                    <a class="cart-btn" href="/shop/cart.html" aria-label="Giỏ hàng">
                        <span class="icon-wrap">${ICONS.bag}<span class="count hidden" id="cartCount"></span></span>
                        <span class="txt"><small>Tạm tính</small><b id="cartTotal">${money(0)}</b></span>
                    </a>
                    ${user ? `
                        <div class="user-menu" id="userMenu">
                            <button class="icon-btn" aria-haspopup="true" aria-label="Tài khoản"><span class="avatar">${escapeHtml(initial ?? "?")}</span></button>
                            <div class="dropdown">
                                <div class="who"><b>${escapeHtml(user.fullName || "Khách hàng")}</b><div class="text-xs text-muted">${escapeHtml(user.email)}</div></div>
                                <a href="/shop/account/index.html">${ICONS.user} Tổng quan tài khoản</a>
                                <a href="/shop/account/orders.html">${ICONS.box} Đơn hàng của tôi</a>
                                <a href="/shop/account/returns.html">${ICONS.ret} Đổi / trả hàng</a>
                                <a href="/shop/account/points.html">${ICONS.coin} Điểm thưởng & hạng</a>
                                <a href="/shop/account/addresses.html">${ICONS.pin} Sổ địa chỉ</a>
                                <a href="/shop/account/reviews.html">${ICONS.chat} Đánh giá của tôi</a>
                                <button id="btnLogout">${ICONS.logout} Đăng xuất</button>
                            </div>
                        </div>` : `<a class="btn btn-sm btn-outline" href="/shop/login.html">Đăng nhập</a>`}
                </div>
            </div>
        </div>
        <nav class="mobile-nav" id="mobileNav" aria-label="Menu điện thoại">
            <a href="/shop/products.html?sort=new">Hàng mới</a>
            ${categories.filter(c => !c.parentCategoryId).map(c => `<a class="sub" href="/shop/products.html?category=${c.id}">${escapeHtml(c.name)}</a>`).join("")}
            <a href="/shop/promotions.html">Ưu đãi</a>
            <a href="/shop/stores.html">Cửa hàng</a>
            ${user ? `<a href="/shop/account/index.html">Tài khoản của tôi</a>` : `<a href="/shop/login.html">Đăng nhập / Đăng ký</a>`}
        </nav>`;
}

function footerHtml() {
    return `
        <div class="container">
            <div class="footer-grid">
                <div>
                    <a class="brand" href="/shop/index.html">${LOGO}<span class="brand-text"><b>AURA</b><small>TECHWEAR</small></span></a>
                    <p class="mt-3">Thời trang công nghệ cho nhịp sống đô thị: vải mát, kháng khuẩn, co giãn và bảo vệ trước thời tiết.</p>
                </div>
                <div>
                    <h4>Hỗ trợ khách hàng</h4>
                    <ul>
                        <li><a href="/shop/account/orders.html">Tra cứu đơn hàng</a></li>
                        <li><a href="/shop/account/returns.html">Chính sách đổi trả 30 ngày</a></li>
                        <li><a href="/shop/account/points.html">Quyền lợi hội viên</a></li>
                        <li><a href="/shop/promotions.html">Mã khuyến mãi</a></li>
                    </ul>
                </div>
                <div>
                    <h4>Hệ thống cửa hàng</h4>
                    <ul>
                        <li>Hơn 100 cửa hàng trên toàn quốc</li>
                        <li>Giao online từ Kho Online AURA</li>
                        <li><a class="text-primary fw-bold" href="/shop/stores.html">Tìm cửa hàng gần bạn →</a></li>
                    </ul>
                </div>
                <div>
                    <h4>Công nghệ vải</h4>
                    <div class="tech-tiles">
                        <div class="tech-tile"><b>Vải Mát</b><span>Mát lạnh khi chạm</span></div>
                        <div class="tech-tile"><b>Kháng Khuẩn</b><span>Khử mùi cả ngày</span></div>
                        <div class="tech-tile"><b>Co Giãn</b><span>Vận động thoải mái</span></div>
                        <div class="tech-tile"><b>Thời Tiết</b><span>Chống nước, cản gió</span></div>
                    </div>
                </div>
            </div>
            <div class="footer-bottom">
                <div class="pay-chips"><span class="upper">Thanh toán:</span>
                    <span class="chip">COD</span><span class="chip">MoMo</span><span class="chip">VNPay</span><span class="chip">Chuyển khoản</span>
                </div>
                <span>© ${new Date().getFullYear()} AURA Techwear - Đồ án ClothesStore</span>
            </div>
        </div>`;
}

/** Cập nhật số trên biểu tượng giỏ hàng và yêu thích */
export async function refreshCounts() {
    if (!isLoggedIn()) return;
    try {
        const [cart, wish] = await Promise.all([shopApi.cart.get(), shopApi.wishlist.getMine()]);
        const cartCount = $("cartCount"), wishCount = $("wishCount");
        if (cartCount) { cartCount.textContent = String(cart.totalItems); cartCount.classList.toggle("hidden", !cart.totalItems); }
        if (wishCount) { wishCount.textContent = String(wish.length); wishCount.classList.toggle("hidden", !wish.length); }
        const total = $("cartTotal"); if (total) total.textContent = money(cart.totalAmount);
    } catch { /* lỗi đếm thì bỏ qua, không ảnh hưởng trang */ }
}

/**
 * Dựng khung trang. Trả về false nếu trang cần đăng nhập mà chưa đăng nhập (đã chuyển trang).
 * @param {{ active?: string, needLogin?: boolean }} [options]
 */
export async function initShop({ active = "", needLogin = false } = {}) {
    if (needLogin && !requireCustomer()) return false;

    let categories = [];
    try { categories = await shopApi.getCategories(); } catch { /* menu trống nếu lỗi */ }

    const header = $("siteHeader"), footer = $("siteFooter");
    header.innerHTML = headerHtml(active, categories);
    footer.className = "site-footer";
    footer.innerHTML = footerHtml();

    $("themeToggle").addEventListener("click", () => setTheme(getTheme() === "dark" ? "light" : "dark"));
    $("menuToggle").addEventListener("click", () => $("mobileNav").classList.toggle("open"));
    const userMenu = $("userMenu");
    if (userMenu) {
        userMenu.querySelector("button")?.addEventListener("click", () => userMenu.classList.toggle("open"));
        document.addEventListener("click", e => { if (!userMenu.contains(/** @type {Node} */ (e.target))) userMenu.classList.remove("open"); });
        $("btnLogout").addEventListener("click", logout);
    }
    refreshCounts();
    return true;
}

/* ===================================================== ẢNH SẢN PHẨM CHO DÒNG ĐƠN HÀNG ===================================================== */
/** Bộ nhớ "id sản phẩm -> ảnh đầu tiên" để khỏi hỏi API nhiều lần. @type {Map<number, string>} */
const imageCache = new Map();

/**
 * Lấy ảnh đại diện cho nhiều sản phẩm (dòng đơn hàng / giỏ không có sẵn ảnh).
 * Gọi GET /api/Product/{id}/detail cho từng sản phẩm chưa có trong bộ nhớ.
 * @param {number[]} productIds
 * @returns {Promise<Map<number, string>>}
 */
export async function productImages(productIds) {
    const missing = [...new Set(productIds)].filter(id => !imageCache.has(id));
    await Promise.all(missing.map(async id => {
        try { imageCache.set(id, (await shopApi.products.getDetail(id)).imageUrls[0] ?? ""); }
        catch { imageCache.set(id, ""); }
    }));
    return imageCache;
}

/** Ảnh nhỏ (có ô trống nếu chưa có ảnh). @param {string | undefined} url */
export const thumb = url => url ? `<img class="thumb" src="${escapeHtml(url)}" alt="" loading="lazy">` : `<span class="thumb" style="display:grid;place-items:center">${ICONS.box}</span>`;
