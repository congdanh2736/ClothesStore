import { initAdminLayout } from "../layout.js";
import { storeApi } from "../../../assets/js/api/storeApi.js";
import { isAdmin } from "../../../assets/js/core/auth.js";
import { escapeHtml, debounce } from "../../../assets/js/core/utils.js";
import { createDataTable } from "../../../assets/js/components/table.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

const table = createDataTable({
    container: document.getElementById("tableContainer"),
    paginationContainer: document.getElementById("paginationContainer"),
    searchFields: ["id", "name", "location"],
    columns: [
        { key: "id", title: "Mã", width: "80px" },
        { key: "name", title: "Tên cửa hàng", render: row => `<strong>${escapeHtml(row.name || "—")}</strong>` },
        { key: "location", title: "Địa chỉ" }
    ],
    actions: [
        { label: "Sửa", visible: () => isAdmin(), onClick: row => openForm(row) },
        { label: "Xóa", danger: true, visible: () => isAdmin(), onClick: row => removeStore(row) }
    ]
});

async function loadStores() {
    table.setLoading();
    try { table.setRows(await storeApi.getAll()); }
    catch (err) { table.setError(err.message); }
}

function openForm(store = null) {
    const editing = store !== null;
    formModal({
        title: editing ? `Sửa cửa hàng ${store.name ?? ""}` : "Thêm cửa hàng",
        values: store ?? {},
        fields: [
            { name: "name", label: "Tên cửa hàng", required: true },
            { name: "location", label: "Địa chỉ", type: "textarea" }
        ],
        onSubmit: async data => {
            if (editing) await storeApi.update(store.id, data);
            else await storeApi.create(data);
            toast.success(editing ? "Đã cập nhật cửa hàng." : "Đã thêm cửa hàng.");
            await loadStores();
        }
    });
}

async function removeStore(store) {
    if (!(await confirmDialog({ title: "Xóa cửa hàng", message: `Bạn có chắc muốn xóa "${store.name}"?`, confirmText: "Xóa", danger: true }))) return;
    try { await storeApi.remove(store.id); toast.success("Đã xóa cửa hàng."); await loadStores(); }
    catch (err) { toast.error(err.message); }
}

if (await initAdminLayout({ title: "Cửa hàng" })) {
    const addButton = document.getElementById("btnAdd");
    if (isAdmin()) addButton.addEventListener("click", () => openForm());
    else addButton.hidden = true;
    document.getElementById("btnReload").addEventListener("click", loadStores);
    const search = debounce(value => table.search(value), 250);
    document.getElementById("searchInput").addEventListener("input", event => search(event.target.value));
    await loadStores();
}
