/*
 * ============================================================================
 *  dashboard.js  -  XỬ LÝ CHO TRANG admin/dashboard.html (TỔNG QUAN)
 * ============================================================================
 *
 *  HIỆN TẠI: trang chỉ vẽ GIAO DIỆN từ DỮ LIỆU MẪU (biến SAMPLE ở PHẦN 1).
 *
 *  ĐỂ NỐI API THẬT SAU NÀY (việc của Nhóm 3), chỉ cần sửa hàm loadDashboardData()
 *  ở PHẦN 2: thay "return SAMPLE" bằng code gọi API rồi trả về object CÓ CÙNG
 *  HÌNH DẠNG với SAMPLE. Các hàm vẽ ở PHẦN 3 giữ nguyên, không phải sửa.
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/ -> "../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout } from "./layout.js";
import { getDisplayName } from "../../assets/js/core/auth.js";
import { ORDER_STATUS, ORDER_STATUS_LABELS } from "../../assets/js/core/config.js";
import {
    escapeHtml, formatMoney, formatNumber, formatDate, formatDateTime,
    orderStatusBadge, paymentStatusBadge, debounce
} from "../../assets/js/core/utils.js";
import { renderTable } from "../../assets/js/components/table.js";
import { toast } from "../../assets/js/components/toast.js";

/**
 * Lấy một thẻ theo id. Viết hàm ngắn "$" cho gọn, khỏi lặp lại document.getElementById.
 * (Chú thích kiểu bên trong báo cho VS Code biết chắc đây là thẻ HTML -> hết báo "possibly null".)
 * @param {string} id
 */
const $ = id => /** @type {HTMLElement} */ (document.getElementById(id));

/* ===========================================================================
 *  PHẦN 1: DỮ LIỆU MẪU
 *  Hình dạng dữ liệu được đặt giống những gì API thật trả về, để sau này thay dễ.
 * ========================================================================= */

/**
 * Tạo ngày cách hôm nay "daysAgo" ngày (để biểu đồ luôn là 7 ngày gần nhất).
 * @param {number} daysAgo
 */
function dayOffset(daysAgo) {
    const d = new Date();
    d.setDate(d.getDate() - daysAgo);
    return d;
}

const SAMPLE = {
    // Ô số liệu: giá trị kỳ này + kỳ trước (để tính tăng/giảm %)
    revenue: { current: 48750000, previous: 41200000 },
    orders: { current: 86, previous: 92 },
    pendingOrders: 6,
    newCustomers: { current: 23, previous: 17 },

    // Doanh thu từng ngày (sau này lấy từ GET /api/StoreDailyStat, cộng các cửa hàng theo ngày)
    revenueByDay: [
        { date: dayOffset(6), revenue: 5200000, orders: 9 },
        { date: dayOffset(5), revenue: 6850000, orders: 12 },
        { date: dayOffset(4), revenue: 4300000, orders: 8 },
        { date: dayOffset(3), revenue: 7900000, orders: 14 },
        { date: dayOffset(2), revenue: 9450000, orders: 16 },
        { date: dayOffset(1), revenue: 8100000, orders: 15 },
        { date: dayOffset(0), revenue: 6950000, orders: 12 }
    ],

    // Số đơn theo trạng thái (sau này đếm từ GET /api/Order)
    ordersByStatus: {
        Pending: 6, Confirmed: 4, Shipping: 9, Completed: 61, Cancelled: 3, Returned: 1
    },

    // 5 đơn mới nhất (cùng tên thuộc tính với OrderDetailDTO của backend)
    latestOrders: [
        { id: 1086, orderDate: dayOffset(0), customerName: "Nguyễn Văn An",  status: "Pending",   paymentStatus: "Pending", totalAmount: 1250000 },
        { id: 1085, orderDate: dayOffset(0), customerName: "Trần Thị Bình",  status: "Confirmed", paymentStatus: "Paid",    totalAmount: 890000 },
        { id: 1084, orderDate: dayOffset(1), customerName: "Lê Hoàng Nam",   status: "Shipping",  paymentStatus: "Pending", totalAmount: 2140000 },
        { id: 1083, orderDate: dayOffset(1), customerName: "Phạm Minh Châu", status: "Completed", paymentStatus: "Paid",    totalAmount: 560000 },
        { id: 1082, orderDate: dayOffset(2), customerName: "Võ Thị Hoa",     status: "Cancelled", paymentStatus: "Cancelled", totalAmount: 1730000 }
    ],

    // Việc cần xử lý
    todos: {
        pendingOrders: 6,       // đơn chờ xác nhận          (GET /api/Order, lọc Pending)
        pendingReturns: 2,      // yêu cầu đổi/trả chờ duyệt (GET /api/ReturnRequest, lọc Pending)
        failedShipments: 1,     // vận đơn giao thất bại     (GET /api/Shipment, lọc Failed)
        lowStock: 4             // biến thể sắp hết hàng     (GET /api/StoreStock, số lượng thấp)
    },

    // Top sản phẩm bán chạy (sau này lấy từ GET /api/StoreItemStat)
    topProducts: [
        { productName: "Áo thun basic cotton",  variant: "Trắng / M", sold: 128, revenue: 25600000 },
        { productName: "Quần jean slim fit",    variant: "Xanh đậm / 30", sold: 74, revenue: 33300000 },
        { productName: "Áo sơ mi oxford",       variant: "Xanh nhạt / L", sold: 61, revenue: 21350000 },
        { productName: "Váy midi hoa nhí",      variant: "Be / S", sold: 45, revenue: 17550000 },
        { productName: "Áo khoác bomber",       variant: "Đen / XL", sold: 32, revenue: 19200000 }
    ]
};

/* ===========================================================================
 *  PHẦN 2: LẤY DỮ LIỆU   (CHỖ DUY NHẤT CẦN SỬA KHI NỐI API)
 * ========================================================================= */

/**
 * Trả về dữ liệu cho cả trang.
 * Bây giờ: trả dữ liệu mẫu. Sau này: gọi API rồi tính ra object giống SAMPLE, ví dụ:
 *
 *     const orders = await get("/Order");
 *     const pending = orders.filter(o => o.status === "Pending").length;
 *     ...
 *     return { revenue: {...}, orders: {...}, pendingOrders: pending, ... };
 *
 * Nhớ xóa khối thông báo "DỮ LIỆU MẪU" (id="sampleNote") trong dashboard.html khi đó.
 *
 * @param {string} range - khoảng thời gian đang chọn: "today" | "7" | "30"
 */
async function loadDashboardData(range) {
    return SAMPLE;
}

/* ===========================================================================
 *  PHẦN 3: CÁC HÀM VẼ GIAO DIỆN
 * ========================================================================= */

/* ---------- 3.1 Đầu trang: lời chào + ngày hôm nay ---------- */
function drawHeader() {
    const name = getDisplayName();
    $("greeting").textContent = name ? `Xin chào, ${name}!` : "Xin chào!";

    // Ví dụ: "Thứ Sáu, 16 tháng 10, 2026"
    const today = new Date().toLocaleDateString("vi-VN", {
        weekday: "long", day: "numeric", month: "long", year: "numeric"
    });
    $("todayText").textContent = `Hôm nay là ${today}. Đây là tình hình kinh doanh của cửa hàng.`;
}

/* ---------- 3.2 Bốn ô số liệu ---------- */

/**
 * Tạo dòng so sánh với kỳ trước, ví dụ "▲ 18,3% so với kỳ trước".
 * @param {number} current  - số kỳ này
 * @param {number} previous - số kỳ trước
 */
function trendHtml(current, previous) {
    if (!previous) return "";
    const percent = ((current - previous) / previous) * 100;
    const up = percent >= 0;
    const text = `${up ? "▲" : "▼"} ${Math.abs(percent).toLocaleString("vi-VN", { maximumFractionDigits: 1 })}%`;
    return `<span class="${up ? "up" : "down"}">${text}</span> so với kỳ trước`;
}

/** @param {any} data */
function drawStats(data) {
    $("statRevenue").textContent = formatMoney(data.revenue.current);
    $("statRevenueTrend").innerHTML = trendHtml(data.revenue.current, data.revenue.previous);

    $("statOrders").textContent = formatNumber(data.orders.current);
    $("statOrdersTrend").innerHTML = trendHtml(data.orders.current, data.orders.previous);

    $("statPending").textContent = formatNumber(data.pendingOrders);

    $("statCustomers").textContent = formatNumber(data.newCustomers.current);
    $("statCustomersTrend").innerHTML = trendHtml(data.newCustomers.current, data.newCustomers.previous);
}

/* ---------- 3.3 Biểu đồ cột doanh thu (vẽ bằng SVG, không cần thư viện) ---------- */

/**
 * Viết gọn số tiền cho trục biểu đồ: 9.450.000 -> "9,5tr", 850.000 -> "850k".
 * @param {number} value
 */
function shortMoney(value) {
    const fmt = (/** @type {number} */ n) => n.toLocaleString("vi-VN", { maximumFractionDigits: 1 });
    if (value >= 1e9) return `${fmt(value / 1e9)} tỷ`;
    if (value >= 1e6) return `${fmt(value / 1e6)}tr`;
    if (value >= 1e3) return `${fmt(value / 1e3)}k`;
    return String(value);
}

/**
 * Tìm số "tròn" làm mốc cao nhất cho trục dọc (để vạch chia đẹp: 2tr, 4tr, 6tr...).
 * Ví dụ max = 9.450.000 -> 10.000.000.
 * @param {number} max
 */
function niceMax(max) {
    if (max <= 0) return 1;
    const power = Math.pow(10, Math.floor(Math.log10(max)));   // bậc của số: 1.000.000
    const steps = [1, 2, 2.5, 5, 10];
    const step = steps.find(s => s * power >= max) ?? 10;
    return step * power;
}

/**
 * Tạo đường viền cho một cột: bo tròn 4px ở 2 góc TRÊN, đáy vuông nằm sát trục.
 * (SVG <rect> chỉ bo được cả 4 góc nên ta tự vẽ bằng <path>.)
 * @param {number} x @param {number} y @param {number} w @param {number} h
 */
function barPath(x, y, w, h) {
    const r = Math.min(4, w / 2, h);
    return `M${x},${y + h} V${y + r} Q${x},${y} ${x + r},${y} H${x + w - r} Q${x + w},${y} ${x + w},${y + r} V${y + h} Z`;
}

/** @param {any[]} days - mảng { date, revenue, orders } */
function drawRevenueChart(days) {
    const box = $("revenueChart");

    // Kích thước khung vẽ = đúng độ rộng thật của thẻ chứa (để chữ luôn cỡ 12px, không bị
    // thu nhỏ trên điện thoại). Tối thiểu 300px. Màn hình hẹp thì biểu đồ thấp hơn một chút.
    const W = Math.max(300, Math.floor(box.clientWidth - 40));   // trừ khoảng đệm 2 bên của .chart-box
    const H = W < 500 ? 220 : 260;
    const narrow = W < 500;
    const pad = { top: 24, right: 8, bottom: 28, left: narrow ? 40 : 52 };   // chừa chỗ cho chữ ở trục
    const plotW = W - pad.left - pad.right;
    const plotH = H - pad.top - pad.bottom;

    const max = niceMax(Math.max(...days.map(d => d.revenue)));
    const gridCount = 4;                                       // 4 khoảng chia trục dọc
    const slot = plotW / days.length;                           // bề rộng dành cho mỗi ngày
    const barW = Math.min(44, slot * 0.6);                      // cột mảnh, chừa khe giữa các cột
    const y = (/** @type {number} */ v) => pad.top + plotH - (v / max) * plotH;   // đổi tiền -> tọa độ dọc

    // Ngày có doanh thu cao nhất -> chỉ ghi số trên cột này (không ghi số trên mọi cột cho đỡ rối)
    const maxIndex = days.reduce((best, d, i) => (d.revenue > days[best].revenue ? i : best), 0);
    const lastIndex = days.length - 1;                          // cột cuối = hôm nay

    // Đường kẻ ngang + nhãn trục dọc
    let grid = "";
    for (let i = 0; i <= gridCount; i++) {
        const value = (max / gridCount) * i;
        const gy = y(value);
        grid += `<line class="chart-grid-line" x1="${pad.left}" x2="${W - pad.right}" y1="${gy}" y2="${gy}"/>`;
        grid += `<text class="chart-axis-text" x="${pad.left - 8}" y="${gy + 4}" text-anchor="end">${shortMoney(value)}</text>`;
    }

    // Các cột
    const bars = days.map((d, i) => {
        const cx = pad.left + slot * i + slot / 2;              // tâm của ô ngày
        const bx = cx - barW / 2;
        const by = y(d.revenue);
        const bh = pad.top + plotH - by;
        // Màn hình hẹp chỉ ghi ngày ("14") cho khỏi chồng chữ; rộng thì ghi "14-10"
        const label = i === lastIndex ? (narrow ? "Nay" : "Hôm nay")
            : narrow ? String(new Date(d.date).getDate())
            : new Date(d.date).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit" });

        return `
            <g class="chart-col" data-index="${i}">
                <rect class="chart-hit" x="${pad.left + slot * i}" y="${pad.top}" width="${slot}" height="${plotH}"/>
                <path class="chart-bar ${i === lastIndex ? "is-today" : ""}" d="${barPath(bx, by, barW, bh)}"/>
                ${i === maxIndex ? `<text class="chart-value" x="${cx}" y="${by - 8}" text-anchor="middle">${shortMoney(d.revenue)}</text>` : ""}
                <text class="chart-axis-text" x="${cx}" y="${H - 8}" text-anchor="middle">${label}</text>
            </g>`;
    }).join("");

    const total = days.reduce((sum, d) => sum + d.revenue, 0);
    $("chartTotal").textContent = `Tổng: ${formatMoney(total)}`;

    // role="img" + aria-label: mô tả biểu đồ cho phần mềm đọc màn hình.
    // Bảng ẩn (sr-only) bên dưới chứa số liệu đầy đủ cho ai không xem được hình.
    box.innerHTML = `
        <svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Biểu đồ cột doanh thu ${days.length} ngày qua, tổng ${formatMoney(total)}">
            ${grid}
            ${bars}
            <line class="chart-grid-line" x1="${pad.left}" x2="${W - pad.right}" y1="${pad.top + plotH}" y2="${pad.top + plotH}" style="stroke:#cfd4dc"/>
        </svg>
        <div class="chart-tooltip hidden" id="chartTooltip"></div>
        <table class="sr-only">
            <caption>Doanh thu theo ngày</caption>
            <tr><th>Ngày</th><th>Doanh thu</th><th>Số đơn</th></tr>
            ${days.map(d => `<tr><td>${formatDate(d.date)}</td><td>${formatMoney(d.revenue)}</td><td>${d.orders}</td></tr>`).join("")}
        </table>`;

    // ---- Rê chuột vào cột -> hiện hộp nhỏ ghi ngày, doanh thu, số đơn ----
    const tooltip = $("chartTooltip");
    const svg = /** @type {SVGSVGElement} */ (box.querySelector("svg"));

    box.querySelectorAll(".chart-col").forEach(col => {
        col.addEventListener("mouseenter", () => {
            const i = Number(/** @type {HTMLElement} */ (/** @type {unknown} */ (col)).dataset.index);
            const d = days[i];
            tooltip.innerHTML = `<strong>${formatMoney(d.revenue)}</strong>${formatDate(d.date)} · ${formatNumber(d.orders)} đơn`;

            // Đổi tọa độ trong SVG sang tọa độ thật trên màn hình (thường bằng nhau, scale ≈ 1)
            const scale = svg.getBoundingClientRect().width / W;
            const svgLeft = svg.getBoundingClientRect().left - box.getBoundingClientRect().left;
            const svgTop = svg.getBoundingClientRect().top - box.getBoundingClientRect().top;
            const cx = pad.left + slot * i + slot / 2;
            tooltip.style.left = `${svgLeft + cx * scale}px`;
            tooltip.style.top = `${svgTop + y(d.revenue) * scale}px`;
            tooltip.classList.remove("hidden");
        });
        col.addEventListener("mouseleave", () => tooltip.classList.add("hidden"));
    });
}

/* ---------- 3.4 Tình trạng đơn hàng (thanh ngang) ---------- */

/** @param {Record<string, number>} counts - số đơn theo từng trạng thái */
function drawStatusList(counts) {
    // Thứ tự hiển thị theo vòng đời đơn hàng
    const order = [
        ORDER_STATUS.PENDING, ORDER_STATUS.CONFIRMED, ORDER_STATUS.SHIPPING,
        ORDER_STATUS.COMPLETED, ORDER_STATUS.CANCELLED, ORDER_STATUS.RETURNED
    ];
    const total = order.reduce((sum, s) => sum + (counts[s] ?? 0), 0) || 1;

    $("statusList").innerHTML = order.map(status => {
        const count = counts[status] ?? 0;
        const info = ORDER_STATUS_LABELS[status];          // { text: "Chờ xác nhận", color: "warning" }
        const percent = (count / total) * 100;
        return `
            <div class="status-row">
                <div class="status-row-head">
                    ${orderStatusBadge(status)}
                    <span><span class="status-count">${formatNumber(count)}</span>
                          <span class="text-muted text-sm">(${percent.toLocaleString("vi-VN", { maximumFractionDigits: 0 })}%)</span></span>
                </div>
                <div class="status-track" role="img" aria-label="${escapeHtml(info.text)}: ${count} đơn">
                    <div class="status-fill fill-${info.color}" style="width:${percent}%"></div>
                </div>
            </div>`;
    }).join("");
}

/* ---------- 3.5 Đơn hàng mới nhất (dùng renderTable có sẵn) ---------- */

/** @param {any[]} orders */
function drawLatestOrders(orders) {
    renderTable($("latestOrders"), {
        rows: orders,
        emptyText: "Chưa có đơn hàng nào.",
        columns: [
            // Trang chi tiết đơn là của Nhóm 4
            { key: "id", title: "Mã đơn", render: row => `<a class="fw-bold" href="sales/order-detail.html?id=${row.id}">#${row.id}</a>` },
            { key: "customerName", title: "Khách hàng" },
            { key: "orderDate", title: "Thời gian", render: row => formatDateTime(row.orderDate) },
            { key: "status", title: "Trạng thái", render: row => orderStatusBadge(row.status) },
            { key: "paymentStatus", title: "Thanh toán", render: row => paymentStatusBadge(row.paymentStatus) },
            { key: "totalAmount", title: "Tổng tiền", align: "right", render: row => formatMoney(row.totalAmount) }
        ]
    });
}

/* ---------- 3.6 Việc cần xử lý ---------- */

/** @param {any} todos */
function drawTodos(todos) {
    // Mỗi việc: số lượng, tiêu đề, mô tả, link tới trang xử lý, màu
    const items = [
        { count: todos.pendingOrders,   title: "Đơn chờ xác nhận",       note: "Xác nhận để chuẩn bị giao hàng",   href: "sales/orders.html?status=Pending",   color: "warning" },
        { count: todos.pendingReturns,  title: "Yêu cầu đổi / trả",      note: "Đang chờ duyệt",                   href: "sales/returns.html?status=Pending",  color: "info" },
        { count: todos.failedShipments, title: "Vận đơn giao thất bại",  note: "Giao lại hoặc hủy đơn",            href: "sales/shipments.html?status=Failed", color: "danger" },
        { count: todos.lowStock,        title: "Sản phẩm sắp hết hàng",  note: "Cần nhập thêm kho",                href: "stores/stock.html",                  color: "secondary" }
    ];

    $("todoList").innerHTML = items.map(item => `
        <a class="todo-item" href="${item.href}">
            <span class="todo-count badge-${item.count ? item.color : "secondary"}">${formatNumber(item.count)}</span>
            <span class="todo-text">${escapeHtml(item.title)}<small>${escapeHtml(item.note)}</small></span>
            <span class="todo-arrow" aria-hidden="true">›</span>
        </a>`).join("");
}

/* ---------- 3.7 Sản phẩm bán chạy ---------- */

/** @param {any[]} products */
function drawTopProducts(products) {
    renderTable($("topProducts"), {
        rows: products,
        emptyText: "Chưa có dữ liệu bán hàng.",
        columns: [
            { key: "rank", title: "#", width: "50px", render: (_row, i) => `<span class="fw-bold">${i + 1}</span>` },
            { key: "productName", title: "Sản phẩm",
              render: row => `<div class="fw-bold">${escapeHtml(row.productName)}</div><div class="text-muted text-sm">${escapeHtml(row.variant)}</div>` },
            { key: "sold", title: "Đã bán", align: "right", render: row => formatNumber(row.sold) },
            { key: "revenue", title: "Doanh thu", align: "right", render: row => formatMoney(row.revenue) }
        ]
    });
}

/* ===========================================================================
 *  PHẦN 4: KHỞI ĐỘNG TRANG
 * ========================================================================= */

const rangeSelect = /** @type {HTMLSelectElement} */ ($("rangeSelect"));

/** Dữ liệu biểu đồ lần vẽ gần nhất. @type {any[]} */
let lastRevenueByDay = [];

/** Tải dữ liệu rồi vẽ lại toàn bộ trang. */
async function render() {
    try {
        const data = await loadDashboardData(rangeSelect.value);
        lastRevenueByDay = data.revenueByDay;    // nhớ lại để vẽ lại biểu đồ khi đổi kích thước màn hình
        drawStats(data);
        drawRevenueChart(data.revenueByDay);
        drawStatusList(data.ordersByStatus);
        drawLatestOrders(data.latestOrders);
        drawTodos(data.todos);
        drawTopProducts(data.topProducts);
    } catch (err) {
        toast.error(`Không tải được dữ liệu tổng quan: ${err.message}`);
    }
}

if (await initAdminLayout({ title: "Tổng quan" })) {
    drawHeader();

    // Đổi khoảng thời gian: hiện tại dữ liệu mẫu không đổi theo, chỉ báo cho biết
    rangeSelect.addEventListener("change", () => {
        toast.info("Bộ lọc thời gian sẽ hoạt động khi nối API thật.");
        render();
    });

    await render();

    // Kéo to / thu nhỏ cửa sổ (hoặc xoay điện thoại) -> vẽ lại biểu đồ cho vừa khung.
    // debounce: chờ ngừng kéo 200ms mới vẽ, khỏi vẽ lại hàng trăm lần.
    window.addEventListener("resize", debounce(() => {
        if (lastRevenueByDay.length) drawRevenueChart(lastRevenueByDay);
    }, 200));
}
