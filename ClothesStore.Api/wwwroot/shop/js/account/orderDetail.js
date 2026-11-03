/*
 * orderDetail.js - CHI TIẾT ĐƠN HÀNG (shop/account/order-detail.html?id=...)
 * API: GET /api/Order/{id}, GET /api/Shipment/order/{orderId}, GET /api/ReturnRequest/customer/{id},
 *      PATCH /api/Order/{id}/cancel
 */
import { initAccount, canCancel, canReturn, returnDaysLeft } from "./accountShell.js";
import { toast, errMsg, confirmBox, productImages, thumb, qs, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatDateTime, formatNumber, estimatePoints, orderStatusBadge, paymentStatusBadge,
    shipmentStatusBadge, returnStatusBadge, paymentMethodText } from "../../../assets/js/core/utils.js";

const CHECK = `<svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5"/></svg>`;

/** Các bước tiến trình theo trạng thái đơn. @param {any} o @param {any} ship */
function stepsHtml(o, ship) {
    if (o.status === "Cancelled") return `<div class="steps">
        <div class="step done"><span class="dot">${CHECK}</span><span class="t">Đặt hàng</span><span class="s">${formatDateTime(o.orderDate)}</span></div>
        <div class="step failed"><span class="dot">✕</span><span class="t">Đã hủy</span><span class="s">Hoàn kho, hoàn tiền nếu đã trả</span></div></div>`;
    const order = ["Pending", "Confirmed", "Shipping", "Completed"];
    const idx = o.status === "Returned" ? 4 : order.indexOf(o.status);
    const steps = [
        { t: "Đặt hàng", s: formatDateTime(o.orderDate) },
        { t: "Xác nhận", s: "Cửa hàng xác nhận" },
        { t: "Đang giao", s: ship?.shippedDate ? formatDateTime(ship.shippedDate) : "Bàn giao vận chuyển" },
        { t: "Hoàn tất", s: ship?.deliveredDate ? formatDateTime(ship.deliveredDate) : "Giao thành công" }
    ];
    if (o.status === "Returned") steps.push({ t: "Đã trả hàng", s: "Đã hoàn tiền" });
    return `<div class="steps">${steps.map((s, i) => {
        const cls = i < idx || (i === idx && i >= 3) ? "done" : i === idx ? "current" : "";
        return `<div class="step ${cls}"><span class="dot">${cls === "done" ? CHECK : i + 1}</span><span class="t">${s.t}</span><span class="s">${s.s}</span></div>`;
    }).join("")}</div>`;
}

if (await initAccount("orders")) {
    const id = Number(qs("id"));
    async function render() {
        let o;
        try { o = await shopApi.orders.getById(id); }
        catch (err) { $("detailRoot").innerHTML = `<div class="panel empty">${ICONS.box}<h3>Không tìm thấy đơn hàng</h3><p>${escapeHtml(errMsg(err))}</p></div>`; return; }
        const [ship, returns, images] = await Promise.all([shopApi.getShipment(id).catch(() => null), shopApi.returns.getMine(),
            productImages(o.orderItems.map((/** @type {any} */ i) => i.productId))]);
        const myReturns = returns.filter((/** @type {any} */ r) => r.orderId === id);
        const pickup = o.storeName && !/Kho Online/i.test(o.storeName);
        $("crumbCurrent").textContent = `Đơn #${o.id}`;

        $("detailRoot").innerHTML = `
            <div class="row-between wrap">
                <div><div class="eyebrow">Chi tiết đơn hàng</div><h1 class="page-title mt-1" style="font-size:clamp(24px,3.4vw,34px)">Đơn <span class="accent">#${o.id}</span></h1>
                    <div class="row wrap mt-2">${orderStatusBadge(o.status)}${paymentStatusBadge(o.paymentStatus)}<span class="text-sm text-muted">${formatDateTime(o.orderDate)}</span></div></div>
                <div class="row wrap">
                    ${canCancel(o) ? `<button class="btn btn-danger" id="btnCancel">Hủy đơn hàng</button>` : ""}
                    ${canReturn(o) ? `<a class="btn btn-outline" href="/shop/account/return-new.html?orderId=${o.id}">${ICONS.ret} Yêu cầu trả hàng (còn ${returnDaysLeft(o)} ngày)</a>` : ""}
                </div>
            </div>
            <section class="panel panel-pad mt-4">${stepsHtml(o, ship)}</section>
            <div class="layout-aside mt-4">
                <div class="stack">
                    <section class="panel">
                        <div class="panel-head"><h2>${ICONS.box} Sản phẩm</h2><span class="badge">${o.orderItems.length} dòng</span></div>
                        <div class="panel-body stack">${o.orderItems.map((/** @type {any} */ i) => `
                            <div class="line-item">${thumb(images.get(i.productId))}<div class="grow"><a class="fw-bold" href="/shop/product.html?id=${i.productId}">${escapeHtml(i.variantName)}</a>
                                <div class="text-sm text-muted">Size: ${escapeHtml(i.size)} · Màu: ${escapeHtml(i.color)} · ${money(i.price)} x ${i.quantity}</div></div>
                                <b>${money(i.price * i.quantity)}</b></div>`).join("")}</div>
                    </section>
                    <section class="panel">
                        <div class="panel-head"><h2>${pickup ? ICONS.store : ICONS.truck} ${pickup ? "Nhận tại cửa hàng" : "Vận chuyển"}</h2>${ship ? shipmentStatusBadge(ship.status) : ""}</div>
                        <div class="panel-body grid-2">
                            <div class="inset"><div class="label">${pickup ? "Cửa hàng nhận" : "Xuất kho từ"}</div><div class="fw-bold mt-1">${escapeHtml(o.storeName ?? "—")}</div></div>
                            <div class="inset"><div class="label">Địa chỉ</div><div class="mt-1">${escapeHtml(o.address ?? "—")}</div></div>
                            ${ship ? `<div class="inset"><div class="label">Đơn vị vận chuyển</div><div class="fw-bold mt-1">${escapeHtml(ship.carrier ?? "—")}</div></div>
                                <div class="inset"><div class="label">Mã vận đơn</div><div class="mono fw-bold mt-1">${escapeHtml(ship.trackingNumber ?? "—")}</div></div>`
                            : `<div class="inset" style="grid-column:1/-1"><span class="text-sm text-muted">Chưa có thông tin vận chuyển. Mã vận đơn sẽ hiện khi cửa hàng giao cho đơn vị vận chuyển.</span></div>`}
                        </div>
                    </section>
                    ${myReturns.length ? `<section class="panel">
                        <div class="panel-head"><h2>${ICONS.ret} Yêu cầu trả hàng của đơn này</h2><a class="btn btn-ghost btn-sm" href="/shop/account/returns.html">Xem tất cả</a></div>
                        <div class="panel-body stack">${myReturns.map((/** @type {any} */ r) => `
                            <div class="inset row-between wrap"><div><b class="mono">Yêu cầu #${r.id}</b> ${returnStatusBadge(r.status)}
                                <div class="text-sm text-muted mt-1">${formatDateTime(r.requestDate)} · ${r.items.length} sản phẩm · ${escapeHtml(r.reason)}</div></div>
                                <b class="text-success">Hoàn ${money(r.totalRefund)}</b></div>`).join("")}</div></section>` : ""}
                </div>
                <aside class="panel panel-pad sticky">
                    <h2 style="font-size:18px;text-transform:uppercase">Thanh toán</h2>
                    <div class="mt-3">
                        <div class="summary-row"><span>Tạm tính</span><b>${money(o.subtotal)}</b></div>
                        <div class="summary-row"><span>Giảm giá ${o.promotionCode ? `<span class="badge badge-primary">${escapeHtml(o.promotionCode)}</span>` : ""}</span><b class="text-success">-${money(o.discountAmount)}</b></div>
                        <div class="summary-row"><span>Phí vận chuyển</span><b>Miễn phí</b></div>
                        <div class="summary-row"><span>Phương thức</span><b>${escapeHtml(paymentMethodText(o.paymentMethod))}</b></div>
                    </div>
                    <div class="summary-total"><span class="upper fw-bold">Tổng</span><span class="price">${money(o.totalAmount)}</span></div>
                    <div class="inset row text-sm mt-3">${ICONS.coin}<span>${o.status === "Completed" ? "Đã cộng" : o.status === "Cancelled" ? "Không tích" : "Sẽ cộng"}
                        <b class="text-primary">${formatNumber(estimatePoints(o.totalAmount))} điểm</b>${o.status === "Completed" || o.status === "Cancelled" ? "" : " khi đơn hoàn tất"}</span></div>
                    <a class="btn btn-block mt-4" href="/shop/account/orders.html">${ICONS.list} Danh sách đơn hàng</a>
                </aside>
            </div>`;

        document.getElementById("btnCancel")?.addEventListener("click", async () => {
            if (!(await confirmBox(`Hủy đơn hàng #${o.id}?`, "Hủy đơn"))) return;
            try { await shopApi.orders.cancel(o.id); toast("Đã hủy đơn hàng.", "success"); render(); }
            catch (err) { toast(errMsg(err), "error"); }
        });
    }
    await render();
}
