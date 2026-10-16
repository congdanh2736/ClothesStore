/*
 * ============================================================================
 *  membership-tiers.js  -  XỬ LÝ CHO TRANG admin/customers/membership-tiers.html
 * ============================================================================
 *
 *  Trang CHỈ dành cho Admin: initAdminLayout({ adminOnly: true }).
 *  Danh sách hạng thường ít (3-5 hạng) nên dùng renderTable (không phân trang,
 *  không tìm kiếm) cho gọn.
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/customers/ -> "../../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout } from "../layout.js";
import { membershipApi } from "../../../assets/js/api/membershipApi.js";
import { escapeHtml, formatNumber } from "../../../assets/js/core/utils.js";
import { renderTable, renderTableMessage } from "../../../assets/js/components/table.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

const tableContainer = /** @type {HTMLDivElement} */ (document.getElementById("tableContainer"));
const btnAdd = /** @type {HTMLButtonElement} */ (document.getElementById("btnAdd"));
const btnReload = /** @type {HTMLButtonElement} */ (document.getElementById("btnReload"));

/* ---------------------------------------------------------------------------
 * Mô tả các cột và nút của bảng (khai báo 1 lần, dùng lại mỗi lần vẽ)
 * ------------------------------------------------------------------------- */
const columns = [
    { key: "id", title: "Mã", width: "70px" },
    { key: "tierName", title: "Tên hạng",
      render: row => `<span class="badge badge-primary">${escapeHtml(row.tierName)}</span>` },
    { key: "minPoints", title: "Điểm tối thiểu", align: "right",
      render: row => `${formatNumber(row.minPoints)} điểm` },
    { key: "customerCount", title: "Số khách đang ở hạng", align: "right",
      render: row => formatNumber(row.customerCount) }
];

const actions = [
    { label: "Sửa", onClick: row => openForm(row) },
    { label: "Xóa", danger: true, onClick: row => removeTier(row) }
];

/* ---------------------------------------------------------------------------
 * TẢI DỮ LIỆU
 * ------------------------------------------------------------------------- */
async function loadTiers() {
    renderTableMessage(tableContainer, columns, `<span class="spinner"></span> Đang tải dữ liệu...`, { hasActions: true });
    try {
        const tiers = await membershipApi.getAll();
        // Sắp xếp theo điểm tối thiểu tăng dần: Đồng -> Bạc -> Vàng -> VIP
        tiers.sort((a, b) => a.minPoints - b.minPoints);
        renderTable(tableContainer, { columns, actions, rows: tiers, emptyText: "Chưa có hạng thành viên nào." });
    } catch (err) {
        renderTableMessage(tableContainer, columns, escapeHtml(err.message), { hasActions: true, isError: true });
    }
}

/* ---------------------------------------------------------------------------
 * THÊM / SỬA
 * ------------------------------------------------------------------------- */

/** @param {any} [tier] - hạng đang sửa; bỏ trống = thêm mới */
function openForm(tier = null) {
    const isEdit = tier !== null;

    formModal({
        title: isEdit ? `Sửa hạng "${tier.tierName}"` : "Thêm hạng thành viên",
        values: tier ?? {},
        fields: [
            { name: "tierName", label: "Tên hạng", required: true, placeholder: "Ví dụ: Bạch kim",
              hint: "Tối đa 50 ký tự." },
            { name: "minPoints", label: "Điểm tối thiểu", type: "number", required: true, min: 0, step: 1,
              hint: "Không được âm và không trùng với hạng khác. 10.000đ mua hàng = 1 điểm." }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Thêm hạng",
        onSubmit: async data => {
            if (isEdit) {
                await membershipApi.update(tier.id, data);
                toast.success("Đã cập nhật hạng thành viên.");
            } else {
                await membershipApi.create(data);
                toast.success("Đã thêm hạng thành viên.");
            }
            await loadTiers();
        }
    });
}

/* ---------------------------------------------------------------------------
 * XÓA
 * ------------------------------------------------------------------------- */

/** @param {any} tier */
async function removeTier(tier) {
    // Còn khách ở hạng này -> server sẽ từ chối, báo luôn cho nhanh khỏi gọi API
    if (tier.customerCount > 0) {
        toast.warning(`Hạng "${tier.tierName}" đang có ${tier.customerCount} khách hàng, không thể xóa.`);
        return;
    }

    const ok = await confirmDialog({
        title: "Xóa hạng thành viên",
        message: `Xóa hạng "${tier.tierName}"?`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await membershipApi.remove(tier.id);
        toast.success("Đã xóa hạng thành viên.");
        await loadTiers();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * KHỞI ĐỘNG TRANG  (adminOnly: Employee vào sẽ bị đưa về dashboard)
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Hạng thành viên", adminOnly: true })) {
    btnAdd.addEventListener("click", () => openForm());
    btnReload.addEventListener("click", () => loadTiers());
    await loadTiers();
}
