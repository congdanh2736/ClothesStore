/*
 * ============================================================================
 *  customerApi.js  -  GỌI API KHÁCH HÀNG, ĐỊA CHỈ, ĐIỂM TÍCH LŨY   (Nhóm 1)
 * ============================================================================
 *
 *  File này gom 3 nhóm API liên quan tới khách hàng:
 *    1. customerApi  -> CustomerController           (/api/Customer)
 *    2. addressApi   -> AddressController            (/api/Address)
 *    3. loyaltyApi   -> LoyaltyTransactionController (/api/LoyaltyTransaction)
 *
 *  Cách dùng:
 *      import { customerApi, addressApi, loyaltyApi } from "../../../assets/js/api/customerApi.js";
 *
 *      const customers = await customerApi.getAll();
 *      const addresses = await addressApi.getByCustomer(5);
 *
 *  QUY ƯỚC VỀ HỌ TÊN (theo backend của nhóm):
 *      firstName = HỌ (và tên đệm), ví dụ "Nguyễn Văn"
 *      lastName  = TÊN,              ví dụ "An"
 *      fullName  = firstName + " " + lastName = "Nguyễn Văn An"
 *  (Xem thông báo lỗi trong CreateEmployeeValidator: FirstName -> "Bắt buộc phải có họ".)
 *
 *  Mỗi hàm trả về kết quả của http.js: thành công thì ra dữ liệu, lỗi thì
 *  "ném" ApiError có sẵn câu thông báo tiếng Việt (bắt bằng try...catch).
 * ============================================================================
 */

import { get, post, put, del } from "../core/http.js";

/* ============================================================================
 *  1. KHÁCH HÀNG  -  /api/Customer
 * ========================================================================== */

const CUSTOMER = "/Customer";

export const customerApi = {
    /**
     * Danh sách khách hàng (chỉ nhân sự Admin/Employee gọi được).
     * Mỗi khách: { id, fullName, firstName, lastName, email, membershipTierId,
     *              membershipTierName, totalPoints, addressCount, wishlistCount }
     */
    getAll: () => get(CUSTOMER),

    /** Một khách hàng theo id. @param {number} id */
    getById: id => get(`${CUSTOMER}/${id}`),

    /**
     * THÊM KHÁCH HÀNG MỚI = tạo luôn TÀI KHOẢN ĐĂNG NHẬP cho khách.
     *
     * Vì sao gọi /Auth/register mà không gọi POST /Customer?
     *   POST /Customer bắt buộc phải có sẵn "ApplicationUserId" (id của một tài khoản
     *   đã tồn tại) -> trang admin không có cách nào biết id đó.
     *   /Auth/register thì làm trọn gói: tạo tài khoản + gán vai trò Customer
     *   + tạo hồ sơ khách hàng + giỏ hàng + hạng mặc định.
     *   (Gọi hàm này KHÔNG làm Admin bị đăng xuất, vì trang không cất kết quả trả về.)
     *
     * @param {{ email: string, password: string, firstName: string, lastName: string }} data
     */
    create: data => post("/Auth/register", data),

    /**
     * Sửa thông tin khách hàng.
     * @param {number} id
     * @param {{ firstName: string, lastName: string, membershipTierId?: number|null }} data
     *        membershipTierId bỏ trống (null) = giữ nguyên hạng hiện tại.
     */
    update: (id, data) => put(`${CUSTOMER}/${id}`, data),

    /**
     * Xóa khách hàng (chỉ Admin). Backend xóa luôn tài khoản đăng nhập, địa chỉ,
     * giỏ hàng, lịch sử điểm. Khách ĐÃ CÓ ĐƠN HÀNG thì server từ chối (báo lỗi 400).
     * @param {number} id
     */
    remove: id => del(`${CUSTOMER}/${id}`),

    /**
     * Các đơn hàng của một khách (API của Nhóm 4: /api/Order/customer/{id}).
     * Để tạm ở đây cho trang chi tiết khách hàng dùng. Khi Nhóm 4 làm xong
     * orderApi.js thì có thể chuyển sang dùng orderApi.getByCustomer.
     * @param {number} customerId
     */
    getOrders: customerId => get(`/Order/customer/${customerId}`)
};

/* ============================================================================
 *  2. ĐỊA CHỈ  -  /api/Address
 * ========================================================================== */

const ADDRESS = "/Address";

export const addressApi = {
    /** Tất cả địa chỉ (chỉ nhân sự). */
    getAll: () => get(ADDRESS),

    /** @param {number} id */
    getById: id => get(`${ADDRESS}/${id}`),

    /**
     * Các địa chỉ của một khách.
     * Mỗi địa chỉ: { id, customerId, street, district, city, country, orderCount }
     * @param {number} customerId
     */
    getByCustomer: customerId => get(`${ADDRESS}/customer/${customerId}`),

    /**
     * @param {{ customerId: number, street: string, district: string, city: string, country: string }} data
     */
    create: data => post(ADDRESS, data),

    /**
     * @param {number} id
     * @param {{ customerId?: number, street: string, district: string, city: string, country: string }} data
     */
    update: (id, data) => put(`${ADDRESS}/${id}`, data),

    /** @param {number} id */
    remove: id => del(`${ADDRESS}/${id}`)
};

/* ============================================================================
 *  3. ĐIỂM TÍCH LŨY  -  /api/LoyaltyTransaction
 *  Mỗi "giao dịch điểm" là một lần cộng (số dương) hoặc trừ (số âm) điểm.
 *  Tổng điểm = cộng dồn tất cả giao dịch. Mỗi lần thêm/sửa/xóa, backend tự
 *  xét lại hạng thành viên của khách.
 * ========================================================================== */

const LOYALTY = "/LoyaltyTransaction";

export const loyaltyApi = {
    /** Tất cả giao dịch điểm (chỉ nhân sự). */
    getAll: () => get(LOYALTY),

    /**
     * Lịch sử điểm của một khách.
     * Mỗi dòng: { id, customerId, txnDate, pointsChange, reason }
     * @param {number} customerId
     */
    getByCustomer: customerId => get(`${LOYALTY}/customer/${customerId}`),

    /**
     * Tổng điểm + hạng hiện tại của khách.
     * Trả về: { customerId, totalPoints, membershipTierId, membershipTierName }
     * @param {number} customerId
     */
    getPoints: customerId => get(`${LOYALTY}/customer/${customerId}/points`),

    /**
     * Cộng / trừ điểm thủ công (chỉ nhân sự).
     * @param {{ customerId: number, txnDate: string, pointsChange: number, reason?: string }} data
     *        pointsChange: số dương = cộng, số âm = trừ, không được bằng 0.
     *        txnDate: thời điểm giao dịch, không được ở tương lai.
     */
    create: data => post(LOYALTY, data),

    /**
     * @param {number} id
     * @param {{ customerId: number, txnDate: string, pointsChange: number, reason?: string }} data
     */
    update: (id, data) => put(`${LOYALTY}/${id}`, data),

    /** @param {number} id */
    remove: id => del(`${LOYALTY}/${id}`)
};
