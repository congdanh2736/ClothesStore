/*
 * ============================================================================
 *  orderApi.js  -  GỌI API ĐƠN HÀNG VÀ CHI TIẾT ĐƠN HÀNG   (Nhóm 4)
 * ============================================================================
 *
 *  File này gom 2 nhóm API:
 *    1. orderApi     -> OrderController     (/api/Order)
 *    2. orderItemApi -> OrderItemController (/api/OrderItem)
 *
 *  Cách dùng:
 *      import { orderApi, orderItemApi } from "../../../assets/js/api/orderApi.js";
 * ============================================================================
 */

import { get, post, put, patch, del } from "../core/http.js";

/* ============================================================================
 *  1. ĐƠN HÀNG  -  /api/Order
 * ========================================================================== */

const ORDER = "/Order";

export const orderApi = {
    /**
     * Danh sách tất cả đơn hàng (chỉ Admin/Employee), đơn mới nhất đứng đầu.
     * Mỗi đơn (OrderDetailDTO):
     *   { id, orderDate, customerId, customerName, status, addressId, address,
     *     promotionId, promotionCode, storeId, storeName, paymentMethod,
     *     paymentStatus, shipmentStatus, trackingNumber, subtotal,
     *     discountAmount, totalAmount, orderItems: [...] }
     */
    getAll: () => get(ORDER),

    /** Chi tiết một đơn hàng theo mã. @param {number} id */
    getById: id => get(`${ORDER}/${id}`),

    /** Danh sách đơn hàng của một khách hàng. @param {number} customerId */
    getByCustomer: customerId => get(`${ORDER}/customer/${customerId}`),

    /**
     * Đặt hàng từ giỏ hàng của khách.
     * @param {{ customerId: number, addressId: number, promotionCode?: string|null, storeId?: number|null, paymentMethod?: string }} data
     */
    checkout: data => post(`${ORDER}/checkout`, data),

    /**
     * Tạo đơn hàng trực tiếp (nhân viên tạo đơn tại quầy).
     * @param {{ customerId: number, addressId: number, promotionId?: number|null, storeId?: number|null, paymentMethod?: string, items: Array<{ variantId: number, quantity: number }> }} data
     */
    create: data => post(ORDER, data),

    /**
     * Sửa thông tin đơn hàng (chỉ khi đơn đang ở trạng thái Pending).
     * @param {number} id
     * @param {{ addressId: number, promotionId?: number|null, items?: Array<{ variantId: number, quantity: number }> }} data
     */
    update: (id, data) => put(`${ORDER}/${id}`, data),

    /** Xác nhận đơn hàng: Pending -> Confirmed. @param {number} id */
    confirm: id => patch(`${ORDER}/${id}/confirm`),

    /** Hủy đơn hàng (hoàn kho, cập nhật trạng thái thanh toán). @param {number} id */
    cancel: id => patch(`${ORDER}/${id}/cancel`),

    /** Hoàn tất đơn hàng (nhận tại cửa hàng -> Completed, cộng điểm, thống kê). @param {number} id */
    complete: id => patch(`${ORDER}/${id}/complete`),

    /** Xóa đơn hàng (chỉ Admin, chỉ xóa được đơn đã hủy). @param {number} id */
    remove: id => del(`${ORDER}/${id}`),

    /**
     * Danh sách phiên bản sản phẩm (API của Nhóm 2: GET /api/ProductVariant) để chọn khi tạo/sửa đơn.
     * Mỗi dòng: { id, productId, productName, color, size, price }
     */
    getVariants: () => get("/ProductVariant"),

    /**
     * Danh sách cửa hàng (API của Nhóm 5: GET /api/Store) để chọn cửa hàng xuất kho.
     * Mỗi dòng: { id, name, location }
     */
    getStores: () => get("/Store")
};

/* ============================================================================
 *  2. CHI TIẾT ĐƠN HÀNG (TỪNG MÓN TRONG ĐƠN)  -  /api/OrderItem
 *  Chỉ thêm / sửa / xóa được khi đơn hàng đang ở trạng thái Pending.
 * ========================================================================== */

const ORDER_ITEM = "/OrderItem";

export const orderItemApi = {
    /** Tất cả dòng chi tiết đơn hàng (chỉ Admin/Employee). */
    getAll: () => get(ORDER_ITEM),

    /** Một dòng chi tiết đơn hàng theo mã. @param {number} id */
    getById: id => get(`${ORDER_ITEM}/${id}`),

    /**
     * Thêm sản phẩm vào đơn hàng (nếu trùng biến thể sẽ tự cộng dồn số lượng).
     * @param {{ orderId: number, variantId: number, quantity: number }} data
     */
    create: data => post(ORDER_ITEM, data),

    /**
     * Sửa biến thể hoặc số lượng của một dòng trong đơn.
     * @param {number} id - orderItemId
     * @param {{ variantId: number, quantity: number }} data
     */
    update: (id, data) => put(`${ORDER_ITEM}/${id}`, data),

    /**
     * Xóa một dòng sản phẩm khỏi đơn (đơn phải còn ít nhất 1 sản phẩm).
     * @param {number} id - orderItemId
     */
    remove: id => del(`${ORDER_ITEM}/${id}`)
};
