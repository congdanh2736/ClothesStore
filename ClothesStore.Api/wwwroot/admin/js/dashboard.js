/*
 * ============================================================================
 *  dashboard.js  -  XỬ LÝ CHO TRANG admin/dashboard.html (TỔNG QUAN)
 * ============================================================================
 *
 *  [SỬA] ĐÃ NỐI API THẬT. Luồng chạy:
 *    1. PHẦN 2 gọi API 1 lần (đơn hàng, thống kê ngày, thống kê sản phẩm, đổi trả,
 *       vận đơn), cất vào biến "raw". Đổi ô "Hôm nay / 7 ngày / 30 ngày" chỉ TÍNH LẠI
 *       từ "raw", không gọi API lại -> đổi rất nhanh.
 *    2. Hàm buildDashboardData() tính ra object có CÙNG HÌNH DẠNG với EMPTY_DATA (PHẦN 1).
 *    3. Các hàm vẽ ở PHẦN 3 nhận object đó và vẽ (gần như giữ nguyên như cũ).
 *    4. Tồn kho (rất nhiều dòng) được tải SAU, không bắt trang phải chờ:
 *       ô "Sản phẩm sắp hết hàng" hiện "…" rồi tự điền số khi tải xong.
 *
 *  Các API dùng (xem assets/js/api/dashboardApi.js):
 *    GET /api/Order, /api/StoreDailyStat, /api/StoreItemStat, /api/ReturnRequest,
 *    /api/Shipment, /api/StoreStock, /api/ProductVariant/{id}
 *
 *  Đường dẫn import: file này ở wwwroot/admin/js/ -> "../../" = wwwroot/
 * ============================================================================
 */

import { initAdminLayout } from "./layout.js";
import { getDisplayName } from "../../assets/js/core/auth.js";
import { ORDER_STATUS, ORDER_STATUS_LABELS } from "../../assets/js/core/config.js";
import {
    escapeHtml, formatMoney, formatNumber, formatDate, formatDateTime, toDate,
    orderStatusBadge, paymentStatusBadge, debounce
} from "../../assets/js/core/utils.js";
import { renderTable } from "../../assets/js/components/table.js";
import { toast } from "../../assets/js/components/toast.js";
import { dashboardApi } from "../../assets/js/api/dashboardApi.js";   // [THÊM]

/**
 * Lấy một thẻ theo id. Viết hàm ngắn "$" cho gọn, khỏi lặp lại document.getElementById.
 * (Chú thích kiểu bên trong báo cho VS Code biết chắc đây là thẻ HTML -> hết báo "possibly null".)
 * @param {string} id
 */
const $ = id => /** @type {HTMLElement} */(document.getElementById(id));

/* ===========================================================================
 *  PHẦN 1: "KHUÔN" DỮ LIỆU RỖNG
 *  Chưa nối API nên mọi thứ đều trống (0 hoặc danh sách rỗng []).
 *  Object này cũng là "khuôn mẫu": dữ liệu thật sau này phải có ĐÚNG các tên
 *  thuộc tính như dưới đây thì các hàm vẽ mới hiểu được.
 * ========================================================================= */

const EMPTY_DATA = {
    // Ô số liệu: current = kỳ này, previous = kỳ trước (để tính tăng/giảm %)
    revenue: { current: 0, previous: 0 },          // doanh thu (đồng)
    orders: { current: 0, previous: 0 },           // số đơn hàng mới
    pendingOrders: 0,                              // số đơn chờ xác nhận
    newCustomers: { current: 0, previous: 0 },     // số khách hàng mới

    // Doanh thu từng ngày, mỗi phần tử: { date, revenue, orders }
    // (doanh thu: GET /api/StoreDailyStat cộng các cửa hàng theo ngày; số đơn: đếm từ GET /api/Order)
    revenueByDay: [],

    // Số đơn theo trạng thái, ví dụ { Pending: 6, Completed: 61, ... }
    // (đếm từ GET /api/Order). Để trống = tất cả bằng 0.
    ordersByStatus: {},

    // Các đơn mới nhất, mỗi đơn: { id, orderDate, customerName, status, paymentStatus, totalAmount }
    // (cùng tên thuộc tính với OrderDetailDTO của backend)
    latestOrders: [],

    // Việc cần xử lý
    todos: {
        pendingOrders: 0,       // đơn chờ xác nhận          (GET /api/Order, lọc Pending)
        pendingReturns: 0,      // yêu cầu đổi/trả chờ duyệt (GET /api/ReturnRequest, lọc Pending)
        failedShipments: 0,     // vận đơn giao thất bại     (GET /api/Shipment, lọc Failed)
        lowStock: 0             // biến thể sắp hết hàng     (GET /api/StoreStock, số lượng thấp)
        // null = đang tải -> ô hiện "…"
    },

    // Sản phẩm bán chạy, mỗi dòng: { productName, variant, sold, revenue }
    // (lấy từ GET /api/StoreItemStat, tên sản phẩm lấy từ GET /api/ProductVariant/{id})
    topProducts: []
};

/**
 * Tạo ngày cách hôm nay "daysAgo" ngày.
 * Dùng để vẽ sẵn trục 7 ngày gần nhất cho biểu đồ khi chưa có dữ liệu.
 * @param {number} daysAgo
 */
function dayOffset(daysAgo) {
    const d = new Date();
    d.setDate(d.getDate() - daysAgo);
    return d;
}

/* ===========================================================================
 *  PHẦN 2: LẤY DỮ LIỆU TỪ API VÀ TÍNH SỐ LIỆU   [SỬA: trước đây trả về EMPTY_DATA]
 * ========================================================================= */

/** "Sắp hết hàng" = tổng tồn kho của biến thể (cộng mọi cửa hàng) còn từ chừng này trở xuống */
const LOW_STOCK_THRESHOLD = 5;

/** Số sản phẩm hiện trong bảng "Sản phẩm bán chạy" */
const TOP_PRODUCT_COUNT = 5;

/** Số đơn hiện trong bảng "Đơn hàng mới nhất" */
const LATEST_ORDER_COUNT = 8;

/**
 * Dữ liệu thô lấy từ API (chỉ gọi API 1 lần, đổi khoảng thời gian thì tính lại từ đây).
 * @type {{ orders: any[], dailyStats: any[], itemStats: any[], returns: any[], shipments: any[] } | null}
 */
let raw = null;

/** Số biến thể sắp hết hàng; null = chưa tải xong tồn kho. @type {number | null} */
let lowStockCount = null;

/** Nhớ tên biến thể đã hỏi API rồi, khỏi hỏi lại: variantId -> { productName, color, size } @type {Map<number, any>} */
const variantCache = new Map();

/**
 * Đổi ngày giờ thành chuỗi "năm-tháng-ngày" theo GIỜ MÁY ĐANG DÙNG, ví dụ "2026-10-21".
 * Dùng làm "chìa khóa" để gom số liệu theo ngày.
 * - Ngày đặt hàng từ API là giờ UTC -> toDate() đổi sang giờ Việt Nam trước.
 * - statDate của bảng thống kê đã là ngày sẵn ("2026-10-21") -> giữ nguyên.
 * @param {any} value
 */
function dayKey(value) {
    if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value)) return value;
    const d = toDate(value) ?? new Date(value);
    const m = String(d.getMonth() + 1).padStart(2, "0");
    const day = String(d.getDate()).padStart(2, "0");
    return `${d.getFullYear()}-${m}-${day}`;
}

/**
 * Danh sách "chìa khóa ngày" của n ngày, kết thúc ở ngày cách hôm nay "endDaysAgo" ngày.
 * Ví dụ hôm nay 21/10: dayKeys(3, 0) = ["2026-10-19", "2026-10-20", "2026-10-21"]
 * @param {number} n @param {number} endDaysAgo
 */
function dayKeys(n, endDaysAgo) {
    const keys = [];
    for (let i = n - 1; i >= 0; i--) keys.push(dayKey(dayOffset(endDaysAgo + i)));
    return keys;
}

/** Gọi các API cần thiết (cùng lúc) và cất vào biến raw. */
async function fetchRawData() {
    const [orders, dailyStats, itemStats, returns, shipments] = await Promise.all([
        dashboardApi.getOrders(),
        dashboardApi.getDailyStats(),
        dashboardApi.getItemStats(),
        dashboardApi.getReturnRequests(),
        dashboardApi.getShipments()
    ]);
    raw = { orders, dailyStats, itemStats, returns, shipments };
}

/**
 * Tải tồn kho ở chế độ "chạy nền" rồi đếm biến thể sắp hết hàng.
 * Tồn kho có vài chục nghìn dòng nên không bắt cả trang phải chờ.
 * @param {() => void} onDone - hàm gọi khi đếm xong (để vẽ lại ô "Việc cần xử lý")
 */
async function loadLowStock(onDone) {
    try {
        const stocks = await dashboardApi.getStocks();
        // Cộng tồn kho của cùng 1 biến thể ở mọi cửa hàng
        /** @type {Map<number, number>} */
        const totalByVariant = new Map();
        for (const s of stocks) {
            totalByVariant.set(s.variantId, (totalByVariant.get(s.variantId) ?? 0) + s.quantity);
        }
        lowStockCount = [...totalByVariant.values()].filter(q => q <= LOW_STOCK_THRESHOLD).length;
    } catch (err) {
        lowStockCount = 0;
        console.warn("Không tải được tồn kho:", err);
    }
    onDone();
}

/**
 * Lấy tên sản phẩm + màu + size cho các biến thể bán chạy (gọi API cho biến thể nào chưa có trong bộ nhớ).
 * @param {number[]} variantIds
 */
async function loadVariantNames(variantIds) {
    const missing = variantIds.filter(id => !variantCache.has(id));
    const results = await Promise.all(missing.map(id => dashboardApi.getVariant(id).catch(() => null)));
    missing.forEach((id, i) => variantCache.set(id, results[i] ?? { productName: `Biến thể #${id}`, color: "", size: "" }));
}

/**
 * Tính toàn bộ số liệu cho khoảng thời gian đang chọn, trả về object giống EMPTY_DATA.
 * @param {string} range - "today" | "7" | "30"
 */
async function buildDashboardData(range) {
    if (!raw) return EMPTY_DATA;
    const { orders, dailyStats, itemStats, returns, shipments } = raw;

    // ---- Khoảng thời gian: kỳ này (n ngày tới hôm nay) và kỳ trước (n ngày liền trước) ----
    const n = range === "today" ? 1 : Number(range) || 7;
    const current = new Set(dayKeys(n, 0));
    const previous = new Set(dayKeys(n, n));

    // ---- Doanh thu theo ngày: cộng TotalRevenue của mọi cửa hàng (ghi nhận lúc đơn hoàn tất) ----
    /** @type {Map<string, number>} */
    const revenueOfDay = new Map();
    for (const s of dailyStats) {
        const key = dayKey(s.statDate);
        revenueOfDay.set(key, (revenueOfDay.get(key) ?? 0) + Number(s.totalRevenue));
    }
    const sumRevenue = (/** @type {Set<string>} */ days) =>
        [...days].reduce((sum, key) => sum + (revenueOfDay.get(key) ?? 0), 0);

    // ---- Đơn hàng: gom theo ngày đặt ----
    /** @type {Map<string, any[]>} */
    const ordersOfDay = new Map();
    for (const o of orders) {
        const key = dayKey(o.orderDate);
        if (!ordersOfDay.has(key)) ordersOfDay.set(key, []);
        ordersOfDay.get(key)?.push(o);
    }
    const ordersIn = (/** @type {Set<string>} */ days) => [...days].flatMap(key => ordersOfDay.get(key) ?? []);
    const currentOrders = ordersIn(current);

    // ---- Khách hàng mới = khách có ĐƠN ĐẦU TIÊN rơi vào kỳ đó ----
    // (bảng Customer không lưu ngày đăng ký nên tính theo lần mua đầu tiên)
    /** @type {Map<number, string>} */
    const firstOrderDay = new Map();
    for (const o of orders) {
        const key = dayKey(o.orderDate);
        const old = firstOrderDay.get(o.customerId);
        if (!old || key < old) firstOrderDay.set(o.customerId, key);
    }
    const newCustomersIn = (/** @type {Set<string>} */ days) =>
        [...firstOrderDay.values()].filter(key => days.has(key)).length;

    // ---- Biểu đồ: ít nhất 7 cột (chọn "Hôm nay" vẫn xem được 7 ngày gần nhất) ----
    const chartDays = Math.max(n, 7);
    const revenueByDay = dayKeys(chartDays, 0).map(key => ({
        date: new Date(`${key}T00:00:00`),          // nửa đêm giờ máy -> hiện đúng ngày
        revenue: revenueOfDay.get(key) ?? 0,
        orders: ordersOfDay.get(key)?.length ?? 0
    }));

    // ---- Số đơn theo trạng thái (đơn đặt trong kỳ) ----
    /** @type {Record<string, number>} */
    const ordersByStatus = {};
    for (const o of currentOrders) ordersByStatus[o.status] = (ordersByStatus[o.status] ?? 0) + 1;

    // ---- Sản phẩm bán chạy trong kỳ: cộng QuantitySold của mọi cửa hàng theo biến thể ----
    /** @type {Map<number, { sold: number, revenue: number }>} */
    const soldByVariant = new Map();
    for (const s of itemStats) {
        if (!current.has(dayKey(s.statDate)) || !s.quantitySold) continue;
        const row = soldByVariant.get(s.variantId) ?? { sold: 0, revenue: 0 };
        row.sold += s.quantitySold;
        row.revenue += Number(s.totalRevenue);
        soldByVariant.set(s.variantId, row);
    }
    const top = [...soldByVariant.entries()]
        .sort((a, b) => b[1].sold - a[1].sold || b[1].revenue - a[1].revenue)
        .slice(0, TOP_PRODUCT_COUNT);
    await loadVariantNames(top.map(([variantId]) => variantId));
    const topProducts = top.map(([variantId, row]) => {
        const v = variantCache.get(variantId);
        return {
            productName: v.productName ?? `Biến thể #${variantId}`,
            variant: [v.color, v.size].filter(Boolean).join(" · "),
            sold: row.sold,
            revenue: row.revenue
        };
    });

    const pendingOrders = orders.filter(o => o.status === ORDER_STATUS.PENDING).length;

    return {
        revenue: { current: sumRevenue(current), previous: sumRevenue(previous) },
        orders: { current: currentOrders.length, previous: ordersIn(previous).length },
        pendingOrders,
        newCustomers: { current: newCustomersIn(current), previous: newCustomersIn(previous) },
        revenueByDay,
        ordersByStatus,
        // API đã trả đơn mới nhất trước, sắp lại cho chắc
        latestOrders: [...orders]
            .sort((a, b) => (toDate(b.orderDate)?.getTime() ?? 0) - (toDate(a.orderDate)?.getTime() ?? 0))
            .slice(0, LATEST_ORDER_COUNT),
        todos: {
            pendingOrders,
            pendingReturns: returns.filter(r => r.status === "Pending").length,
            // Chỉ tính vận đơn thất bại của đơn CÒN đang giao (đơn đã hủy thì xử lý xong rồi)
            failedShipments: shipments.filter(s => s.status === "Failed" && s.orderStatus === ORDER_STATUS.SHIPPING).length,
            lowStock: lowStockCount
        },
        topProducts
    };
}

/**
 * Trả về dữ liệu cho cả trang. Lần đầu gọi API, các lần sau chỉ tính lại.
 * @param {string} range - khoảng thời gian đang chọn: "today" | "7" | "30"
 */
async function loadDashboardData(range) {
    if (!raw) await fetchRawData();
    return buildDashboardData(range);
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
    // Kỳ trước bằng 0 (chưa có dữ liệu) thì không chia được -> ghi chú thay vì phần trăm
    if (!previous) return "Chưa có dữ liệu để so sánh";
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

    // Chưa có dữ liệu ngày nào -> tự tạo 7 ngày gần nhất với doanh thu 0, để vẫn vẽ được trục ngày
    if (!days.length) {
        days = [6, 5, 4, 3, 2, 1, 0].map(n => ({ date: dayOffset(n), revenue: 0, orders: 0 }));
    }
    const total = days.reduce((sum, d) => sum + d.revenue, 0);
    const isEmpty = total === 0;     // tổng = 0 -> vẽ khung trống + dòng "Chưa có dữ liệu"

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
    // [THÊM] Nhiều cột (30 ngày) thì chỉ ghi ngày cách quãng cho khỏi chồng chữ (luôn ghi cột hôm nay)
    const labelEvery = Math.max(1, Math.ceil(days.length / (narrow ? 7 : 10)));

    // Đường kẻ ngang + nhãn trục dọc
    let grid = "";
    for (let i = 0; i <= gridCount; i++) {
        const value = (max / gridCount) * i;
        const gy = y(value);
        grid += `<line class="chart-grid-line" x1="${pad.left}" x2="${W - pad.right}" y1="${gy}" y2="${gy}"/>`;
        // Khi trống thì không ghi số ở trục dọc (số 0,25 - 0,5... không có ý nghĩa)
        if (!isEmpty) {
            grid += `<text class="chart-axis-text" x="${pad.left - 8}" y="${gy + 4}" text-anchor="end">${shortMoney(value)}</text>`;
        }
    }

    // Các cột
    const bars = days.map((d, i) => {
        const cx = pad.left + slot * i + slot / 2;              // tâm của ô ngày
        const bx = cx - barW / 2;
        const by = y(d.revenue);
        const bh = pad.top + plotH - by;
        // Màn hình hẹp chỉ ghi ngày ("14") cho khỏi chồng chữ; rộng thì ghi "14-10"
        const label = (lastIndex - i) % labelEvery !== 0 ? ""
            : i === lastIndex ? (narrow ? "Nay" : "Hôm nay")
                : narrow ? String(new Date(d.date).getDate())
                    : new Date(d.date).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit" });

        return `
            <g class="chart-col" data-index="${i}">
                <rect class="chart-hit" x="${pad.left + slot * i}" y="${pad.top}" width="${slot}" height="${plotH}"/>
                ${isEmpty ? "" : `<path class="chart-bar ${i === lastIndex ? "is-today" : ""}" d="${barPath(bx, by, barW, bh)}"/>`}
                ${i === maxIndex && !isEmpty ? `<text class="chart-value" x="${cx}" y="${by - 8}" text-anchor="middle">${shortMoney(d.revenue)}</text>` : ""}
                <text class="chart-axis-text" x="${cx}" y="${H - 8}" text-anchor="middle">${label}</text>
            </g>`;
    }).join("");

    $("chartTotal").textContent = `Tổng: ${formatMoney(total)}`;
    // [THÊM] Tiêu đề biểu đồ đổi theo số ngày đang vẽ ("Doanh thu 7 ngày qua" / "Doanh thu 30 ngày qua")
    const title = box.closest(".card")?.querySelector(".card-title");
    if (title) title.textContent = `Doanh thu ${days.length} ngày qua`;

    // Dòng chữ giữa khung khi chưa có dữ liệu.
    // Đặt ở 3/8 chiều cao (giữa 2 đường kẻ ngang) để không bị đường kẻ cắt ngang chữ.
    const emptyText = isEmpty
        ? `<text class="chart-axis-text" x="${pad.left + plotW / 2}" y="${pad.top + plotH * 0.375 + 5}" text-anchor="middle" style="font-size:14px">Chưa có dữ liệu doanh thu</text>`
        : "";

    // role="img" + aria-label: mô tả biểu đồ cho phần mềm đọc màn hình.
    // Bảng ẩn (sr-only) bên dưới chứa số liệu đầy đủ cho ai không xem được hình.
    box.innerHTML = `
        <svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Biểu đồ cột doanh thu ${days.length} ngày qua, tổng ${formatMoney(total)}">
            ${grid}
            ${bars}
            ${emptyText}
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

    if (isEmpty) return;   // không có cột thì không cần hộp thông tin khi rê chuột

    box.querySelectorAll(".chart-col").forEach(col => {
        col.addEventListener("mouseenter", () => {
            const i = Number(/** @type {HTMLElement} */(/** @type {unknown} */ (col)).dataset.index);
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
        { count: todos.pendingOrders, title: "Đơn chờ xác nhận", note: "Xác nhận để chuẩn bị giao hàng", href: "sales/orders.html?status=Pending", color: "warning" },
        { count: todos.pendingReturns, title: "Yêu cầu đổi / trả", note: "Đang chờ duyệt", href: "sales/returns.html?status=Pending", color: "info" },
        { count: todos.failedShipments, title: "Vận đơn giao thất bại", note: "Giao lại hoặc hủy đơn", href: "sales/shipments.html?status=Failed", color: "danger" },
        { count: todos.lowStock, title: "Sản phẩm sắp hết hàng", note: "Cần nhập thêm kho", href: "stores/stock.html", color: "secondary" }
    ];

    // [SỬA] count = null nghĩa là đang tải (tồn kho) -> hiện "…"
    $("todoList").innerHTML = items.map(item => `
        <a class="todo-item" href="${item.href}">
            <span class="todo-count badge-${item.count ? item.color : "secondary"}">${item.count == null ? "…" : formatNumber(item.count)}</span>
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
            {
                key: "productName", title: "Sản phẩm",
                render: row => `<div class="fw-bold">${escapeHtml(row.productName)}</div><div class="text-muted text-sm">${escapeHtml(row.variant)}</div>`
            },
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

/** [THÊM] Ô "Việc cần xử lý" lần vẽ gần nhất (để điền số tồn kho khi tải xong). @type {any} */
let lastTodos = null;

/** Tải dữ liệu rồi vẽ lại toàn bộ trang. */
async function render() {
    try {
        const data = await loadDashboardData(rangeSelect.value);
        lastRevenueByDay = data.revenueByDay;    // nhớ lại để vẽ lại biểu đồ khi đổi kích thước màn hình
        drawStats(data);
        drawRevenueChart(data.revenueByDay);
        drawStatusList(data.ordersByStatus);
        drawLatestOrders(data.latestOrders);
        lastTodos = data.todos;
        drawTodos(data.todos);
        drawTopProducts(data.topProducts);
    } catch (err) {
        toast.error(`Không tải được dữ liệu tổng quan: ${err.message}`);
    }
}

if (await initAdminLayout({ title: "Tổng quan" })) {
    drawHeader();

    // [SỬA] Đổi khoảng thời gian -> tính lại số liệu (không gọi lại API)
    rangeSelect.addEventListener("change", () => render());

    await render();

    // [THÊM] Tải tồn kho chạy nền; xong thì vẽ lại ô "Việc cần xử lý" với số thật
    loadLowStock(() => {
        if (lastTodos) drawTodos({ ...lastTodos, lowStock: lowStockCount });
    });

    // Kéo to / thu nhỏ cửa sổ (hoặc xoay điện thoại) -> vẽ lại biểu đồ cho vừa khung.
    // debounce: chờ ngừng kéo 200ms mới vẽ, khỏi vẽ lại hàng trăm lần.
    window.addEventListener("resize", debounce(() => {
        if (lastRevenueByDay.length) drawRevenueChart(lastRevenueByDay);
    }, 200));
}