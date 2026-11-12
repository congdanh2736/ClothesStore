/*
 * ============================================================================
 *  shipments.js  -  XỬ LÝ CHO TRANG admin/sales/shipments.html   (Nhóm 4)
 * ============================================================================
 *
 *  Trang làm các việc:
 *    1. Tải danh sách vận đơn và hiển thị bảng có tìm kiếm, lọc trạng thái, phân trang.
 *    2. Tự nhận tham số ?status=... trên URL (ví dụ ?status=Failed từ trang Tổng quan).
 *    3. Nút "+ Tạo vận đơn": chọn đơn hàng đã xác nhận (Confirmed) để bàn giao vận chuyển.
 *    4. Thao tác trên dòng: Xem đơn, Đã giao (hoàn tất đơn), Thất bại, Giao lại, Sửa.
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/sales/ -> "../../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { shipmentApi } from "../../../assets/js/api/shipmentApi.js";
import { orderApi } from "../../../assets/js/api/orderApi.js";
import {
    ORDER_STATUS,
    SHIPMENT_STATUS,
    SHIPMENT_STATUS_LABELS,
    ORDER_STATUS_LABELS
} from "../../../assets/js/core/config.js";
import {
    escapeHtml,
    formatMoney,
    formatDateTime,
    getQueryParam,
    shipmentStatusBadge,
    statusText,
    debounce
} from "../../../assets/js/core/utils.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

/* ---------------------------------------------------------------------------
 * Lấy các thẻ HTML
 * ------------------------------------------------------------------------- */
const tableContainer = /** @type {HTMLDivElement} */ (document.getElementById("tableContainer"));
const paginationContainer = /** @type {HTMLDivElement} */ (document.getElementById("paginationContainer"));
const searchInput = /** @type {HTMLInputElement} */ (document.getElementById("searchInput"));
const statusFilter = /** @type {HTMLSelectElement} */ (document.getElementById("statusFilter"));
const btnAdd = /** @type {HTMLButtonElement} */ (document.getElementById("btnAdd"));
const btnReload = /** @type {HTMLButtonElement} */ (document.getElementById("btnReload"));

/* ---------------------------------------------------------------------------
 * BẢNG VẬN ĐƠN
 * ------------------------------------------------------------------------- */
const table = createDataTable({
    container: tableContainer,
    paginationContainer,
    searchFields: ["id", "orderId", "carrier", "trackingNumber"],
    sortBy: (a, b) => b.id - a.id,
    emptyText: "Chưa có vận đơn nào.",
    columns: [
        { key: "id", title: "Mã VĐ", width: "80px", render: row => `#${row.id}` },
        {
            key: "orderId",
            title: "Đơn hàng",
            width: "140px",
            render: row => `
                <a class="fw-bold" href="order-detail.html?id=${row.orderId}">Đơn #${row.orderId}</a>
                <div class="text-muted text-sm">${escapeHtml(statusText(row.orderStatus, ORDER_STATUS_LABELS))}</div>`
        },
        {
            key: "carrier",
            title: "Đơn vị vận chuyển & Tracking",
            render: row => `
                <div class="fw-bold">${escapeHtml(row.carrier)}</div>
                <div class="text-muted text-sm">${row.trackingNumber ? `Mã VĐ: <code>${escapeHtml(row.trackingNumber)}</code>` : "Chưa có mã tracking"}</div>`
        },
        {
            key: "shippedDate",
            title: "Ngày gửi",
            width: "155px",
            render: row => formatDateTime(row.shippedDate)
        },
        {
            key: "deliveredDate",
            title: "Ngày giao",
            width: "155px",
            render: row => formatDateTime(row.deliveredDate)
        },
        {
            key: "status",
            title: "Trạng thái",
            render: row => shipmentStatusBadge(row.status)
        }
    ],
    actions: [
        {
            label: "Xem đơn",
            onClick: row => { window.location.href = `order-detail.html?id=${row.orderId}`; }
        },
        {
            label: "Đã giao",
            visible: row => row.status !== SHIPMENT_STATUS.DELIVERED && row.orderStatus === ORDER_STATUS.SHIPPING,
            onClick: row => changeStatus(row, SHIPMENT_STATUS.DELIVERED)
        },
        {
            label: "Thất bại",
            danger: true,
            visible: row => row.status === SHIPMENT_STATUS.SHIPPING && row.orderStatus === ORDER_STATUS.SHIPPING,
            onClick: row => changeStatus(row, SHIPMENT_STATUS.FAILED)
        },
        {
            label: "Giao lại",
            visible: row => row.status === SHIPMENT_STATUS.FAILED && row.orderStatus === ORDER_STATUS.SHIPPING,
            onClick: row => changeStatus(row, SHIPMENT_STATUS.SHIPPING)
        },
        {
            label: "Sửa",
            visible: row => row.status !== SHIPMENT_STATUS.DELIVERED,
            onClick: row => openEditForm(row)
        }
    ]
});

/* ---------------------------------------------------------------------------
 * BỘ LỌC TRẠNG THÁI
 * ------------------------------------------------------------------------- */
function initFilterOptions() {
    statusFilter.innerHTML = `<option value="">Tất cả trạng thái</option>` +
        Object.entries(SHIPMENT_STATUS_LABELS)
            .map(([value, info]) => `<option value="${escapeHtml(value)}">${escapeHtml(info.text)}</option>`)
            .join("");

    const urlStatus = getQueryParam("status");
    if (urlStatus && SHIPMENT_STATUS_LABELS[urlStatus]) {
        statusFilter.value = urlStatus;
        table.setFilter(row => row.status === urlStatus);
    }
}

/* ---------------------------------------------------------------------------
 * TẢI DỮ LIỆU
 * ------------------------------------------------------------------------- */

/** @param {{ keepPage?: boolean }} [options] */
async function loadShipments({ keepPage = false } = {}) {
    table.setLoading();
    try {
        const rows = await shipmentApi.getAll();
        table.setRows(rows, { keepPage });
    } catch (err) {
        table.setError(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * TẠO / SỬA VẬN ĐƠN & CẬP NHẬT TRẠNG THÁI
 * ------------------------------------------------------------------------- */

/** Tạo vận đơn mới cho một đơn hàng đang ở trạng thái Confirmed. */
async function openCreateForm() {
    let orders = [];
    try {
        orders = await orderApi.getAll();
    } catch (err) {
        toast.error(`Không tải được danh sách đơn hàng: ${err.message}`);
        return;
    }

    // Chỉ những đơn đã xác nhận (Confirmed) và chưa có vận đơn mới tạo được
    const confirmedOrders = orders.filter(o => o.status === ORDER_STATUS.CONFIRMED && !o.shipmentStatus);
    if (!confirmedOrders.length) {
        toast.info("Hiện không có đơn hàng nào ở trạng thái 'Đã xác nhận' chờ tạo vận đơn.");
        return;
    }

    const orderOptions = confirmedOrders.map(o => ({
        value: o.id,
        label: `Đơn #${o.id} – ${o.customerName} (${formatMoney(o.totalAmount)}) – ${o.address}`
    }));

    formModal({
        title: "Tạo vận đơn mới",
        description: "Chọn đơn hàng đã xác nhận để bàn giao cho đơn vị vận chuyển. Đơn hàng sẽ tự chuyển sang trạng thái 'Đang giao'.",
        fields: [
            {
                name: "orderId",
                label: "Đơn hàng cần giao",
                type: "select",
                valueType: "number",
                required: true,
                options: orderOptions
            },
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
                hint: "Không bắt buộc, có thể bổ sung sau."
            }
        ],
        submitText: "Tạo vận đơn",
        onSubmit: async data => {
            await shipmentApi.create({
                orderId: data.orderId,
                carrier: data.carrier,
                trackingNumber: data.trackingNumber || null
            });
            toast.success(`Đã tạo vận đơn cho đơn hàng #${data.orderId}.`);
            await loadShipments();
        }
    });
}

/**
 * Sửa đơn vị vận chuyển / mã tracking.
 * @param {any} row
 */
function openEditForm(row) {
    formModal({
        title: `Sửa vận đơn #${row.id} (Đơn #${row.orderId})`,
        values: {
            carrier: row.carrier,
            trackingNumber: row.trackingNumber ?? ""
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
            await shipmentApi.update(row.id, {
                carrier: data.carrier,
                trackingNumber: data.trackingNumber || null
            });
            toast.success("Đã cập nhật thông tin vận đơn.");
            await loadShipments({ keepPage: true });
        }
    });
}

/**
 * Đổi trạng thái vận đơn: Delivered / Failed / Shipping.
 * @param {any} row
 * @param {string} newStatus
 */
async function changeStatus(row, newStatus) {
    const messages = {
        [SHIPMENT_STATUS.DELIVERED]:
            `Xác nhận vận đơn #${row.id} (Đơn #${row.orderId}) đã giao thành công?\n`
            + "Hệ thống sẽ tự động hoàn tất đơn hàng, ghi nhận thanh toán (nếu COD), cộng điểm cho khách và cập nhật thống kê.",
        [SHIPMENT_STATUS.FAILED]:
            `Đánh dấu vận đơn #${row.id} (Đơn #${row.orderId}) giao thất bại?\n`
            + "Sau đó bạn có thể bấm 'Giao lại' hoặc vào chi tiết đơn để 'Hủy đơn hàng'.",
        [SHIPMENT_STATUS.SHIPPING]:
            `Cho vận đơn #${row.id} (Đơn #${row.orderId}) tiến hành giao lại?`
    };

    const ok = await confirmDialog({
        title: "Cập nhật trạng thái vận đơn",
        message: messages[newStatus] ?? `Đổi trạng thái vận đơn #${row.id}?`,
        confirmText: "Xác nhận",
        danger: newStatus === SHIPMENT_STATUS.FAILED
    });
    if (!ok) return;

    try {
        await shipmentApi.updateStatus(row.id, { status: newStatus });
        toast.success(
            newStatus === SHIPMENT_STATUS.DELIVERED
                ? `Vận đơn #${row.id} đã giao thành công (Đơn #${row.orderId} hoàn tất).`
                : `Đã cập nhật trạng thái vận đơn #${row.id}.`
        );
        await loadShipments({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * KHỞI ĐỘNG TRANG
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Vận chuyển" })) {
    initFilterOptions();

    btnAdd.addEventListener("click", openCreateForm);
    btnReload.addEventListener("click", () => loadShipments({ keepPage: true }));

    const onSearch = debounce(text => table.search(text), 300);
    searchInput.addEventListener("input", () => onSearch(searchInput.value));

    statusFilter.addEventListener("change", () => {
        const val = statusFilter.value;
        table.setFilter(val ? row => row.status === val : null);
    });

    await loadShipments();
}
