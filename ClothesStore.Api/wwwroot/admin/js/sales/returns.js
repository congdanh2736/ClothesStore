/*
 * ============================================================================
 *  returns.js  -  XỬ LÝ DANH SÁCH YÊU CẦU ĐỔI / TRẢ HÀNG (admin/sales/returns.html)
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { returnApi } from "../../../assets/js/api/returnApi.js";
import {
    escapeHtml,
    formatMoney,
    formatDateTime,
    debounce,
    returnStatusBadge
} from "../../../assets/js/core/utils.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

const tableContainer = /** @type {HTMLDivElement} */ (document.getElementById("tableContainer"));
const paginationContainer = /** @type {HTMLDivElement} */ (document.getElementById("paginationContainer"));
const searchInput = /** @type {HTMLInputElement} */ (document.getElementById("searchInput"));
const statusFilter = /** @type {HTMLSelectElement} */ (document.getElementById("statusFilter"));
const btnReload = /** @type {HTMLButtonElement} */ (document.getElementById("btnReload"));

/** @type {any[]} */
let allReturns = [];

const columns = [
    {
        key: "id",
        title: "Mã YC",
        width: "90px",
        render: row => `<span class="fw-bold">#${row.id}</span>`
    },
    {
        key: "orderId",
        title: "Đơn hàng",
        width: "110px",
        render: row => `<span class="badge badge-secondary">#${row.orderId}</span>`
    },
    {
        key: "customerId",
        title: "Khách hàng",
        width: "120px",
        render: row => `<span>KH #${row.customerId}</span>`
    },
    {
        key: "requestDate",
        title: "Ngày gửi",
        width: "160px",
        render: row => formatDateTime(row.requestDate)
    },
    {
        key: "totalRefund",
        title: "Tiền hoàn",
        align: "right",
        width: "130px",
        render: row => `<span class="text-danger fw-bold">${formatMoney(row.totalRefund)}</span>`
    },
    {
        key: "status",
        title: "Trạng thái",
        width: "130px",
        render: row => returnStatusBadge(row.status)
    },
    {
        key: "reason",
        title: "Lý do",
        render: row => `<span class="text-muted text-sm">${escapeHtml(row.reason)}</span>`
    }
];

const table = createDataTable({
    container: tableContainer,
    paginationContainer: paginationContainer,
    columns,
    searchFields: ["id", "orderId", "customerId", "reason"],
    sortBy: (a, b) => b.id - a.id,
    emptyText: "Chưa có yêu cầu đổi/trả nào.",
    actions: [
        {
            label: "Chi tiết",
            onClick: row => {
                window.location.href = `return-detail.html?id=${row.id}`;
            }
        },
        {
            label: "Xóa",
            danger: true,
            visible: row => row.status === "Pending" || row.status === "Rejected",
            onClick: row => removeReturn(row)
        }
    ]
});

function applyFilter({ keepPage = false } = {}) {
    const selected = statusFilter.value;
    const filtered = selected
        ? allReturns.filter(r => r.status === selected)
        : allReturns;

    table.setRows(filtered, { keepPage });
    if (searchInput.value.trim()) {
        table.search(searchInput.value.trim());
    }
}

async function loadData({ keepPage = false } = {}) {
    table.setLoading();
    try {
        allReturns = await returnApi.getAll();
        applyFilter({ keepPage });
    } catch (err) {
        table.setError(err.message);
    }
}

async function removeReturn(row) {
    const ok = await confirmDialog({
        title: "Xóa yêu cầu đổi/trả",
        message: `Bạn có chắc muốn xóa yêu cầu #${row.id} của đơn #${row.orderId}?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await returnApi.remove(row.id);
        toast.success("Đã xóa yêu cầu đổi/trả.");
        await loadData({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/* ========================== KHỞI ĐỘNG TRANG ========================== */
if (await initAdminLayout({ title: "Đổi / trả hàng" })) {
    btnReload.addEventListener("click", () => loadData({ keepPage: true }));
    statusFilter.addEventListener("change", () => applyFilter());

    const onSearch = debounce(text => table.search(text), 300);
    searchInput.addEventListener("input", () => onSearch(searchInput.value));

    loadData();
}
