/*
 * chatbotApi.js - GỌI API CHATBOT AI (trang admin)
 *   /api/Chatbot/ask           : hỏi bot (dùng cho ô "Chat thử" ở trang tổng quan)
 *   /api/ChatbotAdmin/...      : thống kê, lịch sử hội thoại, dữ liệu huấn luyện, huấn luyện lại
 */
import { get, post, put, patch, del } from "../core/http.js";

export const chatbotApi = {
    /** Hỏi bot. context = phần "context" của câu trả lời trước (để hỏi nối tiếp) */
    ask: (message, context = null) => post("/Chatbot/ask", { message, context }),

    // ---- Thống kê + lịch sử hội thoại ----
    /** @param {number} days */
    getStats: (days = 30) => get("/ChatbotAdmin/stats", { days }),
    /** @param {{ intent?: string, filter?: string, q?: string, page?: number, pageSize?: number }} query */
    getLogs: query => get("/ChatbotAdmin/logs", query),
    /** Đánh dấu đã xem, gán intent đúng, đưa vào dữ liệu huấn luyện */
    reviewLog: (id, data) => patch(`/ChatbotAdmin/logs/${id}/review`, data),
    deleteLog: id => del(`/ChatbotAdmin/logs/${id}`),

    // ---- Dữ liệu huấn luyện ----
    getIntents: () => get("/ChatbotAdmin/intents"),
    getSamples: (label = "") => get("/ChatbotAdmin/samples", { label }),
    createSample: data => post("/ChatbotAdmin/samples", data),
    updateSample: (id, data) => put(`/ChatbotAdmin/samples/${id}`, data),
    deleteSample: id => del(`/ChatbotAdmin/samples/${id}`),

    // ---- Mô hình + huấn luyện lại ----
    getModel: () => get("/ChatbotAdmin/model"),
    train: () => post("/ChatbotAdmin/train"),
    getTraining: id => get(`/ChatbotAdmin/train/${id}`)
};

/** Tên tiếng Việt của intent (giống ChatIntents.Labels ở backend) */
export const INTENT_LABELS = Object.freeze({
    tim_san_pham: "Tìm sản phẩm",
    tu_van_size: "Tư vấn size",
    tu_van_dip: "Tư vấn theo dịp",
    tu_van_thoi_tiet: "Thời tiết / chất liệu",
    phoi_do: "Phối đồ",
    khuyen_mai: "Khuyến mãi",
    con_hang: "Còn hàng",
    cua_hang: "Cửa hàng",
    chinh_sach: "Chính sách",
    don_hang: "Đơn hàng",
    chao_hoi: "Chào hỏi",
    cam_on: "Cảm ơn / tạm biệt",
    ngoai_pham_vi: "Ngoài phạm vi",
    khong_hieu: "Chưa hiểu"
});

/** Danh sách lựa chọn intent cho ô select (không gồm "Chưa hiểu") */
export const INTENT_OPTIONS = Object.entries(INTENT_LABELS)
    .filter(([key]) => key !== "khong_hieu")
    .map(([value, label]) => ({ value, label: `${label} (${value})` }));

/** Độ tin cậy dưới mức này = bot không chắc chắn (giống backend) */
export const LOW_CONFIDENCE = 0.5;
