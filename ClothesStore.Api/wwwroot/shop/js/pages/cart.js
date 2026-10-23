/*
 * cart.js - GIỎ HÀNG (shop/cart.html)
 *
 * API: GET /api/Cart/{customerId}, PUT /api/Cart/items, DELETE /api/Cart/items/{id}
 * Mã khuyến mãi: kiểm tra trước ở đây (còn hạn hay không), backend kiểm tra lại khi đặt hàng.
 * Giảm giá = min(giá trị mã, tạm tính) - giống OrderPricing ở backend.
 */
import { initShop, toast, errMsg, refreshCounts, productImages, thumb, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatNumber, formatDate, estimatePoints } from "../../../assets/js/core/utils.js";

export const PROMO_KEY = "aura-promo";

if (await initShop({ active: "cart", needLogin: true })) {
    /** Mã đang áp dụng (lưu tạm để trang thanh toán dùng tiếp) */
    let promo = /** @type {any} */ (null);
    const promotions = await shopApi.getPromotions();
    const now = Date.now();
    const active = promotions.filter((/** @type {any} */ p) => new Date(p.startDate).getTime() <= now && new Date(p.endDate).getTime() >= now)
        .sort((/** @type {any} */ a, /** @type {any} */ b) => b.discountValue - a.discountValue);

    try {
        const saved = sessionStorage.getItem(PROMO_KEY);
        promo = saved ? active.find((/** @type {any} */ p) => p.code === saved) ?? null : null;
    } catch { /* bỏ qua */ }

    async function render() {
        const cart = await shopApi.cart.get();
        const images = await productImages(cart.items.map((/** @type {any} */ i) => i.productId));
        $("crumbCount").textContent = `${cart.totalItems} sản phẩm`;

        if (!cart.items.length) {
            $("cartRoot").innerHTML = `<div class="panel empty">${ICONS.bag}<h3>Giỏ hàng đang trống</h3><p>Chọn vài món ưng ý rồi quay lại nhé.</p>
                <a class="btn btn-primary mt-3" href="/shop/products.html">Mua sắm ngay</a></div>`;
            return;
        }
        const discount = promo ? Math.min(promo.discountValue, cart.totalAmount) : 0;
        const total = cart.totalAmount - discount;

        $("cartRoot").innerHTML = `
            <div class="layout-aside">
                <div class="stack">
                    <div class="panel">
                        <div class="panel-head"><h2>${ICONS.bag} Sản phẩm trong giỏ</h2><span class="badge badge-primary">${cart.totalItems} món</span></div>
                        <div>${cart.items.map((/** @type {any} */ i) => `
                            <div class="line-item panel-body" style="border-bottom:1px solid var(--border);align-items:flex-start">
                                <a href="/shop/product.html?id=${i.productId}">${thumb(i.productImageUrl || images.get(i.productId))}</a>
                                <div class="grow">
                                    <a class="fw-bold" href="/shop/product.html?id=${i.productId}">${escapeHtml(i.productName)}</a>
                                    <div class="meta"><span class="chip">Màu: ${escapeHtml(i.color)}</span><span class="chip">Size: ${escapeHtml(i.size)}</span>
                                        <span class="chip">${money(i.price)} / cái</span></div>
                                    <div class="row-between wrap mt-3">
                                        <div class="row wrap">
                                            <div class="qty"><button data-qty="${i.cartItemId}" data-n="${i.quantity - 1}" aria-label="Giảm">−</button>
                                                <input value="${i.quantity}" readonly aria-label="Số lượng"><button data-qty="${i.cartItemId}" data-n="${i.quantity + 1}" aria-label="Tăng">+</button></div>
                                            <button class="btn btn-ghost btn-sm" data-later="${i.cartItemId}" data-product="${i.productId}">${ICONS.heart} Để dành mua sau</button>
                                            <button class="btn btn-ghost btn-sm" data-remove="${i.cartItemId}">${ICONS.trash} Xóa</button>
                                        </div>
                                        <span class="price" style="font-size:20px">${money(i.price * i.quantity)}</span>
                                    </div>
                                </div>
                            </div>`).join("")}</div>
                    </div>

                    <div class="panel">
                        <div class="panel-head"><h2>${ICONS.ticket} Mã khuyến mãi</h2><span class="text-xs text-muted">${active.length} mã đang chạy</span></div>
                        <div class="panel-body stack">
                            <form class="input-group" id="promoForm">
                                <input class="input mono" id="promoInput" placeholder="Nhập mã, ví dụ ${escapeHtml(active[0]?.code ?? "SALE30K")}" value="${escapeHtml(promo?.code ?? "")}" aria-label="Mã khuyến mãi">
                                <button class="btn btn-outline">Áp dụng</button>
                            </form>
                            <div class="grid-3" style="gap:10px">${active.slice(0, 3).map((/** @type {any} */ p) => `
                                <button class="inset" data-pick="${escapeHtml(p.code)}" style="text-align:left;cursor:pointer;${promo?.code === p.code ? "border-color:var(--primary)" : ""}">
                                    <div class="row-between"><b class="mono text-primary">${escapeHtml(p.code)}</b>${promo?.code === p.code ? `<span class="badge badge-primary">Đang dùng</span>` : ""}</div>
                                    <div class="text-sm mt-1">Giảm ${money(p.discountValue)}</div>
                                    <div class="text-xs text-faint mt-1">HSD: ${formatDate(p.endDate)}</div>
                                </button>`).join("")}</div>
                        </div>
                    </div>
                </div>

                <aside class="panel panel-pad sticky">
                    <h2 style="font-size:18px;text-transform:uppercase">Tóm tắt đơn hàng</h2>
                    <div class="mt-3">
                        <div class="summary-row"><span>Tạm tính (${cart.totalItems} sản phẩm)</span><b>${money(cart.totalAmount)}</b></div>
                        <div class="summary-row"><span>Giảm giá ${promo ? `<span class="badge badge-primary">${escapeHtml(promo.code)}</span>` : ""}</span><b class="text-success">${discount ? "-" : ""}${money(discount)}</b></div>
                        <div class="summary-row"><span>Phí vận chuyển</span><b>Miễn phí</b></div>
                    </div>
                    <div class="summary-total"><span class="upper fw-bold">Tổng</span><span class="price">${money(total)}</span></div>
                    <div class="inset row text-sm mt-3">${ICONS.coin}<span>Đơn hoàn tất được cộng <b class="text-primary">+${formatNumber(estimatePoints(total))} điểm</b></span></div>
                    <a class="btn btn-primary btn-lg btn-block mt-3" href="/shop/checkout.html">Tiến hành đặt hàng ${ICONS.arrow}</a>
                    <div class="stack mt-4 text-sm text-muted">
                        <div class="perk"><span class="tile-ico">${ICONS.ret}</span><span>Trả hàng trong 30 ngày kể từ ngày đặt</span></div>
                        <div class="perk"><span class="tile-ico">${ICONS.store}</span><span>Có thể nhận hàng tại cửa hàng khi đặt</span></div>
                    </div>
                </aside>
            </div>`;
    }

    /** @param {string} code */
    function applyCode(code) {
        const found = active.find((/** @type {any} */ p) => p.code.toUpperCase() === code.trim().toUpperCase());
        if (!found) { toast(promotions.some((/** @type {any} */ p) => p.code.toUpperCase() === code.trim().toUpperCase())
            ? "Mã khuyến mãi đã hết hạn hoặc chưa đến thời gian áp dụng." : "Mã khuyến mãi không tồn tại.", "error"); return; }
        promo = found;
        try { sessionStorage.setItem(PROMO_KEY, found.code); } catch { /* bỏ qua */ }
        toast(`Đã áp dụng mã ${found.code}.`, "success");
        render();
    }

    $("cartRoot").addEventListener("submit", e => {
        e.preventDefault();
        applyCode(/** @type {HTMLInputElement} */ ($("promoInput")).value);
    });
    $("cartRoot").addEventListener("click", async e => {
        const t = /** @type {HTMLElement} */ (e.target);
        const q = /** @type {HTMLElement | null} */ (t.closest("[data-qty]"));
        const rm = /** @type {HTMLElement | null} */ (t.closest("[data-remove]"));
        const later = /** @type {HTMLElement | null} */ (t.closest("[data-later]"));
        const pick = /** @type {HTMLElement | null} */ (t.closest("[data-pick]"));
        try {
            if (q) {
                const n = Number(q.dataset.n);
                if (n < 1) await shopApi.cart.remove(Number(q.dataset.qty)); else await shopApi.cart.update(Number(q.dataset.qty), n);
            } else if (rm) {
                await shopApi.cart.remove(Number(rm.dataset.remove)); toast("Đã xóa khỏi giỏ hàng.");
            } else if (later) {
                const favs = new Set((await shopApi.wishlist.getMine()).map((/** @type {any} */ w) => w.productId));
                if (!favs.has(Number(later.dataset.product))) await shopApi.wishlist.toggle(Number(later.dataset.product));
                await shopApi.cart.remove(Number(later.dataset.later));
                toast("Đã chuyển sang danh sách yêu thích.", "success");
            } else if (pick) {
                applyCode(/** @type {string} */ (pick.dataset.pick)); return;
            } else return;
            await render(); refreshCounts();
        } catch (err) { toast(errMsg(err), "error"); }
    });
    await render();
}
