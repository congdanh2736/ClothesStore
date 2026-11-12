/*
 * ============================================================================
 *  orders.js  -  XỬ LÝ CHO TRANG admin/sales/orders.html   (Nhóm 4)
 * ============================================================================
 *
 *  Trang làm các việc:
 *    1. Tải danh sách đơn hàng và hiển thị bảng có tìm kiếm, lọc theo trạng thái
 *       đơn + trạng thái thanh toán, phân trang.
 *    2. Tự nhận tham số ?status=... trên URL (ví dụ từ trang Tổng quan chuyển sang).
 *    3. Nút thao tác: Xem (sang trang chi tiết), Xác nhận nhanh, Hủy đơn, Xóa (Admin).
 *    4. Nút "+ Tạo đơn hàng": tạo đơn trực tiếp cho khách, tự tải sổ địa chỉ theo khách.
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/sales/ -> "../../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { orderApi } from "../../../assets/js/api/orderApi.js";
import { promotionApi } from "../../../assets/js/api/promotionApi.js";
import { customerApi, addressApi } from "../../../assets/js/api/customerApi.js";
import { isAdmin } from "../../../assets/js/core/auth.js";
import {
    ORDER_STATUS,
    ORDER_STATUS_LABELS,
    PAYMENT_STATUS_LABELS,
    SHIPMENT_STATUS,
    PAYMENT_METHOD_LABELS
} from "../../../assets/js/core/config.js";
import {
    escapeHtml,
    formatMoney,
    formatDate,
    formatDateTime,
    getQueryParam,
    orderStatusBadge,
    paymentStatusBadge,
    paymentMethodText,
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
const paymentFilter = /** @type {HTMLSelectElement} */ (document.getElementById("paymentFilter"));
const btnAdd = /** @type {HTMLButtonElement} */ (document.getElementById("btnAdd"));
const btnReload = /** @type {HTMLButtonElement} */ (document.getElementById("btnReload"));

/** Đường dẫn trang chi tiết của một đơn hàng. @param {number} id */
const detailUrl = id => `order-detail.html?id=${id}`;

/* ---------------------------------------------------------------------------
 * BẢNG DANH SÁCH ĐƠN HÀNG
 * ------------------------------------------------------------------------- */
const table = createDataTable({
    container: tableContainer,
    paginationContainer,
    searchFields: ["id", "customerName", "address", "promotionCode", "storeName", "trackingNumber"],
    sortBy: (a, b) => b.id - a.id,
    emptyText: "Chưa có đơn hàng nào.",
    columns: [
        {
            key: "id",
            title: "Mã đơn",
            width: "85px",
            render: row => `<a class="fw-bold" href="${detailUrl(row.id)}">#${row.id}</a>`
        },
        {
            key: "customerName",
            title: "Khách hàng & Giao nhận",
            render: row => `
                <a class="fw-bold" href="../customers/customer-detail.html?id=${row.customerId}">
                    ${escapeHtml(row.customerName || `Khách hàng #${row.customerId}`)}
                </a>
                <div class="text-muted text-sm">${escapeHtml(row.address || "—")}</div>`
        },
        {
            key: "orderDate",
            title: "Ngày đặt",
            width: "150px",
            render: row => formatDateTime(row.orderDate)
        },
        {
            key: "storeName",
            title: "Cửa hàng",
            render: row => row.storeName
                ? escapeHtml(row.storeName)
                : `<span class="text-muted">—</span>`
        },
        {
            key: "status",
            title: "Trạng thái",
            render: row => orderStatusBadge(row.status)
        },
        {
            key: "paymentStatus",
            title: "Thanh toán",
            render: row => `
                <div>${paymentStatusBadge(row.paymentStatus)}</div>
                <div class="text-muted text-sm">${escapeHtml(row.paymentMethod ? paymentMethodText(row.paymentMethod) : "")}</div>`
        },
        {
            key: "totalAmount",
            title: "Tổng tiền",
            align: "right",
            render: row => `
                <div class="fw-bold">${formatMoney(row.totalAmount)}</div>
                ${row.promotionCode ? `<div class="text-muted text-sm">KM: ${escapeHtml(row.promotionCode)}</div>` : ""}`
        }
    ],
    actions: [
        { label: "Xem", onClick: row => { window.location.href = detailUrl(row.id); } },
        {
            label: "Xác nhận",
            visible: row => row.status === ORDER_STATUS.PENDING,
            onClick: row => confirmOrder(row)
        },
        {
            label: "Hủy",
            danger: true,
            visible: row =>
                row.status === ORDER_STATUS.PENDING
                || row.status === ORDER_STATUS.CONFIRMED
                || (row.status === ORDER_STATUS.SHIPPING && row.shipmentStatus === SHIPMENT_STATUS.FAILED),
            onClick: row => cancelOrder(row)
        },
        {
            label: "Xóa",
            danger: true,
            visible: row => isAdmin() && row.status === ORDER_STATUS.CANCELLED,
            onClick: row => removeOrder(row)
        }
    ]
});

/* ---------------------------------------------------------------------------
 * BỘ LỌC TRẠNG THÁI
 * ------------------------------------------------------------------------- */

/** Đổ danh sách lựa chọn vào 2 ô lọc trạng thái. */
function initFilterOptions() {
    statusFilter.innerHTML = `<option value="">Tất cả trạng thái</option>` +
        Object.entries(ORDER_STATUS_LABELS)
            .map(([value, info]) => `<option value="${escapeHtml(value)}">${escapeHtml(info.text)}</option>`)
            .join("");

    paymentFilter.innerHTML = `<option value="">Tất cả thanh toán</option>` +
        Object.entries(PAYMENT_STATUS_LABELS)
            .map(([value, info]) => `<option value="${escapeHtml(value)}">${escapeHtml(info.text)}</option>`)
            .join("");

    // Nếu URL có ?status=Pending (ví dụ bấm từ trang Tổng quan) thì chọn sẵn
    const urlStatus = getQueryParam("status");
    if (urlStatus && ORDER_STATUS_LABELS[urlStatus]) {
        statusFilter.value = urlStatus;
    }
}

/** Áp dụng kết hợp cả 2 ô lọc trạng thái đơn + trạng thái thanh toán. */
function applyFilters() {
    const statusVal = statusFilter.value;
    const paymentVal = paymentFilter.value;

    if (!statusVal && !paymentVal) {
        table.setFilter(null);
        return;
    }

    table.setFilter(row => {
        if (statusVal && row.status !== statusVal) return false;
        if (paymentVal && row.paymentStatus !== paymentVal) return false;
        return true;
    });
}

/* ---------------------------------------------------------------------------
 * TẢI DỮ LIỆU
 * ------------------------------------------------------------------------- */

/** @param {{ keepPage?: boolean }} [options] */
async function loadOrders({ keepPage = false } = {}) {
    table.setLoading();
    try {
        const rows = await orderApi.getAll();
        table.setRows(rows, { keepPage });
    } catch (err) {
        table.setError(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * CÁC THAO TÁC NHANH TRÊN DÒNG (XÁC NHẬN / HỦY / XÓA)
 * ------------------------------------------------------------------------- */

/** @param {any} row */
async function confirmOrder(row) {
    const ok = await confirmDialog({
        title: `Xác nhận đơn hàng #${row.id}`,
        message: `Xác nhận đơn hàng #${row.id} của khách "${row.customerName}" (${formatMoney(row.totalAmount)})?`,
        confirmText: "Xác nhận"
    });
    if (!ok) return;

    try {
        await orderApi.confirm(row.id);
        toast.success(`Đã xác nhận đơn hàng #${row.id}.`);
        await loadOrders({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/** @param {any} row */
async function cancelOrder(row) {
    const ok = await confirmDialog({
        title: `Hủy đơn hàng #${row.id}`,
        message: `Hủy đơn hàng #${row.id} của khách "${row.customerName}"?\nTồn kho sẽ được hoàn lại tự động.`,
        confirmText: "Hủy đơn",
        danger: true
    });
    if (!ok) return;

    try {
        await orderApi.cancel(row.id);
        toast.success(`Đã hủy đơn hàng #${row.id}.`);
        await loadOrders({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/** @param {any} row */
async function removeOrder(row) {
    const ok = await confirmDialog({
        title: `Xóa đơn hàng #${row.id}`,
        message: `Xóa vĩnh viễn đơn hàng đã hủy #${row.id}?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await orderApi.remove(row.id);
        toast.success(`Đã xóa đơn hàng #${row.id}.`);
        await loadOrders({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * TẠO ĐƠN HÀNG MỚI (+ TẠO ĐƠN HÀNG)
 * ------------------------------------------------------------------------- */

async function openCreateOrderModal() {
    let customers = [];
    let variants = [];
    let stores = [];
    let promotions = [];

    try {
        [customers, variants, stores, promotions] = await Promise.all([
            customerApi.getAll(),
            orderApi.getVariants(),
            orderApi.getStores().catch(() => []),
            promotionApi.getAll().catch(() => [])
        ]);
    } catch (err) {
        toast.error(`Không tải được dữ liệu tạo đơn: ${err.message}`);
        return;
    }

    if (!customers.length) {
        toast.warning("Chưa có khách hàng nào trong hệ thống.");
        return;
    }
    if (!variants.length) {
        toast.warning("Chưa có biến thể sản phẩm nào để tạo đơn.");
        return;
    }

    const customerOptions = customers.map(c => ({
        value: c.id,
        label: `#${c.id} – ${c.fullName}${c.email ? ` (${c.email})` : ""}`
    }));

    const variantOptions = variants.map(v => {
        const spec = [v.color, v.size].filter(Boolean).join(" / ");
        return {
            value: v.id,
            label: `#${v.id} – ${v.productName || "Sản phẩm"}${spec ? ` (${spec})` : ""} – ${formatMoney(v.price)}`
        };
    });

    const storeOptions = stores.map(s => ({
        value: s.id,
        label: s.location ? `${s.name} – ${s.location}` : s.name
    }));

    const now = new Date();
    const promoOptions = promotions
        .filter(p => new Date(p.startDate) <= now && new Date(p.endDate) >= now)
        .map(p => ({
            value: p.id,
            label: `${p.code} (Giảm ${formatMoney(p.discountValue)} · HSD: ${formatDate(p.endDate)})`
        }));

    const paymentMethodOptions = Object.entries(PAYMENT_METHOD_LABELS).map(([value, label]) => ({
        value,
        label
    }));

    const modal = formModal({
        title: "Tạo đơn hàng mới",
        description: "Chọn khách hàng, địa chỉ giao hàng và sản phẩm khởi tạo. Sau khi tạo đơn, bạn có thể thêm nhiều sản phẩm khác ở trang Chi tiết đơn hàng.",
        size: "lg",
        columns: 2,
        values: {
            paymentMethod: "COD",
            quantity: 1
        },
        fields: [
            {
                name: "customerId",
                label: "Khách hàng",
                type: "select",
                valueType: "number",
                required: true,
                fullWidth: true,
                emptyOption: "-- Chọn khách hàng --",
                options: customerOptions
            },
            {
                name: "addressId",
                label: "Địa chỉ giao hàng",
                type: "select",
                valueType: "number",
                required: true,
                fullWidth: true,
                emptyOption: "-- Hãy chọn khách hàng trước --",
                options: []
            },
            {
                name: "variantId",
                label: "Sản phẩm (Màu / Size)",
                type: "select",
                valueType: "number",
                required: true,
                fullWidth: true,
                emptyOption: "-- Chọn phiên bản sản phẩm --",
                options: variantOptions
            },
            {
                name: "quantity",
                label: "Số lượng",
                type: "number",
                required: true,
                min: 1,
                step: 1
            },
            {
                name: "paymentMethod",
                label: "Phương thức thanh toán",
                type: "select",
                required: true,
                options: paymentMethodOptions
            },
            {
                name: "storeId",
                label: "Cửa hàng xuất kho",
                type: "select",
                valueType: "number",
                emptyOption: "-- Tự động chọn cửa hàng đủ hàng --",
                options: storeOptions
            },
            {
                name: "promotionId",
                label: "Mã khuyến mãi",
                type: "select",
                valueType: "number",
                emptyOption: "-- Không áp dụng --",
                options: promoOptions
            }
        ],
        submitText: "Tạo đơn hàng",
        onSubmit: async data => {
            const created = await orderApi.create({
                customerId: data.customerId,
                addressId: data.addressId,
                promotionId: data.promotionId || null,
                storeId: data.storeId || null,
                paymentMethod: data.paymentMethod,
                items: [{ variantId: data.variantId, quantity: data.quantity }]
            });
            toast.success(`Đã tạo đơn hàng #${created.id}.`);
            await loadOrders();
        }
    });

    // Khi chọn khách hàng -> tự tải danh sách địa chỉ của khách đó đổ vào ô addressId
    const customerSelect = /** @type {HTMLSelectElement|null} */ (modal.form?.elements.namedItem("customerId"));
    const addressSelect = /** @type {HTMLSelectElement|null} */ (modal.form?.elements.namedItem("addressId"));

    customerSelect?.addEventListener("change", async () => {
        if (!addressSelect) return;
        const cid = Number(customerSelect.value);
        if (!cid) {
            addressSelect.innerHTML = `<option value="">-- Hãy chọn khách hàng trước --</option>`;
            return;
        }
        addressSelect.innerHTML = `<option value="">Đang tải địa chỉ...</option>`;
        try {
            const list = await addressApi.getByCustomer(cid);
            if (!list.length) {
                addressSelect.innerHTML = `<option value="">-- Khách hàng này chưa có địa chỉ (hãy thêm ở trang Khách hàng) --</option>`;
                return;
            }
            addressSelect.innerHTML = list
                .map(a => {
                    const label = [a.street, a.district, a.city, a.country].filter(Boolean).join(", ");
                    return `<option value="${a.id}">${escapeHtml(label)}</option>`;
                })
                .join("");
        } catch (err) {
            addressSelect.innerHTML = `<option value="">Không tải được địa chỉ</option>`;
            toast.error(err.message);
        }
    });
}

/* ---------------------------------------------------------------------------
 * KHỞI ĐỘNG TRANG
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Đơn hàng" })) {
    initFilterOptions();
    applyFilters();

    btnAdd.addEventListener("click", openCreateOrderModal);
    btnReload.addEventListener("click", () => loadOrders({ keepPage: true }));

    const onSearch = debounce(text => table.search(text), 300);
    searchInput.addEventListener("input", () => onSearch(searchInput.value));

    statusFilter.addEventListener("change", applyFilters);
    paymentFilter.addEventListener("change", applyFilters);

    await loadOrders();
}
