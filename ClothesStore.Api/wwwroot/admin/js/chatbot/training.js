/*
 * training.js - HUẤN LUYỆN CHATBOT (admin/chatbot/training.html) - chỉ Quản lý
 */
import { initAdminLayout } from "../layout.js";
import { chatbotApi, INTENT_OPTIONS } from "../../../assets/js/api/chatbotApi.js";
import { escapeHtml, formatNumber, formatDateTime, debounce, toDate } from "../../../assets/js/core/utils.js";
import { renderTable, createDataTable } from "../../../assets/js/components/table.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";
import { accuracyHtml, intentBadge } from "./common.js";

const $ = id => /** @type {HTMLElement} */ (document.getElementById(id));
/** @type {number | null} */
let pollTimer = null;

/* ============================================================ Mô hình + lịch sử huấn luyện */
const RUN_STATUS = {
    Running: '<span class="badge badge-info">Đang chạy</span>',
    Succeeded: '<span class="badge badge-success">Thành công</span>',
    Failed: '<span class="badge badge-danger">Lỗi</span>',
    Interrupted: '<span class="badge badge-warning">Bị dừng</span>'
};

const runColumns = [
    { key: "id", title: "#", width: "50px" },
    { key: "startedAt", title: "Bắt đầu", render: r => formatDateTime(r.startedAt) },
    { key: "duration", title: "Thời gian", render: r => r.finishedAt ? duration(r.startedAt, r.finishedAt) : "—" },
    { key: "status", title: "Trạng thái", render: r => RUN_STATUS[r.status] ?? escapeHtml(r.status) },
    { key: "samples", title: "Số câu", align: "right", render: r => `${formatNumber(r.baseSamples)} + ${formatNumber(r.adminSamples)}` },
    { key: "microAccuracy", title: "Độ chính xác (20% tách ra)", align: "right", render: r => accuracyHtml(r.microAccuracy) },
    { key: "handTestAccuracy", title: "Câu viết tay", align: "right", render: r => r.handTestAccuracy == null ? "—" : `${accuracyHtml(r.handTestAccuracy)} <span class="cb-muted">/ ${r.handTestCount} câu</span>` },
    { key: "triggeredBy", title: "Người chạy", render: r => escapeHtml(r.triggeredBy ?? "—") },
    { key: "message", title: "Ghi chú", render: r => `<span class="cb-ellipsis" title="${escapeHtml(r.message ?? "")}">${escapeHtml(r.message ?? "")}</span>` }
];

function duration(from, to) {
    const s = Math.round(((toDate(to)?.getTime() ?? 0) - (toDate(from)?.getTime() ?? 0)) / 1000);
    return s < 60 ? `${s} giây` : `${Math.floor(s / 60)} phút ${s % 60} giây`;
}

async function loadModel() {
    try {
        const m = await chatbotApi.getModel();
        const status = m.modelExists
            ? (m.modelLoaded ? '<span class="badge badge-success">Đang hoạt động</span>' : '<span class="badge badge-warning">Có file, chạy lại server để nạp</span>')
            : '<span class="badge badge-danger">Chưa có mô hình</span>';
        $("modelInfo").innerHTML = `
            <dl class="cb-model-grid">
                <div><dt>Trạng thái</dt><dd>${status}</dd></div>
                <div><dt>File mô hình</dt><dd>${m.modelExists ? `${(m.modelSizeBytes / 1024).toFixed(0)} KB` : "—"}</dd></div>
                <div><dt>Cập nhật lúc</dt><dd>${m.modelUpdatedAt ? formatDateTime(m.modelUpdatedAt) : "—"}</dd></div>
                <div><dt>Câu có sẵn (intents.csv)</dt><dd>${m.baseDataFound ? formatNumber(m.baseSamples) : '<span class="badge badge-danger">Không tìm thấy file</span>'}</dd></div>
                <div><dt>Câu do quản lý thêm</dt><dd>${formatNumber(m.adminSamples)}</dd></div>
                <div><dt>Bộ câu kiểm tra viết tay</dt><dd>${m.handTestFound ? '<span class="badge badge-success">Có</span>' : '<span class="badge badge-secondary">Không có</span>'}</dd></div>
            </dl>
            <p class="cb-muted" style="margin-top:var(--space-4)">Huấn luyện lại = học lại từ ${formatNumber(m.baseSamples)} câu có sẵn + các câu quản lý thêm (mất vài phút).
                Xong sẽ tự thay mô hình mới, bản cũ được giữ ở <span class="cb-code">MLModels/intent-model.backup.zip</span>.</p>`;
        renderTable($("runsTable"), { columns: runColumns, rows: m.runs, emptyText: "Chưa huấn luyện lại lần nào trên web." });

        const running = m.runs.find(r => r.status === "Running");
        setTraining(m.isTraining, running?.id);
        /** @type {HTMLButtonElement} */ ($("btnTrain")).disabled = m.isTraining || !m.baseDataFound;
    } catch (err) {
        $("modelInfo").innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message)}</div>`;
    }
}

/* Hiện thanh "đang huấn luyện" và hỏi lại trạng thái 3 giây / lần */
function setTraining(isTraining, runId) {
    $("trainingBox").hidden = !isTraining;
    if (pollTimer) { clearInterval(pollTimer); pollTimer = null; }
    if (!isTraining || !runId) return;

    const started = Date.now();
    pollTimer = window.setInterval(async () => {
        $("trainingText").textContent = `Đang huấn luyện lần #${runId}... ${Math.round((Date.now() - started) / 1000)} giây (bạn có thể rời trang, server vẫn chạy tiếp)`;
        try {
            const run = await chatbotApi.getTraining(runId);
            if (run.status === "Running") return;
            if (pollTimer) clearInterval(pollTimer);
            pollTimer = null;
            if (run.status === "Succeeded") toast.success(`Huấn luyện xong! Độ chính xác ${((run.handTestAccuracy ?? run.microAccuracy) * 100).toFixed(1)}%.`);
            else toast.error(`Huấn luyện thất bại: ${run.message ?? ""}`);
            loadModel();
            loadIntents();
        } catch { /* lỗi mạng tạm thời: hỏi lại lần sau */ }
    }, 3000);
}

async function startTraining() {
    const ok = await confirmDialog({
        title: "Huấn luyện lại mô hình",
        message: "Server sẽ học lại toàn bộ dữ liệu (mất vài phút, có thể làm server chậm hơn trong lúc chạy).\nTrong lúc huấn luyện, bot vẫn trả lời bằng mô hình cũ. Tiếp tục?",
        confirmText: "Bắt đầu"
    });
    if (!ok) return;
    try {
        const run = await chatbotApi.train();
        toast.info(`Đã bắt đầu huấn luyện lần #${run.id}.`);
        /** @type {HTMLButtonElement} */ ($("btnTrain")).disabled = true;
        setTraining(true, run.id);
        loadModel();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ============================================================ Số câu theo intent */
async function loadIntents() {
    try {
        const list = await chatbotApi.getIntents();
        const total = list.reduce((s, x) => s + x.baseSamples + x.adminSamples, 0) || 1;
        renderTable($("intentsTable"), {
            columns: [
                { key: "label", title: "Intent", render: r => `${intentBadge(r.intent)} <span class="cb-code">${escapeHtml(r.intent)}</span>` },
                { key: "baseSamples", title: "Câu có sẵn", align: "right", render: r => formatNumber(r.baseSamples) },
                { key: "adminSamples", title: "Quản lý thêm", align: "right", render: r => r.adminSamples ? `<b>${formatNumber(r.adminSamples)}</b>` : "0" },
                { key: "share", title: "Tỉ lệ", align: "right", render: r => `${((r.baseSamples + r.adminSamples) / total * 100).toFixed(1)}%` }
            ],
            rows: list,
            actions: [{ label: "Xem câu", onClick: r => { /** @type {HTMLSelectElement} */ ($("sampleIntent")).value = r.intent; loadSamples(); $("samplesTable").scrollIntoView({ behavior: "smooth" }); } }]
        });
    } catch (err) {
        $("intentsTable").innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message)}</div>`;
    }
}

/* ============================================================ Câu huấn luyện do quản lý thêm */
const samplesTable = createDataTable({
    container: $("samplesTable"),
    paginationContainer: $("samplesPagination"),
    columns: [
        { key: "text", title: "Câu hỏi mẫu", render: r => escapeHtml(r.text) },
        { key: "labelName", title: "Intent", render: r => intentBadge(r.label) },
        { key: "source", title: "Nguồn", render: r => r.source === "ChatLog" ? `<span class="badge badge-info">Từ hội thoại #${r.chatLogId}</span>` : '<span class="badge badge-secondary">Nhập tay</span>' },
        { key: "createdAt", title: "Ngày thêm", render: r => formatDateTime(r.createdAt) }
    ],
    searchFields: ["text", "labelName"],
    emptyText: "Chưa có câu nào. Bấm \"+ Thêm câu\" hoặc gán nhãn ở trang Lịch sử hội thoại.",
    actions: [
        { label: "Sửa", onClick: r => openSampleForm(r) },
        { label: "Xóa", danger: true, onClick: r => removeSample(r) }
    ]
});

async function loadSamples() {
    samplesTable.setLoading();
    try {
        samplesTable.setRows(await chatbotApi.getSamples(/** @type {HTMLSelectElement} */ ($("sampleIntent")).value));
    } catch (err) {
        samplesTable.setError(err.message);
    }
}

/** @param {any} [row] */
function openSampleForm(row = null) {
    const isEdit = row !== null;
    formModal({
        title: isEdit ? `Sửa câu #${row.id}` : "Thêm câu huấn luyện",
        description: "Viết câu như khách thật hay hỏi (có thể không dấu, viết tắt). Mỗi intent nên có nhiều cách hỏi khác nhau.",
        fields: [
            { name: "text", label: "Câu hỏi mẫu", type: "textarea", required: true, placeholder: "Ví dụ: shop ơi áo này mặc đi đám cưới được không" },
            { name: "label", label: "Intent", type: "select", required: true, options: INTENT_OPTIONS, emptyOption: "-- Chọn intent --" }
        ],
        values: row ?? { label: /** @type {HTMLSelectElement} */ ($("sampleIntent")).value },
        submitText: isEdit ? "Lưu" : "Thêm",
        onSubmit: async data => {
            if (isEdit) await chatbotApi.updateSample(row.id, data);
            else await chatbotApi.createSample(data);
            toast.success(isEdit ? "Đã cập nhật." : "Đã thêm. Bấm \"Huấn luyện lại\" để bot học câu mới.");
            await Promise.all([loadSamples(), loadIntents(), loadModel()]);
        }
    });
}

async function removeSample(row) {
    const ok = await confirmDialog({ title: "Xóa câu huấn luyện", message: `Xóa câu "${row.text}"?`, confirmText: "Xóa", danger: true });
    if (!ok) return;
    try {
        await chatbotApi.deleteSample(row.id);
        toast.success("Đã xóa.");
        await Promise.all([loadSamples(), loadIntents(), loadModel()]);
    } catch (err) {
        toast.error(err.message);
    }
}

/* ============================================================ Khởi động */
if (await initAdminLayout({ title: "Huấn luyện chatbot", adminOnly: true })) {
    /** @type {HTMLSelectElement} */ ($("sampleIntent")).innerHTML += INTENT_OPTIONS.map(o => `<option value="${o.value}">${escapeHtml(o.label)}</option>`).join("");
    $("btnTrain").addEventListener("click", startTraining);
    $("btnAddSample").addEventListener("click", () => openSampleForm());
    $("sampleIntent").addEventListener("change", loadSamples);
    const onSearch = debounce(text => samplesTable.search(text), 300);
    $("sampleSearch").addEventListener("input", e => onSearch(/** @type {HTMLInputElement} */ (e.target).value));
    loadModel();
    loadIntents();
    loadSamples();
}
