/*
 * ============================================================================
 *  returnApi.js  -  API ĐỔI / TRẢ HÀNG (ReturnRequestController: /api/ReturnRequest)
 * ============================================================================
 *  Dùng cho cả Admin/Nhân viên quản lý và Khách hàng gửi yêu cầu trả hàng.
 * ============================================================================
 */

import { get, post, patch, del } from "../core/http.js";

const BASE = "/ReturnRequest";

export const returnApi = {
    /**
     * Lấy toàn bộ danh sách yêu cầu đổi/trả (Chỉ Staff/Admin).
     * @returns {Promise<Array>}
     */
    getAll: () => get(BASE),

    /**
     * Lấy chi tiết yêu cầu theo ID.
     * @param {number} id
     * @returns {Promise<Object>}
     */
    getById: id => get(`${BASE}/${id}`),

    /**
     * Lấy danh sách yêu cầu theo mã đơn hàng.
     * @param {number} orderId
     * @returns {Promise<Array>}
     */
    getByOrder: orderId => get(`${BASE}/order/${orderId}`),

    /**
     * Lấy danh sách yêu cầu theo mã khách hàng.
     * @param {number} customerId
     * @returns {Promise<Array>}
     */
    getByCustomer: customerId => get(`${BASE}/customer/${customerId}`),

    /**
     * Khách hàng tạo yêu cầu đổi/trả cho đơn hàng đã hoàn tất.
     * @param {{ orderId: number, reason: string, items: Array<{ orderItemId: number, quantity: number, condition?: string }> }} data
     * @returns {Promise<Object>}
     */
    create: data => post(BASE, data),

    /**
     * Duyệt yêu cầu đổi/trả (Staff/Admin). Trạng thái: Pending -> Approved.
     * @param {number} id
     * @returns {Promise<Object>}
     */
    approve: id => patch(`${BASE}/${id}/approve`),

    /**
     * Từ chối yêu cầu đổi/trả (Staff/Admin). Trạng thái: Pending/Approved -> Rejected.
     * @param {number} id
     * @returns {Promise<Object>}
     */
    reject: id => patch(`${BASE}/${id}/reject`),

    /**
     * Xác nhận hoàn tất khi đã nhận lại hàng (Staff/Admin).
     * Tự động hoàn kho, trừ điểm, cập nhật thanh toán và thống kê.
     * Trạng thái: Approved -> Completed.
     * @param {number} id
     * @returns {Promise<Object>}
     */
    complete: id => patch(`${BASE}/${id}/complete`),

    /**
     * Xóa yêu cầu (khi ở trạng thái Pending hoặc Rejected).
     * @param {number} id
     * @returns {Promise<void>}
     */
    remove: id => del(`${BASE}/${id}`)
};
