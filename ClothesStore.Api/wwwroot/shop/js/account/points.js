/*
 * points.js - ĐIỂM THƯỞNG & HẠNG THÀNH VIÊN (shop/account/points.html)
 * API: GET /api/Customer/{id}, GET /api/MembershipTier, GET /api/LoyaltyTransaction/customer/{id}
 * Backend chỉ TÍCH điểm (10.000đ = 1 điểm khi đơn hoàn tất) và TRỪ khi trả hàng - chưa có đổi điểm lấy quà.
 */
import { initAccount, memberCardHtml, tierLadderHtml } from "./accountShell.js";
import { ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatNumber, formatDateTime } from "../../../assets/js/core/utils.js";

const me = await initAccount("points");
if (me) {
    const [tiers, txns] = await Promise.all([shopApi.getTiers(), shopApi.me.getLoyalty()]);
    const list = [...txns].sort((a, b) => +new Date(b.txnDate) - +new Date(a.txnDate));
    const earned = list.filter(t => t.pointsChange > 0).reduce((s, t) => s + t.pointsChange, 0);
    const lost = list.filter(t => t.pointsChange < 0).reduce((s, t) => s - t.pointsChange, 0);
    /** Thêm liên kết tới đơn hàng trong lý do "... đơn hàng #83" */
    const linkify = (/** @type {string} */ text) => escapeHtml(text).replace(/đơn( hàng)? #(\d+)/g,
        (m, _a, id) => `<a class="text-primary" href="/shop/account/order-detail.html?id=${id}">${m}</a>`);

    $("pointsRoot").innerHTML = `
        <div class="grid-2" style="align-items:stretch">
            ${memberCardHtml(me, tiers)}
            <div class="grid-2" style="gap:14px">
                <div class="stat-tile"><div><div class="label">Điểm hiện có</div><div class="value">${formatNumber(me.totalPoints ?? 0)}</div></div><span class="tile-ico">${ICONS.coin}</span></div>
                <div class="stat-tile"><div><div class="label">Hạng</div><div class="value" style="font-size:22px">${escapeHtml(me.membershipTierName ?? "—")}</div></div><span class="tile-ico">${ICONS.shield}</span></div>
                <div class="stat-tile"><div><div class="label">Đã tích</div><div class="value plus">+${formatNumber(earned)}</div></div><span class="tile-ico">${ICONS.plus}</span></div>
                <div class="stat-tile"><div><div class="label">Bị trừ</div><div class="value minus">-${formatNumber(lost)}</div></div><span class="tile-ico">${ICONS.ret}</span></div>
            </div>
        </div>
        <section class="panel mt-4"><div class="panel-head"><h2>${ICONS.shield} Các hạng thành viên</h2></div>
            <div class="panel-body">${tierLadderHtml(me.totalPoints ?? 0, tiers)}</div></section>
        <section class="panel mt-4"><div class="panel-head"><h2>${ICONS.clock} Lịch sử điểm</h2><span class="badge">${list.length} giao dịch</span></div>
            <div class="panel-body timeline">${list.map(t => `
                <div class="tl-row"><span class="tile-ico" style="${t.pointsChange < 0 ? "background:var(--danger-soft);color:var(--danger)" : ""}">${t.pointsChange < 0 ? ICONS.ret : ICONS.coin}</span>
                    <div class="grow"><div class="fw-bold text-sm">${linkify(t.reason ?? "")}</div><div class="text-xs text-muted">${formatDateTime(t.txnDate)}</div></div>
                    <span class="${t.pointsChange < 0 ? "minus" : "plus"}">${t.pointsChange > 0 ? "+" : ""}${formatNumber(t.pointsChange)}</span></div>`).join("")
                || `<p class="text-muted text-sm">Chưa có giao dịch điểm nào.</p>`}</div></section>`;
}
