/*
 * orders.js - ĐƠN HÀNG CỦA TÔI (shop/account/orders.html)
 * API: GET /api/Order/customer/{id}, PATCH /api/Order/{id}/cancel, POST /api/Cart/items (mua lại)
 * Nút theo luật backend: Hủy (Chờ xác nhận / Đã xác nhận), Trả hàng (Hoàn tất, trong 30 ngày), Đánh giá (đã mua).
 */
import { initAccount, canCancel, canReturn, returnDaysLeft, isPurchased } from "./accountShell.js";
import { toast, errMsg, confirmBox, refreshCounts, productImages, thumb, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatDateTime, orderStatusBadge, paymentStatusBadge, paymentMethodText } from "../../../assets/js/core/utils.js";

const TABS = [
    { key: "all", text: "Tất cả" }, { key: "Pending", text: "Chờ xác nhận" }, { key: "Confirmed", text: "Đã xác nhận" },
    { key: "Shipping", text: "Đang giao" }, { key: "Completed", text: "Hoàn tất" }, { key: "Cancelled", text: "Đã hủy" }, { key: "Returned", text: "Đã trả hàng" }
];

if (await initAccount("orders")) {
    let orders = /** @type {any[]} */ ([]);
    let tab = new URLSearchParams(location.search).get("status") ?? "all";
    const search = /** @type {HTMLInputElement} */ ($("orderSearch"));

    async function load() {
        orders = await shopApi.orders.getMine();
        await productImages(orders.flatMap(o => o.orderItems.map((/** @type {any} */ i) => i.productId)));
        render();
    }

    async function render() {
        const images = await productImages([]);
        $("orderTabs").innerHTML = TABS.map(t => {
            const n = t.key === "all" ? orders.length : orders.filter(o => o.status === t.key).length;
            return `<button class="tab ${t.key === tab ? "active" : ""}" data-tab="${t.key}">${t.text} <span class="n">${n}</span></button>`;
        }).join("");
        const kw = search.value.trim().toLowerCase().replace("#", "");
        const list = orders.filter(o => (tab === "all" || o.status === tab)
            && (!kw || String(o.id).includes(kw) || o.orderItems.some((/** @type {any} */ i) => i.variantName.toLowerCase().includes(kw))));

        $("orderList").innerHTML = list.length ? list.map(o => {
            const qty = o.orderItems.reduce((/** @type {number} */ s, /** @type {any} */ i) => s + i.quantity, 0);
            return `<article class="order-card">
                <div class="oc-head"><div class="row wrap"><b class="mono">#${o.id}</b><span class="text-sm text-muted">${formatDateTime(o.orderDate)}</span>
                    ${o.storeName && !/Kho Online/i.test(o.storeName) ? `<span class="chip">${ICONS.store} Nhận tại ${escapeHtml(o.storeName)}</span>` : ""}</div>
                    <div class="row wrap">${orderStatusBadge(o.status)}${paymentStatusBadge(o.paymentStatus)}</div></div>
                <div class="oc-body">${o.orderItems.slice(0, 2).map((/** @type {any} */ i) => `
                    <div class="line-item">${thumb(images.get(i.productId))}<div class="grow"><a class="fw-bold" href="/shop/product.html?id=${i.productId}">${escapeHtml(i.variantName)}</a>
                        <div class="text-sm text-muted">Size: ${escapeHtml(i.size)} · Màu: ${escapeHtml(i.color)} · x${i.quantity}</div></div><b>${money(i.price * i.quantity)}</b></div>`).join("")}
                    ${o.orderItems.length > 2 ? `<div class="text-sm text-muted">+ ${o.orderItems.length - 2} sản phẩm khác</div>` : ""}</div>
                <div class="oc-foot">
                    <div class="text-sm text-muted">${qty} sản phẩm · ${escapeHtml(paymentMethodText(o.paymentMethod))}
                        ${o.status === "Completed" ? ` · ${canReturn(o) ? `còn ${returnDaysLeft(o)} ngày để trả hàng` : "đã hết hạn trả hàng"}` : ""}
                        <div class="mt-1">Tổng: <b class="price" style="font-size:18px">${money(o.totalAmount)}</b></div></div>
                    <div class="row wrap">
                        ${canCancel(o) ? `<button class="btn btn-sm btn-danger" data-cancel="${o.id}">Hủy đơn</button>` : ""}
                        ${canReturn(o) ? `<a class="btn btn-sm btn-outline" href="/shop/account/return-new.html?orderId=${o.id}">${ICONS.ret} Trả hàng</a>` : ""}
                        ${isPurchased(o) ? `<a class="btn btn-sm btn-ghost" href="/shop/account/reviews.html">${ICONS.star} Đánh giá</a>` : ""}
                        ${["Completed", "Cancelled", "Returned"].includes(o.status) ? `<button class="btn btn-sm btn-ghost" data-rebuy="${o.id}">Mua lại</button>` : ""}
                        <a class="btn btn-sm btn-primary" href="/shop/account/order-detail.html?id=${o.id}">Chi tiết</a>
                    </div></div></article>`;
        }).join("") : `<div class="panel empty">${ICONS.box}<h3>Không có đơn hàng nào</h3><p>Thử chọn trạng thái khác hoặc bắt đầu mua sắm.</p>
            <a class="btn btn-primary mt-3" href="/shop/products.html">Mua sắm</a></div>`;
    }

    $("orderTabs").addEventListener("click", e => {
        const b = /** @type {HTMLElement | null} */ (/** @type {HTMLElement} */ (e.target).closest("[data-tab]"));
        if (b) { tab = b.dataset.tab ?? "all"; render(); }
    });
    search.addEventListener("input", () => render());
    $("orderList").addEventListener("click", async e => {
        const t = /** @type {HTMLElement} */ (e.target);
        const c = /** @type {HTMLElement | null} */ (t.closest("[data-cancel]"));
        const r = /** @type {HTMLElement | null} */ (t.closest("[data-rebuy]"));
        try {
            if (c) {
                if (!(await confirmBox(`Hủy đơn hàng #${c.dataset.cancel}? Hàng sẽ được hoàn lại kho.`, "Hủy đơn"))) return;
                await shopApi.orders.cancel(Number(c.dataset.cancel));
                toast("Đã hủy đơn hàng.", "success"); await load();
            }
            if (r) {
                const o = orders.find(x => x.id === Number(r.dataset.rebuy));
                for (const i of o.orderItems) await shopApi.cart.add(i.variantId, i.quantity);
                await refreshCounts();
                location.href = "/shop/cart.html";
            }
        } catch (err) { toast(errMsg(err), "error"); }
    });
    await load();
}
