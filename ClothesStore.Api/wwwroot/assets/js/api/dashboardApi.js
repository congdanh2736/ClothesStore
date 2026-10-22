/*
 * ============================================================================
 *  dashboardApi.js  -  CÁC API MÀ TRANG TỔNG QUAN (admin/dashboard.html) CẦN
 * ============================================================================
 *
 *  Trang tổng quan chỉ ĐỌC dữ liệu (không thêm / sửa / xóa) nên chỉ có hàm get.
 *  Tất cả API dưới đây cần đăng nhập bằng tài khoản Admin hoặc Employee.
 *
 *  Cách dùng:
 *      import { dashboardApi } from "../../assets/js/api/dashboardApi.js";
 *      const orders = await dashboardApi.getOrders();
 *
 *  Mỗi hàm trả về kết quả của http.js: thành công thì ra dữ liệu, lỗi thì
 *  "ném" ApiError có sẵn câu thông báo tiếng Việt (bắt bằng try...catch).
 * ============================================================================
 */

import { get } from "../core/http.js";
import { returnApi } from "./returnApi.js";

export const dashboardApi = {
    /**
     * Tất cả đơn hàng, đơn mới nhất đứng đầu.            GET /api/Order
     * Mỗi đơn: { id, orderDate, customerId, customerName, status, paymentStatus,
     *            totalAmount, orderItems: [...] , ... }
     */
    getOrders: () => get("/Order"),

    /**
     * Thống kê theo ngày của từng cửa hàng.               GET /api/StoreDailyStat
     * Mỗi dòng: { storeId, statDate: "2026-10-20", totalOrders, completedOrders,
     *             canceledOrders, totalRevenue, totalItemsSold }
     */
    getDailyStats: () => get("/StoreDailyStat"),

    /**
     * Thống kê bán ra của từng biến thể theo ngày.        GET /api/StoreItemStat
     * Mỗi dòng: { storeId, variantId, statDate, quantitySold, totalRevenue, returnQuantity }
     */
    getItemStats: () => get("/StoreItemStat"),

    /** Yêu cầu đổi / trả hàng: { id, orderId, status, requestDate, ... }   GET /api/ReturnRequest */
    getReturnRequests: () => returnApi.getAll(),

    /** Vận đơn: { id, orderId, status, orderStatus, ... }                  GET /api/Shipment */
    getShipments: () => get("/Shipment"),

    /**
     * Tồn kho của mọi cửa hàng: { storeId, variantId, productName, color, size, quantity }
     * GET /api/StoreStock  - danh sách RẤT DÀI (vài chục nghìn dòng) nên trang chỉ gọi 1 lần.
     */
    getStocks: () => get("/StoreStock"),

    /**
     * Một biến thể (để lấy tên sản phẩm, màu, size).      GET /api/ProductVariant/{id}
     * Trả về: { id, productId, productName, color, size, price }
     * @param {number} variantId
     */
    getVariant: variantId => get(`/ProductVariant/${variantId}`)
};