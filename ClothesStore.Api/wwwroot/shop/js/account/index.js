/*
 * index.js - TỔNG QUAN TÀI KHOẢN (shop/account/index.html)
 * API: GET /api/Customer/{id}, PUT /api/Customer/{id} {firstName,lastName}, GET /api/MembershipTier,
 *      GET /api/Order/customer/{id}, GET /api/Address/customer/{id}, GET /api/Promotion
 */
import { initAccount, memberCardHtml, tierLadderHtml, initials } from "./accountShell.js";
import { openModal, toast, errMsg, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatNumber, formatDate, orderStatusBadge } from "../../../assets/js/core/utils.js";

let me = await initAccount("index");
if (me) {
    const [tiers, orders, addresses, promos, wish] = await Promise.all([shopApi.getTiers(), shopApi.orders.getMine(), shopApi.addresses.getMine(),
        shopApi.getPromotions(), shopApi.wishlist.getMine()]);
    const now = Date.now();
    const active = promos.filter((/** @type {any} */ p) => new Date(p.startDate).getTime() <= now && new Date(p.endDate).getTime() >= now);
    const done = orders.filter((/** @type {any} */ o) => o.status === "Completed");
    const spent = done.reduce((/** @type {number} */ s, /** @type {any} */ o) => s + o.totalAmount, 0);
    const running = orders.filter((/** @type {any} */ o) => ["Pending", "Confirmed", "Shipping"].includes(o.status)).length;

    function render() {
        $("accountMain").innerHTML = `
            <section class="page-hero row-between wrap" style="gap:20px">
                <div class="row" style="gap:16px">
                    <span class="avatar-lg" style="width:64px;height:64px;border-radius:50%;display:grid;place-items:center;background:var(--primary);color:var(--on-primary);font:700 24px var(--font-display)">${escapeHtml(initials(me.fullName))}</span>
                    <div><div class="eyebrow">Xin chào</div><h1 class="page-title" style="font-size:clamp(24px,3.4vw,34px)">${escapeHtml(me.fullName)}</h1>
                        <div class="text-sm text-muted mt-1">${escapeHtml(me.email ?? "")}</div></div></div>
                <button class="btn btn-outline" id="btnEditName">${ICONS.edit} Sửa họ tên</button>
            </section>

            <div class="grid-4 mt-4">
                <div class="stat-tile"><div><div class="label">Tổng đơn hàng</div><div class="value">${orders.length}</div><div class="note">${running} đơn đang xử lý</div></div><span class="tile-ico">${ICONS.box}</span></div>
                <div class="stat-tile"><div><div class="label">Đã chi tiêu</div><div class="value" style="font-size:22px">${money(spent)}</div><div class="note">${done.length} đơn hoàn tất</div></div><span class="tile-ico">${ICONS.coin}</span></div>
                <div class="stat-tile"><div><div class="label">Điểm tích lũy</div><div class="value">${formatNumber(me.totalPoints ?? 0)}</div><div class="note">Hạng ${escapeHtml(me.membershipTierName ?? "")}</div></div><span class="tile-ico">${ICONS.star}</span></div>
                <div class="stat-tile"><div><div class="label">Yêu thích</div><div class="value">${wish.length}</div><div class="note">sản phẩm đã lưu</div></div><span class="tile-ico">${ICONS.heart}</span></div>
            </div>

            <div class="grid-2 mt-4" style="align-items:start">
                ${memberCardHtml(me, tiers)}
                <section class="panel">
                    <div class="panel-head"><h2>${ICONS.pin} Sổ địa chỉ</h2><a class="btn btn-ghost btn-sm" href="/shop/account/addresses.html">Quản lý</a></div>
                    <div class="panel-body stack">${addresses.length ? addresses.slice(0, 2).map((/** @type {any} */ a, /** @type {number} */ i) => `
                        <div class="inset"><div class="row wrap"><b>${escapeHtml(a.street)}</b>${i === 0 ? `<span class="badge badge-solid">Mặc định</span>` : ""}</div>
                            <div class="text-sm text-muted mt-1">${escapeHtml(a.district)}, ${escapeHtml(a.city)}, ${escapeHtml(a.country)}</div></div>`).join("")
                        : `<p class="text-muted text-sm">Chưa có địa chỉ. <a class="text-primary" href="/shop/account/addresses.html">Thêm ngay</a></p>`}</div>
                </section>
            </div>

            <section class="panel mt-4">
                <div class="panel-head"><h2>${ICONS.box} Đơn hàng gần đây</h2><a class="btn btn-ghost btn-sm" href="/shop/account/orders.html">Xem tất cả ${ICONS.arrow}</a></div>
                <div class="table-wrap"><table class="table"><thead><tr><th>Mã đơn</th><th>Ngày đặt</th><th>Sản phẩm</th><th>Trạng thái</th><th class="right">Tổng tiền</th></tr></thead>
                    <tbody>${orders.slice(0, 5).map((/** @type {any} */ o) => `<tr>
                        <td><a class="mono fw-bold text-primary" href="/shop/account/order-detail.html?id=${o.id}">#${o.id}</a></td><td>${formatDate(o.orderDate)}</td>
                        <td>${o.orderItems.reduce((/** @type {number} */ s, /** @type {any} */ i) => s + i.quantity, 0)} món</td><td>${orderStatusBadge(o.status)}</td>
                        <td class="right fw-bold">${money(o.totalAmount)}</td></tr>`).join("") || `<tr><td colspan="5" class="text-muted">Chưa có đơn hàng nào.</td></tr>`}</tbody></table></div>
            </section>

            <section class="panel mt-4">
                <div class="panel-head"><h2>${ICONS.shield} Hạng thành viên</h2><a class="btn btn-ghost btn-sm" href="/shop/account/points.html">Lịch sử điểm</a></div>
                <div class="panel-body">${tierLadderHtml(me.totalPoints ?? 0, tiers)}</div>
            </section>

            <section class="panel mt-4">
                <div class="panel-head"><h2>${ICONS.ticket} Mã khuyến mãi đang chạy</h2><a class="btn btn-ghost btn-sm" href="/shop/promotions.html">Tất cả ưu đãi</a></div>
                <div class="panel-body grid-3">${active.slice(0, 3).map((/** @type {any} */ p) => `
                    <div class="ticket"><div class="left"><b>${money(p.discountValue)}</b></div>
                        <div class="right"><b class="mono text-primary">${escapeHtml(p.code)}</b><span class="text-xs text-muted">HSD: ${formatDate(p.endDate)}</span></div></div>`).join("")
                    || `<p class="text-muted text-sm">Hiện chưa có mã nào.</p>`}</div>
            </section>`;
        $("btnEditName").addEventListener("click", editName);
    }

    function editName() {
        const { el, close } = openModal({
            title: "Sửa họ tên",
            body: `<form class="form-grid" id="nameForm">
                <div class="field"><label>Họ và tên đệm</label><input class="input" name="firstName" required maxlength="100" value="${escapeHtml(me.firstName ?? "")}"></div>
                <div class="field"><label>Tên</label><input class="input" name="lastName" required maxlength="100" value="${escapeHtml(me.lastName ?? "")}"></div>
                <div class="field full"><label>Email</label><input class="input" value="${escapeHtml(me.email ?? "")}" disabled><span class="hint">Email dùng để đăng nhập, không đổi được.</span></div></form>`,
            footer: `<button class="btn btn-ghost" data-close>Hủy</button><button class="btn btn-primary" id="nameSave">Lưu</button>`
        });
        el.querySelector("#nameSave")?.addEventListener("click", async () => {
            const form = /** @type {HTMLFormElement} */ (el.querySelector("#nameForm"));
            if (!form.reportValidity()) return;
            const data = /** @type {any} */ (Object.fromEntries(new FormData(form)));
            try {
                await shopApi.me.update({ firstName: data.firstName.trim(), lastName: data.lastName.trim() });
                close(); toast("Đã cập nhật họ tên.", "success");
                me = await initAccount("index"); render();
            } catch (err) { toast(errMsg(err), "error"); }
        });
    }
    render();
}
