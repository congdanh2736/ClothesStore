/*
 * promotions.js - ƯU ĐÃI / MÃ KHUYẾN MÃI (shop/promotions.html)
 * API: GET /api/Promotion -> [{ id, code, discountValue, startDate, endDate }]
 * Mã giảm thẳng số tiền (tối đa bằng tạm tính), dùng chung cho mọi khách, nhập khi đặt hàng.
 */
import { initShop, toast, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatDate } from "../../../assets/js/core/utils.js";

const PROMO_KEY = "aura-promo";

if (await initShop({ active: "promotions" })) {
    const all = await shopApi.getPromotions();
    const now = Date.now();
    const statusOf = (/** @type {any} */ p) => new Date(p.startDate).getTime() > now ? "upcoming" : new Date(p.endDate).getTime() < now ? "ended" : "active";
    const groups = { active: all.filter((/** @type {any} */ p) => statusOf(p) === "active"), upcoming: all.filter((/** @type {any} */ p) => statusOf(p) === "upcoming"), ended: all.filter((/** @type {any} */ p) => statusOf(p) === "ended") };
    groups.active.sort((/** @type {any} */ a, /** @type {any} */ b) => +new Date(a.endDate) - +new Date(b.endDate));
    groups.upcoming.sort((/** @type {any} */ a, /** @type {any} */ b) => +new Date(a.startDate) - +new Date(b.startDate));
    groups.ended.sort((/** @type {any} */ a, /** @type {any} */ b) => +new Date(b.endDate) - +new Date(a.endDate));
    /** @type {"active"|"upcoming"|"ended"} */
    let tab = "active";

    $("statActive").textContent = String(groups.active.length);
    $("statMax").textContent = money(Math.max(0, ...groups.active.map((/** @type {any} */ p) => p.discountValue)));
    const labels = { active: "Đang diễn ra", upcoming: "Sắp diễn ra", ended: "Đã kết thúc" };

    function render() {
        $("promoTabs").innerHTML = (/** @type {const} */ (["active", "upcoming", "ended"])).map(k =>
            `<button class="tab ${k === tab ? "active" : ""}" data-tab="${k}">${labels[k]} <span class="badge">${groups[k].length}</span></button>`).join("");
        const list = groups[tab];
        $("promoGrid").innerHTML = list.length ? list.map((/** @type {any} */ p) => {
            const days = Math.ceil((new Date(p.endDate).getTime() - now) / 86400000);
            return `<article class="ticket ${tab === "ended" ? "disabled" : ""}">
                <div class="left"><span class="text-xs upper">Giảm</span><b class="price" style="font-size:24px">${money(p.discountValue)}</b></div>
                <div class="right grow stack" style="gap:6px">
                    <div class="row-between"><b class="mono text-primary" style="font-size:17px">${escapeHtml(p.code)}</b>
                        ${tab === "active" && days <= 3 ? `<span class="badge badge-warning">Còn ${days} ngày</span>` : ""}</div>
                    <span class="text-xs text-muted">${ICONS.clock} ${formatDate(p.startDate)} - ${formatDate(p.endDate)}</span>
                    <span class="text-xs text-faint">Áp dụng cho mọi đơn, giảm tối đa bằng giá trị đơn.</span>
                    ${tab === "active" ? `<div class="row wrap"><button class="btn btn-sm btn-outline" data-copy="${escapeHtml(p.code)}">${ICONS.copy} Sao chép</button>
                        <button class="btn btn-sm btn-primary" data-use="${escapeHtml(p.code)}">Dùng ngay</button></div>` : ""}
                </div></article>`;
        }).join("") : `<div class="panel empty" style="grid-column:1/-1">${ICONS.ticket}<h3>Chưa có mã nào</h3><p>Hãy quay lại sau nhé.</p></div>`;
    }

    document.addEventListener("click", async e => {
        const t = /** @type {HTMLElement} */ (e.target);
        const tb = /** @type {HTMLElement | null} */ (t.closest("[data-tab]"));
        const cp = /** @type {HTMLElement | null} */ (t.closest("[data-copy]"));
        const use = /** @type {HTMLElement | null} */ (t.closest("[data-use]"));
        if (tb) { tab = /** @type {any} */ (tb.dataset.tab); render(); }
        if (cp) {
            try { await navigator.clipboard.writeText(cp.dataset.copy ?? ""); toast(`Đã sao chép mã ${cp.dataset.copy}.`, "success"); }
            catch { toast(`Mã của bạn: ${cp.dataset.copy}`); }
        }
        if (use) {
            try { sessionStorage.setItem(PROMO_KEY, use.dataset.use ?? ""); } catch { /* bỏ qua */ }
            location.href = "/shop/cart.html";
        }
    });
    render();
}
