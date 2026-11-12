/*
 * ============================================================================
 *  paymentApi.js  -  GỌI API GIAO DỊCH THANH TOÁN   (Nhóm 4)
 * ============================================================================
 *
 *  PaymentTransactionController  ->  /api/PaymentTransaction
 *
 *  Cách dùng:
 *      import { paymentApi } from "../../../assets/js/api/paymentApi.js";
 * ============================================================================
 */

import { get, post, put, patch, del } from "../core/http.js";

const BASE = "/PaymentTransaction";

export const paymentApi = {
    /**
     * Danh sách tất cả giao dịch thanh toán (chỉ Admin/Employee).
     * Mỗi dòng: { id, orderId, paymentMethod, status, transactionCode, paidAt, orderTotal }
     */
    getAll: () => get(BASE),

    /** Một giao dịch thanh toán theo mã. @param {number} id */
    getById: id => get(`${BASE}/${id}`),

    /**
     * Tìm giao dịch thanh toán của một đơn hàng.
     * @param {number} orderId
     */
    getByOrder: async orderId => {
        const list = await get(BASE);
        return (Array.isArray(list) ? list : []).find(p => p.orderId === Number(orderId)) ?? null;
    },

    /**
     * Tạo giao dịch thanh toán cho đơn chưa có giao dịch.
     * @param {{ orderId: number, paymentMethod: string }} data
     */
    create: data => post(BASE, data),

    /**
     * Đổi phương thức thanh toán (chỉ khi chưa thanh toán và đơn còn Pending).
     * @param {number} id
     * @param {{ paymentMethod: string }} data
     */
    update: (id, data) => put(`${BASE}/${id}`, data),

    /**
     * Cập nhật kết quả thanh toán (Paid / Failed / Pending) kèm mã giao dịch đối soát.
     * @param {number} id
     * @param {{ status: string, transactionCode?: string|null }} data
     */
    updateStatus: (id, data) => patch(`${BASE}/${id}/status`, data),

    /** Xóa giao dịch thanh toán (chỉ Admin, không xóa được giao dịch đã Paid). @param {number} id */
    remove: id => del(`${BASE}/${id}`)
};
