/*
 * ============================================================================
 *  order-detail.js  -  XỬ LÝ CHO TRANG admin/sales/order-detail.html   (Nhóm 4)
 * ============================================================================
 *
 *  Luồng chạy:
 *    1. Đọc id đơn hàng trên thanh địa chỉ (?id=10). Không có id -> báo lỗi.
 *    2. Tải thông tin đơn hàng, giao dịch thanh toán và vận đơn của đơn đó.
 *    3. Vẽ 4 ô số liệu, thông tin đơn + tiền, khối thanh toán, khối vận chuyển
 *       và bảng danh sách sản phẩm trong đơn.
 *    4. Hỗ trợ đầy đủ nghiệp vụ:
 *       - Chuyển trạng thái đơn: Xác nhận (Confirm), Hủy (Cancel), Hoàn tất (Complete), Xóa (Admin).
 *       - Sửa địa chỉ giao hàng & mã khuyến mãi (khi đơn còn Pending).
 *       - Quản lý sản phẩm trong đơn (OrderItem): thêm / sửa / xóa món (khi đơn còn Pending).
 *       - Quản lý thanh toán: xác nhận đã thu tiền (Paid) / báo lỗi (Failed), đổi phương thức, tạo mới.
 *       - Quản lý vận chuyển: tạo vận đơn (khi đơn Confirmed), sửa mã vận đơn, cập nhật Đã giao / Thất bại / Giao lại.
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/sales/ -> "../../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout, setPageTitle } from "../layout.js";
import { orderApi, orderItemApi } from "../../../assets/js/api/orderApi.js";
import { paymentApi } from "../../../assets/js/api/paymentApi.js";
import { shipmentApi } from "../../../assets/js/api/shipmentApi.js";
import { promotionApi } from "../../../assets/js/api/promotionApi.js";
import { addressApi } from "../../../assets/js/api/customerApi.js";
import { isAdmin } from "../../../assets/js/core/auth.js";
import {
    ORDER_STATUS,
    PAYMENT_STATUS,
    SHIPMENT_STATUS,
    PAYMENT_METHOD_LABELS
} from "../../../assets/js/core/config.js";
import {
    escapeHtml,
    formatMoney,
    formatNumber,
    formatDate,
    formatDateTime,
    getIdFromUrl,
    orderStatusBadge,
    paymentStatusBadge,
    shipmentStatusBadge,
    paymentMethodText,
    estimatePoints
} from "../../../assets/js/core/utils.js";
import { renderTable } from "../../../assets/js/components/table.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

/** Lấy một thẻ theo id. @param {string} id */
const $ = id => /** @type {HTMLElement} */ (document.getElementById(id));

const pageError = $("pageError");
const pageContent = $("pageContent");

/* ---------------------------------------------------------------------------
 * DỮ LIỆU CỦA TRANG
 * ------------------------------------------------------------------------- */
const orderId = getIdFromUrl() ?? 0;

/** @type {any} */ let order = null;
/** @type {any} */ let payment = null;
/** @type {any} */ let shipment = null;
/** @type {any[]|null} */ let variantsCache = null;

/** Phương thức thanh toán cho ô <select>. */
const paymentMethodOptions = () =>
    Object.entries(PAYMENT_METHOD_LABELS).map(([value, label]) => ({ value, label }));

/** Đơn trả tiền khi nhận hàng (COD / Tiền mặt) hay thanh toán online? @param {string} [method] */
function isPayOnReceive(method) {
    const m = String(method ?? "").toUpperCase();
    return m === "COD" || m === "CASH";
}

/* ===========================================================================
 *  PHẦN 1: TẢI DỮ LIỆU & VẼ TỔNG QUAN ĐƠN HÀNG
 * ========================================================================= */

/** Tải lại toàn bộ dữ liệu của đơn (đơn hàng + giao dịch thanh toán + vận đơn) rồi vẽ giao diện. */
async function loadOrderAll() {
    const [orderData, paymentData, shipmentData] = await Promise.all([
        orderApi.getById(orderId),
        paymentApi.getByOrder(orderId).catch(() => null),
        shipmentApi.getByOrder(orderId).catch(() => null)
    ]);

    order = orderData;
    payment = paymentData;
    shipment = shipmentData;

    drawHeaderAndButtons();
    drawStats();
    drawOrderAndPricingInfo();
    drawPaymentSection();
    drawShipmentSection();
    drawOrderItems();
}

/** Điền tiêu đề đầu trang và ẩn/hiện các nút thao tác theo đúng trạng thái đơn. */
function drawHeaderAndButtons() {
    $("orderTitle").textContent = `Đơn hàng #${order.id}`;
    const parts = [
        `Đặt lúc ${formatDateTime(order.orderDate)}`,
        order.customerName ? `Khách: ${order.customerName}` : `Mã KH: #${order.customerId}`,
        order.storeName ? `Xuất kho: ${order.storeName}` : null
    ].filter(Boolean);
    $("orderSubtitle").textContent = parts.join(" · ");
    setPageTitle(`Đơn hàng #${order.id}`);

    const isPending = order.status === ORDER_STATUS.PENDING;
    const isConfirmed = order.status === ORDER_STATUS.CONFIRMED;
    const isShipping = order.status === ORDER_STATUS.SHIPPING;
    const isCancelled = order.status === ORDER_STATUS.CANCELLED;

    const currentShipmentStatus = shipment?.status ?? order.shipmentStatus;
    const canCancel = isPending
        || isConfirmed
        || (isShipping && currentShipmentStatus === SHIPMENT_STATUS.FAILED);

    // Hoàn tất trực tiếp trên đơn: dùng khi khách nhận tại cửa hàng (chưa tạo vận đơn)
    // hoặc khi vận đơn đã ở trạng thái Delivered
    const canCompleteDirect = (isConfirmed && !shipment)
        || (isShipping && currentShipmentStatus === SHIPMENT_STATUS.DELIVERED);

    $("btnEditOrder").classList.toggle("hidden", !isPending);
    $("btnEditOrderCard").classList.toggle("hidden", !isPending);
    $("btnAddItem").classList.toggle("hidden", !isPending);

    $("btnConfirmOrder").classList.toggle("hidden", !isPending);
    $("btnCreateShipment").classList.toggle("hidden", !(isConfirmed && !shipment));
    $("btnCompleteOrder").classList.toggle("hidden", !canCompleteDirect);
    $("btnCancelOrder").classList.toggle("hidden", !canCancel);
    $("btnDeleteOrder").classList.toggle("hidden", !(isAdmin() && isCancelled));
}

/** Vẽ 4 ô số liệu đầu trang. */
function drawStats() {
    $("statOrderStatus").innerHTML = orderStatusBadge(order.status);

    const payStatus = payment?.status ?? order.paymentStatus;
    const payMethod = payment?.paymentMethod ?? order.paymentMethod;
    $("statPaymentStatus").innerHTML = payStatus
        ? paymentStatusBadge(payStatus)
        : `<span class="badge badge-secondary">Chưa có GD</span>`;
    $("statPaymentMethod").textContent = payMethod ? paymentMethodText(payMethod) : "Chưa chọn phương thức";

    const shipStatus = shipment?.status ?? order.shipmentStatus;
    const tracking = shipment?.trackingNumber ?? order.trackingNumber;
    $("statShipmentStatus").innerHTML = shipStatus
        ? shipmentStatusBadge(shipStatus)
        : `<span class="badge badge-secondary">Chưa gửi hàng</span>`;
    $("statTracking").textContent = shipment
        ? `${shipment.carrier}${tracking ? ` · ${tracking}` : ""}`
        : "Chưa tạo vận đơn";

    $("statTotalAmount").textContent = formatMoney(order.totalAmount);
    const points = estimatePoints(order.totalAmount);
    $("statPoints").textContent = points > 0
        ? `Tích lũy +${formatNumber(points)} điểm khi hoàn tất`
        : "Đơn dưới 10.000đ (0 điểm)";
}

/** Vẽ 2 thẻ: Thông tin đơn hàng & Chi tiết thanh toán. */
function drawOrderAndPricingInfo() {
    const customerLink = order.customerId
        ? `<a class="fw-bold" href="../customers/customer-detail.html?id=${order.customerId}">${escapeHtml(order.customerName || `Khách hàng #${order.customerId}`)}</a> <span class="text-muted text-sm">(#${order.customerId})</span>`
        : escapeHtml(order.customerName || "—");

    const promoHtml = order.promotionCode
        ? `<span class="badge badge-primary">${escapeHtml(order.promotionCode)}</span> <span class="text-muted text-sm">(-${formatMoney(order.discountAmount)})</span>`
        : `<span class="text-muted">Không áp dụng</span>`;

    $("orderInfoList").innerHTML = `
        <dt>Mã đơn hàng</dt>
        <dd>#${order.id}</dd>
        <dt>Ngày đặt hàng</dt>
        <dd>${formatDateTime(order.orderDate)}</dd>
        <dt>Khách hàng</dt>
        <dd>${customerLink}</dd>
        <dt>Địa chỉ giao hàng</dt>
        <dd>${escapeHtml(order.address || "—")}</dd>
        <dt>Cửa hàng xuất kho</dt>
        <dd>${order.storeName ? escapeHtml(order.storeName) : `<span class="text-muted">—</span>`}</dd>
        <dt>Mã khuyến mãi</dt>
        <dd>${promoHtml}</dd>
    `;

    const totalItems = (order.orderItems ?? []).reduce((sum, i) => sum + (Number(i.quantity) || 0), 0);
    const points = estimatePoints(order.totalAmount);

    $("pricingInfoList").innerHTML = `
        <dt>Số lượng sản phẩm</dt>
        <dd>${formatNumber((order.orderItems ?? []).length)} dòng (${formatNumber(totalItems)} sản phẩm)</dd>
        <dt>Tạm tính (Subtotal)</dt>
        <dd>${formatMoney(order.subtotal)}</dd>
        <dt>Giảm giá khuyến mãi</dt>
        <dd class="${order.discountAmount > 0 ? "text-danger" : "text-muted"}">${order.discountAmount > 0 ? `-${formatMoney(order.discountAmount)}` : "0 ₫"}</dd>
        <dt>Tổng phải thanh toán</dt>
        <dd class="fw-bold text-lg">${formatMoney(order.totalAmount)}</dd>
        <dt>Điểm thưởng tích lũy</dt>
        <dd class="text-success">+${formatNumber(points)} điểm <span class="text-muted text-sm">(cộng khi đơn Hoàn tất)</span></dd>
    `;
}

/* ===========================================================================
 *  PHẦN 2: CHUYỂN TRẠNG THÁI ĐƠN HÀNG (XÁC NHẬN / HỦY / HOÀN TẤT / XÓA)
 * ========================================================================= */

/** Xác nhận đơn hàng: Pending -> Confirmed. */
async function handleConfirmOrder() {
    const payMethod = payment?.paymentMethod ?? order.paymentMethod;
    const payStatus = payment?.status ?? order.paymentStatus;

    // Backend chặn xác nhận đơn thanh toán online nếu chưa Paid -> nhắc nhân viên rõ ràng
    if (payMethod && !isPayOnReceive(payMethod) && payStatus !== PAYMENT_STATUS.PAID) {
        toast.warning("Đơn thanh toán online cần được xác nhận 'Đã thanh toán' trước khi xác nhận đơn hàng.");
        return;
    }

    const ok = await confirmDialog({
        title: `Xác nhận đơn hàng #${order.id}`,
        message: `Chuyển đơn hàng #${order.id} sang trạng thái "Đã xác nhận"?\nSau khi xác nhận, thông tin sản phẩm và địa chỉ sẽ bị khóa để chuẩn bị giao hàng.`,
        confirmText: "Xác nhận đơn"
    });
    if (!ok) return;

    try {
        await orderApi.confirm(order.id);
        toast.success(`Đã xác nhận đơn hàng #${order.id}.`);
        await loadOrderAll();
    } catch (err) {
        toast.error(err.message);
    }
}

/** Hủy đơn hàng: hoàn kho, cập nhật thanh toán. */
async function handleCancelOrder() {
    const ok = await confirmDialog({
        title: `Hủy đơn hàng #${order.id}`,
        message: `Bạn có chắc muốn hủy đơn hàng #${order.id}?\nHệ thống sẽ tự động hoàn lại tồn kho cho cửa hàng và chuyển trạng thái thanh toán sang Đã hủy / Hoàn tiền.`,
        confirmText: "Hủy đơn hàng",
        danger: true
    });
    if (!ok) return;

    try {
        await orderApi.cancel(order.id);
        toast.success(`Đã hủy đơn hàng #${order.id}.`);
        await loadOrderAll();
    } catch (err) {
        toast.error(err.message);
    }
}

/** Hoàn tất đơn hàng (nhận tại cửa hàng hoặc vận đơn đã giao). */
async function handleCompleteOrder() {
    const ok = await confirmDialog({
        title: `Hoàn tất đơn hàng #${order.id}`,
        message: `Xác nhận khách đã nhận hàng và hoàn tất đơn #${order.id}?\n`
            + "Hệ thống sẽ tự động ghi nhận thanh toán (nếu COD/Tiền mặt), cộng điểm tích lũy cho khách và cập nhật doanh thu cửa hàng.",
        confirmText: "Hoàn tất đơn"
    });
    if (!ok) return;

    try {
        await orderApi.complete(order.id);
        toast.success(`Đơn hàng #${order.id} đã hoàn tất.`);
        await loadOrderAll();
    } catch (err) {
        toast.error(err.message);
    }
}

/** Xóa đơn hàng (chỉ Admin, đơn đã hủy). */
async function handleDeleteOrder() {
    const ok = await confirmDialog({
        title: `Xóa đơn hàng #${order.id}`,
        message: `Xóa vĩnh viễn đơn hàng đã hủy #${order.id}?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa đơn",
        danger: true
    });
    if (!ok) return;

    try {
        await orderApi.remove(order.id);
        toast.success(`Đã xóa đơn hàng #${order.id}.`);
        window.location.href = "orders.html";
    } catch (err) {
        toast.error(err.message);
    }
}

/* ===========================================================================
 *  PHẦN 3: SỬA THÔNG TIN ĐƠN HÀNG (ĐỊA CHỈ & KHUYẾN MÃI KHI ĐƠN CÒN PENDING)
 * ========================================================================= */

async function openEditOrderModal() {
    let addresses = [];
    let promotions = [];
    try {
        [addresses, promotions] = await Promise.all([
            addressApi.getByCustomer(order.customerId),
            promotionApi.getAll().catch(() => [])
        ]);
    } catch (err) {
        toast.error(`Không tải được sổ địa chỉ của khách: ${err.message}`);
        return;
    }

    if (!addresses.length) {
        toast.warning("Khách hàng này chưa có địa chỉ nào trong sổ địa chỉ.");
        return;
    }

    const addressOptions = addresses.map(a => ({
        value: a.id,
        label: [a.street, a.district, a.city, a.country].filter(Boolean).join(", ")
    }));

    const now = new Date();
    const promoOptions = promotions
        .filter(p => {
            // Giữ các mã đang trong thời hạn hoặc chính mã đang gắn trên đơn
            const active = new Date(p.startDate) <= now && new Date(p.endDate) >= now;
            return active || p.id === order.promotionId;
        })
        .map(p => ({
            value: p.id,
            label: `${p.code} (Giảm ${formatMoney(p.discountValue)} · HSD: ${formatDate(p.endDate)})`
        }));

    formModal({
        title: `Sửa thông tin đơn hàng #${order.id}`,
        description: "Chỉ sửa được địa chỉ giao hàng và mã khuyến mãi khi đơn đang chờ xác nhận (Pending). Tổng tiền sẽ được tính lại tự động.",
        values: {
            addressId: order.addressId,
            promotionId: order.promotionId ?? ""
        },
        fields: [
            {
                name: "addressId",
                label: "Địa chỉ giao hàng",
                type: "select",
                valueType: "number",
                required: true,
                options: addressOptions
            },
            {
                name: "promotionId",
                label: "Mã khuyến mãi",
                type: "select",
                valueType: "number",
                emptyOption: "-- Không áp dụng khuyến mãi --",
                options: promoOptions
            }
        ],
        submitText: "Lưu thay đổi",
        onSubmit: async data => {
            await orderApi.update(order.id, {
                addressId: data.addressId,
                promotionId: data.promotionId || null
            });
            toast.success("Đã cập nhật thông tin đơn hàng.");
            await loadOrderAll();
        }
    });
}

/* ===========================================================================
 *  PHẦN 4: QUẢN LÝ GIAO DỊCH THANH TOÁN
 * ========================================================================= */

function drawPaymentSection() {
    const actionsBox = $("paymentActions");
    const bodyBox = $("paymentBody");
    actionsBox.innerHTML = "";

    const isCancelled = order.status === ORDER_STATUS.CANCELLED;
    const isPendingOrder = order.status === ORDER_STATUS.PENDING;

    if (!payment) {
        bodyBox.innerHTML = `<p class="text-muted">Đơn hàng này chưa có bản ghi giao dịch thanh toán.</p>`;
        if (!isCancelled) {
            const btnCreate = document.createElement("button");
            btnCreate.type = "button";
            btnCreate.className = "btn btn-primary btn-sm";
            btnCreate.textContent = "+ Tạo giao dịch";
            btnCreate.addEventListener("click", openCreatePaymentModal);
            actionsBox.appendChild(btnCreate);
        }
        return;
    }

    bodyBox.innerHTML = `
        <dl class="info-list">
            <dt>Mã giao dịch</dt>
            <dd>#${payment.id}</dd>
            <dt>Phương thức</dt>
            <dd>${escapeHtml(paymentMethodText(payment.paymentMethod))}</dd>
            <dt>Trạng thái</dt>
            <dd>${paymentStatusBadge(payment.status)}</dd>
            <dt>Mã đối soát</dt>
            <dd>${payment.transactionCode ? `<code>${escapeHtml(payment.transactionCode)}</code>` : `<span class="text-muted">Chưa có</span>`}</dd>
            <dt>Thời gian thanh toán</dt>
            <dd>${formatDateTime(payment.paidAt)}</dd>
            <dt>Số tiền giao dịch</dt>
            <dd class="fw-bold">${formatMoney(payment.orderTotal || order.totalAmount)}</dd>
        </dl>
    `;

    // Các nút thao tác trên giao dịch thanh toán
    const canUpdateStatus = !isCancelled
        && (payment.status === PAYMENT_STATUS.PENDING || payment.status === PAYMENT_STATUS.FAILED);

    if (canUpdateStatus) {
        const btnPaid = document.createElement("button");
        btnPaid.type = "button";
        btnPaid.className = "btn btn-primary btn-sm";
        btnPaid.textContent = "Xác nhận đã thu tiền";
        btnPaid.addEventListener("click", () => openUpdatePaymentStatusModal(PAYMENT_STATUS.PAID));
        actionsBox.appendChild(btnPaid);

        if (payment.status === PAYMENT_STATUS.PENDING) {
            const btnFailed = document.createElement("button");
            btnFailed.type = "button";
            btnFailed.className = "btn btn-secondary btn-sm";
            btnFailed.textContent = "Báo lỗi TT";
            btnFailed.addEventListener("click", () => openUpdatePaymentStatusModal(PAYMENT_STATUS.FAILED));
            actionsBox.appendChild(btnFailed);
        }
    }

    // Đổi phương thức thanh toán: khi đơn còn Pending và chưa Paid
    if (isPendingOrder && payment.status !== PAYMENT_STATUS.PAID) {
        const btnMethod = document.createElement("button");
        btnMethod.type = "button";
        btnMethod.className = "btn btn-secondary btn-sm";
        btnMethod.textContent = "Đổi phương thức";
        btnMethod.addEventListener("click", openChangePaymentMethodModal);
        actionsBox.appendChild(btnMethod);
    }

    // Admin có thể xóa giao dịch chưa thanh toán (nếu tạo nhầm)
    if (isAdmin() && payment.status !== PAYMENT_STATUS.PAID) {
        const btnDel = document.createElement("button");
        btnDel.type = "button";
        btnDel.className = "btn btn-ghost btn-danger-text btn-sm";
        btnDel.textContent = "Xóa";
        btnDel.addEventListener("click", handleDeletePayment);
        actionsBox.appendChild(btnDel);
    }
}

/** Tạo giao dịch thanh toán cho đơn cũ chưa có giao dịch. */
function openCreatePaymentModal() {
    formModal({
        title: `Tạo giao dịch thanh toán – Đơn #${order.id}`,
        values: { paymentMethod: order.paymentMethod || "COD" },
        fields: [
            {
                name: "paymentMethod",
                label: "Phương thức thanh toán",
                type: "select",
                required: true,
                options: paymentMethodOptions()
            }
        ],
        submitText: "Tạo giao dịch",
        onSubmit: async data => {
            await paymentApi.create({
                orderId: order.id,
                paymentMethod: data.paymentMethod
            });
            toast.success("Đã tạo giao dịch thanh toán.");
            await loadOrderAll();
        }
    });
}

/** Đổi phương thức thanh toán khi đơn còn Pending. */
function openChangePaymentMethodModal() {
    formModal({
        title: "Đổi phương thức thanh toán",
        description: "Chỉ đổi được phương thức khi đơn hàng đang chờ xác nhận và chưa thanh toán.",
        values: { paymentMethod: payment.paymentMethod },
        fields: [
            {
                name: "paymentMethod",
                label: "Phương thức thanh toán mới",
                type: "select",
                required: true,
                options: paymentMethodOptions()
            }
        ],
        submitText: "Lưu thay đổi",
        onSubmit: async data => {
            await paymentApi.update(payment.id, { paymentMethod: data.paymentMethod });
            toast.success("Đã đổi phương thức thanh toán.");
            await loadOrderAll();
        }
    });
}

/**
 * Cập nhật kết quả thanh toán (Paid / Failed).
 * @param {string} defaultStatus
 */
function openUpdatePaymentStatusModal(defaultStatus = PAYMENT_STATUS.PAID) {
    formModal({
        title: defaultStatus === PAYMENT_STATUS.PAID ? "Xác nhận đã thu tiền" : "Cập nhật trạng thái thanh toán",
        description: `Số tiền đơn hàng: ${formatMoney(order.totalAmount)} (${paymentMethodText(payment.paymentMethod)}).`,
        values: {
            status: defaultStatus,
            transactionCode: payment.transactionCode ?? ""
        },
        fields: [
            {
                name: "status",
                label: "Kết quả thanh toán",
                type: "select",
                required: true,
                options: [
                    { value: PAYMENT_STATUS.PAID, label: "Đã thanh toán (Paid)" },
                    { value: PAYMENT_STATUS.FAILED, label: "Thanh toán lỗi (Failed)" }
                ]
            },
            {
                name: "transactionCode",
                label: "Mã giao dịch đối soát (nếu có)",
                placeholder: "Ví dụ: MOMO12345678, VNP987654...",
                hint: "Mã tham chiếu từ cổng thanh toán hoặc ngân hàng (tối đa 100 ký tự)."
            }
        ],
        submitText: "Cập nhật thanh toán",
        onSubmit: async data => {
            await paymentApi.updateStatus(payment.id, {
                status: data.status,
                transactionCode: data.transactionCode || null
            });
            toast.success(data.status === PAYMENT_STATUS.PAID ? "Đã ghi nhận thanh toán thành công." : "Đã cập nhật trạng thái thanh toán.");
            await loadOrderAll();
        }
    });
}

async function handleDeletePayment() {
    const ok = await confirmDialog({
        title: "Xóa giao dịch thanh toán",
        message: `Xóa giao dịch thanh toán #${payment.id} của đơn hàng #${order.id}?`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await paymentApi.remove(payment.id);
        toast.success("Đã xóa giao dịch thanh toán.");
        await loadOrderAll();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ===========================================================================
 *  PHẦN 5: QUẢN LÝ VẬN CHUYỂN (VẬN ĐƠN - SHIPMENT)
 * ========================================================================= */

function drawShipmentSection() {
    const actionsBox = $("shipmentActions");
    const bodyBox = $("shipmentBody");
    actionsBox.innerHTML = "";

    const isConfirmed = order.status === ORDER_STATUS.CONFIRMED;
    const isShipping = order.status === ORDER_STATUS.SHIPPING;

    if (!shipment) {
        if (isConfirmed) {
            bodyBox.innerHTML = `<p class="text-muted">Đơn hàng đã xác nhận và sẵn sàng bàn giao cho đơn vị vận chuyển (hoặc bấm "Hoàn tất" ở trên nếu khách nhận trực tiếp tại quầy).</p>`;
            const btnCreate = document.createElement("button");
            btnCreate.type = "button";
            btnCreate.className = "btn btn-primary btn-sm";
            btnCreate.textContent = "+ Tạo vận đơn";
            btnCreate.addEventListener("click", openCreateShipmentModal);
            actionsBox.appendChild(btnCreate);
        } else {
            bodyBox.innerHTML = `<p class="text-muted">Đơn hàng chưa có vận đơn. ${order.status === ORDER_STATUS.PENDING ? "Hãy xác nhận đơn hàng trước khi tạo vận đơn." : ""}</p>`;
        }
        return;
    }

    bodyBox.innerHTML = `
        <dl class="info-list">
            <dt>Mã vận đơn</dt>
            <dd>#${shipment.id}</dd>
            <dt>Đơn vị vận chuyển</dt>
            <dd class="fw-bold">${escapeHtml(shipment.carrier)}</dd>
            <dt>Mã theo dõi (Tracking)</dt>
            <dd>${shipment.trackingNumber ? `<code>${escapeHtml(shipment.trackingNumber)}</code>` : `<span class="text-muted">Chưa cập nhật</span>`}</dd>
            <dt>Trạng thái giao</dt>
            <dd>${shipmentStatusBadge(shipment.status)}</dd>
            <dt>Ngày bàn giao</dt>
            <dd>${formatDateTime(shipment.shippedDate)}</dd>
            <dt>Ngày giao thành công</dt>
            <dd>${formatDateTime(shipment.deliveredDate)}</dd>
        </dl>
    `;

    // Vận đơn chưa Delivered thì được sửa thông tin và cập nhật trạng thái (khi đơn đang Shipping)
    if (shipment.status !== SHIPMENT_STATUS.DELIVERED) {
        if (isShipping) {
            const btnDelivered = document.createElement("button");
            btnDelivered.type = "button";
            btnDelivered.className = "btn btn-primary btn-sm";
            btnDelivered.textContent = "Đã giao hàng";
            btnDelivered.addEventListener("click", () => handleUpdateShipmentStatus(SHIPMENT_STATUS.DELIVERED));
            actionsBox.appendChild(btnDelivered);

            if (shipment.status === SHIPMENT_STATUS.SHIPPING) {
                const btnFailed = document.createElement("button");
                btnFailed.type = "button";
                btnFailed.className = "btn btn-danger btn-sm";
                btnFailed.textContent = "Giao thất bại";
                btnFailed.addEventListener("click", () => handleUpdateShipmentStatus(SHIPMENT_STATUS.FAILED));
                actionsBox.appendChild(btnFailed);
            } else if (shipment.status === SHIPMENT_STATUS.FAILED) {
                const btnRetry = document.createElement("button");
                btnRetry.type = "button";
                btnRetry.className = "btn btn-secondary btn-sm";
                btnRetry.textContent = "Giao lại";
                btnRetry.addEventListener("click", () => handleUpdateShipmentStatus(SHIPMENT_STATUS.SHIPPING));
                actionsBox.appendChild(btnRetry);
            }
        }

        const btnEdit = document.createElement("button");
        btnEdit.type = "button";
        btnEdit.className = "btn btn-secondary btn-sm";
        btnEdit.textContent = "Sửa vận đơn";
        btnEdit.addEventListener("click", openEditShipmentModal);
        actionsBox.appendChild(btnEdit);
    }
}

/** Tạo vận đơn cho đơn hàng đã xác nhận -> đơn tự chuyển sang Shipping. */
function openCreateShipmentModal() {
    formModal({
        title: `Tạo vận đơn – Đơn hàng #${order.id}`,
        description: "Sau khi tạo vận đơn, trạng thái đơn hàng sẽ tự động chuyển sang 'Đang giao' (Shipping).",
        fields: [
            {
                name: "carrier",
                label: "Đơn vị vận chuyển",
                required: true,
                placeholder: "Ví dụ: Giao Hàng Nhanh (GHN), Giao Hàng Tiết Kiệm, Viettel Post..."
            },
            {
                name: "trackingNumber",
                label: "Mã vận đơn (Tracking Number)",
                placeholder: "Ví dụ: GHN84930219",
                hint: "Có thể bỏ trống và bổ sung sau."
            }
        ],
        submitText: "Tạo vận đơn & Giao hàng",
        onSubmit: async data => {
            await shipmentApi.create({
                orderId: order.id,
                carrier: data.carrier,
                trackingNumber: data.trackingNumber || null
            });
            toast.success("Đã tạo vận đơn và chuyển đơn sang trạng thái Đang giao.");
            await loadOrderAll();
        }
    });
}

/** Sửa thông tin đơn vị vận chuyển / mã tracking. */
function openEditShipmentModal() {
    formModal({
        title: `Sửa vận đơn #${shipment.id}`,
        values: {
            carrier: shipment.carrier,
            trackingNumber: shipment.trackingNumber ?? ""
        },
        fields: [
            {
                name: "carrier",
                label: "Đơn vị vận chuyển",
                required: true,
                placeholder: "Ví dụ: Giao Hàng Nhanh (GHN)"
            },
            {
                name: "trackingNumber",
                label: "Mã vận đơn (Tracking Number)",
                placeholder: "Ví dụ: GHN84930219"
            }
        ],
        submitText: "Lưu thay đổi",
        onSubmit: async data => {
            await shipmentApi.update(shipment.id, {
                carrier: data.carrier,
                trackingNumber: data.trackingNumber || null
            });
            toast.success("Đã cập nhật thông tin vận đơn.");
            await loadOrderAll();
        }
    });
}

/**
 * Cập nhật trạng thái vận đơn: Delivered / Failed / Shipping.
 * @param {string} newStatus
 */
async function handleUpdateShipmentStatus(newStatus) {
    const messages = {
        [SHIPMENT_STATUS.DELIVERED]:
            `Xác nhận vận đơn #${shipment.id} đã giao hàng thành công?\n`
            + "Đơn hàng sẽ tự động chuyển sang 'Hoàn tất', ghi nhận thu tiền (nếu COD), cộng điểm thưởng cho khách và cập nhật thống kê doanh thu.",
        [SHIPMENT_STATUS.FAILED]:
            `Đánh dấu vận đơn #${shipment.id} giao thất bại?\n`
            + "Sau khi đánh dấu thất bại, bạn có thể chọn 'Giao lại' hoặc 'Hủy đơn hàng' (để hoàn kho).",
        [SHIPMENT_STATUS.SHIPPING]:
            `Cho vận đơn #${shipment.id} tiến hành giao lại?`
    };

    const ok = await confirmDialog({
        title: "Cập nhật trạng thái vận chuyển",
        message: messages[newStatus] ?? `Đổi trạng thái vận đơn sang ${newStatus}?`,
        confirmText: "Xác nhận",
        danger: newStatus === SHIPMENT_STATUS.FAILED
    });
    if (!ok) return;

    try {
        await shipmentApi.updateStatus(shipment.id, { status: newStatus });
        toast.success(
            newStatus === SHIPMENT_STATUS.DELIVERED
                ? "Đã giao hàng thành công và hoàn tất đơn hàng!"
                : "Đã cập nhật trạng thái vận đơn."
        );
        await loadOrderAll();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ===========================================================================
 *  PHẦN 6: SẢN PHẨM TRONG ĐƠN (ORDER ITEMS) - XEM & THÊM/SỬA/XÓA KHI PENDING
 * ========================================================================= */

function drawOrderItems() {
    const items = order.orderItems ?? [];
    const isPending = order.status === ORDER_STATUS.PENDING;

    $("orderItemsSub").textContent = isPending
        ? "Đơn đang chờ xác nhận: có thể thêm, sửa số lượng hoặc xóa sản phẩm (tồn kho và tổng tiền tự cập nhật)."
        : "Đơn hàng đã khóa danh sách sản phẩm.";

    const actions = isPending
        ? [
            { label: "Sửa", onClick: row => openOrderItemForm(row) },
            {
                label: "Xóa",
                danger: true,
                // Đơn phải còn ít nhất 1 sản phẩm
                visible: () => items.length > 1,
                onClick: row => removeOrderItem(row)
            }
        ]
        : [];

    renderTable($("itemsTable"), {
        rows: items,
        emptyText: "Đơn hàng chưa có sản phẩm nào.",
        columns: [
            { key: "orderItemId", title: "Mã dòng", width: "90px", render: row => `#${row.orderItemId}` },
            {
                key: "variantName",
                title: "Sản phẩm",
                render: row => `
                    <div class="fw-bold">${escapeHtml(row.variantName || `Biến thể #${row.variantId}`)}</div>
                    <div class="text-muted text-sm">Mã biến thể (SKU): #${row.variantId}</div>`
            },
            {
                key: "color",
                title: "Màu / Size",
                render: row => {
                    const spec = [row.color, row.size].filter(Boolean).join(" · ");
                    return spec ? `<span class="badge badge-secondary">${escapeHtml(spec)}</span>` : "—";
                }
            },
            { key: "price", title: "Đơn giá", align: "right", render: row => formatMoney(row.price) },
            { key: "quantity", title: "Số lượng", align: "right", render: row => formatNumber(row.quantity) },
            {
                key: "lineTotal",
                title: "Thành tiền",
                align: "right",
                render: row => `<span class="fw-bold">${formatMoney(row.lineTotal ?? row.price * row.quantity)}</span>`
            }
        ],
        actions
    });
}

/** Tải danh sách biến thể sản phẩm (chỉ gọi API 1 lần rồi nhớ lại). */
async function ensureVariants() {
    if (!variantsCache) {
        variantsCache = await orderApi.getVariants();
    }
    return variantsCache;
}

/**
 * Mở form Thêm / Sửa sản phẩm trong đơn.
 * @param {any} [item] - dòng đang sửa; null = thêm mới
 */
async function openOrderItemForm(item = null) {
    const isEdit = item !== null;

    let variants = [];
    try {
        variants = await ensureVariants();
    } catch (err) {
        toast.error(`Không tải được danh sách biến thể sản phẩm: ${err.message}`);
        return;
    }

    const variantOptions = variants.map(v => {
        const spec = [v.color, v.size].filter(Boolean).join(" / ");
        return {
            value: v.id,
            label: `#${v.id} – ${v.productName || "Sản phẩm"}${spec ? ` (${spec})` : ""} – ${formatMoney(v.price)}`
        };
    });

    formModal({
        title: isEdit ? `Sửa sản phẩm trong đơn #${order.id}` : `Thêm sản phẩm vào đơn #${order.id}`,
        description: isEdit
            ? "Thay đổi số lượng hoặc biến thể. Hệ thống sẽ tự điều chỉnh tồn kho tại cửa hàng xuất kho và tính lại tổng tiền."
            : "Nếu chọn biến thể đã có trong đơn, hệ thống sẽ tự cộng dồn số lượng.",
        values: isEdit
            ? { variantId: item.variantId, quantity: item.quantity }
            : { quantity: 1 },
        fields: [
            {
                name: "variantId",
                label: "Phiên bản sản phẩm (Màu / Size)",
                type: "select",
                valueType: "number",
                required: true,
                emptyOption: isEdit ? undefined : "-- Chọn phiên bản sản phẩm --",
                options: variantOptions
            },
            {
                name: "quantity",
                label: "Số lượng",
                type: "number",
                required: true,
                min: 1,
                step: 1
            }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Thêm vào đơn",
        onSubmit: async data => {
            if (isEdit) {
                await orderItemApi.update(item.orderItemId, {
                    variantId: data.variantId,
                    quantity: data.quantity
                });
                toast.success("Đã cập nhật sản phẩm trong đơn.");
            } else {
                await orderItemApi.create({
                    orderId: order.id,
                    variantId: data.variantId,
                    quantity: data.quantity
                });
                toast.success("Đã thêm sản phẩm vào đơn.");
            }
            await loadOrderAll();
        }
    });
}

/** @param {any} item */
async function removeOrderItem(item) {
    const spec = [item.color, item.size].filter(Boolean).join(" · ");
    const name = `${item.variantName || `Biến thể #${item.variantId}`}${spec ? ` (${spec})` : ""}`;

    const ok = await confirmDialog({
        title: "Xóa sản phẩm khỏi đơn",
        message: `Xóa "${name}" (SL: ${item.quantity}) khỏi đơn hàng #${order.id}?\nSố lượng tồn kho sẽ được hoàn lại tự động.`,
        confirmText: "Xóa món này",
        danger: true
    });
    if (!ok) return;

    try {
        await orderItemApi.remove(item.orderItemId);
        toast.success("Đã xóa sản phẩm khỏi đơn hàng.");
        await loadOrderAll();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ===========================================================================
 *  KHỞI ĐỘNG TRANG
 * ========================================================================= */

/** @param {string} message */
function showPageError(message) {
    pageError.textContent = message;
    pageContent.classList.add("hidden");
}

if (await initAdminLayout({ title: "Chi tiết đơn hàng", activeMenu: "/admin/sales/orders.html" })) {
    if (!orderId) {
        showPageError("Thiếu mã đơn hàng trên địa chỉ trang (ví dụ: order-detail.html?id=10). Hãy mở từ trang Danh sách đơn hàng.");
    } else {
        try {
            await loadOrderAll();
            pageContent.classList.remove("hidden");

            // Gắn sự kiện cho các nút cố định trên trang
            $("btnEditOrder").addEventListener("click", openEditOrderModal);
            $("btnEditOrderCard").addEventListener("click", openEditOrderModal);
            $("btnConfirmOrder").addEventListener("click", handleConfirmOrder);
            $("btnCreateShipment").addEventListener("click", openCreateShipmentModal);
            $("btnCompleteOrder").addEventListener("click", handleCompleteOrder);
            $("btnCancelOrder").addEventListener("click", handleCancelOrder);
            $("btnDeleteOrder").addEventListener("click", handleDeleteOrder);
            $("btnAddItem").addEventListener("click", () => openOrderItemForm());
        } catch (err) {
            showPageError(err.status === 404 ? `Không tìm thấy đơn hàng #${orderId}.` : err.message);
        }
    }
}
