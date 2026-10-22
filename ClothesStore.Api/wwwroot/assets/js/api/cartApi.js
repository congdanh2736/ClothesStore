/*
 * ============================================================================
 *  cartApi.js  -  API GIỎ HÀNG (CartController: /api/carts)
 * ============================================================================
 */

import { get, post, put, del } from "../core/http.js";

const BASE = "/carts";

export const cartApi = {
    /**
     * Xem giỏ hàng của khách hàng theo customerId.
     * @param {number} customerId
     * @returns {Promise<Object>}
     */
    getCart: customerId => get(`${BASE}/${customerId}`),

    /**
     * Thêm sản phẩm (biến thể) vào giỏ hàng.
     * @param {{ customerId: number, variantId: number, quantity: number }} data
     * @returns {Promise<Object>}
     */
    addItem: data => post(`${BASE}/items`, data),

    /**
     * Cập nhật số lượng của một mặt hàng trong giỏ.
     * @param {{ cartItemId: number, quantity: number }} data
     * @returns {Promise<Object>}
     */
    updateQuantity: data => put(`${BASE}/items`, data),

    /**
     * Xóa một mặt hàng khỏi giỏ hàng theo cartItemId.
     * @param {number} cartItemId
     * @returns {Promise<Object>}
     */
    removeItem: cartItemId => del(`${BASE}/items/${cartItemId}`),

    /**
     * Làm trống (xóa toàn bộ) giỏ hàng của khách hàng.
     * @param {number} customerId
     * @returns {Promise<Object>}
     */
    clearCart: customerId => del(`${BASE}/${customerId}/clear`)
};
