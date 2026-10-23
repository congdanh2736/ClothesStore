/*
 * returnNew.js - TẠO YÊU CẦU TRẢ HÀNG (shop/account/return-new.html?orderId=...)
 * API: POST /api/ReturnRequest { orderId, reason, items: [{ orderItemId, quantity, condition }] }
 * Luật backend: đơn Hoàn tất, trong 30 ngày kể từ ngày đặt, không trả quá số đã mua (trừ các yêu cầu chưa bị từ chối).
 * Tiền hoàn = giá x số lượng x (tổng đơn / (tổng đơn + giảm giá))  -> chia đều phần giảm giá cho từng món.
 */
import { initAccount, canReturn, returnDaysLeft } from "./accountShell.js";
import { toast, errMsg, productImages, thumb, qs, money, ICONS, $ } from "../shell.js";
import { shopApi, remainingReturnQty } from "../shopApi.js";
import { escapeHtml, formatDate } from "../../../assets/js/core/utils.js";

const CONDITIONS = ["Còn nguyên tem mác, chưa sử dụng", "Đã mặc thử", "Bị lỗi từ nhà sản xuất", "Bị dính bẩn nhẹ", "Giao sai mẫu / sai size"];
const REASONS = ["Không vừa size", "Sản phẩm bị lỗi / hư hỏng", "Giao sai sản phẩm", "Không giống mô tả / hình ảnh", "Đổi ý, không muốn mua nữa"];

if (await initAccount("returns")) {
    const [orders, returns] = await Promise.all([shopApi.orders.getMine(), shopApi.returns.getMine()]);
    const eligible = orders.filter((/** @type {any} */ o) => canReturn(o)
        && Object.values(remainingReturnQty(o, returns)).some(n => n > 0));
    let order = eligible.find((/** @type {any} */ o) => o.id === Number(qs("orderId"))) ?? eligible[0];

    if (!order) {
        $("returnRoot").innerHTML = `<div class="panel empty">${ICONS.ret}<h3>Không có đơn hàng nào trả được</h3>
            <p>Chỉ đơn đã Hoàn tất, trong vòng 30 ngày và còn sản phẩm chưa trả mới tạo được yêu cầu.</p>
            <a class="btn btn-primary mt-3" href="/shop/account/orders.html">Xem đơn hàng</a></div>`;
    } else {
        await productImages(eligible.flatMap((/** @type {any} */ o) => o.orderItems.map((/** @type {any} */ i) => i.productId)));
        render();
    }

    async function render() {
        const images = await productImages([]);
        const left = remainingReturnQty(order, returns);
        const ratio = order.totalAmount / (order.totalAmount + order.discountAmount);
        $("returnRoot").innerHTML = `
            <div class="layout-aside">
                <form class="stack" id="returnForm">
                    <section class="panel">
                        <div class="panel-head"><h2><span class="badge badge-solid">1</span> Chọn đơn hàng</h2></div>
                        <div class="panel-body"><select class="select" id="orderSelect">${eligible.map((/** @type {any} */ o) =>
                            `<option value="${o.id}" ${o.id === order.id ? "selected" : ""}>Đơn #${o.id} - ${formatDate(o.orderDate)} - ${money(o.totalAmount)} (còn ${returnDaysLeft(o)} ngày)</option>`).join("")}</select></div>
                    </section>
                    <section class="panel">
                        <div class="panel-head"><h2><span class="badge badge-solid">2</span> Sản phẩm muốn trả</h2></div>
                        <div class="panel-body stack">${order.orderItems.map((/** @type {any} */ i) => {
                            const max = left[i.orderItemId] ?? 0;
                            return `<div class="inset stack" data-line="${i.orderItemId}" style="${max ? "" : "opacity:.5"}">
                                <label class="check" style="align-items:flex-start">
                                    <input type="checkbox" data-pick ${max ? "" : "disabled"}>
                                    <span class="line-item grow">${thumb(images.get(i.productId))}<span class="grow"><b>${escapeHtml(i.variantName)}</b>
                                        <span class="text-sm text-muted" style="display:block">Size: ${escapeHtml(i.size)} · Màu: ${escapeHtml(i.color)} · ${money(i.price)}</span>
                                        <span class="text-xs text-faint">${max ? `Có thể trả tối đa ${max}/${i.quantity}` : "Đã yêu cầu trả hết"}</span></span></span></label>
                                <div class="form-grid hidden" data-extra>
                                    <div class="field"><label>Số lượng trả</label><input class="input" type="number" min="1" max="${max}" value="${max}" data-qty></div>
                                    <div class="field"><label>Tình trạng hàng</label><select class="select" data-cond>${CONDITIONS.map(c => `<option>${c}</option>`).join("")}</select></div>
                                </div></div>`;
                        }).join("")}</div>
                    </section>
                    <section class="panel">
                        <div class="panel-head"><h2><span class="badge badge-solid">3</span> Lý do trả hàng</h2></div>
                        <div class="panel-body stack">
                            <div class="row wrap" id="reasonChips">${REASONS.map(r => `<button type="button" class="chip" data-reason="${r}">${r}</button>`).join("")}</div>
                            <textarea class="textarea" id="reason" required maxlength="500" placeholder="Mô tả chi tiết để cửa hàng duyệt nhanh hơn"></textarea>
                            <span class="hint">Backend chỉ hỗ trợ trả hàng - hoàn tiền. Muốn đổi size, bạn trả hàng rồi đặt đơn mới.</span>
                        </div>
                    </section>
                </form>
                <aside class="panel panel-pad sticky">
                    <h2 style="font-size:18px;text-transform:uppercase">${ICONS.ret} Dự kiến hoàn tiền</h2>
                    <div class="mt-3" id="refundBox"></div>
                    <button class="btn btn-primary btn-lg btn-block mt-4" id="btnSubmit" form="returnForm">Gửi yêu cầu</button>
                    <div class="stack mt-4 text-sm text-muted">
                        <div class="perk"><span class="tile-ico">${ICONS.clock}</span><span>Hạn trả: 30 ngày kể từ ngày đặt (${formatDate(order.orderDate)}).</span></div>
                        <div class="perk"><span class="tile-ico">${ICONS.coin}</span><span>Điểm thưởng của phần trả lại sẽ bị trừ khi hoàn tất.</span></div>
                    </div>
                </aside>
            </div>`;

        const refresh = () => {
            let total = 0, count = 0;
            document.querySelectorAll("[data-line]").forEach(line => {
                const on = /** @type {HTMLInputElement} */ (line.querySelector("[data-pick]")).checked;
                line.querySelector("[data-extra]")?.classList.toggle("hidden", !on);
                if (!on) return;
                const it = order.orderItems.find((/** @type {any} */ x) => x.orderItemId === Number(/** @type {HTMLElement} */ (line).dataset.line));
                const q = Number(/** @type {HTMLInputElement} */ (line.querySelector("[data-qty]")).value) || 0;
                total += Math.round(it.price * q * ratio); count += q;
            });
            $("refundBox").innerHTML = `
                <div class="summary-row"><span>Số sản phẩm trả</span><b>${count}</b></div>
                <div class="summary-row"><span>Giảm giá được chia lại</span><b>${order.discountAmount ? `${Math.round((1 - ratio) * 100)}%` : "Không"}</b></div>
                <div class="summary-total"><span class="upper fw-bold">Hoàn lại</span><span class="price">${money(total)}</span></div>`;
        };
        $("returnForm").addEventListener("input", refresh);
        $("returnForm").addEventListener("change", refresh);
        $("orderSelect").addEventListener("change", e => {
            order = eligible.find((/** @type {any} */ o) => o.id === Number(/** @type {HTMLSelectElement} */ (e.target).value)); render();
        });
        $("reasonChips").addEventListener("click", e => {
            const b = /** @type {HTMLElement | null} */ (/** @type {HTMLElement} */ (e.target).closest("[data-reason]"));
            if (b) /** @type {HTMLTextAreaElement} */ ($("reason")).value = b.dataset.reason ?? "";
        });
        $("returnForm").addEventListener("submit", async e => {
            e.preventDefault();
            const items = [...document.querySelectorAll("[data-line]")]
                .filter(l => /** @type {HTMLInputElement} */ (l.querySelector("[data-pick]")).checked)
                .map(l => ({ orderItemId: Number(/** @type {HTMLElement} */ (l).dataset.line),
                    quantity: Number(/** @type {HTMLInputElement} */ (l.querySelector("[data-qty]")).value),
                    condition: /** @type {HTMLSelectElement} */ (l.querySelector("[data-cond]")).value }));
            if (!items.length) { toast("Hãy chọn ít nhất 1 sản phẩm muốn trả.", "warning"); return; }
            const reason = /** @type {HTMLTextAreaElement} */ ($("reason")).value.trim();
            if (!reason) { toast("Vui lòng nhập lý do trả hàng.", "warning"); return; }
            const btn = /** @type {HTMLButtonElement} */ ($("btnSubmit"));
            btn.disabled = true;
            try {
                await shopApi.returns.create({ orderId: order.id, reason, items });
                toast("Đã gửi yêu cầu trả hàng.", "success");
                setTimeout(() => { location.href = "/shop/account/returns.html"; }, 500);
            } catch (err) { toast(errMsg(err), "error"); btn.disabled = false; }
        });
        refresh();
    }
}
