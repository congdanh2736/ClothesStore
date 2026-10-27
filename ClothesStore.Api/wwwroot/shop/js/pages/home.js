/*
 * home.js - TRANG CHỦ (shop/index.html)
 * Dữ liệu: danh mục, sản phẩm, bộ sưu tập/công nghệ, cửa hàng (đếm số lượng).
 */
import { initShop, productCard, bindFavButtons, favoriteIds, ICONS, money, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatNumber } from "../../../assets/js/core/utils.js";

/** Mô tả ngắn cho các công nghệ vải (tên lấy từ bảng CollectionTech) */
const TECH_INFO = {
    "Vải Mát": { icon: ICONS.wind, text: "Sợi vải tản nhiệt nhanh, chạm vào thấy mát. Hợp thời tiết nóng ẩm." },
    "Kháng Khuẩn": { icon: ICONS.shield, text: "Hạn chế vi khuẩn gây mùi, giữ áo thơm tho suốt ngày dài." },
    "Bảo Vệ Thời Tiết": { icon: ICONS.drop, text: "Chống nước nhẹ, cản gió và chống nắng cho ngày di chuyển ngoài trời." },
    "Co Giãn": { icon: ICONS.layers, text: "Co giãn theo chuyển động, mặc cả ngày không bị bó." },
    "Khô Thoáng": { icon: ICONS.wind, text: "Thoát mồ hôi nhanh, khô ráo khi vận động." }
};

if (await initShop({ active: "home" })) {
    const [categories, products, collections, stores, favs] = await Promise.all([
        shopApi.getCategories(), shopApi.products.getAll(), shopApi.getCollections(), shopApi.getStores(), favoriteIds()
    ]);

    // ---- Danh mục con của 1 danh mục (kể cả cháu) ----
    const descendants = (/** @type {number} */ id) => {
        const ids = new Set([id]);
        let grew = true;
        while (grew) {
            grew = false;
            for (const c of categories) if (c.parentCategoryId && ids.has(c.parentCategoryId) && !ids.has(c.id)) { ids.add(c.id); grew = true; }
        }
        return ids;
    };

    // ---- Biểu tượng 3 ô cam kết cuối trang ----
    $("icoTruck").innerHTML = ICONS.truck; $("icoStore").innerHTML = ICONS.store; $("icoRet").innerHTML = ICONS.ret;

    // ---- Hero ----
    const featured = [...products].filter(p => /Áo Khoác/.test(p.categoryName ?? "")).sort((a, b) => b.minPrice - a.minPrice)[0] ?? products[0];
    $("heroVisual").innerHTML = featured ? `
        <img src="${escapeHtml(featured.mainProductImageUrl)}" alt="">
        <a class="overlay-card row-between" href="/shop/product.html?id=${featured.id}">
            <span><span class="eyebrow">Nổi bật</span><b class="fw-bold" style="display:block;margin-top:4px">${escapeHtml(featured.name)}</b></span>
            <span class="price">${money(featured.minPrice)}</span>
        </a>` : "";
    $("statProducts").textContent = formatNumber(products.length);
    $("statStores").textContent = formatNumber(stores.length);
    $("statTech").textContent = formatNumber(collections.length);

    // ---- Danh mục nổi bật: 4 nhóm lớn nhiều sản phẩm nhất ----
    const roots = categories.filter(c => !c.parentCategoryId).map(c => {
        const ids = descendants(c.id);
        const list = products.filter(p => ids.has(p.categoryId));
        return { ...c, count: list.length, image: list[0]?.mainProductImageUrl };
    }).sort((a, b) => b.count - a.count).slice(0, 4);
    $("featuredCats").innerHTML = roots.map(c => `
        <a class="cat-card" href="/shop/products.html?category=${c.id}">
            ${c.image ? `<img src="${escapeHtml(c.image)}" alt="" loading="lazy">` : ""}
            <span class="shade"></span>
            <span class="txt"><small>${formatNumber(c.count)} sản phẩm</small><b>${escapeHtml(c.name)}</b></span>
        </a>`).join("");

    // ---- Công nghệ vải (CollectionTech loại Technology) ----
    const techs = collections.filter(c => c.type === "Technology");
    const top = techs.filter(t => t.name in TECH_INFO).slice(0, 3);
    $("techCards").innerHTML = top.map((t, i) => {
        const info = /** @type {any} */ (TECH_INFO)[t.name];
        return `
            <div class="tech-card ${i === 1 ? "featured" : ""}">
                <span class="tile-ico">${info.icon}</span>
                <span class="eyebrow">Công nghệ</span>
                <h3>${escapeHtml(t.name)}</h3>
                <p class="text-muted text-sm">${info.text}</p>
            </div>`;
    }).join("");
    $("techChips").innerHTML = techs.map(t => `<span class="chip tech">${escapeHtml(t.name)}</span>`).join("")
        + collections.filter(c => c.type === "Collection").slice(0, 8).map(c => `<span class="chip coll">${escapeHtml(c.name)}</span>`).join("");

    // ---- Sản phẩm tiêu biểu (3 tab) ----
    const tabs = {
        new: { label: "Hàng mới", list: () => [...products].sort((a, b) => b.id - a.id), tag: "Mới" },
        price: { label: "Giá tốt", list: () => [...products].sort((a, b) => a.minPrice - b.minPrice), tag: "" },
        jacket: { label: "Áo khoác", list: () => products.filter(p => /Áo Khoác/.test(p.categoryName ?? "")), tag: "" }
    };
    /** @param {keyof typeof tabs} key */
    const showTab = key => {
        document.querySelectorAll("#productTabs .tab").forEach(b => b.classList.toggle("active", /** @type {HTMLElement} */ (b).dataset.tab === key));
        $("featuredProducts").innerHTML = tabs[key].list().slice(0, 10).map(p => productCard(p, favs, tabs[key].tag)).join("");
    };
    $("productTabs").innerHTML = Object.entries(tabs).map(([k, t]) => `<button class="tab" data-tab="${k}">${t.label}</button>`).join("");
    $("productTabs").addEventListener("click", e => {
        const b = /** @type {HTMLElement} */ (e.target).closest("[data-tab]");
        if (b) showTab(/** @type {any} */ (/** @type {HTMLElement} */ (b).dataset.tab));
    });
    showTab("new");
    bindFavButtons($("featuredProducts"));
}
