/*
 * reviewForm.js - HỘP THOẠI VIẾT / SỬA ĐÁNH GIÁ (dùng ở trang sản phẩm và trang "Đánh giá của tôi")
 *
 * Luật của backend (ReviewService):
 *   - Chỉ đánh giá sản phẩm đã mua trong đơn Hoàn tất / Đã trả hàng, mỗi sản phẩm 1 lần
 *   - 1 đến 5 sao, bình luận tối đa 1000 ký tự, tối đa 3 ảnh
 *   - Backend nhận ĐƯỜNG DẪN ảnh (chưa có API tải ảnh lên) nên form cho dán link ảnh.
 */
import { openModal, toast, errMsg, ICONS } from "./shell.js";
import { shopApi } from "./shopApi.js";
import { escapeHtml } from "../../assets/js/core/utils.js";

/**
 * @param {{ productId: number, productName: string, review?: any, onSaved?: () => void }} opts
 *   review = đánh giá cũ (để sửa); bỏ trống = viết mới
 */
export function openReviewForm({ productId, productName, review, onSaved }) {
    let rating = review?.rating ?? 5;
    const urls = review?.imageUrls ?? [];
    const { el, close } = openModal({
        title: review ? "Sửa đánh giá" : "Viết đánh giá",
        body: `
            <p class="text-muted text-sm">Sản phẩm: <b class="fw-bold" style="color:var(--text)">${escapeHtml(productName)}</b></p>
            <div class="field mt-3"><span class="label">Chấm điểm</span><div class="star-input" id="starInput"></div></div>
            <div class="field mt-3"><label for="rvComment">Nhận xét</label>
                <textarea class="textarea" id="rvComment" maxlength="1000" placeholder="Chất vải, form dáng, size có vừa không...">${escapeHtml(review?.comment ?? "")}</textarea>
                <span class="hint"><span id="rvCount">0</span>/1000 ký tự</span></div>
            <div class="field mt-3"><span class="label">${ICONS.camera} Ảnh thực tế (tối đa 3 đường dẫn ảnh)</span>
                ${[0, 1, 2].map(i => `<input class="input" type="url" data-img placeholder="https://..." value="${escapeHtml(urls[i] ?? "")}">`).join("")}
            </div>`,
        footer: `<button class="btn btn-ghost" data-close>Hủy</button><button class="btn btn-primary" id="rvSave">${review ? "Lưu thay đổi" : "Gửi đánh giá"}</button>`
    });
    const q = (/** @type {string} */ s) => /** @type {HTMLElement} */ (el.querySelector(s));
    const drawStars = () => {
        q("#starInput").innerHTML = [1, 2, 3, 4, 5].map(n =>
            `<button type="button" class="${n <= rating ? "on" : ""}" data-star="${n}" aria-label="${n} sao">${ICONS.star}</button>`).join("");
    };
    drawStars();
    q("#starInput").addEventListener("click", e => {
        const b = /** @type {HTMLElement} */ (e.target).closest("[data-star]");
        if (b) { rating = Number(/** @type {HTMLElement} */ (b).dataset.star); drawStars(); }
    });
    const comment = /** @type {HTMLTextAreaElement} */ (q("#rvComment"));
    const count = () => { q("#rvCount").textContent = String(comment.value.length); };
    comment.addEventListener("input", count); count();

    q("#rvSave").addEventListener("click", async () => {
        const imageUrls = [...el.querySelectorAll("[data-img]")].map(i => /** @type {HTMLInputElement} */ (i).value.trim()).filter(Boolean);
        const data = { productId, rating, comment: comment.value.trim(), imageUrls };
        try {
            if (review) await shopApi.reviews.update(review.id, data); else await shopApi.reviews.create(data);
            toast(review ? "Đã cập nhật đánh giá." : "Cảm ơn bạn đã đánh giá!", "success");
            close();
            onSaved?.();
        } catch (err) { toast(errMsg(err), "error"); }
    });
}
