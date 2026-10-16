/*
 * ============================================================================
 *  customers.js  -  XỬ LÝ CHO TRANG admin/customers/customers.html
 * ============================================================================
 *
 *  Trang làm các việc:
 *    1. Tải danh sách khách hàng + danh sách hạng thành viên.
 *    2. Hiện bảng có tìm kiếm, lọc theo hạng, phân trang.
 *    3. Nút: Xem (sang trang chi tiết), Sửa, Xóa (chỉ Admin), Thêm (chỉ Admin).
 *
 *  Đường dẫn import: file này ở  wwwroot/admin/js/customers/
 *      "../../../" = lùi 3 thư mục -> wwwroot/
 *      "../layout.js" = wwwroot/admin/js/layout.js
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { customerApi } from "../../../assets/js/api/customerApi.js";
import { membershipApi } from "../../../assets/js/api/membershipApi.js";
import { isAdmin } from "../../../assets/js/core/auth.js";
import { escapeHtml, formatNumber, debounce } from "../../../assets/js/core/utils.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";
import { openCustomerForm } from "./customer-form.js";

/* ---------------------------------------------------------------------------
 * Lấy các thẻ HTML. Chú thích @type để VS Code biết chắc loại thẻ (hết báo "possibly null").
 * ------------------------------------------------------------------------- */
const tableContainer = /** @type {HTMLDivElement} */ (document.getElementById("tableContainer"));
const paginationContainer = /** @type {HTMLDivElement} */ (document.getElementById("paginationContainer"));
const searchInput = /** @type {HTMLInputElement} */ (document.getElementById("searchInput"));
const tierFilter = /** @type {HTMLSelectElement} */ (document.getElementById("tierFilter"));
const btnAdd = /** @type {HTMLButtonElement} */ (document.getElementById("btnAdd"));
const btnReload = /** @type {HTMLButtonElement} */ (document.getElementById("btnReload"));

/** Danh sách hạng thành viên (tải 1 lần, dùng cho ô lọc và form Sửa). @type {any[]} */
let tiers = [];

/** Đường dẫn trang chi tiết của một khách. @param {number} id */
const detailUrl = id => `customer-detail.html?id=${id}`;

/* ---------------------------------------------------------------------------
 * BẢNG
 * ------------------------------------------------------------------------- */
const table = createDataTable({
    container: tableContainer,
    paginationContainer,
    searchFields: ["fullName", "email", "id"],   // gõ tên, email hoặc mã đều tìm được
    sortBy: (a, b) => b.id - a.id,               // khách mới nhất lên đầu
    emptyText: "Chưa có khách hàng nào.",
    columns: [
        { key: "id", title: "Mã", width: "70px" },
        {
            key: "fullName", title: "Khách hàng",
            // Tên (bấm vào sang trang chi tiết) + email nhỏ bên dưới
            render: row => `
                <a class="fw-bold" href="${detailUrl(row.id)}">${escapeHtml(row.fullName)}</a>
                <div class="text-muted text-sm">${escapeHtml(row.email ?? "")}</div>`
        },
        {
            key: "membershipTierName", title: "Hạng",
            render: row => row.membershipTierName
                ? `<span class="badge badge-primary">${escapeHtml(row.membershipTierName)}</span>`
                : `<span class="text-muted">—</span>`
        },
        { key: "totalPoints", title: "Điểm", align: "right", render: row => formatNumber(row.totalPoints) },
        { key: "addressCount", title: "Địa chỉ", align: "right", render: row => formatNumber(row.addressCount) },
        { key: "wishlistCount", title: "Yêu thích", align: "right", render: row => formatNumber(row.wishlistCount) }
    ],
    actions: [
        { label: "Xem", onClick: row => { window.location.href = detailUrl(row.id); } },
        { label: "Sửa", onClick: row => openCustomerForm({ customer: row, tiers, onSaved: () => loadCustomers({ keepPage: true }) }) },
        // visible: chỉ hiện nút Xóa cho Admin (Employee gọi API xóa cũng bị server chặn 403)
        { label: "Xóa", danger: true, visible: () => isAdmin(), onClick: row => removeCustomer(row) }
    ]
});

/* ---------------------------------------------------------------------------
 * TẢI DỮ LIỆU
 * ------------------------------------------------------------------------- */

/**
 * Tải danh sách khách hàng và đổ vào bảng.
 * @param {{ keepPage?: boolean }} [options] - keepPage: true = ở nguyên trang hiện tại (sau khi Sửa/Xóa)
 */
async function loadCustomers({ keepPage = false } = {}) {
    table.setLoading();
    try {
        table.setRows(await customerApi.getAll(), { keepPage });
    } catch (err) {
        table.setError(err.message);
    }
}

/** Tải danh sách hạng rồi đổ vào ô "Lọc theo hạng". Lỗi thì bỏ qua (trang vẫn dùng được). */
async function loadTiers() {
    try {
        tiers = await membershipApi.getAll();
        tierFilter.innerHTML = `<option value="">Tất cả hạng</option>` + [...tiers]
            .sort((a, b) => a.minPoints - b.minPoints)
            .map(t => `<option value="${t.id}">${escapeHtml(t.tierName)}</option>`)
            .join("");
    } catch (err) {
        console.warn("Không tải được hạng thành viên:", err);
    }
}

/* ---------------------------------------------------------------------------
 * XÓA
 * ------------------------------------------------------------------------- */

/** @param {any} row - khách hàng cần xóa */
async function removeCustomer(row) {
    const ok = await confirmDialog({
        title: "Xóa khách hàng",
        message: `Xóa khách hàng "${row.fullName}"?\n`
            + "Tài khoản đăng nhập, địa chỉ, giỏ hàng và lịch sử điểm của khách cũng bị xóa theo.\n"
            + "Khách đã có đơn hàng thì không xóa được.",
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await customerApi.remove(row.id);
        toast.success("Đã xóa khách hàng.");
        await loadCustomers({ keepPage: true });
    } catch (err) {
        toast.error(err.message); // ví dụ: "Khách hàng đã có đơn hàng, không thể xóa."
    }
}

/* ---------------------------------------------------------------------------
 * KHỞI ĐỘNG TRANG
 * ------------------------------------------------------------------------- */

if (await initAdminLayout({ title: "Khách hàng" })) {
    // Nút Thêm chỉ hiện cho Admin
    if (isAdmin()) {
        btnAdd.classList.remove("hidden");
        btnAdd.addEventListener("click", () =>
            openCustomerForm({ tiers, onSaved: () => loadCustomers() }));
    }

    btnReload.addEventListener("click", () => loadCustomers({ keepPage: true }));

    // Gõ tìm kiếm: chờ ngừng gõ 300ms rồi mới lọc
    const onSearch = debounce(text => table.search(text), 300);
    searchInput.addEventListener("input", () => onSearch(searchInput.value));

    // Lọc theo hạng: chọn "Tất cả hạng" (giá trị rỗng) thì bỏ lọc
    tierFilter.addEventListener("change", () => {
        const tierId = Number(tierFilter.value);
        table.setFilter(tierId ? row => row.membershipTierId === tierId : null);
    });

    // Tải hạng và khách hàng cùng lúc cho nhanh
    await Promise.all([loadTiers(), loadCustomers()]);
}
