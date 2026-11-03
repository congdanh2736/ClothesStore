/*
 * accountShell.js - KHUNG CHUNG CHO CÁC TRANG "TÀI KHOẢN CỦA TÔI" (shop/account/*.html)
 *
 * Mỗi trang tài khoản gọi:  const me = await initAccount("orders");
 *   - Bắt buộc đăng nhập (chưa đăng nhập -> chuyển sang trang đăng nhập)
 *   - Vẽ menu bên trái vào <aside id="accountNav">
 *   - Trả về thông tin khách (GET /api/Customer/{id}) hoặc null nếu đã chuyển trang
 */
import { initShop, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { logout } from "../../../assets/js/core/auth.js";
import { escapeHtml, formatNumber, toDate } from "../../../assets/js/core/utils.js";

const LINKS = [
    { key: "index", href: "/shop/account/index.html", icon: "user", text: "Tổng quan" },
    { key: "orders", href: "/shop/account/orders.html", icon: "box", text: "Đơn hàng của tôi" },
    { key: "returns", href: "/shop/account/returns.html", icon: "ret", text: "Đổi / trả hàng" },
    { key: "wishlist", href: "/shop/account/wishlist.html", icon: "heart", text: "Yêu thích" },
    { key: "reviews", href: "/shop/account/reviews.html", icon: "star", text: "Đánh giá của tôi" },
    { key: "points", href: "/shop/account/points.html", icon: "coin", text: "Điểm & hạng thành viên" },
    { key: "addresses", href: "/shop/account/addresses.html", icon: "pin", text: "Sổ địa chỉ" }
];

/** Chữ cái đầu để làm ảnh đại diện. @param {string} name */
export const initials = name => (name ?? "?").trim().split(/\s+/).slice(-2).map(w => w[0]).join("").toUpperCase();

/** @param {string} active - khóa của trang đang mở */
export async function initAccount(active) {
    if (!(await initShop({ active: "account", needLogin: true }))) return null;
    const me = await shopApi.me.get();
    const nav = $("accountNav");
    nav.className = "panel account-nav";
    nav.innerHTML = `
        <div class="me"><span class="avatar-lg">${escapeHtml(initials(me.fullName))}</span>
            <div style="min-width:0"><div class="fw-bold" style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escapeHtml(me.fullName)}</div>
                <div class="row text-xs mt-1"><span class="badge badge-primary">${escapeHtml(me.membershipTierName ?? "Thành viên")}</span>
                    <span class="mono text-muted">${formatNumber(me.totalPoints ?? 0)} điểm</span></div></div></div>
        ${LINKS.map(l => `<a class="nav-item ${l.key === active ? "active" : ""}" href="${l.href}">${/** @type {any} */ (ICONS)[l.icon]} ${l.text}</a>`).join("")}
        <div class="sep" style="border-top:1px solid var(--border);margin:8px 0"></div>
        <button class="nav-item" id="navLogout">${ICONS.logout} Đăng xuất</button>`;
    $("navLogout").addEventListener("click", logout);
    // Trên điện thoại menu thành thanh cuộn ngang: cuộn tới mục đang mở
    if (innerWidth <= 960) /** @type {HTMLElement | null} */ (nav.querySelector(".active"))?.scrollIntoView({ inline: "center", block: "nearest" });
    return me;
}

/**
 * Tính hạng hiện tại, hạng kế tiếp và % tiến độ.
 * @param {number} points @param {any[]} tiers - [{ id, tierName, minPoints }]
 */
export function tierInfo(points, tiers) {
    const sorted = [...tiers].sort((a, b) => a.minPoints - b.minPoints);
    const current = [...sorted].reverse().find(t => t.minPoints <= points) ?? sorted[0];
    const next = sorted.find(t => t.minPoints > points) ?? null;
    const from = current?.minPoints ?? 0;
    const pct = next ? Math.min(100, Math.round(((points - from) / (next.minPoints - from)) * 100)) : 100;
    return { sorted, current, next, pct, need: next ? next.minPoints - points : 0 };
}

/** Thẻ hội viên (giống thẻ ngân hàng). @param {any} me @param {any[]} tiers */
export function memberCardHtml(me, tiers) {
    const t = tierInfo(me.totalPoints ?? 0, tiers);
    return `<div class="member-card">
        <div class="row-between" style="position:relative;z-index:1">
            <div><div class="muted">AURA Member</div><div class="tier mt-1">HẠNG ${escapeHtml((t.current?.tierName ?? "").toUpperCase())}</div></div>
            <span class="brand-mark" style="font-family:var(--font-display);font-weight:700;letter-spacing:.2em">AURA</span></div>
        <div style="position:relative;z-index:1">
            <div class="muted">Điểm tích lũy</div>
            <div style="font-family:var(--font-display);font-size:34px;font-weight:700;color:#ff8a4c">${formatNumber(me.totalPoints ?? 0)}</div>
            <div class="progress mt-2" style="background:rgba(255,255,255,.12)"><span style="width:${t.pct}%"></span></div>
            <div class="row-between mt-2" style="font-size:12px;color:#c9ced6">
                <span>${escapeHtml(me.fullName)}</span>
                <span>${t.next ? `Còn ${formatNumber(t.need)} điểm lên ${escapeHtml(t.next.tierName)}` : "Bạn đang ở hạng cao nhất"}</span></div>
        </div></div>`;
}

/** Bậc thang các hạng thành viên. @param {number} points @param {any[]} tiers */
export function tierLadderHtml(points, tiers) {
    const t = tierInfo(points, tiers);
    return `<div class="grid-4">${t.sorted.map(x => `
        <div class="tier-card ${x.id === t.current?.id ? "current" : ""}">
            <div class="row-between"><span class="tile-ico">${ICONS.shield}</span>${x.id === t.current?.id ? `<span class="badge badge-solid">Hạng của bạn</span>`
                : x.minPoints <= points ? `<span class="badge badge-success">Đã đạt</span>` : ""}</div>
            <h3>${escapeHtml(x.tierName)}</h3>
            <div class="text-sm text-muted">Từ <b class="mono" style="color:var(--text)">${formatNumber(x.minPoints)}</b> điểm</div>
        </div>`).join("")}</div>
        <p class="hint mt-3">Mỗi 10.000đ trong đơn hàng hoàn tất = 1 điểm. Hạng được tự động nâng khi đủ điểm. Trả hàng sẽ bị trừ lại số điểm tương ứng.</p>`;
}

/* ---------------- luật đơn hàng (giống backend) ---------------- */
/** Hủy được khi đơn còn Chờ xác nhận / Đã xác nhận. @param {any} o */
export const canCancel = o => ["Pending", "Confirmed"].includes(o.status);
/** Số ngày còn lại để trả hàng (30 ngày kể từ ngày đặt). @param {any} o */
export const returnDaysLeft = o => 30 - Math.floor((Date.now() - (toDate(o.orderDate)?.getTime() ?? 0)) / 86400000);
/** Còn trả hàng được không (đơn Hoàn tất, trong 30 ngày). @param {any} o */
export const canReturn = o => o.status === "Completed" && returnDaysLeft(o) > 0;
/** Đơn đã mua xong (được phép đánh giá sản phẩm). @param {any} o */
export const isPurchased = o => ["Completed", "Returned"].includes(o.status);
