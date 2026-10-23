/*
 * stores.js - HỆ THỐNG CỬA HÀNG (shop/stores.html)
 * API: GET /api/Store -> [{ id, name, location }]
 * Lọc theo thành phố (lấy phần cuối của địa chỉ) và tìm theo tên/địa chỉ.
 * "Chỉ đường" mở Google Maps tìm theo địa chỉ (backend không có tọa độ).
 */
import { initShop, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml } from "../../../assets/js/core/utils.js";

if (await initShop({ active: "stores" })) {
    const stores = await shopApi.getStores();
    const cityOf = (/** @type {any} */ s) => (s.location ?? "").split(",").pop()?.trim() || "Khác";
    const cities = [...new Set(stores.map(cityOf))].sort();
    const select = /** @type {HTMLSelectElement} */ ($("citySelect"));
    const input = /** @type {HTMLInputElement} */ ($("storeSearch"));
    select.innerHTML = `<option value="">Tất cả tỉnh / thành (${stores.length})</option>` +
        cities.map(c => `<option>${escapeHtml(c)}</option>`).join("");
    $("statStores").textContent = String(stores.filter((/** @type {any} */ s) => !/Kho Online/i.test(s.name)).length);
    $("statCities").textContent = String(cities.length);

    function render() {
        const kw = input.value.trim().toLowerCase();
        const list = stores.filter((/** @type {any} */ s) => (!select.value || cityOf(s) === select.value)
            && (!kw || `${s.name} ${s.location}`.toLowerCase().includes(kw)));
        $("storeCount").textContent = `${list.length} địa điểm`;
        $("storeGrid").innerHTML = list.length ? list.map((/** @type {any} */ s) => {
            const online = /Kho Online/i.test(s.name);
            return `<article class="panel panel-pad stack" style="gap:10px">
                <div class="row-between"><span class="tile-ico">${online ? ICONS.box : ICONS.store}</span>
                    ${online ? `<span class="badge badge-info">Kho giao hàng online</span>` : `<span class="badge badge-success"><span class="live-dot"></span> Đang mở cửa</span>`}</div>
                <h3 style="font-size:17px">${escapeHtml(s.name)}</h3>
                <p class="text-sm text-muted row" style="align-items:flex-start">${ICONS.pin}<span>${escapeHtml(s.location ?? "")}</span></p>
                <div class="row wrap mt-1">
                    <a class="btn btn-sm btn-outline" target="_blank" rel="noopener" href="https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(s.location ?? s.name)}">${ICONS.pin} Chỉ đường</a>
                    ${online ? "" : `<span class="chip">${ICONS.bag} Nhận hàng tại đây</span>`}
                </div></article>`;
        }).join("") : `<div class="panel empty" style="grid-column:1/-1">${ICONS.store}<h3>Không tìm thấy cửa hàng</h3><p>Thử từ khóa khác nhé.</p></div>`;
    }
    select.addEventListener("change", render);
    input.addEventListener("input", render);
    render();
}
