/*
 * ============================================================================
 *  size-charts.js - Quản lý Bảng size (Nhóm 2)
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { escapeHtml } from "../../../assets/js/core/utils.js";
import { sizeChartApi, categoryApi } from "../../../assets/js/api/catalogApi.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

// Lấy các thẻ HTML trên trang
const tableContainer = document.getElementById("tableContainer");
const paginationContainer = document.getElementById("paginationContainer");
const searchInput = document.getElementById("searchInput");
const categoryFilter = document.getElementById("categoryFilter");
const btnAdd = document.getElementById("btnAdd");

// Danh sách danh mục để đổ vào ô lọc và form
let categories = [];

/* ---------------------------------------------------------------------------
 * 1. Cấu hình bảng dữ liệu Bảng size
 * ------------------------------------------------------------------------- */
const table = createDataTable({
    container: tableContainer,
    paginationContainer: paginationContainer,
    searchFields: ["sizeLabel", "measurements", "categoryName"],
    sortBy: (a, b) => b.id - a.id,
    emptyText: "Chưa có bảng size nào.",
    columns: [
        { key: "id", title: "Mã", width: "70px" },
        {
            key: "sizeLabel",
            title: "Kích cỡ (Size)",
            width: "120px",
            render: row => `<span class="badge badge-primary fw-bold">${escapeHtml(row.sizeLabel ?? "")}</span>`
        },
        {
            key: "categoryName",
            title: "Áp dụng danh mục",
            width: "180px",
            render: row => row.categoryName
                ? `<span class="badge badge-secondary">${escapeHtml(row.categoryName)}</span>`
                : `<span class="text-muted">Chung</span>`
        },
        {
            key: "measurements",
            title: "Thông số chi tiết (Dài, rộng, vòng ngực, eo...)",
            render: row => `<span class="text-sm">${escapeHtml(row.measurements ?? "Chưa có thông số")}</span>`
        }
    ],
    actions: [
        { label: "Sửa", onClick: row => openForm(row) },
        { label: "Xóa", danger: true, onClick: row => removeRow(row) }
    ]
});

/* ---------------------------------------------------------------------------
 * 2. Tải danh mục (dùng cho ô lọc và Form)
 * ------------------------------------------------------------------------- */
async function loadCategories() {
    try {
        const data = await categoryApi.getAll();
        categories = Array.isArray(data) ? data : [];

        if (categoryFilter) {
            categoryFilter.innerHTML = '<option value="">-- Tất cả danh mục --</option>' +
                categories.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("");
        }
    } catch (err) {
        console.error("Không tải được danh mục:", err);
    }
}

/* ---------------------------------------------------------------------------
 * 3. Tải danh sách Bảng size từ API
 * ------------------------------------------------------------------------- */
async function loadData({ keepPage = false } = {}) {
    table.setLoading();
    try {
        const data = await sizeChartApi.getAll();
        table.setRows(data, { keepPage });
    } catch (err) {
        table.setError("Lỗi khi tải bảng size: " + err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 4. Hộp thoại Popup Thêm / Sửa
 * ------------------------------------------------------------------------- */
function openForm(row = null) {
    const isEdit = row !== null;

    const categoryOptions = categories.map(c => ({
        value: c.id,
        label: c.name
    }));

    formModal({
        title: isEdit ? `Sửa size "${row.sizeLabel}"` : "Thêm thông số size mới",
        values: row ?? {},
        fields: [
            {
                name: "categoryId",
                label: "Danh mục áp dụng",
                type: "select",
                valueType: "number",
                required: true,
                emptyOption: "-- Chọn danh mục --",
                options: categoryOptions
            },
            {
                name: "sizeLabel",
                label: "Tên kích cỡ",
                required: true,
                placeholder: "Ví dụ: S, M, L, XL, 2XL, 29, 30, 31..."
            },
            {
                name: "measurements",
                label: "Thông số chi tiết",
                required: true,
                placeholder: "Ví dụ: Dài áo 68cm, Ngực 96cm, Dài tay 20cm (hoặc Vòng eo 75cm, Dài quần 98cm)..."
            }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Thêm size",
        onSubmit: async data => {
            if (isEdit) {
                await sizeChartApi.update(row.id, data);
                toast.success("Cập nhật bảng size thành công!");
            } else {
                await sizeChartApi.create(data);
                toast.success("Thêm mới size thành công!");
            }
            await loadData({ keepPage: isEdit });
        }
    });
}

/* ---------------------------------------------------------------------------
 * 5. Xóa
 * ------------------------------------------------------------------------- */
async function removeRow(row) {
    const ok = await confirmDialog({
        title: "Xác nhận xóa",
        message: `Bạn có chắc muốn xóa size "${row.sizeLabel}" của danh mục "${row.categoryName ?? ""}"?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await sizeChartApi.remove(row.id);
        toast.success("Đã xóa size thành công.");
        await loadData({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 6. Khởi động trang
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Bảng size" })) {
    await loadCategories();
    await loadData();

    btnAdd?.addEventListener("click", () => openForm());

    searchInput?.addEventListener("input", () => {
        table.search(searchInput.value);
    });

    categoryFilter?.addEventListener("change", () => {
        const selectedId = Number(categoryFilter.value);
        if (selectedId) {
            table.setFilter(row => row.categoryId === selectedId);
        } else {
            table.setFilter(null);
        }
    });
}
