/*
 * product.js - CHI TIẾT SẢN PHẨM (shop/product.html?id=...)
 *
 * Dữ liệu: GET /api/Product/{id}/detail (ảnh, biến thể màu/size/giá, bộ sưu tập-công nghệ)
 *          GET /api/StoreStock/variant/{id}  (còn hàng ở cửa hàng nào)
 *          GET /api/SizeChart/category/{id}  (bảng size)
 *          GET /api/Review/product/{id} + /summary (đánh giá)
 */
import { initShop, productCard, bindFavButtons, favoriteIds, openModal, toast, errMsg, goLogin, refreshCounts, stars, colorHex, qs, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { openReviewForm } from "../reviewForm.js";
import { isLoggedIn } from "../../../assets/js/core/auth.js";
import { escapeHtml, formatNumber, formatDate, estimatePoints } from "../../../assets/js/core/utils.js";

if (await initShop({ active: "products" })) {
    const id = Number(qs("id"));
    try {
        await load(id);
    } catch (err) {
        $("productRoot").innerHTML = `<div class="empty panel"><h3>Không tìm thấy sản phẩm</h3><p>${escapeHtml(errMsg(err))}</p>
            <a class="btn btn-primary mt-3" href="/shop/products.html">Xem sản phẩm khác</a></div>`;
    }
}

/** @param {number} id */
async function load(id) {
    const [p, favs, all] = await Promise.all([shopApi.products.getDetail(id), favoriteIds(), shopApi.products.getAll()]);
    const chart = await shopApi.getSizeChart(p.categoryId);
    document.title = `${p.name} | AURA Techwear`;
    $("crumbCat").textContent = p.categoryName;
    $("crumbCat").setAttribute("href", `/shop/products.html?category=${p.categoryId}`);
    $("crumbCurrent").textContent = p.name;

    const colors = [...new Set(p.variants.map((/** @type {any} */ v) => v.color))];
    const sizes = [...new Set(p.variants.map((/** @type {any} */ v) => v.size))];
    /** Ảnh theo thứ tự màu (dữ liệu: mỗi màu 1 ảnh) */
    const images = p.imageUrls;
    let color = colors[0], size = /** @type {string | null} */ (null), qty = 1, imageIndex = 0;
    /** @type {Record<number, number>} tồn kho từng biến thể (cộng mọi cửa hàng) */
    const stockOf = {};
    /** @type {Record<number, any[]>} */
    const stockRows = {};
    await Promise.all(p.variants.map(async (/** @type {any} */ v) => {
        const rows = await shopApi.products.getStock(v.id);
        stockRows[v.id] = rows;
        stockOf[v.id] = rows.reduce((/** @type {number} */ s, /** @type {any} */ r) => s + r.quantity, 0);
    }));
    const variantOf = (/** @type {string} */ c, /** @type {string | null} */ s) => p.variants.find((/** @type {any} */ v) => v.color === c && v.size === s);
    size = sizes.find(s => { const v = variantOf(color, s); return v && stockOf[v.id] > 0; }) ?? sizes[0];

    // ---------- khung chính ----------
    $("productRoot").innerHTML = `
        <div class="grid-2" style="gap:36px;align-items:start">
            <div class="gallery">
                <div class="thumbs" id="thumbs"></div>
                <div class="main"><img id="mainImg" alt="${escapeHtml(p.name)}"></div>
            </div>
            <div class="stack" style="gap:18px">
                <div class="row wrap">${p.collections.map((/** @type {any} */ c) =>
                    `<span class="chip ${c.type === "Technology" ? "tech" : "coll"}">${escapeHtml(c.name)}</span>`).join("")}</div>
                <h1 class="page-title" style="font-size:clamp(24px,3vw,34px)">${escapeHtml(p.name)}</h1>
                <div class="row wrap text-sm text-muted">
                    <a href="/shop/products.html?category=${p.categoryId}" class="text-info">${escapeHtml(p.categoryName)}</a>
                    <span id="ratingLine"></span>
                </div>
                <div class="inset">
                    <div class="price" id="price" style="font-size:34px"></div>
                    <div class="text-sm text-muted mt-1" id="pointsNote"></div>
                </div>
                <div class="field">
                    <span class="label">Màu sắc: <b id="colorName" style="color:var(--text)"></b></span>
                    <div class="swatches" id="colorList"></div>
                </div>
                <div class="field">
                    <div class="row-between"><span class="label">Kích cỡ</span>
                        ${chart.length ? `<button class="btn btn-ghost btn-sm" id="btnSizeChart">${ICONS.ruler} Bảng size</button>` : ""}</div>
                    <div class="swatches" id="sizeList"></div>
                </div>
                <div class="panel" id="stockPanel"></div>
                <div class="row wrap">
                    <div class="qty"><button type="button" id="qtyMinus" aria-label="Giảm">−</button><input id="qtyInput" type="number" min="1" value="1" aria-label="Số lượng"><button type="button" id="qtyPlus" aria-label="Tăng">+</button></div>
                    <button class="btn btn-primary btn-lg grow" id="btnAdd">${ICONS.bag} Thêm vào giỏ hàng</button>
                    <button class="icon-btn fav ${favs.has(p.id) ? "on" : ""}" id="btnFav" style="border:1px solid var(--border);width:50px;height:50px" aria-label="Yêu thích">${ICONS.heart}</button>
                </div>
                <button class="btn btn-lg btn-outline" id="btnBuyNow">Mua ngay</button>
                <div class="grid-3" style="gap:10px">
                    <div class="inset text-sm row" style="gap:8px">${ICONS.ret}<span>Đổi trả trong 30 ngày</span></div>
                    <div class="inset text-sm row" style="gap:8px">${ICONS.coin}<span>Tích điểm hội viên</span></div>
                    <div class="inset text-sm row" style="gap:8px">${ICONS.store}<span>${formatNumber(Object.keys(stockRows).length ? new Set(Object.values(stockRows).flat().map(r => r.storeId)).size : 0)} cửa hàng có mẫu</span></div>
                </div>
            </div>
        </div>`;

    const drawGallery = () => {
        $("thumbs").innerHTML = images.map((/** @type {string} */ url, /** @type {number} */ i) =>
            `<button class="${i === imageIndex ? "active" : ""}" data-img="${i}" aria-label="Ảnh ${i + 1}"><img src="${escapeHtml(url)}" alt=""></button>`).join("");
        /** @type {HTMLImageElement} */ ($("mainImg")).src = images[imageIndex] ?? "";
    };

    const draw = () => {
        const v = variantOf(color, size);
        $("colorName").textContent = color;
        $("colorList").innerHTML = colors.map(c => `<button class="swatch color ${c === color ? "active" : ""}" data-color="${escapeHtml(c)}">
            <span class="color-dot" style="background:${colorHex(c)}"></span>${escapeHtml(c)}</button>`).join("");
        $("sizeList").innerHTML = sizes.map(s => {
            const sv = variantOf(color, s);
            const left = sv ? stockOf[sv.id] : 0;
            return `<button class="swatch ${s === size ? "active" : ""}" data-size="${escapeHtml(s)}" ${!sv || left <= 0 ? "disabled" : ""}>
                ${escapeHtml(s)}<small>${!sv ? "không có" : left <= 0 ? "hết hàng" : left <= 5 ? `còn ${left}` : "còn hàng"}</small></button>`;
        }).join("");
        $("price").textContent = v ? money(v.price) : "—";
        $("pointsNote").innerHTML = v ? `Đơn hoàn tất được cộng <b class="text-primary">+${formatNumber(estimatePoints(v.price * qty))} điểm</b> (10.000đ = 1 điểm)` : "";
        const rows = v ? [...(stockRows[v.id] ?? [])].filter(r => r.quantity > 0).sort((a, b) => b.quantity - a.quantity) : [];
        $("stockPanel").innerHTML = `
            <div class="panel-head" style="padding:12px 16px"><h3 style="font-size:14px">${ICONS.store} Còn hàng tại cửa hàng</h3>
                <span class="text-xs text-muted">${v ? `${escapeHtml(color)} / ${escapeHtml(size ?? "")}` : ""}</span></div>
            <div class="panel-body" style="padding:10px 16px">
                ${rows.length ? rows.slice(0, 4).map(r => `
                    <div class="row-between text-sm" style="padding:5px 0"><span class="row" style="gap:8px"><span class="live-dot"></span>${escapeHtml(r.storeName)}</span>
                        <span class="${r.quantity <= 5 ? "text-primary" : "text-success"}">Còn ${formatNumber(r.quantity)}</span></div>`).join("")
                    + (rows.length > 4 ? `<div class="text-xs text-faint mt-1">và ${rows.length - 4} cửa hàng khác</div>` : "")
                  : `<div class="text-sm text-muted">Tạm hết hàng ở mọi cửa hàng.</div>`}
            </div>`;
        /** @type {HTMLButtonElement} */ ($("btnAdd")).disabled = !v || stockOf[v.id] <= 0;
        /** @type {HTMLButtonElement} */ ($("btnBuyNow")).disabled = !v || stockOf[v.id] <= 0;
    };

    drawGallery(); draw();

    // ---------- sự kiện ----------
    $("thumbs").addEventListener("click", e => {
        const b = /** @type {HTMLElement} */ (e.target).closest("[data-img]");
        if (b) { imageIndex = Number(/** @type {HTMLElement} */ (b).dataset.img); drawGallery(); }
    });
    $("colorList").addEventListener("click", e => {
        const b = /** @type {HTMLElement} */ (e.target).closest("[data-color]");
        if (!b) return;
        color = /** @type {string} */ (/** @type {HTMLElement} */ (b).dataset.color);
        imageIndex = Math.min(colors.indexOf(color), images.length - 1);
        if (!variantOf(color, size)) size = sizes.find(s => variantOf(color, s)) ?? null;
        drawGallery(); draw();
    });
    $("sizeList").addEventListener("click", e => {
        const b = /** @type {HTMLButtonElement | null} */ (/** @type {HTMLElement} */ (e.target).closest("[data-size]"));
        if (b && !b.disabled) { size = b.dataset.size ?? null; draw(); }
    });
    const qtyInput = /** @type {HTMLInputElement} */ ($("qtyInput"));
    const setQty = (/** @type {number} */ n) => { qty = Math.max(1, Math.min(99, n || 1)); qtyInput.value = String(qty); draw(); };
    $("qtyMinus").addEventListener("click", () => setQty(qty - 1));
    $("qtyPlus").addEventListener("click", () => setQty(qty + 1));
    qtyInput.addEventListener("change", () => setQty(Number(qtyInput.value)));

    const addToCart = async (/** @type {boolean} */ goCart) => {
        if (!isLoggedIn()) { goLogin(); return; }
        const v = variantOf(color, size);
        if (!v) return;
        try {
            await shopApi.cart.add(v.id, qty);
            await refreshCounts();
            if (goCart) location.href = "/shop/cart.html";
            else toast(`Đã thêm ${qty} × ${p.name} (${color} / ${size}) vào giỏ.`, "success");
        } catch (err) { toast(errMsg(err), "error"); }
    };
    $("btnAdd").addEventListener("click", () => addToCart(false));
    $("btnBuyNow").addEventListener("click", () => addToCart(true));
    $("btnFav").addEventListener("click", async () => {
        if (!isLoggedIn()) { goLogin(); return; }
        try {
            const on = await shopApi.wishlist.toggle(p.id);
            $("btnFav").classList.toggle("on", on);
            toast(on ? "Đã thêm vào yêu thích." : "Đã bỏ khỏi yêu thích.", "success");
            refreshCounts();
        } catch (err) { toast(errMsg(err), "error"); }
    });
    $("btnSizeChart")?.addEventListener("click", () => openModal({
        title: `Bảng size - ${p.categoryName}`,
        body: `<div class="table-wrap"><table class="table"><thead><tr><th>Size</th><th>Số đo</th></tr></thead><tbody>
            ${chart.map((/** @type {any} */ s) => `<tr><td class="fw-bold">${escapeHtml(s.sizeLabel)}</td><td>${escapeHtml(s.measurements)}</td></tr>`).join("")}
            </tbody></table></div><p class="hint mt-3">Phân vân giữa 2 size: chọn size lớn hơn nếu thích mặc rộng.</p>`
    }));

    await drawReviews(p);

    // ---------- sản phẩm cùng danh mục ----------
    const related = all.filter((/** @type {any} */ x) => x.categoryId === p.categoryId && x.id !== p.id).slice(0, 4);
    $("related").innerHTML = related.map((/** @type {any} */ x) => productCard(x, favs)).join("");
    $("relatedSection").classList.toggle("hidden", !related.length);
    bindFavButtons($("related"));
}

/** Khu vực đánh giá. @param {any} p */
async function drawReviews(p) {
    const [summary, reviews] = await Promise.all([shopApi.reviews.getSummary(p.id), shopApi.reviews.getByProduct(p.id)]);
    $("ratingLine").innerHTML = summary.totalReviews
        ? `${stars(summary.averageRating)} <b>${summary.averageRating.toFixed(1)}</b> (${summary.totalReviews} đánh giá)` : "Chưa có đánh giá";

    // Đủ điều kiện viết đánh giá? (đã mua trong đơn Hoàn tất / Đã trả, chưa đánh giá)
    let canReview = false;
    if (isLoggedIn()) {
        try {
            const [orders, mine] = await Promise.all([shopApi.orders.getMine(), shopApi.reviews.getMine()]);
            canReview = orders.some((/** @type {any} */ o) => ["Completed", "Returned"].includes(o.status) && o.orderItems.some((/** @type {any} */ i) => i.productId === p.id))
                && !mine.some((/** @type {any} */ r) => r.productId === p.id);
        } catch { /* chưa đăng nhập đúng vai trò: bỏ qua */ }
    }
    const dist = summary.ratingDistribution ?? {};
    $("reviewSection").innerHTML = `
        <div class="section-head"><div><span class="eyebrow">Đánh giá thực tế</span><h2>Khách hàng nói gì</h2></div>
            ${canReview ? `<button class="btn btn-primary" id="btnWriteReview">${ICONS.edit} Viết đánh giá</button>`
                : `<span class="text-sm text-muted">Chỉ khách đã nhận hàng mới đánh giá được</span>`}</div>
        <div class="layout-reviews">
            <div class="panel panel-pad">
                <div style="font-family:var(--font-display);font-size:44px;font-weight:700">${summary.totalReviews ? summary.averageRating.toFixed(1) : "–"}<span class="text-muted" style="font-size:18px"> / 5</span></div>
                ${stars(summary.averageRating || 0)}
                <div class="text-sm text-muted mt-1">${summary.totalReviews} đánh giá</div>
                <div class="stack mt-3" style="gap:6px">${[5, 4, 3, 2, 1].map(n => {
                    const c = dist[n] ?? 0; const pct = summary.totalReviews ? (c / summary.totalReviews) * 100 : 0;
                    return `<div class="rating-bar"><span>${n} ★</span><div class="progress"><span style="width:${pct}%"></span></div><span>${c}</span></div>`;
                }).join("")}</div>
            </div>
            <div class="stack">${reviews.length ? reviews.sort((/** @type {any} */ a, /** @type {any} */ b) => b.createdAt.localeCompare(a.createdAt)).map((/** @type {any} */ r) => `
                <article class="panel panel-pad">
                    <div class="row-between"><div class="row"><span class="avatar" style="width:36px;height:36px">${escapeHtml((r.customerName ?? "?").split(" ").pop()?.charAt(0) ?? "?")}</span>
                        <div><b>${escapeHtml(r.customerName ?? "Khách hàng")}</b><div class="text-xs text-success row" style="gap:4px">${ICONS.check} Đã mua hàng</div></div></div>
                        <span class="text-xs text-faint">${formatDate(r.createdAt)}</span></div>
                    <div class="mt-2">${stars(r.rating)}</div>
                    ${r.comment ? `<p class="mt-2">${escapeHtml(r.comment)}</p>` : ""}
                    ${r.imageUrls?.length ? `<div class="row wrap mt-3">${r.imageUrls.map((/** @type {string} */ u) => `<img src="${escapeHtml(u)}" alt="Ảnh đánh giá" style="width:96px;height:96px;object-fit:cover;border-radius:8px;border:1px solid var(--border)" loading="lazy">`).join("")}</div>` : ""}
                </article>`).join("") : `<div class="panel empty">${ICONS.chat}<h3>Chưa có đánh giá</h3><p>Hãy là người đầu tiên đánh giá sau khi nhận hàng.</p></div>`}
            </div>
        </div>`;
    $("btnWriteReview")?.addEventListener("click", () => openReviewForm({ productId: p.id, productName: p.name, onSaved: () => drawReviews(p) }));
}
