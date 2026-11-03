/*
 * checkout.js - THANH TOÁN / ĐẶT HÀNG (shop/checkout.html)
 *
 * API: POST /api/Order/checkout { customerId, addressId, promotionCode, storeId, paymentMethod }
 *  - Đặt TOÀN BỘ giỏ hàng thành 1 đơn, đơn mới ở trạng thái "Chờ xác nhận".
 *  - storeId: bỏ trống = hệ thống tự chọn kho còn đủ hàng; chọn cửa hàng = nhận tại cửa hàng đó.
 *    (Backend vẫn bắt buộc có địa chỉ của khách, kể cả khi nhận tại cửa hàng.)
 *  - Thanh toán: COD, Momo, VNPay, BankTransfer (Cash chỉ dùng khi mua trực tiếp tại quầy).
 */
import { initShop, toast, errMsg, openModal, refreshCounts, productImages, thumb, money, ICONS, $, ts } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatNumber, estimatePoints } from "../../../assets/js/core/utils.js";

const PROMO_KEY = "aura-promo";
const METHODS = [
    { value: "COD", title: "Thanh toán khi nhận hàng (COD)", desc: "Trả tiền mặt cho shipper khi nhận hàng." },
    { value: "VNPay", title: "VNPay", desc: "Quét mã QR bằng ứng dụng ngân hàng." },
    { value: "Momo", title: "Ví MoMo", desc: "Thanh toán qua ví điện tử MoMo." },
    { value: "BankTransfer", title: "Chuyển khoản ngân hàng", desc: "Chuyển khoản theo thông tin trên đơn." }
];

if (await initShop({ active: "cart", needLogin: true })) {
    const [cart, stores, promotions] = await Promise.all([shopApi.cart.get(), shopApi.getStores(), shopApi.getPromotions()]);
    if (!cart.items.length) location.replace("/shop/cart.html");
    let addresses = await shopApi.addresses.getMine();
    const images = await productImages(cart.items.map((/** @type {any} */ i) => i.productId));
    const state = {
        mode: "delivery", addressId: addresses[0]?.id ?? null, storeId: /** @type {number | null} */ (null),
        method: "COD", code: (() => { try { return sessionStorage.getItem(PROMO_KEY) ?? ""; } catch { return ""; } })()
    };
    const physicalStores = stores.filter((/** @type {any} */ s) => !/Kho Online/i.test(s.name ?? ""));

    const promoOf = (/** @type {string} */ code) => {
        const p = promotions.find((/** @type {any} */ x) => x.code.toUpperCase() === code.trim().toUpperCase());
        const now = Date.now();
        return p && ts(p.startDate) <= now && ts(p.endDate) >= now ? p : null;
    };

    function renderAddresses() {
        $("addressList").innerHTML = addresses.length ? addresses.map((/** @type {any} */ a, /** @type {number} */ i) => `
            <label class="option-card"><input type="radio" name="address" value="${a.id}" ${a.id === state.addressId ? "checked" : ""}>
                <span class="grow"><span class="row wrap"><span class="title">${escapeHtml(a.street)}</span>${i === 0 ? `<span class="badge badge-solid">Mặc định</span>` : ""}</span>
                <span class="desc">${escapeHtml(a.district)}, ${escapeHtml(a.city)}, ${escapeHtml(a.country)}</span></span></label>`).join("")
            : `<div class="inset text-sm text-muted">Bạn chưa có địa chỉ nào. Thêm địa chỉ để đặt hàng.</div>`;
    }

    function renderSummary() {
        const promo = state.code ? promoOf(state.code) : null;
        const discount = promo ? Math.min(promo.discountValue, cart.totalAmount) : 0;
        const total = cart.totalAmount - discount;
        $("summary").innerHTML = `
            <div class="row-between"><h2 style="font-size:18px;text-transform:uppercase">${ICONS.bag} Đơn hàng</h2><span class="badge badge-primary">${cart.totalItems} sản phẩm</span></div>
            <div class="stack mt-3">${cart.items.map((/** @type {any} */ i) => `
                <div class="line-item">${thumb(i.productImageUrl || images.get(i.productId))}
                    <div class="grow"><div class="fw-bold text-sm">${escapeHtml(i.productName)}</div>
                        <div class="text-xs text-muted">Size: ${escapeHtml(i.size)} · Màu: ${escapeHtml(i.color)} · x${i.quantity}</div></div>
                    <b class="text-sm">${money(i.price * i.quantity)}</b></div>`).join("")}</div>
            <form class="input-group mt-4" id="promoForm"><input class="input mono" id="promoInput" placeholder="Mã khuyến mãi" value="${escapeHtml(state.code)}" aria-label="Mã khuyến mãi">
                <button class="btn">${promo ? "Đổi mã" : "Áp dụng"}</button></form>
            ${state.code && !promo ? `<div class="error-text mt-1">Mã "${escapeHtml(state.code)}" không tồn tại hoặc đã hết hạn.</div>` : ""}
            <div class="mt-3">
                <div class="summary-row"><span>Tạm tính</span><b>${money(cart.totalAmount)}</b></div>
                <div class="summary-row"><span>Giảm giá ${promo ? `<span class="badge badge-primary">${escapeHtml(promo.code)}</span>` : ""}</span><b class="text-success">${discount ? "-" : ""}${money(discount)}</b></div>
                <div class="summary-row"><span>Phí vận chuyển</span><b>Miễn phí</b></div>
            </div>
            <div class="summary-total"><span class="upper fw-bold">Tổng thanh toán</span><span class="price">${money(total)}</span></div>
            <div class="row-between text-sm mt-2"><span class="text-muted">Điểm nhận được khi đơn hoàn tất</span><b class="text-primary">+${formatNumber(estimatePoints(total))} điểm</b></div>
            <button class="btn btn-primary btn-lg btn-block mt-4" id="btnPlace">${ICONS.check} Đặt hàng</button>
            <p class="hint mt-2" style="text-align:center">Đơn sẽ ở trạng thái "Chờ xác nhận" cho tới khi cửa hàng xác nhận.</p>`;
    }

    // ---------- khung trang ----------
    $("checkoutRoot").innerHTML = `
        <div class="layout-aside">
            <div class="stack" style="gap:18px">
                <section class="panel">
                    <div class="panel-head"><h2><span class="badge badge-solid">1</span> Hình thức nhận hàng</h2></div>
                    <div class="panel-body stack">
                        <div class="tabs" id="modeTabs" style="display:grid;grid-template-columns:1fr 1fr">
                            <button class="tab active" data-mode="delivery">${ICONS.truck} Giao tận nơi</button>
                            <button class="tab" data-mode="pickup">${ICONS.store} Nhận tại cửa hàng</button>
                        </div>
                        <div id="pickupBox" class="hidden stack">
                            <div class="field"><label for="storeSelect">Chọn cửa hàng nhận hàng</label>
                                <select class="select" id="storeSelect"><option value="">-- Chọn cửa hàng --</option>
                                    ${physicalStores.map((/** @type {any} */ s) => `<option value="${s.id}">${escapeHtml(s.name)} - ${escapeHtml(s.location)}</option>`).join("")}</select>
                                <span class="hint">Cửa hàng phải còn đủ hàng cho cả đơn, nếu không hệ thống sẽ báo khi đặt.</span></div>
                        </div>
                        <div class="row-between"><span class="label" id="addrLabel">${ICONS.pin} Địa chỉ giao hàng</span>
                            <button class="btn btn-ghost btn-sm" id="btnAddAddress">${ICONS.plus} Thêm địa chỉ</button></div>
                        <div class="stack" id="addressList" style="gap:10px"></div>
                    </div>
                </section>
                <section class="panel">
                    <div class="panel-head"><h2><span class="badge badge-solid">2</span> Phương thức thanh toán</h2></div>
                    <div class="panel-body stack" style="gap:10px" id="methodList">
                        ${METHODS.map(m => `<label class="option-card"><input type="radio" name="method" value="${m.value}" ${m.value === state.method ? "checked" : ""}>
                            <span class="grow"><span class="title">${m.title}</span><span class="desc" style="display:block">${m.desc}</span></span></label>`).join("")}
                    </div>
                </section>
            </div>
            <aside class="panel panel-pad sticky" id="summary"></aside>
        </div>`;
    renderAddresses(); renderSummary();

    // ---------- sự kiện ----------
    $("modeTabs").addEventListener("click", e => {
        const b = /** @type {HTMLElement | null} */ (/** @type {HTMLElement} */ (e.target).closest("[data-mode]"));
        if (!b) return;
        state.mode = b.dataset.mode ?? "delivery";
        document.querySelectorAll("#modeTabs .tab").forEach(x => x.classList.toggle("active", x === b));
        $("pickupBox").classList.toggle("hidden", state.mode !== "pickup");
        $("addrLabel").innerHTML = state.mode === "pickup" ? `${ICONS.pin} Địa chỉ liên hệ (ghi trên đơn)` : `${ICONS.pin} Địa chỉ giao hàng`;
        if (state.mode === "delivery") state.storeId = null;
    });
    $("storeSelect").addEventListener("change", e => { state.storeId = Number(/** @type {HTMLSelectElement} */ (e.target).value) || null; });
    $("addressList").addEventListener("change", e => { state.addressId = Number(/** @type {HTMLInputElement} */ (e.target).value); });
    $("methodList").addEventListener("change", e => { state.method = /** @type {HTMLInputElement} */ (e.target).value; });
    $("summary").addEventListener("submit", e => {
        e.preventDefault();
        state.code = /** @type {HTMLInputElement} */ ($("promoInput")).value.trim();
        try { if (state.code) sessionStorage.setItem(PROMO_KEY, state.code); else sessionStorage.removeItem(PROMO_KEY); } catch { /* bỏ qua */ }
        renderSummary();
    });
    $("btnAddAddress").addEventListener("click", () => {
        const { el, close } = openModal({
            title: "Thêm địa chỉ",
            body: `<form class="form-grid" id="addrForm">
                <div class="field full"><label>Số nhà, tên đường</label><input class="input" name="street" required maxlength="100"></div>
                <div class="field"><label>Quận / Huyện</label><input class="input" name="district" required maxlength="50"></div>
                <div class="field"><label>Tỉnh / Thành phố</label><input class="input" name="city" required maxlength="50"></div>
                <div class="field full"><label>Quốc gia</label><input class="input" name="country" value="Việt Nam" required maxlength="50"></div></form>`,
            footer: `<button class="btn btn-ghost" data-close>Hủy</button><button class="btn btn-primary" id="addrSave">Lưu địa chỉ</button>`
        });
        el.querySelector("#addrSave")?.addEventListener("click", async () => {
            const form = /** @type {HTMLFormElement} */ (el.querySelector("#addrForm"));
            if (!form.reportValidity()) return;
            try {
                const created = await shopApi.addresses.create(Object.fromEntries(new FormData(form)));
                addresses = await shopApi.addresses.getMine();
                state.addressId = created?.id ?? addresses[addresses.length - 1]?.id;
                renderAddresses(); close(); toast("Đã thêm địa chỉ.", "success");
            } catch (err) { toast(errMsg(err), "error"); }
        });
    });
    $("summary").addEventListener("click", async e => {
        if (!/** @type {HTMLElement} */ (e.target).closest("#btnPlace")) return;
        if (!state.addressId) { toast("Vui lòng chọn hoặc thêm địa chỉ.", "warning"); return; }
        if (state.mode === "pickup" && !state.storeId) { toast("Vui lòng chọn cửa hàng nhận hàng.", "warning"); return; }
        const btn = /** @type {HTMLButtonElement} */ ($("btnPlace"));
        btn.disabled = true; btn.textContent = "Đang đặt hàng...";
        try {
            const order = await shopApi.orders.checkout({ addressId: state.addressId, promotionCode: state.code || undefined,
                storeId: state.mode === "pickup" ? state.storeId : null, paymentMethod: state.method });
            try { sessionStorage.removeItem(PROMO_KEY); } catch { /* bỏ qua */ }
            await refreshCounts();
            location.href = `/shop/order-success.html?id=${order.id}`;
        } catch (err) {
            toast(errMsg(err), "error");
            btn.disabled = false; btn.innerHTML = `${ICONS.check} Đặt hàng`;
        }
    });
}
