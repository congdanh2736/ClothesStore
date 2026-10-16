/*
 * ============================================================================
 *  customer-detail.js  -  XỬ LÝ CHO TRANG admin/customers/customer-detail.html
 * ============================================================================
 *
 *  Luồng chạy:
 *    1. Đọc id khách trên thanh địa chỉ (?id=5). Không có id -> báo lỗi.
 *    2. Tải cùng lúc: thông tin khách, địa chỉ, lịch sử điểm, đơn hàng, hạng thành viên.
 *    3. Vẽ 4 ô số liệu + 3 bảng (địa chỉ, điểm, đơn hàng).
 *    4. Các nút thêm / sửa / xóa gọi API rồi tải lại phần liên quan.
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/customers/ -> "../../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout, setPageTitle } from "../layout.js";
import { customerApi, addressApi, loyaltyApi } from "../../../assets/js/api/customerApi.js";
import { membershipApi } from "../../../assets/js/api/membershipApi.js";
import {
    escapeHtml, formatMoney, formatNumber, formatDateTime, getIdFromUrl,
    orderStatusBadge, paymentStatusBadge
} from "../../../assets/js/core/utils.js";
import { renderTable, createDataTable } from "../../../assets/js/components/table.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";
import { openCustomerForm } from "./customer-form.js";

/* ---------------------------------------------------------------------------
 * Lấy các thẻ HTML (chú thích @type để VS Code biết chắc loại thẻ)
 * ------------------------------------------------------------------------- */
/** Lấy một thẻ theo id. Viết hàm ngắn cho gọn, khỏi lặp lại document.getElementById. @param {string} id */
const $ = id => /** @type {HTMLElement} */ (document.getElementById(id));

const pageError = $("pageError");
const pageContent = $("pageContent");

/* ---------------------------------------------------------------------------
 * DỮ LIỆU CỦA TRANG
 * ------------------------------------------------------------------------- */
// Số id trên URL. Không hợp lệ thì getIdFromUrl() trả null -> đổi thành 0 ("?? 0")
// để biến luôn là SỐ (VS Code khỏi báo lỗi kiểu). Bên dưới kiểm tra: id = 0 thì báo lỗi.
const customerId = getIdFromUrl() ?? 0;
/** @type {any} */  let customer = null;   // thông tin khách đang xem
/** @type {any[]} */ let tiers = [];       // danh sách hạng (cho form Sửa)

/* ===========================================================================
 *  PHẦN 1: THÔNG TIN KHÁCH + Ô SỐ LIỆU
 * ========================================================================= */

/** Tải lại thông tin khách (tên, email, hạng, điểm, số địa chỉ) rồi vẽ lên đầu trang. */
async function loadCustomer() {
    customer = await customerApi.getById(customerId);

    $("customerName").textContent = customer.fullName || `Khách hàng #${customer.id}`;
    $("customerEmail").textContent = `Mã KH: #${customer.id}` + (customer.email ? ` · ${customer.email}` : "");
    $("statTier").textContent = customer.membershipTierName ?? "—";
    $("statPoints").textContent = formatNumber(customer.totalPoints);
    $("statAddresses").textContent = formatNumber(customer.addressCount);
    setPageTitle(customer.fullName || "Chi tiết khách hàng");
}

/* ===========================================================================
 *  PHẦN 2: ĐỊA CHỈ GIAO HÀNG
 * ========================================================================= */

/** @type {any[]} */ let addresses = [];

/** Vẽ bảng địa chỉ (ít dòng nên không cần phân trang). */
function drawAddresses() {
    renderTable($("addressTable"), {
        rows: addresses,
        emptyText: "Khách chưa lưu địa chỉ nào.",
        columns: [
            { key: "street", title: "Số nhà, đường" },
            { key: "district", title: "Quận / Huyện" },
            { key: "city", title: "Tỉnh / Thành phố" },
            { key: "country", title: "Quốc gia" },
            { key: "orderCount", title: "Số đơn đã dùng", align: "right", render: row => formatNumber(row.orderCount) }
        ],
        actions: [
            { label: "Sửa", onClick: row => openAddressForm(row) },
            { label: "Xóa", danger: true, onClick: row => removeAddress(row) }
        ]
    });
}

async function loadAddresses() {
    addresses = await addressApi.getByCustomer(customerId);
    drawAddresses();
}

/**
 * Form thêm / sửa địa chỉ.
 * @param {any} [address] - địa chỉ đang sửa; bỏ trống = thêm mới
 */
function openAddressForm(address = null) {
    const isEdit = address !== null;

    formModal({
        title: isEdit ? "Sửa địa chỉ" : "Thêm địa chỉ",
        columns: 2,
        values: address ?? { country: "Việt Nam" },   // thêm mới: điền sẵn quốc gia
        fields: [
            { name: "street", label: "Số nhà, tên đường", required: true, fullWidth: true,
              placeholder: "Ví dụ: 12 Nguyễn Huệ" },
            { name: "district", label: "Quận / Huyện", required: true, placeholder: "Quận 1" },
            { name: "city", label: "Tỉnh / Thành phố", required: true, placeholder: "TP. Hồ Chí Minh" },
            { name: "country", label: "Quốc gia", required: true }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Thêm địa chỉ",
        onSubmit: async data => {
            // Gắn thêm id khách vào dữ liệu gửi đi (form không có ô này)
            const payload = { ...data, customerId };
            if (isEdit) {
                await addressApi.update(address.id, payload);
                toast.success("Đã cập nhật địa chỉ.");
            } else {
                await addressApi.create(payload);
                toast.success("Đã thêm địa chỉ.");
            }
            // Số địa chỉ ở ô số liệu cũng đổi -> tải lại cả thông tin khách
            await Promise.all([loadAddresses(), loadCustomer()]);
        }
    });
}

/** @param {any} address */
async function removeAddress(address) {
    const ok = await confirmDialog({
        title: "Xóa địa chỉ",
        message: `Xóa địa chỉ "${address.street}, ${address.district}, ${address.city}"?`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await addressApi.remove(address.id);
        toast.success("Đã xóa địa chỉ.");
        await Promise.all([loadAddresses(), loadCustomer()]);
    } catch (err) {
        // Ví dụ địa chỉ đã dùng cho đơn hàng -> server báo lỗi ràng buộc dữ liệu
        toast.error(err.message);
    }
}

/* ===========================================================================
 *  PHẦN 3: LỊCH SỬ ĐIỂM TÍCH LŨY
 * ========================================================================= */

/** Hiện số điểm có dấu và màu: +120 (xanh), -50 (đỏ). @param {number} points */
function pointsCell(points) {
    const sign = points > 0 ? "+" : "";
    const color = points > 0 ? "text-success" : "text-danger";
    return `<span class="fw-bold ${color}">${sign}${formatNumber(points)}</span>`;
}

const loyaltyTable = createDataTable({
    container: $("loyaltyTable"),
    paginationContainer: $("loyaltyPagination"),
    pageSize: 5,
    sortBy: (a, b) => new Date(b.txnDate).getTime() - new Date(a.txnDate).getTime(), // mới nhất lên đầu
    emptyText: "Chưa có giao dịch điểm nào.",
    columns: [
        { key: "txnDate", title: "Thời gian", width: "170px", render: row => formatDateTime(row.txnDate) },
        { key: "pointsChange", title: "Điểm", align: "right", width: "110px", render: row => pointsCell(row.pointsChange) },
        { key: "reason", title: "Lý do" }
    ],
    actions: [
        { label: "Sửa", onClick: row => openPointsForm(row) },
        { label: "Xóa", danger: true, onClick: row => removePoints(row) }
    ]
});

/** @param {{ keepPage?: boolean }} [options] */
async function loadLoyalty({ keepPage = false } = {}) {
    loyaltyTable.setRows(await loyaltyApi.getByCustomer(customerId), { keepPage });
}

/**
 * Form cộng / trừ điểm.
 * @param {any} [txn] - giao dịch đang sửa; bỏ trống = thêm mới
 */
function openPointsForm(txn = null) {
    const isEdit = txn !== null;

    formModal({
        title: isEdit ? "Sửa giao dịch điểm" : "Cộng / trừ điểm",
        description: "Nhập số dương để CỘNG điểm, số âm để TRỪ điểm (ví dụ: -50). Hạng thành viên sẽ được xét lại tự động.",
        values: txn ?? {},
        fields: [
            { name: "pointsChange", label: "Số điểm", type: "number", required: true, step: 1,
              placeholder: "Ví dụ: 100 hoặc -50" },
            { name: "reason", label: "Lý do", type: "textarea", placeholder: "Ví dụ: Tặng điểm sinh nhật" }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Lưu",
        onSubmit: async data => {
            if (!data.pointsChange) {
                // Kiểm tra trước ở trình duyệt cho nhanh (server cũng chặn số 0)
                throw new Error("Số điểm phải khác 0.");
            }

            const payload = {
                customerId,
                pointsChange: data.pointsChange,
                reason: data.reason || null,
                // Thời điểm giao dịch:
                //  - SỬA: giữ nguyên thời điểm cũ
                //  - THÊM: lấy "bây giờ" lùi 1 giây (server không nhận thời điểm ở tương lai,
                //    lùi 1 giây cho chắc nếu đồng hồ máy lệch một chút)
                txnDate: isEdit ? txn.txnDate : new Date(Date.now() - 1000).toISOString()
            };

            if (isEdit) {
                await loyaltyApi.update(txn.id, payload);
                toast.success("Đã cập nhật giao dịch điểm.");
            } else {
                await loyaltyApi.create(payload);
                toast.success(data.pointsChange > 0 ? "Đã cộng điểm." : "Đã trừ điểm.");
            }
            // Tổng điểm + hạng thay đổi -> tải lại cả thông tin khách
            await Promise.all([loadLoyalty({ keepPage: isEdit }), loadCustomer()]);
        }
    });
}

/** @param {any} txn */
async function removePoints(txn) {
    const ok = await confirmDialog({
        title: "Xóa giao dịch điểm",
        message: `Xóa giao dịch ${txn.pointsChange > 0 ? "+" : ""}${txn.pointsChange} điểm?\nTổng điểm và hạng của khách sẽ được tính lại.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await loyaltyApi.remove(txn.id);
        toast.success("Đã xóa giao dịch điểm.");
        await Promise.all([loadLoyalty({ keepPage: true }), loadCustomer()]);
    } catch (err) {
        toast.error(err.message);
    }
}

/* ===========================================================================
 *  PHẦN 4: ĐƠN HÀNG (chỉ xem)
 * ========================================================================= */

const orderTable = createDataTable({
    container: $("orderTable"),
    paginationContainer: $("orderPagination"),
    pageSize: 5,
    sortBy: (a, b) => b.id - a.id,
    emptyText: "Khách chưa có đơn hàng nào.",
    columns: [
        // Trang chi tiết đơn là của Nhóm 4 (admin/sales/order-detail.html)
        { key: "id", title: "Mã đơn", width: "90px",
          render: row => `<a class="fw-bold" href="../sales/order-detail.html?id=${row.id}">#${row.id}</a>` },
        { key: "orderDate", title: "Ngày đặt", render: row => formatDateTime(row.orderDate) },
        { key: "status", title: "Trạng thái", render: row => orderStatusBadge(row.status) },
        { key: "paymentStatus", title: "Thanh toán", render: row => paymentStatusBadge(row.paymentStatus) },
        { key: "totalAmount", title: "Tổng tiền", align: "right", render: row => formatMoney(row.totalAmount) }
    ]
});

async function loadOrders() {
    const orders = await customerApi.getOrders(customerId);
    orderTable.setRows(orders);
    $("statOrders").textContent = formatNumber(orders.length);
}

/* ===========================================================================
 *  KHỞI ĐỘNG TRANG
 * ========================================================================= */

/** Hiện lỗi to ở đầu trang (dùng khi không mở được trang). @param {string} message */
function showPageError(message) {
    pageError.textContent = message;
    pageContent.classList.add("hidden");
}

// activeMenu: để mục "Khách hàng" trên menu vẫn sáng khi đang ở trang chi tiết
if (await initAdminLayout({ title: "Chi tiết khách hàng", activeMenu: "/admin/customers/customers.html" })) {
    if (!customerId) {
        showPageError("Thiếu mã khách hàng trên địa chỉ trang (ví dụ: customer-detail.html?id=5). Hãy mở từ trang Danh sách khách hàng.");
    } else {
        try {
            // Phải có thông tin khách trước (404 = không tồn tại)
            await loadCustomer();
            pageContent.classList.remove("hidden");

            // Gắn các nút
            $("btnEditCustomer").addEventListener("click", () =>
                openCustomerForm({ customer, tiers, onSaved: loadCustomer }));
            $("btnAddAddress").addEventListener("click", () => openAddressForm());
            $("btnAddPoints").addEventListener("click", () => openPointsForm());

            // Các phần còn lại tải cùng lúc. Phần nào lỗi thì chỉ báo lỗi phần đó.
            loyaltyTable.setLoading();
            orderTable.setLoading();
            await Promise.all([
                loadAddresses().catch(err => toast.error(`Không tải được địa chỉ: ${err.message}`)),
                loadLoyalty().catch(err => loyaltyTable.setError(err.message)),
                loadOrders().catch(err => orderTable.setError(err.message)),
                membershipApi.getAll().then(list => { tiers = list; }).catch(() => { /* chỉ ảnh hưởng ô chọn hạng */ })
            ]);
        } catch (err) {
            showPageError(err.status === 404 ? `Không tìm thấy khách hàng #${customerId}.` : err.message);
        }
    }
}
