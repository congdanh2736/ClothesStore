import { initAdminLayout } from "../layout.js";
import { storeApi, storeStockApi } from "../../../assets/js/api/storeApi.js";
import { escapeHtml, formatNumber } from "../../../assets/js/core/utils.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { formModal } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

const storeFilter = document.getElementById("storeFilter");
const table = createDataTable({
    container: document.getElementById("tableContainer"),
    paginationContainer: document.getElementById("paginationContainer"),
    searchFields: ["productName", "color", "size", "variantId"],
    columns: [
        { key: "variantId", title: "Mã biến thể", width: "110px" },
        { key: "productName", title: "Sản phẩm", render: row => escapeHtml(row.productName || `Biến thể #${row.variantId}`) },
        { key: "color", title: "Màu" },
        { key: "size", title: "Kích cỡ" },
        { key: "quantity", title: "Tồn kho", align: "right", render: row => formatNumber(row.quantity) }
    ]
});

async function loadStock() {
    if (!storeFilter.value) {
        table.setRows([]);
        return;
    }
    table.setLoading();
    try { table.setRows(await storeStockApi.getByStore(Number(storeFilter.value))); }
    catch (err) { table.setError(err.message); }
}

function openAdjustment() {
    if (!storeFilter.value) {
        toast.warning("Vui lòng chọn cửa hàng trước.");
        return;
    }
    formModal({
        title: "Nhập, xuất hoặc điều chỉnh tồn kho",
        description: "Nhập số dương để nhập hàng, số âm để xuất hàng. Ví dụ: 10 nhập 10 sản phẩm, -2 xuất 2 sản phẩm.",
        fields: [
            { name: "variantId", label: "Mã biến thể sản phẩm", type: "number", min: 1, step: 1, required: true },
            { name: "quantityChange", label: "Số lượng thay đổi", type: "number", step: 1, required: true, hint: "Không nhập 0; không thể xuất nhiều hơn số đang có." }
        ],
        submitText: "Lưu điều chỉnh",
        onSubmit: async data => {
            if (!data.quantityChange) throw new Error("Số lượng thay đổi phải khác 0.");
            await storeStockApi.adjust({ storeId: Number(storeFilter.value), variantId: data.variantId, quantityChange: data.quantityChange });
            toast.success("Đã cập nhật tồn kho.");
            await loadStock();
        }
    });
}

if (await initAdminLayout({ title: "Tồn kho cửa hàng" })) {
    try {
        const stores = await storeApi.getAll();
        storeFilter.innerHTML = '<option value="">Chọn cửa hàng</option>' + stores.map(store => `<option value="${Number(store.id)}">${escapeHtml(store.name || `Cửa hàng #${store.id}`)}</option>`).join("");
    } catch (err) { toast.error(err.message); }
    storeFilter.addEventListener("change", loadStock);
    document.getElementById("btnReload").addEventListener("click", loadStock);
    document.getElementById("btnAdjust").addEventListener("click", openAdjustment);
}
