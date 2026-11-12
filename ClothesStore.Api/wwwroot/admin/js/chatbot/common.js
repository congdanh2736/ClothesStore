/*
 * common.js - HÀM DÙNG CHUNG CHO 3 TRANG QUẢN LÝ CHATBOT
 */
import { escapeHtml } from "../../../assets/js/core/utils.js";
import { INTENT_LABELS, LOW_CONFIDENCE } from "../../../assets/js/api/chatbotApi.js";

/** 0.8734 -> "87,3%" */
export function percent(value, digits = 1) {
    if (value === null || value === undefined) return "—";
    return (value * 100).toLocaleString("vi-VN", { minimumFractionDigits: digits, maximumFractionDigits: digits }) + "%";
}

/** Tên tiếng Việt của intent */
export const intentLabel = intent => INTENT_LABELS[intent] ?? intent ?? "—";

/** Nhãn intent: "Chưa hiểu" màu đỏ, còn lại màu xanh */
export function intentBadge(intent) {
    const color = intent === "khong_hieu" ? "danger" : intent === "ngoai_pham_vi" ? "secondary" : "info";
    return `<span class="badge badge-${color}">${escapeHtml(intentLabel(intent))}</span>`;
}

/** Độ tin cậy: thanh nhỏ màu đỏ / vàng / xanh + số % */
export function confidenceHtml(value) {
    const level = value < LOW_CONFIDENCE ? "is-low" : value < 0.8 ? "is-mid" : "";
    return `<span class="cb-conf ${level}" title="Độ tin cậy của mô hình">
        <span class="cb-conf-track"><span class="cb-conf-fill" style="width:${Math.round(value * 100)}%"></span></span>${percent(value, 0)}</span>`;
}

/** Độ chính xác có màu: >= 90% xanh, >= 75% vàng, còn lại đỏ */
export function accuracyHtml(value) {
    if (value === null || value === undefined) return "—";
    const cls = value >= 0.9 ? "good" : value >= 0.75 ? "mid" : "bad";
    return `<span class="cb-pct ${cls}">${percent(value)}</span>`;
}

/** Khách đánh giá */
export function feedbackHtml(isHelpful) {
    if (isHelpful === true) return `<span class="badge badge-success" title="Khách thấy hữu ích">👍 Hữu ích</span>`;
    if (isHelpful === false) return `<span class="badge badge-danger" title="Khách thấy chưa đúng">👎 Chưa đúng</span>`;
    return `<span class="cb-muted">—</span>`;
}
