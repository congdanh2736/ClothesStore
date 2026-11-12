/*
 * ============================================================================
 *  promotionApi.js  -  GỌI API KHUYẾN MÃI (VOUCHER)   (Nhóm 4)
 * ============================================================================
 *
 *  PromotionController  ->  /api/Promotion
 *
 *  Cách dùng:
 *      import { promotionApi } from "../../../assets/js/api/promotionApi.js";
 * ============================================================================
 */

import { get, post, put, del } from "../core/http.js";

const BASE = "/Promotion";

export const promotionApi = {
    /**
     * Danh sách tất cả mã khuyến mãi.
     * Mỗi dòng: { id, code, discountValue, startDate, endDate }
     */
    getAll: () => get(BASE),

    /** Một mã khuyến mãi theo id. @param {number} id */
    getById: id => get(`${BASE}/${id}`),

    /**
     * Thêm mã khuyến mãi mới (chỉ Admin/Employee).
     * @param {{ code: string, discountValue: number, startDate: string, endDate: string }} data
     */
    create: data => post(BASE, data),

    /**
     * Sửa mã khuyến mãi (chỉ Admin/Employee).
     * @param {number} id
     * @param {{ code: string, discountValue: number, startDate: string, endDate: string }} data
     */
    update: (id, data) => put(`${BASE}/${id}`, data),

    /**
     * Xóa mã khuyến mãi (chỉ Admin/Employee). Mã đã áp dụng cho đơn hàng không xóa được.
     * @param {number} id
     */
    remove: id => del(`${BASE}/${id}`)
};
