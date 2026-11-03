/*
 * addresses.js - SỔ ĐỊA CHỈ (shop/account/addresses.html)
 * API: GET /api/Address/customer/{id}, POST /api/Address, PUT /api/Address/{id}, DELETE /api/Address/{id}
 * Địa chỉ đã dùng cho đơn hàng thì backend không cho xóa (vẫn sửa được).
 */
import { initAccount } from "./accountShell.js";
import { toast, errMsg, confirmBox, openModal, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml } from "../../../assets/js/core/utils.js";

if (await initAccount("addresses")) {
    let list = /** @type {any[]} */ ([]);

    async function render() {
        list = await shopApi.addresses.getMine();
        $("addressGrid").innerHTML = list.length ? list.map((a, i) => `
            <article class="panel panel-pad stack" style="gap:10px">
                <div class="row-between"><span class="tile-ico">${ICONS.pin}</span>${i === 0 ? `<span class="badge badge-solid">Mặc định</span>` : ""}</div>
                <h3 style="font-size:17px">${escapeHtml(a.street)}</h3>
                <p class="text-sm text-muted">${escapeHtml(a.district)}, ${escapeHtml(a.city)}, ${escapeHtml(a.country)}</p>
                ${a.orderCount ? `<span class="text-xs text-faint">Đã dùng cho ${a.orderCount} đơn hàng</span>` : ""}
                <div class="row wrap mt-1"><button class="btn btn-sm btn-outline" data-edit="${a.id}">${ICONS.edit} Sửa</button>
                    <button class="btn btn-sm btn-ghost" data-del="${a.id}" ${a.orderCount ? `disabled title="Đã dùng cho đơn hàng nên không xóa được"` : ""}>${ICONS.trash} Xóa</button></div>
            </article>`).join("")
            : `<div class="panel empty" style="grid-column:1/-1">${ICONS.pin}<h3>Chưa có địa chỉ</h3><p>Thêm địa chỉ để đặt hàng nhanh hơn.</p></div>`;
    }

    /** @param {any} [a] - địa chỉ cần sửa, bỏ trống = thêm mới */
    function openForm(a) {
        const { el, close } = openModal({
            title: a ? "Sửa địa chỉ" : "Thêm địa chỉ",
            body: `<form class="form-grid" id="addrForm">
                <div class="field full"><label>Số nhà, tên đường</label><input class="input" name="street" required maxlength="100" value="${escapeHtml(a?.street ?? "")}"></div>
                <div class="field"><label>Quận / Huyện</label><input class="input" name="district" required maxlength="50" value="${escapeHtml(a?.district ?? "")}"></div>
                <div class="field"><label>Tỉnh / Thành phố</label><input class="input" name="city" required maxlength="50" value="${escapeHtml(a?.city ?? "")}"></div>
                <div class="field full"><label>Quốc gia</label><input class="input" name="country" required maxlength="50" value="${escapeHtml(a?.country ?? "Việt Nam")}"></div></form>`,
            footer: `<button class="btn btn-ghost" data-close>Hủy</button><button class="btn btn-primary" id="addrSave">Lưu địa chỉ</button>`
        });
        el.querySelector("#addrSave")?.addEventListener("click", async () => {
            const form = /** @type {HTMLFormElement} */ (el.querySelector("#addrForm"));
            if (!form.reportValidity()) return;
            const data = Object.fromEntries(new FormData(form));
            try {
                if (a) await shopApi.addresses.update(a.id, data); else await shopApi.addresses.create(data);
                close(); toast(a ? "Đã cập nhật địa chỉ." : "Đã thêm địa chỉ.", "success"); render();
            } catch (err) { toast(errMsg(err), "error"); }
        });
    }

    $("btnAdd").addEventListener("click", () => openForm());
    $("addressGrid").addEventListener("click", async e => {
        const t = /** @type {HTMLElement} */ (e.target);
        const ed = /** @type {HTMLElement | null} */ (t.closest("[data-edit]"));
        const dl = /** @type {HTMLElement | null} */ (t.closest("[data-del]"));
        if (ed) openForm(list.find(a => a.id === Number(ed.dataset.edit)));
        if (dl) {
            if (!(await confirmBox("Xóa địa chỉ này?", "Xóa"))) return;
            try { await shopApi.addresses.remove(Number(dl.dataset.del)); toast("Đã xóa địa chỉ.", "success"); render(); }
            catch (err) { toast(errMsg(err), "error"); }
        }
    });
    await render();
}
