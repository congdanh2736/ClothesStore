/*
 * ============================================================================
 *  shipmentApi.js  -  GỌI API VẬN CHUYỂN (VẬN ĐƠN)   (Nhóm 4)
 * ============================================================================
 *
 *  ShipmentController  ->  /api/Shipment
 *
 *  Cách dùng:
 *      import { shipmentApi } from "../../../assets/js/api/shipmentApi.js";
 * ============================================================================
 */

import { get, post, put, patch } from "../core/http.js";

const SHIPMENT = "/Shipment";

export const shipmentApi = {
    /**
     * Danh sách tất cả vận đơn (chỉ Admin/Employee).
     * Mỗi vận đơn: { id, orderId, carrier, trackingNumber, shippedDate, deliveredDate, status, orderStatus }
     */
    getAll: () => get(SHIPMENT),

    /** Một vận đơn theo mã. @param {number} id */
    getById: id => get(`${SHIPMENT}/${id}`),

    /**
     * Tra cứu vận đơn của một đơn hàng (nếu đơn chưa có vận đơn thì backend trả 404).
     * @param {number} orderId
     */
    getByOrder: orderId => get(`${SHIPMENT}/order/${orderId}`),

    /**
     * Tạo vận đơn cho đơn hàng đã xác nhận (Confirmed) -> đơn tự chuyển sang Shipping.
     * @param {{ orderId: number, carrier: string, trackingNumber?: string|null }} data
     */
    create: data => post(SHIPMENT, data),

    /**
     * Sửa đơn vị vận chuyển / mã vận đơn (khi chưa Delivered).
     * @param {number} id
     * @param {{ carrier: string, trackingNumber?: string|null }} data
     */
    update: (id, data) => put(`${SHIPMENT}/${id}`, data),

    /**
     * Cập nhật trạng thái vận chuyển: Shipping | Delivered | Failed.
     * Khi chuyển sang Delivered, backend tự động hoàn tất đơn hàng (thu tiền COD, cộng điểm, thống kê).
     * @param {number} id
     * @param {{ status: string }} data
     */
    updateStatus: (id, data) => patch(`${SHIPMENT}/${id}/status`, data)
};
