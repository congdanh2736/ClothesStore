/*
 * returns.js - DANH SÁCH YÊU CẦU ĐỔI / TRẢ HÀNG (shop/account/returns.html)
 * API: GET /api/ReturnRequest/customer/{id}, DELETE /api/ReturnRequest/{id} (chỉ Chờ duyệt / Từ chối)
 * Lưu ý: backend chỉ hỗ trợ TRẢ HÀNG - HOÀN TIỀN (không đổi size/màu trực tiếp).
 */
import { initAccount, canReturn } from "./accountShell.js";
import { toast, errMsg, confirmBox, money, ICONS, $ } from "../shell.js";
import { shopApi } from "../shopApi.js";
import { escapeHtml, formatDateTime, returnStatusBadge } from "../../../assets/js/core/utils.js";

const NOTE = /** @type {Record<string,string>} */ ({
    Pending: "Cửa hàng đang xem xét yêu cầu của bạn.",
    Approved: "Đã duyệt. Vui lòng gửi hàng về cửa hàng / kho để được hoàn tiền.",
    Rejected: "Yêu cầu bị từ chối. Bạn có thể xóa và gửi lại yêu cầu khác.",
    Completed: "Đã nhận hàng trả và hoàn tiền. Điểm thưởng tương ứng đã bị trừ."
});

if (await initAccount("returns")) {
    async function render() {
        const [returns, orders] = await Promise.all([shopApi.returns.getMine(), shopApi.orders.getMine()]);
        const eligible = orders.filter((/** @type {any} */ o) => canReturn(o)).length;
        $("eligibleNote").innerHTML = eligible
            ? `Bạn có <b class="text-primary">${eligible}</b> đơn hàng còn trong hạn trả (30 ngày).` : "Hiện không có đơn hàng nào còn trong hạn trả.";
        $("returnList").innerHTML = returns.length ? returns.map((/** @type {any} */ r) => `
            <article class="order-card">
                <div class="oc-head"><div class="row wrap"><b class="mono">Yêu cầu #${r.id}</b>
                    <a class="chip" href="/shop/account/order-detail.html?id=${r.orderId}">Đơn #${r.orderId}</a>
                    <span class="text-sm text-muted">${formatDateTime(r.requestDate)}</span></div>${returnStatusBadge(r.status)}</div>
                <div class="oc-body">
                    <div class="text-sm"><span class="text-muted">Lý do:</span> ${escapeHtml(r.reason)}</div>
                    ${r.items.map((/** @type {any} */ i) => `<div class="inset row-between wrap"><div><b>${escapeHtml(i.productName)}</b>
                        <div class="text-sm text-muted">Size: ${escapeHtml(i.size)} · Màu: ${escapeHtml(i.color)} · x${i.quantity} · Tình trạng: ${escapeHtml(i.condition ?? "—")}</div></div>
                        <b>${money(i.refundAmount)}</b></div>`).join("")}
                </div>
                <div class="oc-foot"><span class="text-sm text-muted">${NOTE[r.status] ?? ""}</span>
                    <div class="row wrap"><span>Hoàn tiền: <b class="price" style="font-size:18px">${money(r.totalRefund)}</b></span>
                        ${["Pending", "Rejected"].includes(r.status) ? `<button class="btn btn-sm btn-danger" data-del="${r.id}">${ICONS.trash} Xóa yêu cầu</button>` : ""}</div></div>
            </article>`).join("")
            : `<div class="panel empty">${ICONS.ret}<h3>Chưa có yêu cầu trả hàng</h3><p>Đơn hoàn tất trong vòng 30 ngày có thể yêu cầu trả hàng.</p></div>`;
    }
    $("returnList").addEventListener("click", async e => {
        const b = /** @type {HTMLElement | null} */ (/** @type {HTMLElement} */ (e.target).closest("[data-del]"));
        if (!b || !(await confirmBox(`Xóa yêu cầu trả hàng #${b.dataset.del}?`, "Xóa"))) return;
        try { await shopApi.returns.remove(Number(b.dataset.del)); toast("Đã xóa yêu cầu.", "success"); render(); }
        catch (err) { toast(errMsg(err), "error"); }
    });
    await render();
}
