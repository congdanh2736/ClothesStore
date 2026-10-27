/*
 * wishlist.js - SẢN PHẨM YÊU THÍCH (shop/account/wishlist.html)
 * API: GET /api/Wishlist/customer/{id}, POST /api/Wishlist/toggle (bỏ thích)
 * Muốn thêm vào giỏ cần chọn màu/size nên nút "Chọn mua" dẫn sang trang sản phẩm.
 */
import { initAccount } from "./accountShell.js";
import { toast, errMsg, refreshCounts, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatDate } from "../../../assets/js/core/utils.js";

if (await initAccount("wishlist")) {
    async function render() {
        const list = await shopApi.wishlist.getMine();
        $("wishCountText").textContent = `${list.length} sản phẩm`;
        $("wishGrid").innerHTML = list.length ? list.map((/** @type {any} */ w) => `
            <article class="product-card">
                <div class="product-media">
                    <a href="/shop/product.html?id=${w.productId}" aria-label="${escapeHtml(w.productName)}">
                        ${w.productImageUrl ? `<img src="${escapeHtml(w.productImageUrl)}" alt="" loading="lazy">` : ""}</a>
                    ${w.inStock === false ? `<span class="badge badge-danger tag">Hết hàng</span>` : ""}
                    <button class="fav on" data-unfav="${w.productId}" aria-label="Bỏ yêu thích">${ICONS.heart}</button></div>
                <div class="product-info">
                    <a class="name" href="/shop/product.html?id=${w.productId}">${escapeHtml(w.productName)}</a>
                    <div class="row-between"><span class="price">${money(w.price)}</span><span class="text-xs text-faint">Lưu ${formatDate(w.createdAt)}</span></div>
                    <a class="btn btn-sm btn-primary btn-block mt-2" href="/shop/product.html?id=${w.productId}">${ICONS.bag} Chọn mua</a>
                </div></article>`).join("")
            : `<div class="panel empty" style="grid-column:1/-1">${ICONS.heart}<h3>Chưa có sản phẩm yêu thích</h3><p>Bấm biểu tượng trái tim trên sản phẩm để lưu lại.</p>
                <a class="btn btn-primary mt-3" href="/shop/products.html">Khám phá sản phẩm</a></div>`;
    }
    $("wishGrid").addEventListener("click", async e => {
        const b = /** @type {HTMLElement | null} */ (/** @type {HTMLElement} */ (e.target).closest("[data-unfav]"));
        if (!b) return;
        e.preventDefault();
        try { await shopApi.wishlist.toggle(Number(b.dataset.unfav)); toast("Đã bỏ khỏi yêu thích."); await render(); refreshCounts(); }
        catch (err) { toast(errMsg(err), "error"); }
    });
    await render();
}
