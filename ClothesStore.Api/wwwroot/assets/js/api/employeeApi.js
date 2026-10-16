/*
 * ============================================================================
 *  employeeApi.js  -  GỌI API NHÂN VIÊN   (Nhóm 1)
 * ============================================================================
 *
 *  EmployeeController  ->  /api/Employee      (TOÀN BỘ API chỉ Admin gọi được)
 *
 *  Thêm nhân viên = backend tạo luôn TÀI KHOẢN ĐĂNG NHẬP (vai trò Employee).
 *  Xóa nhân viên  = backend xóa luôn tài khoản đăng nhập đó.
 *
 *  Quy ước họ tên giống customerApi.js: firstName = HỌ (và tên đệm), lastName = TÊN.
 *
 *  Cách dùng:
 *      import { employeeApi } from "../../../assets/js/api/employeeApi.js";
 *      const employees = await employeeApi.getAll();
 * ============================================================================
 */

import { get, post, put, del } from "../core/http.js";

const BASE = "/Employee";

export const employeeApi = {
    /**
     * Danh sách nhân viên.
     * Mỗi nhân viên: { id, fullName, firstName, lastName, email, storeId, storeName }
     */
    getAll: () => get(BASE),

    /** @param {number} id */
    getById: id => get(`${BASE}/${id}`),

    /**
     * Thêm nhân viên (kèm tài khoản đăng nhập).
     * @param {{ email: string, password: string, firstName: string, lastName: string, storeId?: number|null }} data
     *        storeId bỏ trống (null) = nhân viên không thuộc cửa hàng nào (ví dụ quản trị website).
     *        password: ít nhất 6 ký tự, có chữ HOA, chữ thường và chữ số (quy định của ASP.NET Identity).
     */
    create: data => post(BASE, data),

    /**
     * Sửa nhân viên (không đổi được email / mật khẩu ở đây).
     * @param {number} id
     * @param {{ firstName: string, lastName: string, storeId?: number|null }} data
     *        LƯU Ý: storeId gửi null = gỡ nhân viên khỏi cửa hàng. Muốn giữ cửa hàng cũ thì gửi lại storeId cũ.
     */
    update: (id, data) => put(`${BASE}/${id}`, data),

    /** Xóa nhân viên (và tài khoản đăng nhập). @param {number} id */
    remove: id => del(`${BASE}/${id}`),

    /**
     * Danh sách cửa hàng để chọn trong form (API của Nhóm 5: GET /api/Store, ai cũng gọi được).
     * Để tạm ở đây; khi Nhóm 5 làm xong storeApi.js thì có thể chuyển sang storeApi.getAll.
     * Mỗi cửa hàng: { id, name, location }
     */
    getStores: () => get("/Store")
};
