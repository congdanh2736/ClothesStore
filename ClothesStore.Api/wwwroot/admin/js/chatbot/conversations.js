/*
 * conversations.js - LỊCH SỬ HỘI THOẠI (admin/chatbot/conversations.html)
 * Bảng phân trang phía SERVER (dữ liệu chat có thể rất nhiều): mỗi lần đổi trang / bộ lọc thì gọi lại API.
 */
import { initAdminLayout } from "../layout.js";
import { chatbotApi, INTENT_LABELS, INTENT_OPTIONS } from "../../../assets/js/api/chatbotApi.js";
import { isAdmin } from "../../../assets/js/core/auth.js";
import { escapeHtml, formatDateTime, debounce, getQueryParam } from "../../../assets/js/core/utils.js";
import { renderTable, renderTableMessage } from "../../../assets/js/components/table.js";
import { renderPagination } from "../../../assets/js/components/pagination.js";
import { openModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";
import { intentBadge, intentLabel, confidenceHtml, feedbackHtml, percent } from "./common.js";

const $ = id => /** @type {HTMLElement} */ (document.getElementById(id));
const PAGE_SIZE = 20;
const state = { page: 1, filter: getQueryParam("filter") ?? "all", intent: "", q: "" };
/** @type {any[]} */
let rows = [];

const columns = [
    { key: "createdAt", title: "Thời gian", width: "150px", render: r => formatDateTime(r.createdAt) },
    { key: "message", title: "Câu hỏi", render: r => `<span class="cb-ellipsis" title="${escapeHtml(r.message)}">${escapeHtml(r.message)}</span>` },
    {
        key: "intent", title: "Bot hiểu là", render: r => intentBadge(r.intent)
            + (r.predictedIntent && r.predictedIntent !== r.intent ? `<div class="cb-code">mô hình: ${escapeHtml(r.predictedIntent)}</div>` : "")
    },
    { key: "confidence", title: "Độ tin cậy", render: r => confidenceHtml(r.confidence) },
    { key: "customerName", title: "Khách", render: r => r.customerName ? escapeHtml(r.customerName) : '<span class="cb-muted">Vãng lai</span>' },
    { key: "isHelpful", title: "Đánh giá", render: r => feedbackHtml(r.isHelpful) },
    {
        key: "isReviewed", title: "Admin", render: r => r.isReviewed
            ? `<span class="badge badge-success">Đã xem</span>${r.correctIntent && r.correctIntent !== r.intent ? `<div class="cb-code">đúng: ${escapeHtml(r.correctIntent)}</div>` : ""}`
            : '<span class="badge badge-secondary">Chưa xem</span>'
    }
];

const actions = [
    { label: "Xem / gán nhãn", onClick: row => openDetail(row) },
    { label: "Xóa", danger: true, visible: () => isAdmin(), onClick: row => removeRow(row) }
];

/* ============================================================ Tải dữ liệu */
async function load() {
    renderTableMessage($("tableContainer"), columns, '<span class="spinner"></span> Đang tải...', { hasActions: true });
    try {
        const result = await chatbotApi.getLogs({ filter: state.filter, intent: state.intent, q: state.q, page: state.page, pageSize: PAGE_SIZE });
        rows = result.items;
        renderTable($("tableContainer"), { columns, rows, actions, emptyText: "Không có câu hỏi nào phù hợp." });
        const totalPages = Math.max(1, Math.ceil(result.total / PAGE_SIZE));
        renderPagination($("paginationContainer"), {
            page: result.page, totalPages, total: result.total,
            from: result.total ? (result.page - 1) * PAGE_SIZE + 1 : 0,
            to: Math.min(result.page * PAGE_SIZE, result.total),
            onChange: p => { state.page = p; load(); }
        });
    } catch (err) {
        renderTableMessage($("tableContainer"), columns, escapeHtml(err.message), { hasActions: true, isError: true });
        $("paginationContainer").innerHTML = "";
    }
}

/* ============================================================ Xem chi tiết + gán nhãn */
function openDetail(row) {
    const content = document.createElement("div");
    content.className = "stack";
    content.innerHTML = `
        <div><div class="form-label">Khách hỏi (${formatDateTime(row.createdAt)} · ${row.customerName ? escapeHtml(row.customerName) : "khách vãng lai"})</div>
            <div class="cb-bubble is-question">${escapeHtml(row.message)}</div></div>
        <div><div class="form-label">Bot trả lời (${row.productCount} sản phẩm gợi ý)</div>
            <div class="cb-bubble">${escapeHtml(row.reply)}</div></div>
        <dl class="info-list">
            <dt>Bot hiểu là</dt><dd>${intentBadge(row.intent)} <span class="cb-code">${escapeHtml(row.intent)}</span></dd>
            <dt>Mô hình đoán</dt><dd>${row.predictedIntent ? `${escapeHtml(intentLabel(row.predictedIntent))} <span class="cb-code">${escapeHtml(row.predictedIntent)}</span>` : "—"} · độ tin cậy ${percent(row.confidence)}</dd>
            <dt>Khách đánh giá</dt><dd>${feedbackHtml(row.isHelpful)}</dd>
        </dl>
        <div class="form-group">
            <label class="form-label" for="correctIntent">Intent đúng của câu này</label>
            <select class="select" id="correctIntent">
                ${INTENT_OPTIONS.map(o => `<option value="${o.value}" ${o.value === (row.correctIntent ?? row.intent) ? "selected" : ""}>${escapeHtml(o.label)}</option>`).join("")}
            </select>
            <div class="form-hint">Bot hiểu đúng thì giữ nguyên. Hiểu sai thì chọn intent đúng.</div>
        </div>
        <label class="checkbox"><input type="checkbox" id="addToTraining" ${row.intent === "khong_hieu" || row.isHelpful === false || row.confidence < 0.5 ? "checked" : ""}>
            Đưa câu này vào dữ liệu huấn luyện (bot sẽ học khi huấn luyện lại)</label>`;

    const footer = document.createElement("div");
    footer.innerHTML = `<button type="button" class="btn btn-secondary" data-close>Đóng</button>
        <button type="button" class="btn btn-primary" data-save>Lưu & đánh dấu đã xem</button>`;

    const modal = openModal({ title: `Câu hỏi #${row.id}`, content, footer, size: "lg" });
    footer.querySelector("[data-close]")?.addEventListener("click", () => modal.close());
    footer.querySelector("[data-save]")?.addEventListener("click", async () => {
        const correctIntent = /** @type {HTMLSelectElement} */ (content.querySelector("#correctIntent")).value;
        const addToTraining = /** @type {HTMLInputElement} */ (content.querySelector("#addToTraining")).checked;
        try {
            await chatbotApi.reviewLog(row.id, { correctIntent, addToTraining });
            toast.success(addToTraining ? "Đã lưu và thêm vào dữ liệu huấn luyện." : "Đã đánh dấu đã xem.");
            modal.close();
            load();
        } catch (err) {
            toast.error(err.message);
        }
    });
}

async function removeRow(row) {
    const ok = await confirmDialog({ title: "Xóa câu hỏi", message: `Xóa câu hỏi #${row.id} khỏi lịch sử?`, confirmText: "Xóa", danger: true });
    if (!ok) return;
    try {
        await chatbotApi.deleteLog(row.id);
        toast.success("Đã xóa.");
        load();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ============================================================ Khởi động */
if (await initAdminLayout({ title: "Lịch sử hội thoại" })) {
    const filterSelect = /** @type {HTMLSelectElement} */ ($("filterSelect"));
    const intentSelect = /** @type {HTMLSelectElement} */ ($("intentSelect"));
    const searchInput = /** @type {HTMLInputElement} */ ($("searchInput"));

    intentSelect.innerHTML += Object.entries(INTENT_LABELS).map(([v, l]) => `<option value="${v}">${escapeHtml(l)}</option>`).join("");
    filterSelect.value = state.filter;

    filterSelect.addEventListener("change", () => { state.filter = filterSelect.value; state.page = 1; load(); });
    intentSelect.addEventListener("change", () => { state.intent = intentSelect.value; state.page = 1; load(); });
    const onSearch = debounce(() => { state.q = searchInput.value.trim(); state.page = 1; load(); }, 400);
    searchInput.addEventListener("input", onSearch);
    $("btnReload").addEventListener("click", load);
    load();
}
