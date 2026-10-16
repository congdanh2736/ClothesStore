/*
 * ============================================================================
 *  employees.js  -  XỬ LÝ CHO TRANG admin/employees/employees.html
 * ============================================================================
 *
 *  Trang CHỈ dành cho Admin: initAdminLayout({ adminOnly: true }).
 *
 *  Luồng chạy:
 *    1. Tải cùng lúc danh sách nhân viên + danh sách cửa hàng.
 *    2. Hiện bảng có tìm kiếm, lọc theo cửa hàng, phân trang.
 *    3. Thêm (kèm email + mật khẩu), Sửa (họ tên, cửa hàng), Xóa.
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/employees/ -> "../../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { employeeApi } from "../../../assets/js/api/employeeApi.js";
import { getEmployeeId } from "../../../assets/js/core/auth.js";
import { escapeHtml, debounce } from "../../../assets/js/core/utils.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

/* ---------------------------------------------------------------------------
 * Lấy các thẻ HTML (chú thích @type để VS Code biết chắc loại thẻ)
 * ------------------------------------------------------------------------- */
const tableContainer = /** @type {HTMLDivElement} */ (document.getElementById("tableContainer"));
const paginationContainer = /** @type {HTMLDivElement} */ (document.getElementById("paginationContainer"));
const searchInput = /** @type {HTMLInputElement} */ (document.getElementById("searchInput"));
const storeFilter = /** @type {HTMLSelectElement} */ (document.getElementById("storeFilter"));
const btnAdd = /** @type {HTMLButtonElement} */ (document.getElementById("btnAdd"));
const btnReload = /** @type {HTMLButtonElement} */ (document.getElementById("btnReload"));

/** Danh sách cửa hàng (cho ô lọc và ô chọn trong form). @type {any[]} */
let stores = [];

/** Biến danh sách cửa hàng thành lựa chọn cho ô <select> của form. */
const storeOptions = () => stores.map(s => ({
    value: s.id,
    label: s.location ? `${s.name} – ${s.location}` : s.name
}));

/* ---------------------------------------------------------------------------
 * BẢNG
 * ------------------------------------------------------------------------- */
const table = createDataTable({
    container: tableContainer,
    paginationContainer,
    searchFields: ["fullName", "email", "storeName", "id"],
    sortBy: (a, b) => b.id - a.id,
    emptyText: "Chưa có nhân viên nào.",
    columns: [
        { key: "id", title: "Mã", width: "70px" },
        {
            key: "fullName", title: "Nhân viên",
            render: row => `
                <div class="fw-bold">${escapeHtml(row.fullName)}</div>
                <div class="text-muted text-sm">${escapeHtml(row.email ?? "")}</div>`
        },
        {
            key: "storeName", title: "Cửa hàng",
            render: row => row.storeName
                ? escapeHtml(row.storeName)
                : `<span class="badge badge-secondary">Không thuộc cửa hàng</span>`
        }
    ],
    actions: [
        { label: "Sửa", onClick: row => openForm(row) },
        // Không cho tự xóa chính mình (nếu tài khoản Admin cũng có hồ sơ nhân viên)
        { label: "Xóa", danger: true, visible: row => row.id !== getEmployeeId(), onClick: row => removeEmployee(row) }
    ]
});

/* ---------------------------------------------------------------------------
 * TẢI DỮ LIỆU
 * ------------------------------------------------------------------------- */

/** @param {{ keepPage?: boolean }} [options] */
async function loadEmployees({ keepPage = false } = {}) {
    table.setLoading();
    try {
        table.setRows(await employeeApi.getAll(), { keepPage });
    } catch (err) {
        table.setError(err.message);
    }
}

/** Tải danh sách cửa hàng rồi đổ vào ô lọc. Lỗi thì bỏ qua (form vẫn chọn được "Không thuộc cửa hàng"). */
async function loadStores() {
    try {
        stores = await employeeApi.getStores();
        stores.sort((a, b) => String(a.name).localeCompare(String(b.name), "vi"));
        storeFilter.innerHTML =
            `<option value="">Tất cả cửa hàng</option><option value="none">Không thuộc cửa hàng</option>`
            + stores.map(s => `<option value="${s.id}">${escapeHtml(s.name)}</option>`).join("");
    } catch (err) {
        console.warn("Không tải được danh sách cửa hàng:", err);
    }
}

/* ---------------------------------------------------------------------------
 * THÊM / SỬA
 * ------------------------------------------------------------------------- */

/** @param {any} [employee] - nhân viên đang sửa; bỏ trống = thêm mới */
function openForm(employee = null) {
    const isEdit = employee !== null; // isEdit giúp form biết là đang sửa hay thêm mới bằng cách kiểm tra có nhân viên truyền vào hay không

    // Quy ước của backend: firstName = HỌ (và tên đệm), lastName = TÊN
    // "@type {any[]}": mảng chứa các ô nhập đủ kiểu (chữ, chọn, mật khẩu...) -> báo VS Code đừng bắt bẻ
    /** @type {any[]} */
    const fields = [
        { name: "firstName", label: "Họ và tên đệm", required: true, placeholder: "Ví dụ: Trần Thị" },
        { name: "lastName", label: "Tên", required: true, placeholder: "Ví dụ: Bình" }
    ];

    if (!isEdit) {
        // Thêm mới: cần email + mật khẩu để tạo tài khoản đăng nhập
        fields.push(
            { name: "email", label: "Email đăng nhập", type: "email", required: true, fullWidth: true,
              placeholder: "nhanvien@clothesstore.com" },
            { name: "password", label: "Mật khẩu", type: "password", required: true, fullWidth: true,
              hint: "Ít nhất 6 ký tự, có chữ HOA, chữ thường và chữ số. Ví dụ: NhanVien123" }
        );
    }

    fields.push({
        name: "storeId", label: "Cửa hàng làm việc", type: "select", valueType: "number", fullWidth: true,
        emptyOption: "-- Không thuộc cửa hàng nào --",
        options: storeOptions(),
        hint: "Bỏ trống nếu là nhân viên quản trị website, không đứng ở cửa hàng."
    });

    formModal({
        title: isEdit ? `Sửa nhân viên #${employee.id}` : "Thêm nhân viên", 
        description: isEdit ? "Email và mật khẩu đăng nhập không đổi được ở đây." : "Hệ thống sẽ tạo luôn tài khoản đăng nhập với vai trò Nhân viên.",
        columns: 2,
        values: employee ?? {},       // SỬA: điền sẵn họ, tên, cửa hàng hiện tại
        fields,
        submitText: isEdit ? "Lưu thay đổi" : "Thêm nhân viên",
        onSubmit: async data => {
            if (isEdit) {
                await employeeApi.update(employee.id, data);
                toast.success("Đã cập nhật nhân viên.");
            } else {
                await employeeApi.create(data);
                toast.success(`Đã thêm nhân viên ${data.firstName} ${data.lastName}.`);
            }
            await loadEmployees({ keepPage: isEdit });
        }
    });
}

/* ---------------------------------------------------------------------------
 * XÓA
 * ------------------------------------------------------------------------- */

/** @param {any} employee */
async function removeEmployee(employee) {
    const ok = await confirmDialog({
        title: "Xóa nhân viên",
        message: `Xóa nhân viên "${employee.fullName}"?\nTài khoản đăng nhập của nhân viên này cũng bị xóa, không đăng nhập được nữa.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await employeeApi.remove(employee.id);
        toast.success("Đã xóa nhân viên.");
        await loadEmployees({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * KHỞI ĐỘNG TRANG  (adminOnly: Employee vào sẽ bị đưa về dashboard)
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Nhân viên", adminOnly: true })) {
    btnAdd.addEventListener("click", () => openForm());
    btnReload.addEventListener("click", () => loadEmployees({ keepPage: true }));

    const onSearch = debounce(text => table.search(text), 300);
    searchInput.addEventListener("input", () => onSearch(searchInput.value));

    // Lọc theo cửa hàng: "" = tất cả, "none" = không thuộc cửa hàng, còn lại = id cửa hàng
    storeFilter.addEventListener("change", () => {
        const value = storeFilter.value;
        if (!value) table.setFilter(null);
        else if (value === "none") table.setFilter(row => !row.storeId);
        else table.setFilter(row => row.storeId === Number(value));
    });

    await Promise.all([loadStores(), loadEmployees()]);
}
