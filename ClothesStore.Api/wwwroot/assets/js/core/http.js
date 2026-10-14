/*
 * ============================================================================
 *  http.js  -  "NGƯỜI ĐƯA THƯ" GIỮA GIAO DIỆN VÀ BACKEND
 * ============================================================================
 *
 *  Mọi lần frontend cần nói chuyện với backend (lấy danh sách, thêm, sửa, xóa)
 *  đều đi qua file này. Trình duyệt có sẵn hàm fetch() để gửi yêu cầu, nhưng
 *  nếu mỗi chỗ tự gọi fetch() thì lần nào cũng phải tự làm lại các việc:
 *    - gắn thẻ đăng nhập (token) vào yêu cầu
 *    - đổi dữ liệu thành dạng JSON trước khi gửi
 *    - đọc kết quả trả về
 *    - kiểm tra có lỗi không, lỗi gì, hiện thông báo gì
 *    - thẻ hết hạn thì đưa về trang đăng nhập
 *  => File này làm hết các việc đó MỘT LẦN, các file khác chỉ việc gọi:
 *
 *      import { get, post, put, patch, del } from "../core/http.js";
 *
 *      const customers = await get("/Customer");                 // lấy danh sách
 *      const created   = await post("/Customer", { firstName: "An" }); // thêm mới
 *      await put("/Customer/5", { firstName: "Bình" });          // sửa toàn bộ
 *      await patch("/Order/5/confirm");                          // sửa một phần / thao tác
 *      await del("/Customer/5");                                 // xóa
 *
 *  Đường dẫn chỉ cần ghi phần SAU "/api" (ví dụ "/Customer"), file này tự
 *  ghép thêm API_BASE ở đằng trước.
 *
 *  ---- Về "async / await" (đọc 1 lần cho hiểu) ----
 *  Gửi yêu cầu lên server mất thời gian (vài chục đến vài trăm mili-giây).
 *  "await" nghĩa là "đợi kết quả về rồi mới chạy dòng tiếp theo".
 *  Hàm nào có dùng await bên trong thì phải ghi chữ "async" ở đầu hàm.
 *
 *  ---- Về xử lý lỗi ----
 *  Khi có lỗi, các hàm ở đây sẽ "ném" (throw) ra một ApiError có sẵn câu
 *  thông báo tiếng Việt dễ đọc. Ở trang của bạn, bắt lỗi như sau:
 *
 *      try {
 *          await del(`/Employee/${id}`);
 *          alert("Đã xóa!");
 *      } catch (err) {
 *          alert(err.message);   // ví dụ: "Không tìm thấy nhân viên."
 *      }
 * ============================================================================
 */

import { API_BASE } from "./config.js";
import { getToken, clearLogin, redirectToLogin } from "./auth.js";

/* ============================================================================
 *  PHẦN 1: KIỂU LỖI RIÊNG CỦA API
 * ========================================================================== */

/**
 * ApiError là một "loại lỗi" riêng, giống lỗi thường (Error) nhưng mang thêm:
 *   - status : mã trạng thái HTTP server trả về, ví dụ 400, 404, 409, 500
 *              (0 nghĩa là không kết nối được tới server)
 *   - errors : danh sách lỗi theo từng ô nhập (khi bị FluentValidation từ chối),
 *              ví dụ { Email: ["Email không hợp lệ"], Password: ["Quá ngắn"] }
 *              -> trang có thể dùng để tô đỏ đúng ô bị sai.
 *   - data   : toàn bộ nội dung lỗi gốc server gửi về (để xem khi cần gỡ lỗi)
 *
 * "class ... extends Error" = tạo khuôn mới dựa trên khuôn Error có sẵn.
 */
export class ApiError extends Error {
    constructor(message, status = 0, errors = null, data = null) {
        super(message);          // gọi phần khởi tạo của Error để có err.message
        this.name = "ApiError";
        this.status = status;
        this.errors = errors;
        this.data = data;
    }
}

/* ============================================================================
 *  PHẦN 2: CÁC HÀM PHỤ (chỉ dùng trong file này, không export)
 * ========================================================================== */

/**
 * Ghép đường dẫn đầy đủ + thêm các tham số tìm kiếm (query string) nếu có.
 *
 * Ví dụ: buildUrl("/Product", { keyword: "áo", page: 2, categoryId: null })
 *        => "/api/Product?keyword=%C3%A1o&page=2"
 *   - categoryId bị bỏ qua vì rỗng (null), không gửi lên làm gì.
 *   - chữ "áo" được mã hóa thành %C3%A1o để URL không bị lỗi ký tự tiếng Việt
 *     (URLSearchParams tự làm việc này).
 */
function buildUrl(path, params) {
    // Nếu ai đó lỡ truyền đường dẫn đầy đủ "http://..." thì giữ nguyên
    let url = path.startsWith("http") ? path : API_BASE + path;

    if (params) {
        const search = new URLSearchParams();
        for (const [key, value] of Object.entries(params)) {
            // Bỏ qua giá trị rỗng: undefined, null, "" (chuỗi trống)
            if (value === undefined || value === null || value === "") continue;
            search.append(key, value);
        }
        const queryString = search.toString();
        if (queryString) {
            // Nếu path đã có sẵn "?" thì nối bằng "&", chưa có thì bắt đầu bằng "?"
            url += (url.includes("?") ? "&" : "?") + queryString;
        }
    }
    return url;
}

/**
 * Đọc nội dung server trả về.
 *
 * Không phải lúc nào server cũng trả JSON:
 *   - Xóa/sửa thành công thường trả 204 No Content = KHÔNG có nội dung gì.
 *     Nếu cứ gọi response.json() thì sẽ báo lỗi -> nên đọc dạng chữ trước.
 *   - Có lúc server trả chữ thường (không phải JSON).
 * Cách làm: đọc thành chữ -> rỗng thì trả null -> thử "mở gói" JSON
 * -> mở không được thì trả về nguyên chữ đó.
 */
async function readBody(response) {
    const text = await response.text();
    if (!text) return null;

    try {
        return JSON.parse(text);
    } catch {
        return text;
    }
}

/**
 * Lấy ra câu thông báo lỗi dễ hiểu từ nội dung lỗi của server.
 *
 * Backend ClothesStore trả lỗi theo mấy kiểu sau:
 *   (a) { message: "Không tìm thấy đơn hàng." }
 *       <- từ Controller (BadRequest/NotFound) hoặc ExceptionMiddleware
 *   (b) { title: "One or more validation errors occurred.",
 *         errors: { Email: ["Email không hợp lệ"] } }
 *       <- từ FluentValidation khi dữ liệu nhập sai (gọi là ProblemDetails)
 *   (c) một chuỗi chữ thường
 *   (d) không có gì cả (ví dụ 404 trống)
 * Hàm này thử lần lượt từng kiểu, kiểu nào có thì lấy kiểu đó.
 */
function extractErrorMessage(status, data) {
    // (c) server trả về chữ thường
    if (typeof data === "string" && data.trim()) return data;

    if (data && typeof data === "object") {
        // (a) có sẵn message
        if (data.message) return data.message;

        // (b) lỗi kiểm tra dữ liệu: gom tất cả các câu lỗi lại, mỗi câu một dòng
        if (data.errors && typeof data.errors === "object") {
            const allMessages = Object.values(data.errors).flat();
            // .flat() "làm phẳng": [["a","b"],["c"]] -> ["a","b","c"]
            if (allMessages.length) return allMessages.join("\n");
        }

        if (data.title) return data.title;
    }

    // (d) không có nội dung -> dùng câu mặc định theo mã trạng thái
    return defaultMessageFor(status);
}

/** Câu thông báo mặc định cho từng mã trạng thái HTTP thường gặp. */
function defaultMessageFor(status) {
    switch (status) {
        case 400: return "Dữ liệu gửi lên không hợp lệ.";
        case 401: return "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.";
        case 403: return "Bạn không có quyền thực hiện thao tác này.";
        case 404: return "Không tìm thấy dữ liệu.";
        case 409: return "Dữ liệu bị trùng hoặc đang được dùng ở nơi khác.";
        case 500: return "Máy chủ gặp lỗi, vui lòng thử lại sau.";
        default: return `Có lỗi xảy ra (mã ${status}).`;
    }
}

/* ============================================================================
 *  PHẦN 3: HÀM GỬI YÊU CẦU CHÍNH
 * ========================================================================== */

/**
 * request() là hàm "gốc", các hàm get/post/put/patch/del bên dưới chỉ là
 * cách gọi tắt của nó.
 *
 * @param {string} method - kiểu yêu cầu: "GET" (lấy), "POST" (thêm),
 *                          "PUT" (sửa), "PATCH" (sửa một phần), "DELETE" (xóa)
 * @param {string} path   - đường dẫn sau /api, ví dụ "/Customer/5"
 * @param {object} [options]
 *        options.body   - dữ liệu gửi kèm (object thường, hoặc FormData khi upload file)
 *        options.params - các tham số tìm kiếm ghép vào URL (xem buildUrl)
 * @returns dữ liệu server trả về (đã "mở gói" JSON), hoặc null nếu không có nội dung
 */
export async function request(method, path, { body, params } = {}) {
    const url = buildUrl(path, params);

    // "headers" = phần ghi chú đính kèm bức thư, cho server biết thêm thông tin
    const headers = { Accept: "application/json" }; // "tôi muốn nhận lại JSON"

    // Có thẻ đăng nhập thì gắn vào. Backend sẽ đọc dòng này để biết bạn là ai.
    // Chữ "Bearer " (có dấu cách) là quy ước bắt buộc của JWT.
    const token = getToken();
    if (token) {
        headers["Authorization"] = `Bearer ${token}`;
    }

    // Chuẩn bị phần thân (dữ liệu gửi kèm)
    let payload;
    if (body instanceof FormData) {
        // FormData dùng khi gửi FILE (ví dụ upload ảnh sản phẩm).
        // Trường hợp này KHÔNG tự đặt Content-Type, trình duyệt sẽ tự điền cho đúng.
        payload = body;
    } else if (body !== undefined) {
        // Object thường -> "đóng gói" thành chữ JSON, và báo server biết đây là JSON
        headers["Content-Type"] = "application/json";
        payload = JSON.stringify(body);
    }

    // ---- Gửi đi ----
    let response;
    try {
        response = await fetch(url, { method, headers, body: payload });
    } catch {
        // fetch chỉ "ném lỗi" khi KHÔNG gửi được: mất mạng, server chưa bật...
        throw new ApiError("Không kết nối được tới máy chủ. Kiểm tra lại mạng hoặc server đã chạy chưa.", 0);
    }

    // ---- Đọc kết quả ----
    const data = await readBody(response);

    // response.ok = true khi mã trạng thái từ 200 tới 299 (nghĩa là thành công)
    if (response.ok) {
        return data;
    }

    // ---- Từ đây trở xuống là xử lý KHI CÓ LỖI ----

    // 401 = server không chấp nhận thẻ (hết hạn / sai / chưa đăng nhập).
    // Chỉ tự đưa về trang đăng nhập khi TRƯỚC ĐÓ ta CÓ gửi thẻ.
    // Lý do: lúc đang đăng nhập mà gõ sai mật khẩu, server cũng có thể trả 401,
    // khi đó ta chỉ cần báo lỗi cho người dùng thấy, không cần chuyển trang.
    if (response.status === 401 && token) {
        clearLogin();
        redirectToLogin();
        throw new ApiError(defaultMessageFor(401), 401, null, data);
    }

    // 403 = đã đăng nhập nhưng KHÔNG ĐỦ QUYỀN (ví dụ Employee gọi API chỉ dành cho Admin).
    // Server thường không gửi kèm nội dung, nên dùng câu mặc định.
    if (response.status === 403) {
        throw new ApiError(defaultMessageFor(403), 403, null, data);
    }

    // Các lỗi còn lại (400, 404, 409, 500...): lấy câu thông báo từ server
    const errors = data && typeof data === "object" ? data.errors ?? null : null;
    throw new ApiError(extractErrorMessage(response.status, data), response.status, errors, data);
}

/* ============================================================================
 *  PHẦN 4: CÁC HÀM GỌI TẮT - đây là những hàm bạn sẽ dùng nhiều nhất
 * ========================================================================== */

/**
 * LẤY dữ liệu.
 *   get("/Customer")                         -> GET /api/Customer
 *   get("/Product", { keyword: "áo" })       -> GET /api/Product?keyword=...
 */
export function get(path, params) {
    return request("GET", path, { params });
}

/**
 * THÊM MỚI (hoặc gửi một hành động như đăng nhập).
 *   post("/Auth/login", { email, password })
 */
export function post(path, body, params) {
    return request("POST", path, { body, params });
}

/**
 * SỬA (gửi lại toàn bộ thông tin mới).
 *   put("/Employee/3", { firstName, lastName, storeId })
 */
export function put(path, body, params) {
    return request("PUT", path, { body, params });
}

/**
 * SỬA MỘT PHẦN hoặc thực hiện thao tác đổi trạng thái.
 *   patch("/Order/7/cancel")
 * (body có thể bỏ trống nếu API không cần dữ liệu gửi kèm)
 */
export function patch(path, body, params) {
    return request("PATCH", path, { body, params });
}

/**
 * XÓA.
 *   del("/Customer/5")
 * Đặt tên "del" chứ không phải "delete" vì "delete" là từ khóa có sẵn của
 * JavaScript, không được dùng làm tên hàm.
 */
export function del(path, params) {
    return request("DELETE", path, { params });
}
