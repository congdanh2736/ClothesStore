/*
 * ============================================================================
 *  membershipApi.js  -  GỌI API HẠNG THÀNH VIÊN   (Nhóm 1)
 * ============================================================================
 *
 *  MembershipTierController  ->  /api/MembershipTier
 *
 *  Hạng thành viên (Đồng / Bạc / Vàng / VIP...) xác định bằng "điểm tối thiểu":
 *  khách có tổng điểm >= minPoints của hạng nào cao nhất thì thuộc hạng đó.
 *  Backend tự xếp hạng lại mỗi khi điểm của khách thay đổi.
 *
 *  Quyền:
 *    - Xem danh sách: ai cũng xem được (không cần đăng nhập)
 *    - Thêm / sửa / xóa: chỉ Admin
 *
 *  Cách dùng:
 *      import { membershipApi } from "../../../assets/js/api/membershipApi.js";
 *      const tiers = await membershipApi.getAll();
 * ============================================================================
 */

import { get, post, put, del } from "../core/http.js";

const BASE = "/MembershipTier";

export const membershipApi = {
    /**
     * Danh sách hạng.
     * Mỗi hạng: { id, tierName, minPoints, customerCount }
     *   customerCount = số khách đang ở hạng này.
     */
    getAll: () => get(BASE),

    /** @param {number} id */
    getById: id => get(`${BASE}/${id}`),

    /**
     * Thêm hạng (chỉ Admin). Hai hạng không được trùng minPoints.
     * @param {{ tierName: string, minPoints: number }} data
     */
    create: data => post(BASE, data),

    /**
     * Sửa hạng (chỉ Admin).
     * @param {number} id
     * @param {{ tierName: string, minPoints: number }} data
     */
    update: (id, data) => put(`${BASE}/${id}`, data),

    /**
     * Xóa hạng (chỉ Admin). Hạng đang có khách thì server từ chối.
     * @param {number} id
     */
    remove: id => del(`${BASE}/${id}`)
};
