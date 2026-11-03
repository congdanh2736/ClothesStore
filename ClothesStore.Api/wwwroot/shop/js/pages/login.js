/*
 * login.js - ĐĂNG NHẬP (shop/login.html)
 * API: POST /api/Auth/login { email, password } -> lưu token, chuyển về trang trước (returnUrl) hoặc trang chủ
 */
import { initShop, toast, errMsg, ICONS, $ } from "../shell.js";
import { shopApi, USE_MOCK, DEMO_ACCOUNT } from "../shopApi.js";
import { redirectIfLoggedIn, redirectAfterLogin } from "../../../assets/js/core/auth.js";

if (!redirectIfLoggedIn()) {
    await initShop({});
    const form = /** @type {HTMLFormElement} */ ($("loginForm"));
    const email = /** @type {HTMLInputElement} */ ($("email"));
    const password = /** @type {HTMLInputElement} */ ($("password"));

    // Gợi ý tài khoản khách có sẵn trong dữ liệu mẫu (seed) để chạy thử nhanh
    $("demoBox").innerHTML = `<div class="inset text-sm">${ICONS.user}
        <span>${USE_MOCK ? "Đang dùng <b>dữ liệu mẫu trên trình duyệt</b>." : "Tài khoản khách có sẵn trong dữ liệu mẫu:"}
            <b class="mono">${DEMO_ACCOUNT.email}</b> / <b class="mono">${DEMO_ACCOUNT.password}</b></span>
        <button type="button" class="btn btn-sm btn-outline mt-2" id="btnDemo">Điền tài khoản mẫu</button></div>`;
    $("btnDemo").addEventListener("click", () => { email.value = DEMO_ACCOUNT.email; password.value = DEMO_ACCOUNT.password; });

    $("togglePass").addEventListener("click", () => { password.type = password.type === "password" ? "text" : "password"; });

    // Giữ returnUrl khi chuyển sang trang đăng ký
    const link = /** @type {HTMLAnchorElement} */ ($("registerLink"));
    link.href += location.search;

    form.addEventListener("submit", async e => {
        e.preventDefault();
        if (!form.reportValidity()) return;
        const btn = /** @type {HTMLButtonElement} */ ($("btnLogin"));
        btn.disabled = true; btn.textContent = "Đang đăng nhập...";
        try {
            const result = await shopApi.auth.login({ email: email.value.trim(), password: password.value });
            if (!result.customerId && !(result.roles ?? []).some((/** @type {string} */ r) => r !== "Customer"))
                toast("Tài khoản chưa gắn với hồ sơ khách hàng.", "warning");
            toast(`Xin chào ${result.fullName ?? ""}!`, "success");
            setTimeout(redirectAfterLogin, 400);
        } catch (err) {
            toast(errMsg(err), "error");
            btn.disabled = false; btn.textContent = "Đăng nhập";
        }
    });
}
