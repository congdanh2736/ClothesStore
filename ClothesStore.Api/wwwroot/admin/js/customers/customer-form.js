/*
 * ============================================================================
 *  customer-form.js  -  FORM THÊM / SỬA KHÁCH HÀNG (dùng chung)
 * ============================================================================
 *
 *  Trang danh sách (customers.html) và trang chi tiết (customer-detail.html)
 *  đều có nút "Sửa khách hàng". Thay vì viết form 2 lần, ta để chung ở đây,
 *  hai trang chỉ việc import và gọi.
 *
 *      import { openCustomerForm, tierOptions } from "./customer-form.js";
 *
 *      openCustomerForm({
 *          customer: null,         // null = THÊM MỚI, có giá trị = SỬA khách đó
 *          tiers,                  // danh sách hạng thành viên (để đổ vào ô chọn)
 *          onSaved: () => reload() // chạy sau khi lưu thành công
 *      });
 * ============================================================================
 */

import { customerApi } from "../../../assets/js/api/customerApi.js";
import { formModal } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";
import { formatNumber } from "../../../assets/js/core/utils.js";

/**
 * Biến danh sách hạng thành viên thành các lựa chọn cho ô <select>.
 * Ví dụ: [{ id: 1, tierName: "Bạc", minPoints: 100 }]
 *     -> [{ value: 1, label: "Bạc (từ 100 điểm)" }]
 * Sắp xếp theo điểm tối thiểu tăng dần cho dễ nhìn.
 *
 * @param {any[]} tiers
 */
export function tierOptions(tiers) {
    return [...tiers]
        .sort((a, b) => a.minPoints - b.minPoints)
        .map(t => ({ value: t.id, label: `${t.tierName} (từ ${formatNumber(t.minPoints)} điểm)` }));
}

/**
 * Mở hộp thoại Thêm / Sửa khách hàng.
 *
 * THÊM: tạo tài khoản đăng nhập cho khách (email + mật khẩu) qua /Auth/register.
 *       Hạng được backend tự gán là hạng thấp nhất.
 * SỬA : chỉ đổi họ, tên, hạng. Email / mật khẩu không đổi ở đây.
 *
 * @param {object} options
 * @param {any} [options.customer] - khách đang sửa; bỏ trống / null = thêm mới
 * @param {any[]} [options.tiers]  - danh sách hạng thành viên
 * @param {() => any} [options.onSaved] - hàm chạy sau khi lưu thành công (thường là tải lại dữ liệu)
 */
export function openCustomerForm({ customer = null, tiers = [], onSaved } = {}) {
    const isEdit = customer !== null;

    // ---- Các ô nhập ----
    // Quy ước của backend: firstName = HỌ (và tên đệm), lastName = TÊN
    // "@type {any[]}": mảng chứa các ô nhập đủ kiểu (chữ, chọn, mật khẩu...) -> báo VS Code đừng bắt bẻ
    /** @type {any[]} */
    const fields = [
        { name: "firstName", label: "Họ và tên đệm", required: true, placeholder: "Ví dụ: Nguyễn Văn" },
        { name: "lastName", label: "Tên", required: true, placeholder: "Ví dụ: An" }
    ];

    if (isEdit) {
        // SỬA: cho đổi hạng. Bỏ trống = giữ nguyên hạng hiện tại.
        fields.push({
            name: "membershipTierId",
            label: "Hạng thành viên",
            type: "select",
            valueType: "number",
            emptyOption: "-- Giữ nguyên --",
            options: tierOptions(tiers),
            fullWidth: true,
            hint: "Hạng cũng được hệ thống tự xét lại mỗi khi điểm của khách thay đổi."
        });
    } else {
        // THÊM: cần email + mật khẩu để tạo tài khoản đăng nhập
        fields.push(
            { name: "email", label: "Email đăng nhập", type: "email", required: true, fullWidth: true,
              placeholder: "khachhang@gmail.com" },
            { name: "password", label: "Mật khẩu", type: "password", required: true, fullWidth: true,
              hint: "Ít nhất 6 ký tự, có chữ HOA, chữ thường và chữ số. Ví dụ: Khach123" }
        );
    }

    formModal({
        title: isEdit ? `Sửa khách hàng #${customer.id}` : "Thêm khách hàng",
        description: isEdit ? "" : "Hệ thống sẽ tạo luôn tài khoản để khách đăng nhập vào trang mua sắm.",
        fields,
        columns: 2,                   // form 2 cột: Họ | Tên
        values: customer ?? {},       // SỬA: điền sẵn dữ liệu cũ
        submitText: isEdit ? "Lưu thay đổi" : "Thêm khách hàng",
        onSubmit: async data => {
            if (isEdit) {
                await customerApi.update(customer.id, data);
                toast.success("Đã cập nhật khách hàng.");
            } else {
                await customerApi.create(data);
                toast.success(`Đã thêm khách hàng ${data.firstName} ${data.lastName}.`);
            }
            await onSaved?.();
        }
    });
}
