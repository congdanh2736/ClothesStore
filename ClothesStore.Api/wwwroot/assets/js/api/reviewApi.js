/*
 * ============================================================================
 *  reviewApi.js  -  API ĐÁNH GIÁ SẢN PHẨM (ReviewController: /api/Review)
 * ============================================================================
 */

import { get, post, put, del } from "../core/http.js";

const BASE = "/Review";

export const reviewApi = {
    /**
     * Lấy tất cả đánh giá sản phẩm.
     * @returns {Promise<Array>}
     */
    getAll: () => get(BASE),

    /**
     * Lấy chi tiết đánh giá theo ID.
     * @param {number} id
     * @returns {Promise<Object>}
     */
    getById: id => get(`${BASE}/${id}`),

    /**
     * Lấy danh sách đánh giá của một sản phẩm.
     * @param {number} productId
     * @returns {Promise<Array>}
     */
    getByProduct: productId => get(`${BASE}/product/${productId}`),

    /**
     * Lấy tóm tắt thống kê đánh giá của sản phẩm (sao TB, tổng đánh giá, phân bố sao).
     * @param {number} productId
     * @returns {Promise<Object>}
     */
    getProductSummary: productId => get(`${BASE}/product/${productId}/summary`),

    /**
     * Lấy danh sách đánh giá của một khách hàng.
     * @param {number} customerId
     * @returns {Promise<Array>}
     */
    getByCustomer: customerId => get(`${BASE}/customer/${customerId}`),

    /**
     * Tạo đánh giá mới cho sản phẩm.
     * @param {{ customerId: number, productId: number, rating: number, comment?: string, imageUrls?: string[] }} data
     * @returns {Promise<Object>}
     */
    create: data => post(BASE, data),

    /**
     * Cập nhật đánh giá đã viết.
     * @param {number} id
     * @param {{ rating: number, comment?: string, imageUrls?: string[] }} data
     * @returns {Promise<void>}
     */
    update: (id, data) => put(`${BASE}/${id}`, data),

    /**
     * Xóa đánh giá.
     * @param {number} id
     * @returns {Promise<void>}
     */
    remove: id => del(`${BASE}/${id}`)
};
