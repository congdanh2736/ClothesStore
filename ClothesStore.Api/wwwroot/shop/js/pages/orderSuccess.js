/*
 * orderSuccess.js - ĐẶT HÀNG THÀNH CÔNG (shop/order-success.html?id=...)
 * API: GET /api/Order/{id}
 */
import { initShop, productImages, thumb, qs, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatNumber, formatDateTime, estimatePoints, orderStatusBadge, paymentStatusBadge, paymentMethodText } from "../../../assets/js/core/utils.js";

if (await initShop({ needLogin: true })) {
    const o = await shopApi.orders.getById(Number(qs("id")));
    const images = await productImages(o.orderItems.map((/** @type {any} */ i) => i.productId));
    const online = ["Momo", "VNPay", "BankTransfer"].includes(o.paymentMethod);
    const pickup = o.storeName && !/Kho Online/i.test(o.storeName);

    $("successRoot").innerHTML = `
        <section class="page-hero row wrap" style="gap:28px">
            <span class="tile-ico" style="width:84px;height:84px;border-radius:50%;background:var(--primary-soft);color:var(--primary);display:grid;place-items:center;box-shadow:var(--glow)">
                <svg viewBox="0 0 24 24" width="40" height="40" fill="none" stroke="currentColor" stroke-width="2.4"><path d="M20 6 9 17l-5-5"/></svg></span>
            <div class="grow">
                <div class="row wrap">${orderStatusBadge(o.status)}${paymentStatusBadge(o.paymentStatus)}</div>
                <h1 class="page-title mt-2">Đặt hàng thành công <span class="accent">// #${o.id}</span></h1>
                <p class="text-muted mt-2">Cảm ơn ${escapeHtml(o.customerName)}! Đơn hàng đã được ghi nhận và đang chờ cửa hàng xác nhận.
                    ${online ? "Đơn thanh toán online sẽ được xác nhận sau khi giao dịch thanh toán thành công." : ""}</p>
                <div class="row wrap mt-3 text-sm"><span class="chip mono">Mã đơn #${o.id}</span><span class="chip">${ICONS.clock} ${formatDateTime(o.orderDate)}</span></div>
            </div>
        </section>
        <div class="layout-aside mt-4">
            <div class="stack">
                <section class="panel">
                    <div class="panel-head"><h2>${pickup ? ICONS.store : ICONS.truck} ${pickup ? "Nhận tại cửa hàng" : "Giao hàng tận nơi"}</h2></div>
                    <div class="panel-body stack">
                        <div class="steps">
                            <div class="step done"><span class="dot"><svg viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5"/></svg></span><span class="t">Đặt hàng</span><span class="s">Vừa xong</span></div>
                            <div class="step current"><span class="dot">2</span><span class="t">Xác nhận</span><span class="s">Cửa hàng xử lý</span></div>
                            <div class="step"><span class="dot">3</span><span class="t">${pickup ? "Chuẩn bị hàng" : "Đang giao"}</span><span class="s">${pickup ? "Báo khi sẵn sàng" : "Có mã vận đơn"}</span></div>
                            <div class="step"><span class="dot">4</span><span class="t">Hoàn tất</span><span class="s">Cộng điểm</span></div>
                        </div>
                        <div class="grid-2">
                            <div class="inset"><div class="label">${pickup ? "Cửa hàng nhận" : "Xuất kho từ"}</div><div class="fw-bold mt-1">${escapeHtml(o.storeName ?? "Hệ thống tự chọn")}</div></div>
                            <div class="inset"><div class="label">Địa chỉ</div><div class="mt-1">${escapeHtml(o.address)}</div></div>
                        </div>
                    </div>
                </section>
                <section class="panel">
                    <div class="panel-head"><h2>${ICONS.box} Sản phẩm (${o.orderItems.length})</h2></div>
                    <div class="panel-body stack">${o.orderItems.map((/** @type {any} */ i) => `
                        <div class="line-item">${thumb(images.get(i.productId))}<div class="grow"><a class="fw-bold" href="/shop/product.html?id=${i.productId}">${escapeHtml(i.variantName)}</a>
                            <div class="text-sm text-muted">Size: ${escapeHtml(i.size)} · Màu: ${escapeHtml(i.color)} · x${i.quantity}</div></div><b>${money(i.price * i.quantity)}</b></div>`).join("")}</div>
                </section>
            </div>
            <aside class="panel panel-pad sticky">
                <h2 style="font-size:18px;text-transform:uppercase">Thanh toán</h2>
                <div class="mt-3">
                    <div class="summary-row"><span>Tạm tính</span><b>${money(o.subtotal)}</b></div>
                    <div class="summary-row"><span>Giảm giá ${o.promotionCode ? `<span class="badge badge-primary">${escapeHtml(o.promotionCode)}</span>` : ""}</span><b class="text-success">-${money(o.discountAmount)}</b></div>
                    <div class="summary-row"><span>Phương thức</span><b>${escapeHtml(paymentMethodText(o.paymentMethod))}</b></div>
                </div>
                <div class="summary-total"><span class="upper fw-bold">Tổng</span><span class="price">${money(o.totalAmount)}</span></div>
                <div class="inset row text-sm mt-3">${ICONS.coin}<span>Nhận <b class="text-primary">+${formatNumber(estimatePoints(o.totalAmount))} điểm</b> khi đơn hoàn tất</span></div>
                <a class="btn btn-primary btn-block mt-4" href="/shop/account/order-detail.html?id=${o.id}">Theo dõi đơn hàng</a>
                <a class="btn btn-block mt-2" href="/shop/products.html">Tiếp tục mua sắm</a>
            </aside>
        </div>`;
}
