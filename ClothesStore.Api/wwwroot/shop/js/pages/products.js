/*
 * products.js - DANH SÁCH SẢN PHẨM (shop/products.html)
 *
 * Tham số trên địa chỉ trang:  ?category=12  ?q=áo khoác  ?sort=new|price-asc|price-desc|name
 * Lọc được: danh mục (kể cả danh mục con), từ khóa, khoảng giá.
 * (API /api/Product chỉ trả tên, danh mục, ảnh, giá thấp nhất -> nên chỉ lọc theo những thứ này.)
 */
import { initShop, productCard, bindFavButtons, favoriteIds, qs, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatNumber, formatMoney } from "../../../assets/js/core/utils.js";

const PAGE_SIZE = 12;
const PRICE_RANGES = [
    { label: "Dưới 200.000đ", min: 0, max: 199999 },
    { label: "200.000đ - 400.000đ", min: 200000, max: 400000 },
    { label: "400.000đ - 600.000đ", min: 400001, max: 600000 },
    { label: "Trên 600.000đ", min: 600001, max: Infinity }
];

if (await initShop({ active: "products" })) {
    const [categories, products, favs] = await Promise.all([shopApi.getCategories(), shopApi.products.getAll(), favoriteIds()]);

    /** Trạng thái bộ lọc */
    const state = {
        category: Number(qs("category")) || null,
        q: qs("q") ?? "",
        sort: qs("sort") ?? "new",
        range: /** @type {number | null} */ (null),
        page: 1
    };

    const children = (/** @type {number | null} */ id) => categories.filter(c => (c.parentCategoryId ?? null) === id);
    const descendants = (/** @type {number} */ id) => {
        /** @type {Set<number>} */ const ids = new Set([id]);
        const walk = (/** @type {number} */ x) => children(x).forEach(c => { ids.add(c.id); walk(c.id); });
        walk(id);
        return ids;
    };
    const countIn = (/** @type {number} */ id) => { const ids = descendants(id); return products.filter(p => ids.has(p.categoryId)).length; };
    const catName = (/** @type {number | null} */ id) => categories.find(c => c.id === id)?.name;

    // ---- Cột lọc: cây danh mục ----
    /** @param {number | null} parent @param {number} depth */
    const tree = (parent, depth) => children(parent).map(c => `
        <a class="row-between cat-link ${state.category === c.id ? "text-primary fw-bold" : ""}" data-cat="${c.id}"
           href="?category=${c.id}" style="padding:6px 0 6px ${depth * 14}px;font-size:${depth ? 13.5 : 14}px">
            <span>${depth ? "· " : ""}${escapeHtml(c.name)}</span><span class="text-faint text-xs mono">${countIn(c.id)}</span>
        </a>${tree(c.id, depth + 1)}`).join("");

    function renderSidebar() {
        $("catTree").innerHTML = `<a class="row-between ${!state.category ? "text-primary fw-bold" : ""}" data-cat="" href="?" style="padding:6px 0">
            <span>Tất cả sản phẩm</span><span class="text-faint text-xs mono">${products.length}</span></a>` + tree(null, 0);
        $("priceList").innerHTML = PRICE_RANGES.map((r, i) => `
            <label class="check"><input type="radio" name="price" value="${i}" ${state.range === i ? "checked" : ""}> ${r.label}</label>`).join("")
            + `<label class="check"><input type="radio" name="price" value="" ${state.range === null ? "checked" : ""}> Mọi mức giá</label>`;
    }

    function filtered() {
        let list = products;
        if (state.category) { const ids = descendants(state.category); list = list.filter(p => ids.has(p.categoryId)); }
        if (state.q.trim()) {
            const words = state.q.toLowerCase().split(/\s+/).filter(Boolean);
            list = list.filter(p => words.every(w => `${p.name} ${p.categoryName}`.toLowerCase().includes(w)));
        }
        if (state.range !== null) { const r = PRICE_RANGES[state.range]; list = list.filter(p => p.minPrice >= r.min && p.minPrice <= r.max); }
        const sorters = {
            new: (/** @type {any} */ a, /** @type {any} */ b) => b.id - a.id,
            "price-asc": (/** @type {any} */ a, /** @type {any} */ b) => a.minPrice - b.minPrice,
            "price-desc": (/** @type {any} */ a, /** @type {any} */ b) => b.minPrice - a.minPrice,
            name: (/** @type {any} */ a, /** @type {any} */ b) => a.name.localeCompare(b.name, "vi")
        };
        return [...list].sort(/** @type {any} */ (sorters)[state.sort] ?? sorters.new);
    }

    function render() {
        const list = filtered();
        const pages = Math.max(1, Math.ceil(list.length / PAGE_SIZE));
        state.page = Math.min(state.page, pages);
        const slice = list.slice((state.page - 1) * PAGE_SIZE, state.page * PAGE_SIZE);

        const title = state.q ? `Kết quả cho "${state.q}"` : catName(state.category) ?? "Tất cả sản phẩm";
        $("pageTitle").textContent = title;
        $("pageCount").textContent = `[${formatNumber(list.length)} sản phẩm]`;
        $("crumbCurrent").textContent = title;
        document.title = `${title} | AURA Techwear`;

        // Chip bộ lọc đang bật
        const chips = [];
        if (state.category) chips.push(`<span class="chip">Danh mục: <b>${escapeHtml(catName(state.category) ?? "")}</b><button data-clear="category" aria-label="Bỏ">×</button></span>`);
        if (state.q) chips.push(`<span class="chip">Từ khóa: <b>${escapeHtml(state.q)}</b><button data-clear="q" aria-label="Bỏ">×</button></span>`);
        if (state.range !== null) chips.push(`<span class="chip">Giá: <b>${PRICE_RANGES[state.range].label}</b><button data-clear="range" aria-label="Bỏ">×</button></span>`);
        $("activeFilters").innerHTML = chips.length
            ? chips.join("") + `<button class="btn btn-ghost btn-sm" data-clear="all">Xóa tất cả</button>`
            : `<span class="text-muted text-sm">Chưa bật bộ lọc nào</span>`;

        $("grid").innerHTML = slice.length ? slice.map(p => productCard(p, favs)).join("")
            : `<div class="empty panel" style="grid-column:1/-1"><h3>Không tìm thấy sản phẩm phù hợp</h3><p>Thử bỏ bớt bộ lọc hoặc đổi từ khóa.</p></div>`;

        // Phân trang
        $("pager").innerHTML = pages <= 1 ? "" : `
            <span class="text-muted text-sm">Hiển thị ${(state.page - 1) * PAGE_SIZE + 1}-${Math.min(state.page * PAGE_SIZE, list.length)} / ${formatNumber(list.length)}</span>
            <div class="row">
                <button class="btn btn-sm" data-page="${state.page - 1}" ${state.page === 1 ? "disabled" : ""}>‹ Trước</button>
                <span class="mono text-sm">${state.page} / ${pages}</span>
                <button class="btn btn-sm" data-page="${state.page + 1}" ${state.page === pages ? "disabled" : ""}>Sau ›</button>
            </div>`;
        renderSidebar();
    }

    // ---- Sự kiện ----
    /** @type {HTMLSelectElement} */ ($("sortSelect")).value = state.sort;
    /** @type {HTMLInputElement} */ ($("searchInput")).value = state.q;
    $("sortSelect").addEventListener("change", e => { state.sort = /** @type {HTMLSelectElement} */ (e.target).value; render(); });
    $("searchForm").addEventListener("submit", e => {
        e.preventDefault(); state.q = /** @type {HTMLInputElement} */ ($("searchInput")).value.trim(); state.page = 1; render();
    });
    $("catTree").addEventListener("click", e => {
        const a = /** @type {HTMLElement} */ (e.target).closest("[data-cat]");
        if (!a) return;
        e.preventDefault();
        state.category = Number(/** @type {HTMLElement} */ (a).dataset.cat) || null; state.page = 1;
        history.replaceState(null, "", state.category ? `?category=${state.category}` : "?");
        render();
    });
    $("priceList").addEventListener("change", e => {
        const v = /** @type {HTMLInputElement} */ (e.target).value; state.range = v === "" ? null : Number(v); state.page = 1; render();
    });
    $("activeFilters").addEventListener("click", e => {
        const b = /** @type {HTMLElement} */ (e.target).closest("[data-clear]");
        if (!b) return;
        const what = /** @type {HTMLElement} */ (b).dataset.clear;
        if (what === "category" || what === "all") state.category = null;
        if (what === "q" || what === "all") { state.q = ""; /** @type {HTMLInputElement} */ ($("searchInput")).value = ""; }
        if (what === "range" || what === "all") state.range = null;
        state.page = 1; render();
    });
    $("pager").addEventListener("click", e => {
        const b = /** @type {HTMLElement} */ (e.target).closest("[data-page]");
        if (!b) return;
        state.page = Number(/** @type {HTMLElement} */ (b).dataset.page); render(); window.scrollTo({ top: 0, behavior: "smooth" });
    });
    bindFavButtons($("grid"));
    $("priceHint").textContent = `Giá hiển thị là giá thấp nhất của sản phẩm (từ ${formatMoney(Math.min(...products.map(p => p.minPrice)))})`;
    render();
}
