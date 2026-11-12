/*
 * ============================================================================
 *  chatbot.js - KHUNG CHAT "AURA STYLIST" (trợ lý tư vấn AI) Ở GÓC PHẢI MỌI TRANG SHOP
 * ============================================================================
 *  - shell.js gọi mountChatbot() trong initShop() nên trang nào cũng có.
 *  - Gọi API POST /api/Chatbot/ask (mô hình do nhóm tự huấn luyện, chạy trên server của shop).
 *  - Lịch sử chat + ngữ cảnh lưu trong sessionStorage: chuyển trang vẫn còn, đóng tab thì mất.
 *  - Đang ở trang chi tiết sản phẩm -> gửi kèm id sản phẩm, khách hỏi "còn size L không" là bot hiểu.
 *  - Mỗi câu trả lời có nút 👍 / 👎 (gửi về /api/Chatbot/feedback để admin xem lại).
 * ============================================================================
 */
import { shopApi } from "./shopApi.js";
import { escapeHtml, formatMoney } from "../../assets/js/core/utils.js";

const STORAGE_KEY = "aura-chat-v1";
const MAX_MESSAGES = 40;
const WELCOME = "Chào bạn! Mình là AURA Stylist 👋\nMình có thể tìm sản phẩm, chọn size theo chiều cao cân nặng, gợi ý cách phối đồ, xem mã khuyến mãi, cửa hàng và đơn hàng của bạn.";
const STARTERS = ["Cao 1m70 nặng 65kg mặc size gì?", "Đi đám cưới nên mặc gì?", "Tìm áo khoác dưới 500k", "Có mã giảm giá không?"];

const svg = (/** @type {string} */ d) => `<svg viewBox="0 0 24 24" aria-hidden="true">${d}</svg>`;
const ICON = {
    chat: svg('<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/><path d="M8 9h8"/><path d="M8 13h5"/>'),
    close: svg('<path d="M18 6 6 18"/><path d="m6 6 12 12"/>'),
    reset: svg('<path d="M3 12a9 9 0 1 0 3-6.7L3 8"/><path d="M3 3v5h5"/>'),
    send: svg('<path d="m22 2-7 20-4-9-9-4z"/><path d="M22 2 11 13"/>'),
    up: svg('<path d="M7 10v11"/><path d="M15 5.9 14 10h5.8a2 2 0 0 1 2 2.3l-1.4 7A2 2 0 0 1 18.4 21H7V10l4-8a3 3 0 0 1 4 3.9z"/>'),
    down: svg('<path d="M17 14V3"/><path d="M9 18.1 10 14H4.2a2 2 0 0 1-2-2.3l1.4-7A2 2 0 0 1 5.6 3H17v11l-4 8a3 3 0 0 1-4-3.9z"/>')
};

/**
 * @typedef {{ who: "user" | "bot", text: string, products?: any[], links?: any[], logId?: number | null, feedback?: boolean | null }} ChatMessage
 * @typedef {{ open: boolean, messages: ChatMessage[], context: any, suggestions: string[] }} ChatState
 */

/** @returns {ChatState} */
function loadState() {
    try {
        const saved = JSON.parse(sessionStorage.getItem(STORAGE_KEY) ?? "null");
        if (saved && Array.isArray(saved.messages)) return saved;
    } catch { /* trình duyệt chặn lưu: dùng trạng thái mới */ }
    return { open: false, messages: [{ who: "bot", text: WELCOME }], context: null, suggestions: STARTERS };
}

/** @param {ChatState} state */
function saveState(state) {
    try {
        state.messages = state.messages.slice(-MAX_MESSAGES);
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch { /* bỏ qua */ }
}

/** Id sản phẩm nếu đang ở trang chi tiết sản phẩm */
function currentProductId() {
    if (!location.pathname.endsWith("/shop/product.html")) return null;
    const id = Number(new URLSearchParams(location.search).get("id"));
    return Number.isFinite(id) && id > 0 ? id : null;
}

let mounted = false;

export function mountChatbot() {
    if (mounted || document.getElementById("auraChat")) return;
    mounted = true;
    const state = loadState();

    const root = document.createElement("div");
    root.id = "auraChat";
    root.className = "aura-chat";
    root.innerHTML = `
        <button type="button" class="aura-chat-fab" id="auraChatFab" aria-label="Mở trợ lý tư vấn AI" aria-expanded="false">
            ${ICON.chat}<span class="aura-chat-fab-text">Tư vấn AI</span>
        </button>
        <section class="aura-chat-panel" id="auraChatPanel" role="dialog" aria-label="AURA Stylist - trợ lý tư vấn" hidden>
            <header class="aura-chat-head">
                <span class="aura-chat-avatar" aria-hidden="true">AI</span>
                <div class="aura-chat-title"><b>AURA Stylist</b><small><span class="live-dot"></span> Trợ lý tư vấn thời trang</small></div>
                <button type="button" class="aura-chat-icon" id="auraChatReset" title="Cuộc trò chuyện mới" aria-label="Cuộc trò chuyện mới">${ICON.reset}</button>
                <button type="button" class="aura-chat-icon" id="auraChatClose" title="Thu nhỏ" aria-label="Đóng">${ICON.close}</button>
            </header>
            <div class="aura-chat-log" id="auraChatLog" aria-live="polite"></div>
            <div class="aura-chat-chips" id="auraChatChips"></div>
            <form class="aura-chat-form" id="auraChatForm">
                <input class="aura-chat-input" id="auraChatInput" maxlength="500" autocomplete="off"
                       placeholder="Hỏi về size, phối đồ, sản phẩm..." aria-label="Nhập câu hỏi">
                <button type="submit" class="aura-chat-send" aria-label="Gửi">${ICON.send}</button>
            </form>
            <p class="aura-chat-note">Bot do nhóm tự huấn luyện, có thể trả lời chưa chính xác.</p>
        </section>`;
    document.body.append(root);

    const $ = (/** @type {string} */ id) => /** @type {HTMLElement} */ (root.querySelector("#" + id));
    const panel = $("auraChatPanel"), log = $("auraChatLog"), chips = $("auraChatChips");
    const input = /** @type {HTMLInputElement} */ ($("auraChatInput"));
    let busy = false;

    /* ---------------- Vẽ tin nhắn ---------------- */
    /** @param {ChatMessage} m @param {number} index */
    function messageHtml(m, index) {
        if (m.who === "user") return `<div class="aura-msg user">${escapeHtml(m.text)}</div>`;
        const products = (m.products ?? []).map(p => `
            <a class="aura-msg-product" href="/shop/product.html?id=${p.id}">
                ${p.imageUrl ? `<img src="${escapeHtml(p.imageUrl)}" alt="" loading="lazy">` : `<span class="aura-msg-noimg"></span>`}
                <span class="name">${escapeHtml(p.name)}</span>
                <span class="price">${formatMoney(p.minPrice)}</span>
            </a>`).join("");
        const links = (m.links ?? []).map(l => `<a class="aura-msg-link" href="${escapeHtml(l.url)}">${escapeHtml(l.text)} →</a>`).join("");
        const feedback = m.logId ? `
            <div class="aura-msg-feedback">
                <button type="button" data-fb="${index}" data-v="1" class="${m.feedback === true ? "on" : ""}" ${m.feedback != null ? "disabled" : ""} aria-label="Hữu ích">${ICON.up}</button>
                <button type="button" data-fb="${index}" data-v="0" class="${m.feedback === false ? "on" : ""}" ${m.feedback != null ? "disabled" : ""} aria-label="Chưa đúng">${ICON.down}</button>
                ${m.feedback != null ? `<span>Cảm ơn bạn đã đánh giá!</span>` : ""}
            </div>` : "";
        return `<div class="aura-msg bot">
            <div class="aura-msg-text">${escapeHtml(m.text)}</div>
            ${products ? `<div class="aura-msg-products">${products}</div>` : ""}
            ${links ? `<div class="aura-msg-links">${links}</div>` : ""}
            ${feedback}
        </div>`;
    }

    function render() {
        log.innerHTML = state.messages.map(messageHtml).join("")
            + (busy ? `<div class="aura-msg bot typing"><span></span><span></span><span></span></div>` : "");
        chips.innerHTML = busy ? "" : state.suggestions.slice(0, 4)
            .map(s => `<button type="button" class="aura-chip">${escapeHtml(s)}</button>`).join("");
        log.scrollTop = log.scrollHeight;
    }

    /** @param {boolean} open */
    function setOpen(open) {
        state.open = open;
        panel.hidden = !open;
        root.classList.toggle("is-open", open);
        $("auraChatFab").setAttribute("aria-expanded", String(open));
        saveState(state);
        if (open) { render(); setTimeout(() => input.focus(), 50); }
    }

    /* ---------------- Gửi câu hỏi ---------------- */
    /** @param {string} text */
    async function send(text) {
        text = text.trim();
        if (!text || busy) return;
        state.messages.push({ who: "user", text });
        busy = true;
        render();

        const context = { ...(state.context ?? {}), productId: currentProductId() ?? state.context?.productId ?? null };
        try {
            const r = await shopApi.chatbot.ask(text, context);
            state.context = r.context;
            state.suggestions = r.suggestions?.length ? r.suggestions : STARTERS;
            state.messages.push({ who: "bot", text: r.reply, products: (r.products ?? []).slice(0, 6), links: r.links ?? [], logId: r.logId ?? null, feedback: null });
        } catch (err) {
            state.messages.push({ who: "bot", text: "Xin lỗi, mình đang gặp sự cố kết nối. Bạn thử lại sau ít phút nhé.\n(" + (err instanceof Error ? err.message : String(err)) + ")" });
        }
        busy = false;
        saveState(state);
        render();
    }

    /* ---------------- Sự kiện ---------------- */
    $("auraChatFab").addEventListener("click", () => setOpen(!state.open));
    $("auraChatClose").addEventListener("click", () => setOpen(false));
    $("auraChatReset").addEventListener("click", () => {
        state.messages = [{ who: "bot", text: WELCOME }];
        state.context = null;
        state.suggestions = STARTERS;
        saveState(state);
        render();
    });
    $("auraChatForm").addEventListener("submit", e => {
        e.preventDefault();
        const text = input.value;
        input.value = "";
        send(text);
    });
    chips.addEventListener("click", e => {
        const chip = /** @type {HTMLElement} */ (e.target).closest(".aura-chip");
        if (chip) send(chip.textContent ?? "");
    });
    log.addEventListener("click", async e => {
        const btn = /** @type {HTMLButtonElement | null} */ (/** @type {HTMLElement} */ (e.target).closest("[data-fb]"));
        if (!btn) return;
        const message = state.messages[Number(btn.dataset.fb)];
        if (!message?.logId || message.feedback != null) return;
        message.feedback = btn.dataset.v === "1";
        saveState(state);
        render();
        try { await shopApi.chatbot.feedback(message.logId, message.feedback); } catch { /* đánh giá lỗi thì bỏ qua */ }
    });
    document.addEventListener("keydown", e => { if (e.key === "Escape" && state.open) setOpen(false); });

    setOpen(state.open);
}
