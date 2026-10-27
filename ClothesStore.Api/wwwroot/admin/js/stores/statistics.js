import { initAdminLayout } from "../layout.js";
import { storeApi } from "../../../assets/js/api/storeApi.js";
import { statisticApi } from "../../../assets/js/api/statisticApi.js";
import { escapeHtml, formatMoney, formatNumber, formatDate } from "../../../assets/js/core/utils.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { toast } from "../../../assets/js/components/toast.js";

const storeFilter = document.getElementById("storeFilter");
const dateFrom = document.getElementById("dateFrom");
const dateTo = document.getElementById("dateTo");
const dailyTable = createDataTable({
    container: document.getElementById("dailyTable"), paginationContainer: document.getElementById("dailyPagination"),
    columns: [
        { key: "storeId", title: "Mã cửa hàng" }, { key: "statDate", title: "Ngày", render: row => formatDate(row.statDate) },
        { key: "totalOrders", title: "Đơn hàng", align: "right", render: row => formatNumber(row.totalOrders) },
        { key: "completedOrders", title: "Hoàn tất", align: "right", render: row => formatNumber(row.completedOrders) },
        { key: "canceledOrders", title: "Đã hủy", align: "right", render: row => formatNumber(row.canceledOrders) },
        { key: "totalItemsSold", title: "Sản phẩm bán", align: "right", render: row => formatNumber(row.totalItemsSold) },
        { key: "totalRevenue", title: "Doanh thu", align: "right", render: row => formatMoney(row.totalRevenue) }
    ]
});
const itemTable = createDataTable({
    container: document.getElementById("itemTable"), paginationContainer: document.getElementById("itemPagination"),
    columns: [
        { key: "storeId", title: "Mã cửa hàng" }, { key: "variantId", title: "Mã biến thể" },
        { key: "statDate", title: "Ngày", render: row => formatDate(row.statDate) },
        { key: "quantitySold", title: "Đã bán", align: "right", render: row => formatNumber(row.quantitySold) },
        { key: "returnQuantity", title: "Đã trả", align: "right", render: row => formatNumber(row.returnQuantity) },
        { key: "totalRevenue", title: "Doanh thu", align: "right", render: row => formatMoney(row.totalRevenue) }
    ]
});

let allDaily = [];
let allItems = [];

function filterRows(rows) {
    const selectedStore = storeFilter.value;
    const from = dateFrom.value;
    const to = dateTo.value;
    return rows.filter(row => {
        const date = String(row.statDate ?? "").slice(0, 10);
        return (!selectedStore || String(row.storeId) === selectedStore) && (!from || date >= from) && (!to || date <= to);
    });
}

function renderFiltered() {
    dailyTable.setRows(filterRows(allDaily));
    itemTable.setRows(filterRows(allItems));
}

async function loadStatistics() {
    dailyTable.setLoading();
    itemTable.setLoading();
    try {
        [allDaily, allItems] = await Promise.all([statisticApi.getDailyStats(), statisticApi.getItemStats()]);
        renderFiltered();
    } catch (err) {
        dailyTable.setError(err.message);
        itemTable.setError(err.message);
        toast.error(err.message);
    }
}

if (await initAdminLayout({ title: "Thống kê cửa hàng" })) {
    try {
        const stores = await storeApi.getAll();
        storeFilter.innerHTML = '<option value="">Tất cả cửa hàng</option>' + stores.map(store => `<option value="${Number(store.id)}">${escapeHtml(store.name || `Cửa hàng #${store.id}`)}</option>`).join("");
    } catch (err) { toast.error(err.message); }
    document.getElementById("btnFilter").addEventListener("click", renderFiltered);
    document.getElementById("btnReload").addEventListener("click", loadStatistics);
    await loadStatistics();
}
