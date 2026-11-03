// hàm dựng khung giao diện từ file layout.js
import { initAdminLayout } from "../layout.js";
// hàm vẽ bảng từ file table.js
import { createDataTable } from "../../../assets/js/components/table.js";
// hàm escapeHtml chống lỗi hiển thị ký tự đặc biệt từ file utils.js
import { escapeHtml } from "../../../assets/js/core/utils.js";
// lấy Api của Category từ file catalogApi.js
import { categoryApi } from "../../../assets/js/api/catalogApi.js";

// Thêm 2 dòng này cùng với các import khác ở đầu file
import { formModal, confirmDialog} from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";




// tìm các tag có ID tương ứng trong file html được nạp vào
const tableContainer = document.getElementById("tableContainer");
const paginationContainer = document.getElementById("paginationContainer");



const table = createDataTable({
    container: tableContainer, //vẽ bảng vào bên trong cái thẻ này
    paginationContainer: paginationContainer, //thanh chuyển trang
    searchFields: ["name","id"], //cấu hình tìm kiếm theo thuộc tích được chỉ định
    sortBy: (a, b) => a.id - b.id, // sắp xếp theo thứ tự Id
    emptyText: "chưa có danh mục nào",
    columns: [
        {key: "id", title: "mã"}, //key: là cột dữ liệu được lấy từ database(Dto), title: phần hiệu liên cho người dùng thấy
        {key: "name", title: "tên danh mục", render: row => `${escapeHtml(row.name ?? "")}`},
        {key: "parentCategoryName", title: "Danh mục cha", render: row => row.parentCategoryName ? `${escapeHtml(row.parentCategoryName)}` : `Không có`},
        {key: "childrenCategoriesCount", title: "tổng danh mục con", render: row => row.childrenCategoriesCount ?? 0},
        {key: "productCount", title: "tổng sản phẩm", render: row => row.productCount ?? 0}
    ],

    //các hành động có thể thực hiện
    actions: [
    {label: "Sửa", onClick: row => openForm(row)},
    {label: "Xóa", danger: true, onClick: row => removeRow(row)
    }
]
});



async function loadData({ keepPage = false } = {}){
    // hiệu ứng load data
    table.setLoading();
    try {
        //lấy dữ liệu từ Api
        const data = await categoryApi.getAll();
        // hiện vào trong bảng
        table.setRows(data, { keepPage });
    } catch (error) {
        // nếu lỗi thì hiện hiện thông báo đồng thời ghi rỏ lỗi gì
        table.setError("Lỗi khi tải dữ liệu: " + error.message);
    }
}

/* ---------------------------------------------------------------------------
 * Hàm định nghĩa các ô nhập trong Form
 * (Viết thành hàm để khi SỬA thì loại bỏ chính danh mục đó khỏi ô chọn cha)
 * ------------------------------------------------------------------------- */
function getFields(editingRow = null) {
    // Lấy toàn bộ danh mục đang có trên bảng để làm danh sách lựa chọn cha
    const parentOptions = table.getRows()
        .filter(c => c.id !== editingRow?.id) // Không cho chọn chính nó làm cha của nó!
        .map(c => ({ value: c.id, label: c.name }));

    return [
        {name: "name", label: "Tên danh mục", required: true, placeholder: "Ví dụ: Áo sơ mi" },
        {name: "parentCategoryId", label: "Thuộc danh mục cha", type: "select", valueType: "number", emptyOption: "-- Không có", options: parentOptions }
    ];
}

/* ---------------------------------------------------------------------------
 * Hàm mở Popup Form: row = null là THÊM MỚI; có row là SỬA
 * ------------------------------------------------------------------------- */
function openForm(row = null) {
    const isEdit = row !== null;

    formModal({
        title: isEdit ? `Sửa danh mục #${row.id}` : "Thêm danh mục mới",
        fields: getFields(row),
        values: row ?? {}, // Điền sẵn thông tin cũ nếu đang Sửa
        submitText: isEdit ? "Lưu thay đổi" : "Thêm danh mục",
        onSubmit: async data => {
            if (isEdit) {
                // Gọi API PUT /api/Category/{id} mà bạn viết trong catalogApi.js
                await categoryApi.update(row.id, data);
                toast.success("Đã cập nhật danh mục thành công!");
            } else {
                // Gọi API POST /api/Category
                await categoryApi.create(data);
                toast.success("Đã thêm danh mục mới thành công!");
            }
            // Tải lại bảng để thấy dữ liệu mới nhất
            await loadData({ keepPage: isEdit });
        }
    });
}

async function removeRow(row) {
    const ok = await confirmDialog({
        title: "Xóa danh mục",
        message: `Bạn có chắc muốn xóa "${row.name}"?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await categoryApi.remove(row.id);
        toast.success("Đã xóa.");
        await loadData({ keepPage: true });
    } catch (err) 
    {
        // Ví dụ: danh mục còn sản phẩm -> server báo lỗi 409 hoặc 400
        toast.error(err.message);
    }
    }



// khi vào trang thì thực hiện loadData luôn
if (await initAdminLayout({ title: "Danh mục sản phẩm" })) {
    await loadData();

    // Nút Thêm
    const btnAdd = document.getElementById("btnAdd");
    btnAdd?.addEventListener("click", () => openForm());

    // Ô Tìm kiếm (bổ sung đoạn này)
    const searchInput = document.getElementById("searchInput");
    searchInput?.addEventListener("input", () => table.search(searchInput.value));
}