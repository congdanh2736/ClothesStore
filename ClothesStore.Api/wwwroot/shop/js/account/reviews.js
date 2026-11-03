/*
 * reviews.js - ĐÁNH GIÁ CỦA TÔI (shop/account/reviews.html)
 * API: GET /api/Review/customer/{id}, PUT /api/Review/{id}, DELETE /api/Review/{id}
 *      + "Chờ đánh giá" = sản phẩm trong đơn Hoàn tất / Đã trả hàng mà chưa đánh giá (GET /api/Order/customer/{id})
 */
import { initAccount, isPurchased } from "./accountShell.js";
import { toast, errMsg, confirmBox, productImages, thumb, stars, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { openReviewForm } from "../reviewForm.js";
import { escapeHtml, formatDate, formatDateTime } from "../../../assets/js/core/utils.js";

if (await initAccount("reviews")) {
    let tab = "pending";
    let mine = /** @type {any[]} */ ([]);
    /** @type {{productId:number, name:string, orderId:number, date:string}[]} */
    let pending = [];

    async function load() {
        const [reviews, orders] = await Promise.all([shopApi.reviews.getMine(), shopApi.orders.getMine()]);
        mine = reviews;
        const done = new Set(reviews.map((/** @type {any} */ r) => r.productId));
        /** @type {Map<number, any>} */
        const map = new Map();
        for (const o of orders.filter(isPurchased))
            for (const i of o.orderItems)
                if (!done.has(i.productId) && !map.has(i.productId)) map.set(i.productId, { productId: i.productId, name: i.variantName, orderId: o.id, date: o.orderDate });
        pending = [...map.values()];
        await productImages([...pending.map(p => p.productId), ...mine.map(r => r.productId)]);
        if (!pending.length && mine.length) tab = "mine";
        render();
    }

    async function render() {
        const images = await productImages([]);
        $("reviewTabs").innerHTML = `<button class="tab ${tab === "pending" ? "active" : ""}" data-tab="pending">Chờ đánh giá <span class="n">${pending.length}</span></button>
            <button class="tab ${tab === "mine" ? "active" : ""}" data-tab="mine">Đã đánh giá <span class="n">${mine.length}</span></button>`;
        if (tab === "pending") {
            $("reviewList").innerHTML = pending.length ? pending.map(p => `
                <div class="panel panel-pad line-item">${thumb(images.get(p.productId))}
                    <div class="grow"><a class="fw-bold" href="/shop/product.html?id=${p.productId}">${escapeHtml(p.name)}</a>
                        <div class="text-sm text-muted">Từ đơn <a class="text-primary" href="/shop/account/order-detail.html?id=${p.orderId}">#${p.orderId}</a> · ${formatDate(p.date)}</div></div>
                    <button class="btn btn-sm btn-primary" data-write="${p.productId}">${ICONS.star} Viết đánh giá</button></div>`).join("")
                : `<div class="panel empty">${ICONS.star}<h3>Không còn sản phẩm chờ đánh giá</h3><p>Sản phẩm trong đơn hoàn tất sẽ xuất hiện ở đây.</p></div>`;
        } else {
            $("reviewList").innerHTML = mine.length ? mine.map(r => `
                <article class="panel panel-pad stack" style="gap:10px">
                    <div class="line-item">${thumb(images.get(r.productId))}<div class="grow"><a class="fw-bold" href="/shop/product.html?id=${r.productId}">${escapeHtml(r.productName)}</a>
                        <div class="row wrap mt-1">${stars(r.rating)}<span class="text-xs text-muted">${formatDateTime(r.createdAt)}</span></div></div>
                        <div class="row"><button class="btn btn-sm btn-outline" data-edit="${r.id}">${ICONS.edit} Sửa</button>
                            <button class="btn btn-sm btn-ghost" data-del="${r.id}">${ICONS.trash}</button></div></div>
                    ${r.comment ? `<p>${escapeHtml(r.comment)}</p>` : ""}
                    ${r.imageUrls?.length ? `<div class="row wrap">${r.imageUrls.map((/** @type {string} */ u) => `<img src="${escapeHtml(u)}" alt="" style="width:72px;height:72px;object-fit:cover;border-radius:var(--radius-sm)">`).join("")}</div>` : ""}
                </article>`).join("")
                : `<div class="panel empty">${ICONS.chat}<h3>Bạn chưa viết đánh giá nào</h3></div>`;
        }
    }

    $("reviewTabs").addEventListener("click", e => {
        const b = /** @type {HTMLElement | null} */ (/** @type {HTMLElement} */ (e.target).closest("[data-tab]"));
        if (b) { tab = b.dataset.tab ?? "pending"; render(); }
    });
    $("reviewList").addEventListener("click", async e => {
        const t = /** @type {HTMLElement} */ (e.target);
        const w = /** @type {HTMLElement | null} */ (t.closest("[data-write]"));
        const ed = /** @type {HTMLElement | null} */ (t.closest("[data-edit]"));
        const dl = /** @type {HTMLElement | null} */ (t.closest("[data-del]"));
        if (w) {
            const p = pending.find(x => x.productId === Number(w.dataset.write));
            if (p) openReviewForm({ productId: p.productId, productName: p.name, onSaved: () => { tab = "mine"; load(); } });
        }
        if (ed) {
            const r = mine.find(x => x.id === Number(ed.dataset.edit));
            openReviewForm({ productId: r.productId, productName: r.productName, review: r, onSaved: load });
        }
        if (dl) {
            if (!(await confirmBox("Xóa đánh giá này?", "Xóa"))) return;
            try { await shopApi.reviews.remove(Number(dl.dataset.del)); toast("Đã xóa đánh giá.", "success"); load(); }
            catch (err) { toast(errMsg(err), "error"); }
        }
    });
    await load();
}
