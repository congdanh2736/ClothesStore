/*
 * ============================================================================
 *  products.js - Quản lý Sản phẩm (Nhóm 2)
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { escapeHtml, formatMoney } from "../../../assets/js/core/utils.js";
import { productApi } from "../../../assets/js/api/productApi.js";
import { categoryApi } from "../../../assets/js/api/catalogApi.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

// Lấy các thẻ HTML trên trang
const tableContainer = document.getElementById("tableContainer");
const paginationContainer = document.getElementById("paginationContainer");
const searchInput = document.getElementById("searchInput");
const categoryFilter = document.getElementById("categoryFilter");
const btnAdd = document.getElementById("btnAdd");

// Biến lưu danh sách danh mục để dùng cho Form Thêm/Sửa và Ô lọc
let categories = [];

/* ---------------------------------------------------------------------------
 * 1. Cấu hình bảng dữ liệu Sản phẩm
 * ------------------------------------------------------------------------- */
const table = createDataTable({
    container: tableContainer,
    paginationContainer: paginationContainer,
    searchFields: ["name", "id", "categoryName"],
    sortBy: (a, b) => b.id - a.id, // Sản phẩm mới nhất lên đầu
    emptyText: "Chưa có sản phẩm nào.",
    columns: [
        { key: "id", title: "Mã", width: "70px" },
        {
            key: "mainProductImageUrl",
            title: "Hình ảnh",
            width: "90px",
            render: row => row.mainProductImageUrl
                ? `<img src="${escapeHtml(row.mainProductImageUrl)}" alt="${escapeHtml(row.name ?? "")}" style="width: 48px; height: 48px; object-fit: cover; border-radius: 6px; border: 1px solid var(--color-border);">`
                : `<span class="text-muted text-sm">Chưa có</span>`
        },
        {
            key: "name",
            title: "Tên sản phẩm",
            render: row => `<strong>${escapeHtml(row.name ?? "")}</strong>`
        },
        {
            key: "categoryName",
            title: "Danh mục",
            render: row => row.categoryName
                ? `<span class="badge badge-secondary">${escapeHtml(row.categoryName)}</span>`
                : `<span class="text-muted">Chưa phân loại</span>`
        },
        {
            key: "minPrice",
            title: "Giá từ",
            align: "right",
            render: row => row.minPrice != null
                ? `<span class="text-primary fw-bold">${formatMoney(row.minPrice)}</span>`
                : `<span class="text-muted text-sm">Chưa có giá</span>`
        }
    ],
    actions: [
        { label: "Sửa", onClick: row => openForm(row) },
        { label: "Xóa", danger: true, onClick: row => removeRow(row) }
    ]
});

/* ---------------------------------------------------------------------------
 * 2. Tải danh sách Danh mục (để nạp vào ô lọc và form)
 * ------------------------------------------------------------------------- */
async function loadCategories() {
    try {
        const data = await categoryApi.getAll();
        categories = Array.isArray(data) ? data : [];

        // Đổ danh mục vào ô lọc ở thanh công cụ
        if (categoryFilter) {
            categoryFilter.innerHTML = '<option value="">-- Tất cả danh mục --</option>' +
                categories.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("");
        }
    } catch (err) {
        console.error("Không tải được danh mục:", err);
    }
}

/* ---------------------------------------------------------------------------
 * 3. Tải danh sách Sản phẩm từ API
 * ------------------------------------------------------------------------- */
async function loadData({ keepPage = false } = {}) {
    table.setLoading();
    try {
        const data = await productApi.getAll();
        table.setRows(data, { keepPage });
    } catch (err) {
        table.setError("Lỗi khi tải danh sách sản phẩm: " + err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 4. Hộp thoại Popup Thêm / Sửa Sản phẩm
 * ------------------------------------------------------------------------- */
function openForm(row = null) {
    const isEdit = row !== null;

    // Danh sách danh mục để chọn trong Form
    const categoryOptions = categories.map(c => ({
        value: c.id,
        label: c.name
    }));

    formModal({
        title: isEdit ? `Sửa sản phẩm #${row.id}` : "Thêm sản phẩm mới",
        values: row ?? {},
        fields: [
            {
                name: "name",
                label: "Tên sản phẩm",
                required: true,
                placeholder: "Ví dụ: Áo thun nam Cotton Compact..."
            },
            {
                name: "categoryId",
                label: "Danh mục",
                type: "select",
                valueType: "number",
                required: true,
                emptyOption: "-- Chọn danh mục --",
                options: categoryOptions
            }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Thêm sản phẩm",
        onSubmit: async data => {
            if (isEdit) {
                await productApi.update(row.id, data);
                toast.success("Cập nhật sản phẩm thành công!");
            } else {
                await productApi.create(data);
                toast.success("Thêm sản phẩm mới thành công!");
            }
            await loadData({ keepPage: isEdit });
        }
    });
}

/* ---------------------------------------------------------------------------
 * 5. Xóa Sản phẩm
 * ------------------------------------------------------------------------- */
async function removeRow(row) {
    const ok = await confirmDialog({
        title: "Xóa sản phẩm",
        message: `Bạn có chắc muốn xóa sản phẩm "${row.name}"?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await productApi.remove(row.id);
        toast.success("Đã xóa sản phẩm thành công.");
        await loadData({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 6. Khởi động trang Quản lý Sản phẩm
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Sản phẩm" })) {
    // 1. Tải danh mục trước, sau đó tải danh sách sản phẩm
    await loadCategories();
    await loadData();

    // 2. Gắn sự kiện nút Thêm
    btnAdd?.addEventListener("click", () => openForm());

    // 3. Gắn sự kiện ô Tìm kiếm
    searchInput?.addEventListener("input", () => {
        table.search(searchInput.value);
    });

    // 4. Gắn sự kiện ô Lọc theo danh mục
    categoryFilter?.addEventListener("change", () => {
        const selectedId = Number(categoryFilter.value);
        if (selectedId) {
            table.setFilter(row => row.categoryId === selectedId);
        } else {
            table.setFilter(null); // Chọn "-- Tất cả danh mục --" thì bỏ lọc
        }
    });
}
