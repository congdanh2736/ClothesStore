/*
 * ============================================================================
 *  wishlistApi.js  -  API DANH SÁCH YÊU THÍCH (WishlistController: /api/Wishlist)
 * ============================================================================
 */

import { get, post, del } from "../core/http.js";

const BASE = "/Wishlist";

export const wishlistApi = {
    /**
     * Lấy toàn bộ danh sách yêu thích trong hệ thống.
     * @returns {Promise<Array>}
     */
    getAll: () => get(BASE),

    /**
     * Lấy danh sách sản phẩm yêu thích của một khách hàng.
     * @param {number} customerId
     * @returns {Promise<Array>}
     */
    getByCustomer: customerId => get(`${BASE}/customer/${customerId}`),

    /**
     * Kiểm tra xem sản phẩm đã có trong danh sách yêu thích của khách hàng chưa.
     * @param {number} customerId
     * @param {number} productId
     * @returns {Promise<{ isWishlisted: boolean }>}
     */
    check: (customerId, productId) => get(`${BASE}/check`, { customerId, productId }),

    /**
     * Bật / tắt (toggle) trạng thái yêu thích một sản phẩm.
     * @param {number} customerId
     * @param {number} productId
     * @returns {Promise<{ message: string, isWishlisted: boolean, data?: Object }>}
     */
    toggle: (customerId, productId) => post(`${BASE}/toggle`, null, { customerId, productId }),

    /**
     * Thêm sản phẩm vào danh sách yêu thích.
     * @param {{ customerId: number, productId: number }} data
     * @returns {Promise<Object>}
     */
    create: data => post(BASE, data),

    /**
     * Xóa khỏi danh sách yêu thích theo mã Wishlist ID.
     * @param {number} id
     * @returns {Promise<void>}
     */
    remove: id => del(`${BASE}/${id}`),

    /**
     * Xóa khỏi danh sách yêu thích theo customerId và productId.
     * @param {number} customerId
     * @param {number} productId
     * @returns {Promise<void>}
     */
    removeByCustomerAndProduct: (customerId, productId) =>
        del(`${BASE}/customer/${customerId}/product/${productId}`)
};
