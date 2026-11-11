/*
 * ============================================================================
 *  return-detail.js  -  CHI TIẾT VÀ XỬ LÝ YÊU CẦU ĐỔI / TRẢ (admin/sales/return-detail.html)
 * ============================================================================
 */

import { initAdminLayout, setPageTitle } from "../layout.js";
import { returnApi } from "../../../assets/js/api/returnApi.js";
import {
    escapeHtml,
    formatMoney,
    formatNumber,
    formatDateTime,
    getIdFromUrl,
    returnStatusBadge
} from "../../../assets/js/core/utils.js";
import { renderTable } from "../../../assets/js/components/table.js";
import { confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

const $ = id => /** @type {HTMLElement} */ (document.getElementById(id));

const pageError = $("pageError");
const pageContent = $("pageContent");

const returnTitle = $("returnTitle");
const returnSubtitle = $("returnSubtitle");
const statStatus = $("statStatus");
const statRefund = $("statRefund");
const statOrder = $("statOrder");
const statCustomer = $("statCustomer");
const returnReason = $("returnReason");
const statusNotice = $("statusNotice");
const itemsTable = $("itemsTable");

const btnApprove = $("btnApprove");
const btnComplete = $("btnComplete");
const btnReject = $("btnReject");
const btnDelete = $("btnDelete");

const returnId = getIdFromUrl();

/** @type {any} */
let currentRequest = null;

function showError(message) {
    pageError.textContent = message;
    pageError.classList.remove("hidden");
    pageContent.classList.add("hidden");
}

function updateButtons(status) {
    btnApprove.classList.add("hidden");
    btnComplete.classList.add("hidden");
    btnReject.classList.add("hidden");
    btnDelete.classList.add("hidden");

    if (status === "Pending") {
        btnApprove.classList.remove("hidden");
        btnReject.classList.remove("hidden");
        btnDelete.classList.remove("hidden");
    } else if (status === "Approved") {
        btnComplete.classList.remove("hidden");
        btnReject.classList.remove("hidden");
    } else if (status === "Rejected") {
        btnDelete.classList.remove("hidden");
    }
}

function updateStatusNotice(status) {
    let html = "";
    if (status === "Pending") {
        html = `<div class="alert alert-warning">Yêu cầu đang chờ duyệt. Nhân viên có thể chọn <strong>Duyệt yêu cầu</strong> hoặc <strong>Từ chối</strong>.</div>`;
    } else if (status === "Approved") {
        html = `<div class="alert alert-info">Yêu cầu đã được duyệt. Sau khi nhận lại hàng thực tế tại kho, hãy bấm <strong>Hoàn tất nhận hàng</strong> để tự động nhập kho, trừ điểm tích lũy và hoàn tiền.</div>`;
    } else if (status === "Completed") {
        html = `<div class="alert alert-success">Yêu cầu đã hoàn tất. Hàng đã được nhập lại kho và ghi nhận hoàn tiền thành công.</div>`;
    } else if (status === "Rejected") {
        html = `<div class="alert alert-danger">Yêu cầu đổi / trả này đã bị từ chối.</div>`;
    }
    statusNotice.innerHTML = html;
}

function drawItemsTable(items = []) {
    renderTable(itemsTable, {
        rows: items,
        emptyText: "Không có sản phẩm nào trong yêu cầu đổi/trả.",
        columns: [
            {
                key: "index",
                title: "#",
                width: "50px",
                render: (_, index) => `${index + 1}`
            },
            {
                key: "productName",
                title: "Sản phẩm",
                render: row => `
                    <div class="fw-bold">${escapeHtml(row.productName || `Biến thể #${row.variantId}`)}</div>
                    <div class="text-muted text-sm">Mã dòng đơn: #${row.orderItemId} · Mã biến thể: #${row.variantId}</div>
                `
            },
            {
                key: "variantInfo",
                title: "Phân loại",
                width: "150px",
                render: row => `${escapeHtml(row.color || "—")} / ${escapeHtml(row.size || "—")}`
            },
            {
                key: "condition",
                title: "Tình trạng hàng",
                width: "180px",
                render: row => escapeHtml(row.condition || "—")
            },
            {
                key: "quantity",
                title: "Số lượng trả",
                align: "right",
                width: "120px",
                render: row => formatNumber(row.quantity)
            },
            {
                key: "refundAmount",
                title: "Tiền hoàn",
                align: "right",
                width: "140px",
                render: row => `<span class="text-danger fw-bold">${formatMoney(row.refundAmount)}</span>`
            }
        ]
    });
}

async function loadDetail() {
    if (!returnId) {
        showError("Không tìm thấy mã yêu cầu đổi / trả trên địa chỉ trang (thiếu ?id=...).");
        return;
    }

    try {
        currentRequest = await returnApi.getById(returnId);
        if (!currentRequest) {
            showError(`Không tìm thấy yêu cầu đổi / trả #${returnId}.`);
            return;
        }

        pageError.classList.add("hidden");
        pageContent.classList.remove("hidden");

        returnTitle.textContent = `Yêu cầu đổi / trả #${currentRequest.id}`;
        returnSubtitle.textContent = `Mã đơn hàng: #${currentRequest.orderId} · Ngày gửi: ${formatDateTime(currentRequest.requestDate)}`;
        setPageTitle(`Chi tiết yêu cầu #${currentRequest.id}`);

        statStatus.innerHTML = returnStatusBadge(currentRequest.status);
        statRefund.textContent = formatMoney(currentRequest.totalRefund);
        statOrder.innerHTML = `<span class="badge badge-secondary">#${currentRequest.orderId}</span>`;
        statCustomer.textContent = `KH #${currentRequest.customerId}`;

        returnReason.textContent = currentRequest.reason || "Không có ghi chú lý do.";

        updateButtons(currentRequest.status);
        updateStatusNotice(currentRequest.status);
        drawItemsTable(currentRequest.items || []);
    } catch (err) {
        showError(err.message || "Không thể tải thông tin yêu cầu đổi / trả.");
    }
}

async function handleApprove() {
    const ok = await confirmDialog({
        title: "Duyệt yêu cầu đổi/trả",
        message: `Xác nhận duyệt yêu cầu đổi/trả #${returnId} cho đơn #${currentRequest.orderId}?`,
        confirmText: "Duyệt yêu cầu"
    });
    if (!ok) return;

    try {
        await returnApi.approve(returnId);
        toast.success("Đã duyệt yêu cầu đổi/trả thành công.");
        await loadDetail();
    } catch (err) {
        toast.error(err.message);
    }
}

async function handleReject() {
    const ok = await confirmDialog({
        title: "Từ chối yêu cầu đổi/trả",
        message: `Bạn có chắc muốn từ chối yêu cầu đổi/trả #${returnId}?\nKhách hàng sẽ không được hoàn tiền cho yêu cầu này.`,
        confirmText: "Từ chối",
        danger: true
    });
    if (!ok) return;

    try {
        await returnApi.reject(returnId);
        toast.success("Đã từ chối yêu cầu đổi/trả.");
        await loadDetail();
    } catch (err) {
        toast.error(err.message);
    }
}

async function handleComplete() {
    const ok = await confirmDialog({
        title: "Hoàn tất nhận hàng & Hoàn tiền",
        message: `Xác nhận đã nhận lại hàng cho yêu cầu #${returnId}?\n\nHệ thống sẽ tự động:\n- Nhập lại sản phẩm vào kho của cửa hàng\n- Ghi nhận số lượng hoàn trả vào thống kê\n- Trừ điểm tích lũy tương ứng của khách hàng\n- Cập nhật trạng thái hoàn tiền cho đơn hàng.`,
        confirmText: "Xác nhận hoàn tất"
    });
    if (!ok) return;

    try {
        await returnApi.complete(returnId);
        toast.success("Đã hoàn tất xử lý đổi/trả hàng.");
        await loadDetail();
    } catch (err) {
        toast.error(err.message);
    }
}

async function handleDelete() {
    const ok = await confirmDialog({
        title: "Xóa yêu cầu đổi/trả",
        message: `Bạn có chắc muốn xóa vĩnh viễn yêu cầu #${returnId}?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await returnApi.remove(returnId);
        toast.success("Đã xóa yêu cầu đổi/trả.");
        setTimeout(() => {
            window.location.href = "returns.html";
        }, 800);
    } catch (err) {
        toast.error(err.message);
    }
}

/* ========================== KHỞI ĐỘNG TRANG ========================== */
if (await initAdminLayout({ title: "Chi tiết yêu cầu đổi / trả" })) {
    btnApprove.addEventListener("click", handleApprove);
    btnReject.addEventListener("click", handleReject);
    btnComplete.addEventListener("click", handleComplete);
    btnDelete.addEventListener("click", handleDelete);

    await loadDetail();
}
