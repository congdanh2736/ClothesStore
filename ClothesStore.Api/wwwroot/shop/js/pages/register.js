/*
 * register.js - ĐĂNG KÝ TÀI KHOẢN KHÁCH HÀNG (shop/register.html)
 * API: POST /api/Auth/register { email, password, firstName, lastName }
 *  - Backend (RegisterValidator + Identity): mật khẩu ≥ 6 ký tự, có chữ hoa, chữ thường và chữ số.
 *  - Đăng ký xong tự đăng nhập luôn rồi chuyển trang.
 */
import { initShop, toast, errMsg, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { redirectIfLoggedIn, redirectAfterLogin } from "../../../assets/js/core/auth.js";

if (!redirectIfLoggedIn()) {
    await initShop({});
    const form = /** @type {HTMLFormElement} */ ($("registerForm"));
    const pass = /** @type {HTMLInputElement} */ ($("password"));
    const pass2 = /** @type {HTMLInputElement} */ ($("password2"));
    /** @type {HTMLAnchorElement} */ ($("loginLink")).href += location.search;

    // Kiểm tra từng điều kiện mật khẩu và tô màu ngay khi gõ
    const rules = [
        { id: "ruleLen", test: (/** @type {string} */ v) => v.length >= 6 },
        { id: "ruleUpper", test: (/** @type {string} */ v) => /[A-Z]/.test(v) },
        { id: "ruleLower", test: (/** @type {string} */ v) => /[a-z]/.test(v) },
        { id: "ruleDigit", test: (/** @type {string} */ v) => /\d/.test(v) }
    ];
    const check = () => rules.every(r => {
        const ok = r.test(pass.value);
        $(r.id).classList.toggle("text-success", ok);
        return ok;
    });
    pass.addEventListener("input", check);

    form.addEventListener("submit", async e => {
        e.preventDefault();
        if (!form.reportValidity()) return;
        if (!check()) { toast("Mật khẩu chưa đủ điều kiện.", "warning"); return; }
        if (pass.value !== pass2.value) { toast("Mật khẩu nhập lại không khớp.", "warning"); return; }
        const data = /** @type {Record<string,string>} */ (Object.fromEntries(new FormData(form)));
        const btn = /** @type {HTMLButtonElement} */ ($("btnRegister"));
        btn.disabled = true; btn.textContent = "Đang tạo tài khoản...";
        try {
            await shopApi.auth.register({ email: data.email.trim(), password: data.password, firstName: data.firstName.trim(), lastName: data.lastName.trim() });
            await shopApi.auth.login({ email: data.email.trim(), password: data.password });
            toast("Tạo tài khoản thành công!", "success");
            setTimeout(redirectAfterLogin, 400);
        } catch (err) {
            toast(errMsg(err), "error");
            btn.disabled = false; btn.textContent = "Tạo tài khoản";
        }
    });
}
