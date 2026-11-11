/*
 * overview.js - TRANG TỔNG QUAN CHATBOT (admin/chatbot/overview.html)
 */
import { initAdminLayout } from "../layout.js";
import { chatbotApi } from "../../../assets/js/api/chatbotApi.js";
import { escapeHtml, formatNumber, formatMoney, formatDate, formatDateTime } from "../../../assets/js/core/utils.js";
import { toast } from "../../../assets/js/components/toast.js";
import { percent, intentBadge, confidenceHtml, accuracyHtml } from "./common.js";

const $ = id => /** @type {HTMLElement} */ (document.getElementById(id));
const ICON = {
    chat: '<svg viewBox="0 0 24 24"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>',
    target: '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="4"/></svg>',
    help: '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="M9.1 9a3 3 0 0 1 5.8 1c0 2-3 3-3 3"/><path d="M12 17h.01"/></svg>',
    like: '<svg viewBox="0 0 24 24"><path d="M7 10v11"/><path d="M15 5.9 14 10h5.8a2 2 0 0 1 2 2.3l-1.4 7A2 2 0 0 1 18.4 21H7V10l4-8a3 3 0 0 1 4 3.9z"/></svg>',
    review: '<svg viewBox="0 0 24 24"><path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/></svg>'
};

/* ============================================================ Thống kê */
async function loadStats() {
    const days = Number(/** @type {HTMLSelectElement} */ ($("daysSelect")).value);
    try {
        const s = await chatbotApi.getStats(days);
        const unsure = s.totalMessages ? (s.lowConfidenceCount + s.notUnderstoodCount) / s.totalMessages : 0;
        const rated = s.helpfulCount + s.unhelpfulCount;
        $("statGrid").innerHTML = `
            ${statCard("Câu hỏi của khách", formatNumber(s.totalMessages), `${formatNumber(s.loggedInMessages)} câu từ khách đã đăng nhập`, ICON.chat)}
            ${statCard("Độ tin cậy trung bình", percent(s.averageConfidence), "Mô hình chắc chắn bao nhiêu khi đoán ý định", ICON.target, "is-success")}
            ${statCard("Bot chưa chắc / chưa hiểu", percent(unsure), `${formatNumber(s.lowConfidenceCount)} câu tin cậy &lt; 50%, ${formatNumber(s.notUnderstoodCount)} câu chưa hiểu`, ICON.help, "is-warning")}
            ${statCard("Khách đánh giá hữu ích", rated ? percent(s.helpfulCount / rated, 0) : "—", `👍 ${formatNumber(s.helpfulCount)} · 👎 ${formatNumber(s.unhelpfulCount)}`, ICON.like)}
            ${statCard("Cần xem lại", formatNumber(s.unreviewedCount), `<a href="/admin/chatbot/conversations.html?filter=unreviewed">Mở danh sách →</a>`, ICON.review, s.unreviewedCount ? "is-warning" : "")}`;
        drawDayChart(s.byDay, days);
        drawIntentChart(s.byIntent, s.totalMessages);
    } catch (err) {
        $("statGrid").innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message)} (Đã chạy Update-Database để tạo bảng ChatLogs chưa?)</div>`;
        $("dayChart").textContent = "—";
        $("intentChart").textContent = "—";
    }
}

function statCard(label, value, note, icon, cls = "") {
    return `<div class="stat-card ${cls}"><div class="stat-icon" aria-hidden="true">${icon}</div>
        <div class="stat-label">${label}</div><div class="stat-value">${value}</div><div class="stat-note">${note}</div></div>`;
}

/* Biểu đồ cột: đủ N ngày (ngày không có câu hỏi = 0) */
function drawDayChart(byDay, days) {
    const counts = new Map(byDay.map(d => [String(d.date).slice(0, 10), d.count]));
    const list = [];
    for (let i = days - 1; i >= 0; i--) {
        const d = new Date(); d.setUTCDate(d.getUTCDate() - i);
        const key = d.toISOString().slice(0, 10);
        list.push({ key, count: counts.get(key) ?? 0 });
    }
    const max = Math.max(1, ...list.map(x => x.count));
    if (list.every(x => x.count === 0)) { $("dayChart").innerHTML = `<p class="cb-muted">Chưa có câu hỏi nào trong khoảng thời gian này.</p>`; return; }
    $("dayChart").innerHTML = `
        <div class="cb-bars">${list.map(x => `<div class="cb-bar" style="height:${Math.max(2, x.count / max * 100)}%"
            data-tip="${formatDate(x.key)}: ${x.count} câu"></div>`).join("")}</div>
        <div class="cb-axis"><span>${formatDate(list[0].key)}</span><span>Cao nhất: ${max} câu/ngày</span><span>${formatDate(list[list.length - 1].key)}</span></div>`;
}

function drawIntentChart(byIntent, total) {
    if (!byIntent.length) { $("intentChart").innerHTML = `<p class="cb-muted">Chưa có dữ liệu.</p>`; return; }
    const max = Math.max(...byIntent.map(x => x.count));
    $("intentChart").innerHTML = `<div class="cb-hbars">${byIntent.map(x => `
        <div class="cb-hbar-row" title="Độ tin cậy trung bình ${percent(x.averageConfidence)}">
            <span>${escapeHtml(x.label)}</span>
            <span class="cb-hbar-track"><span class="cb-hbar-fill" style="width:${x.count / max * 100}%;${x.intent === "khong_hieu" ? "background:var(--color-danger)" : ""}"></span></span>
            <span class="num">${formatNumber(x.count)} · ${percent(x.count / total, 0)}</span>
        </div>`).join("")}</div>`;
}

/* ============================================================ Mô hình */
async function loadModel() {
    try {
        const m = await chatbotApi.getModel();
        const last = m.runs.find(r => r.status === "Succeeded");
        $("modelInfo").innerHTML = `
            <dl class="cb-model-grid">
                <div><dt>Trạng thái</dt><dd>${m.modelExists ? (m.modelLoaded ? '<span class="badge badge-success">Đang hoạt động</span>' : '<span class="badge badge-warning">Có file, chưa nạp</span>') : '<span class="badge badge-danger">Chưa có mô hình</span>'}</dd></div>
                <div><dt>Cập nhật lúc</dt><dd>${m.modelUpdatedAt ? formatDateTime(m.modelUpdatedAt) : "—"}</dd></div>
                <div><dt>Câu huấn luyện</dt><dd>${formatNumber(m.baseSamples)} <span class="cb-muted">+ ${formatNumber(m.adminSamples)} do admin thêm</span></dd></div>
                <div><dt>Độ chính xác (lần train gần nhất)</dt><dd>${last ? accuracyHtml(last.handTestAccuracy ?? last.microAccuracy) : "—"}</dd></div>
            </dl>
            ${m.isTraining ? '<div class="cb-training"><span class="spinner"></span> Đang huấn luyện lại mô hình...</div>' : ""}`;
    } catch (err) {
        $("modelInfo").innerHTML = `<p class="cb-muted">${escapeHtml(err.message)}</p>`;
    }
}

/* ============================================================ Chat thử */
let context = null;
const SAMPLE_QUESTIONS = ["cao 1m70 nặng 65kg mặc size gì", "tìm áo khoác đen dưới 500k", "đi đám cưới nên mặc gì", "có mã giảm giá nào không", "cửa hàng ở gò vấp"];

function addMessage(html, who) {
    const div = document.createElement("div");
    div.className = `cb-msg ${who}`;
    div.innerHTML = html;
    $("chatLog").append(div);
    $("chatLog").scrollTop = $("chatLog").scrollHeight;
    return div;
}

function renderChips(list) {
    $("chatChips").innerHTML = list.slice(0, 4).map(s => `<button type="button" class="cb-chip">${escapeHtml(s)}</button>`).join("");
}

async function ask(text) {
    addMessage(escapeHtml(text), "user");
    const waiting = addMessage('<span class="spinner"></span>', "bot");
    try {
        const r = await chatbotApi.ask(text, context);
        context = r.context;
        waiting.innerHTML = `${escapeHtml(r.reply)}
            <div class="cb-msg-meta">${intentBadge(r.intent)} ${confidenceHtml(r.confidence)}
                ${r.logId ? `<span class="cb-code">log #${r.logId}</span>` : ""}</div>
            ${r.products.length ? `<div class="cb-msg-products">${r.products.slice(0, 4).map(p => `
                <a href="/shop/product.html?id=${p.id}" target="_blank" rel="noopener">
                    ${p.imageUrl ? `<img src="${escapeHtml(p.imageUrl)}" alt="">` : "<img alt=''>"}
                    <span><b>${escapeHtml(p.name)}</b><br><span class="cb-muted">${formatMoney(p.minPrice)}</span></span></a>`).join("")}
                ${r.products.length > 4 ? `<span class="cb-muted">+ ${r.products.length - 4} sản phẩm khác</span>` : ""}</div>` : ""}`;
        renderChips(r.suggestions);
    } catch (err) {
        waiting.innerHTML = `<span style="color:var(--color-danger)">${escapeHtml(err.message)}</span>`;
    }
    $("chatLog").scrollTop = $("chatLog").scrollHeight;
}

function resetChat() {
    context = null;
    $("chatLog").innerHTML = "";
    addMessage("Nhập câu hỏi như khách hàng để xem bot hiểu thế nào. Mỗi câu trả lời hiện kèm intent và độ tin cậy của mô hình.", "bot");
    renderChips(SAMPLE_QUESTIONS);
}

/* ============================================================ Khởi động */
if (await initAdminLayout({ title: "Chatbot AI" })) {
    resetChat();
    $("chatForm").addEventListener("submit", e => {
        e.preventDefault();
        const input = /** @type {HTMLInputElement} */ ($("chatInput"));
        const text = input.value.trim();
        if (!text) return;
        input.value = "";
        ask(text);
    });
    $("chatChips").addEventListener("click", e => {
        const chip = /** @type {HTMLElement} */ (e.target).closest(".cb-chip");
        if (chip) ask(chip.textContent ?? "");
    });
    $("btnClearChat").addEventListener("click", resetChat);
    $("daysSelect").addEventListener("change", loadStats);
    $("btnReload").addEventListener("click", () => { loadStats(); loadModel(); toast.info("Đã tải lại."); });
    loadStats();
    loadModel();
}
