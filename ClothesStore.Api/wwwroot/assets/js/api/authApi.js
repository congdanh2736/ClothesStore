/*
 * ============================================================================
 *  authApi.js  -  GỌI API ĐĂNG NHẬP / ĐĂNG KÝ   (FILE MẪU cho thư mục api/)
 * ============================================================================
 *
 *  Thư mục assets/js/api/ chứa các file "gọi API" - mỗi file ứng với một
 *  nhóm Controller bên backend. Trang HTML KHÔNG gọi thẳng get("/Auth/login")
 *  mà gọi qua các hàm có tên dễ hiểu như authApi.login(email, password).
 *
 *  Lợi ích:
 *    - Đọc code trang dễ hiểu hơn (biết ngay đang làm gì).
 *    - Backend đổi đường dẫn API thì chỉ sửa trong file api, không phải
 *      đi tìm sửa ở từng trang.
 *    - Trang admin và trang shop dùng chung được.
 *
 *  ---- KHUÔN MẪU cho các nhóm viết file api của mình ----
 *
 *      import { get, post, put, patch, del } from "../core/http.js";
 *
 *      const BASE = "/Customer";   // phần sau /api, GIỐNG [Route] của Controller
 *
 *      export const customerApi = {
 *          getAll:  ()         => get(BASE),                 // GET    /api/Customer
 *          getById: id         => get(`${BASE}/${id}`),      // GET    /api/Customer/5
 *          create:  data       => post(BASE, data),          // POST   /api/Customer
 *          update:  (id, data) => put(`${BASE}/${id}`, data),// PUT    /api/Customer/5
 *          remove:  id         => del(`${BASE}/${id}`)       // DELETE /api/Customer/5
 *      };
 *
 *  Quy ước đặt tên hàm cho cả nhóm thống nhất:
 *      getAll, getById, create, update, remove      (CRUD cơ bản)
 *      getByXxx(id)    ví dụ getByCustomer(customerId), getByOrder(orderId)
 *      các thao tác đặc biệt đặt theo động từ: confirm(id), cancel(id), approve(id)...
 *  (Dùng "remove" chứ không dùng "delete" vì "delete" là từ khóa của JavaScript.)
 *
 *  Tên thuộc tính trong data gửi lên viết kiểu camelCase (chữ đầu viết thường):
 *  backend có FirstName thì gửi firstName. ASP.NET tự hiểu được.
 * ============================================================================
 */

import { post } from "../core/http.js";

const BASE = "/Auth"; // AuthController: [Route("api/[controller]")] -> /api/Auth

export const authApi = {
    /**
     * Đăng nhập.
     * Thành công -> server trả { token, email, roles, customerId, employeeId, fullName }
     * Sai email/mật khẩu -> server trả 400 { message: "..." } -> http.js ném ApiError
     * Lưu ý: hàm này CHỈ gọi API. Muốn cất thông tin đăng nhập thì gọi tiếp
     * saveLogin(result) trong core/auth.js (trang login làm việc này).
     */
    login: (email, password) => post(`${BASE}/login`, { email, password }),

    /**
     * Đăng ký tài khoản khách hàng mới (dùng cho trang shop/register.html).
     * data = { email, password, firstName, lastName }
     */
    register: data => post(`${BASE}/register`, data)
};
