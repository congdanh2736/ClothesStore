/*
 * ============================================================================
 *  promotions.js  -  XỬ LÝ CHO TRANG admin/sales/promotions.html   (Nhóm 4)
 * ============================================================================
 *
 *  Trang làm các việc:
 *    1. Tải danh sách mã khuyến mãi (voucher).
 *    2. Hiện bảng có tìm kiếm theo mã, lọc theo tình trạng hiệu lực, phân trang.
 *    3. Thêm, Sửa, Xóa mã khuyến mãi.
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/sales/ -> "../../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { promotionApi } from "../../../assets/js/api/promotionApi.js";
import {
    escapeHtml,
    formatMoney,
    formatDate,
    toDate,
    toDateInputValue,
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
const stateFilter = /** @type {HTMLSelectElement} */ (document.getElementById("stateFilter"));
const btnAdd = /** @type {HTMLButtonElement} */ (document.getElementById("btnAdd"));
const btnReload = /** @type {HTMLButtonElement} */ (document.getElementById("btnReload"));

/**
 * Xác định trạng thái hiệu lực của mã khuyến mãi dựa vào ngày bắt đầu và ngày kết thúc.
 * @param {any} row
 * @returns {"active"|"upcoming"|"expired"}
 */
function getPromotionState(row) {
    const now = new Date();
    const start = toDate(row.startDate);
    const end = toDate(row.endDate);
    if (start && start > now) return "upcoming";
    if (end && end < now) return "expired";
    return "active";
}

/**
 * Tạo nhãn màu hiển thị tình trạng hiệu lực của mã khuyến mãi.
 * @param {any} row
 */
function promotionStateBadge(row) {
    const state = getPromotionState(row);
    if (state === "active") return `<span class="badge badge-success">Đang áp dụng</span>`;
    if (state === "upcoming") return `<span class="badge badge-info">Sắp diễn ra</span>`;
    return `<span class="badge badge-secondary">Đã hết hạn</span>`;
}

/* ---------------------------------------------------------------------------
 * BẢNG KHUYẾN MÃI
 * ------------------------------------------------------------------------- */
const table = createDataTable({
    container: tableContainer,
    paginationContainer,
    searchFields: ["id", "code"],
    sortBy: (a, b) => b.id - a.id,
    emptyText: "Chưa có mã khuyến mãi nào.",
    columns: [
        { key: "id", title: "Mã", width: "70px" },
        {
            key: "code",
            title: "Mã khuyến mãi",
            render: row => `<span class="badge badge-primary">${escapeHtml(row.code)}</span>`
        },
        {
            key: "discountValue",
            title: "Mức giảm giá",
            align: "right",
            render: row => `<span class="fw-bold">${formatMoney(row.discountValue)}</span>`
        },
        {
            key: "startDate",
            title: "Ngày bắt đầu",
            render: row => formatDate(row.startDate)
        },
        {
            key: "endDate",
            title: "Ngày kết thúc",
            render: row => formatDate(row.endDate)
        },
        {
            key: "state",
            title: "Tình trạng",
            render: row => promotionStateBadge(row)
        }
    ],
    actions: [
        { label: "Sửa", onClick: row => openForm(row) },
        { label: "Xóa", danger: true, onClick: row => removePromotion(row) }
    ]
});

/* ---------------------------------------------------------------------------
 * TẢI DỮ LIỆU
 * ------------------------------------------------------------------------- */

/** @param {{ keepPage?: boolean }} [options] */
async function loadPromotions({ keepPage = false } = {}) {
    table.setLoading();
    try {
        table.setRows(await promotionApi.getAll(), { keepPage });
    } catch (err) {
        table.setError(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * THÊM / SỬA KHUYẾN MÃI
 * ------------------------------------------------------------------------- */

/**
 * Mở form Thêm / Sửa khuyến mãi.
 * @param {any} [row] - dòng đang sửa; null = thêm mới
 */
function openForm(row = null) {
    const isEdit = row !== null;

    // Khi thêm mới: gợi ý sẵn ngày bắt đầu là hôm nay, ngày kết thúc là 30 ngày sau
    const today = new Date();
    const nextMonth = new Date();
    nextMonth.setDate(today.getDate() + 30);

    const initialValues = isEdit
        ? {
            code: row.code,
            discountValue: row.discountValue,
            startDate: toDateInputValue(row.startDate),
            endDate: toDateInputValue(row.endDate)
        }
        : {
            startDate: toDateInputValue(today),
            endDate: toDateInputValue(nextMonth)
        };

    formModal({
        title: isEdit ? `Sửa khuyến mãi "${row.code}"` : "Thêm khuyến mãi",
        columns: 2,
        values: initialValues,
        fields: [
            {
                name: "code",
                label: "Mã khuyến mãi (Code)",
                required: true,
                placeholder: "Ví dụ: SALE50K, SUMMER2026",
                hint: "Tối đa 50 ký tự, không trùng với mã khác."
            },
            {
                name: "discountValue",
                label: "Số tiền giảm (VNĐ)",
                type: "number",
                required: true,
                min: 1000,
                step: 1000,
                placeholder: "Ví dụ: 50000",
                hint: "Trừ trực tiếp vào tổng tiền hàng (không vượt quá tiền đơn)."
            },
            {
                name: "startDate",
                label: "Ngày bắt đầu",
                type: "date",
                required: true
            },
            {
                name: "endDate",
                label: "Ngày kết thúc",
                type: "date",
                required: true,
                hint: "Phải sau ngày bắt đầu."
            }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Thêm khuyến mãi",
        onSubmit: async data => {
            // Đổi ngày kết thúc về cuối ngày (23:59:59) để mã dùng được trọn ngày kết thúc
            const payload = {
                code: data.code,
                discountValue: data.discountValue,
                startDate: `${data.startDate}T00:00:00`,
                endDate: `${data.endDate}T23:59:59`
            };

            if (isEdit) {
                await promotionApi.update(row.id, payload);
                toast.success("Đã cập nhật khuyến mãi.");
            } else {
                await promotionApi.create(payload);
                toast.success(`Đã thêm mã khuyến mãi "${data.code}".`);
            }
            await loadPromotions({ keepPage: isEdit });
        }
    });
}

/* ---------------------------------------------------------------------------
 * XÓA KHUYẾN MÃI
 * ------------------------------------------------------------------------- */

/** @param {any} row */
async function removePromotion(row) {
    const ok = await confirmDialog({
        title: "Xóa khuyến mãi",
        message: `Xóa mã khuyến mãi "${row.code}" (${formatMoney(row.discountValue)})?\n`
            + "Lưu ý: Mã đã được áp dụng cho đơn hàng sẽ không thể xóa (hãy sửa Ngày kết thúc nếu muốn ngừng áp dụng).",
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await promotionApi.remove(row.id);
        toast.success(`Đã xóa mã khuyến mãi "${row.code}".`);
        await loadPromotions({ keepPage: true });
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * KHỞI ĐỘNG TRANG
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Khuyến mãi" })) {
    btnAdd.addEventListener("click", () => openForm());
    btnReload.addEventListener("click", () => loadPromotions({ keepPage: true }));

    const onSearch = debounce(text => table.search(text), 300);
    searchInput.addEventListener("input", () => onSearch(searchInput.value));

    stateFilter.addEventListener("change", () => {
        const val = stateFilter.value;
        table.setFilter(val ? row => getPromotionState(row) === val : null);
    });

    await loadPromotions();
}
