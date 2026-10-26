/*
 * ============================================================================
 *  collections.js - Quản lý Bộ sưu tập & Công nghệ (Nhóm 2)
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { escapeHtml } from "../../../assets/js/core/utils.js";
import { collectionTechApi } from "../../../assets/js/api/catalogApi.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

// Lấy các thẻ HTML trên trang
const tableContainer = document.getElementById("tableContainer");
const paginationContainer = document.getElementById("paginationContainer");
const searchInput = document.getElementById("searchInput");
const typeFilter = document.getElementById("typeFilter");
const btnAdd = document.getElementById("btnAdd");

/* ---------------------------------------------------------------------------
 * 1. Cấu hình bảng dữ liệu Bộ sưu tập & Công nghệ
 * ------------------------------------------------------------------------- */
const table = createDataTable({
    container: tableContainer,
    paginationContainer: paginationContainer,
    searchFields: ["name", "type", "id"],
    sortBy: (a, b) => b.id - a.id,
    emptyText: "Chưa có bộ sưu tập hay công nghệ nào.",
    columns: [
        { key: "id", title: "Mã", width: "70px" },
        {
            key: "name",
            title: "Tên bộ sưu tập / Công nghệ",
            render: row => `<strong>${escapeHtml(row.name ?? "")}</strong>`
        },
        {
            key: "type",
            title: "Phân loại",
            width: "160px",
            render: row => {
                const isTech = row.type === "Technology" || row.type === "Công nghệ";
                return isTech
                    ? `<span class="badge badge-info">Công nghệ</span>`
                    : `<span class="badge badge-primary">Bộ sưu tập</span>`;
            }
        }
    ],
    actions: [
        { label: "Sửa", onClick: row => openForm(row) },
        { label: "Xóa", danger: true, onClick: row => removeRow(row) }
    ]
});

/* ---------------------------------------------------------------------------
 * 2. Tải dữ liệu từ API
 * ------------------------------------------------------------------------- */
async function loadData({ keepPage = false } = {}) {
    table.setLoading();
    try {
        const data = await collectionTechApi.getAll();
        table.setRows(data, { keepPage });
    } catch (err) {
        table.setError("Lỗi khi tải dữ liệu: " + err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 3. Hộp thoại Popup Thêm / Sửa
 * ------------------------------------------------------------------------- */
function openForm(row = null) {
    const isEdit = row !== null;

    formModal({
        title: isEdit ? `Sửa mục #${row.id}` : "Thêm bộ sưu tập / công nghệ",
        values: row ?? { type: "Bộ sưu tập" },
        fields: [
            {
                name: "name",
                label: "Tên hiển thị",
                required: true,
                placeholder: "Ví dụ: BST Thu Đông 2026, Vải AirDry, Công nghệ FlexFit..."
            },
            {
                name: "type",
                label: "Loại mục",
                type: "select",
                required: true,
                options: [
                    { value: "Bộ sưu tập", label: "Bộ sưu tập (Collection)" },
                    { value: "Công nghệ", label: "Công nghệ chất liệu (Technology)" }
                ]
            }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Tạo mới",
        onSubmit: async data => {
            if (isEdit) {
                await collectionTechApi.update(row.id, data);
                toast.success("Cập nhật thành công!");
            } else {
                await collectionTechApi.create(data);
                toast.success("Thêm mới thành công!");
            }
            await loadData({ keepPage: isEdit });
        }
    });
}

/* ---------------------------------------------------------------------------
 * 4. Xóa
 * ------------------------------------------------------------------------- */
async function removeRow(row) {
    const ok = await confirmDialog({
        title: "Xác nhận xóa",
        message: `Bạn có chắc muốn xóa "${row.name}"?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await collectionTechApi.remove(row.id);
        toast.success("Đã xóa thành công.");
        await loadData({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 5. Khởi động trang
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Bộ sưu tập & Công nghệ" })) {
    await loadData();

    // Nút Thêm mới
    btnAdd?.addEventListener("click", () => openForm());

    // Ô Tìm kiếm
    searchInput?.addEventListener("input", () => {
        table.search(searchInput.value);
    });

    // Ô Lọc theo Loại
    typeFilter?.addEventListener("change", () => {
        const val = typeFilter.value;
        if (val) {
            table.setFilter(row => row.type === val);
        } else {
            table.setFilter(null);
        }
    });
}
