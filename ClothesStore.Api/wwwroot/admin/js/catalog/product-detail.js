/*
 * ============================================================================
 *  product-detail.js - Chi tiết sản phẩm (Biến thể, Ảnh, Bộ sưu tập)
 * ============================================================================
 */

import { initAdminLayout, setPageTitle } from "../layout.js";
import { get, post, del } from "../../../assets/js/core/http.js";
import { productApi, productVariantApi, productImageApi } from "../../../assets/js/api/productApi.js";
import { categoryApi, collectionTechApi } from "../../../assets/js/api/catalogApi.js";
import { escapeHtml, formatMoney, formatNumber, getIdFromUrl } from "../../../assets/js/core/utils.js";
import { renderTable } from "../../../assets/js/components/table.js";
import { formModal, confirmDialog } from "../../../assets/js/components/modal.js";
import { toast } from "../../../assets/js/components/toast.js";

// Đảm bảo an toàn nếu trình duyệt đang lưu cache file productApi.js cũ
if (!productApi.getDetail) {
    productApi.getDetail = id => get(`/Product/${id}/detail`);
}
if (!productApi.addToCollection) {
    productApi.addToCollection = (id, colId) => post(`/Product/${id}/collections/${colId}`);
}
if (!productApi.removeFromCollection) {
    productApi.removeFromCollection = (id, colId) => del(`/Product/${id}/collections/${colId}`);
}
if (!productVariantApi.getByProduct) {
    productVariantApi.getByProduct = productId => get(`/ProductVariant/product/${productId}`);
}

// Tiện ích lấy thẻ theo id
const $ = id => document.getElementById(id);

const pageError = $("pageError");
const pageContent = $("pageContent");
const productId = getIdFromUrl() ?? 0;

// Các biến lưu dữ liệu hiện tại của sản phẩm
let product = null;
let variants = [];
let attachedCollections = [];
let allCollections = [];
let images = [];
let categories = [];

/* ---------------------------------------------------------------------------
 * 1. Tải toàn bộ thông tin sản phẩm và cập nhật thống kê
 * ------------------------------------------------------------------------- */
async function loadProductDetail() {
    try {
        const detail = await productApi.getDetail(productId);
        product = detail;

        // Điền thông tin lên Header
        $("productName").textContent = product.name || `Sản phẩm #${product.id}`;
        $("productSubtitle").textContent = `Mã SP: #${product.id} · Danh mục: ${product.categoryName ?? "Chưa phân loại"}`;
        setPageTitle(`Chi tiết: ${product.name}`);

        // Cập nhật 4 ô số liệu
        $("statCategory").textContent = product.categoryName ?? "—";
        $("statVariants").textContent = formatNumber(product.variants?.length ?? 0);
        $("statImages").textContent = formatNumber(product.imageUrls?.length ?? 0);
        $("statCollections").textContent = formatNumber(product.collections?.length ?? 0);

        // Lưu danh sách cho các bảng
        variants = product.variants ?? [];
        attachedCollections = product.collections ?? [];

        // Vẽ các bảng
        drawVariants();
        drawCollections();
        await loadImages();
    } catch (err) {
        throw new Error("Không thể tải thông tin sản phẩm: " + err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 2. BẢNG BIẾN THỂ (MÀU, SIZE, GIÁ)
 * ------------------------------------------------------------------------- */
function drawVariants() {
    renderTable($("variantTable"), {
        rows: variants,
        emptyText: "Chưa có biến thể nào. Bấm '+ Thêm biến thể' để tạo màu sắc/kích cỡ và giá bán.",
        columns: [
            { key: "id", title: "Mã SKU", width: "80px" },
            {
                key: "color",
                title: "Màu sắc",
                width: "140px",
                render: row => `<span class="badge badge-secondary fw-bold">${escapeHtml(row.color ?? "Mặc định")}</span>`
            },
            {
                key: "size",
                title: "Kích cỡ (Size)",
                width: "120px",
                render: row => `<span class="badge badge-primary fw-bold">${escapeHtml(row.size ?? "Free")}</span>`
            },
            {
                key: "price",
                title: "Giá bán lẻ",
                align: "right",
                render: row => `<span class="text-primary fw-bold">${formatMoney(row.price)}</span>`
            }
        ],
        actions: [
            { label: "Sửa", onClick: row => openVariantForm(row) },
            { label: "Xóa", danger: true, onClick: row => removeVariant(row) }
        ]
    });
}

function openVariantForm(row = null) {
    const isEdit = row !== null;

    formModal({
        title: isEdit ? `Sửa biến thể #${row.id}` : "Thêm biến thể mới",
        values: row ?? { price: 150000 },
        fields: [
            {
                name: "color",
                label: "Màu sắc",
                required: true,
                placeholder: "Ví dụ: Đen, Trắng, Xanh Navy, Be..."
            },
            {
                name: "size",
                label: "Kích cỡ (Size)",
                required: true,
                placeholder: "Ví dụ: S, M, L, XL, 30, 31, Freesize..."
            },
            {
                name: "price",
                label: "Giá bán (VNĐ)",
                type: "number",
                valueType: "number",
                required: true,
                min: 0,
                step: 1000
            }
        ],
        submitText: isEdit ? "Lưu thay đổi" : "Tạo biến thể",
        onSubmit: async data => {
            const payload = {
                productId: productId,
                color: data.color.trim(),
                size: data.size.trim(),
                price: Number(data.price)
            };

            if (isEdit) {
                await productVariantApi.update(row.id, payload);
                toast.success("Cập nhật biến thể thành công!");
            } else {
                await productVariantApi.create(payload);
                toast.success("Thêm biến thể mới thành công!");
            }
            await loadProductDetail();
        }
    });
}

async function removeVariant(row) {
    const ok = await confirmDialog({
        title: "Xóa biến thể",
        message: `Bạn có chắc muốn xóa biến thể "${row.color} - Size ${row.size}"?\nThao tác này không thể hoàn tác.`,
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await productVariantApi.remove(row.id);
        toast.success("Đã xóa biến thể thành công.");
        await loadProductDetail();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 3. BẢNG BỘ SƯU TẬP & CÔNG NGHỆ
 * ------------------------------------------------------------------------- */
function drawCollections() {
    renderTable($("collectionTable"), {
        rows: attachedCollections,
        emptyText: "Sản phẩm chưa thuộc bộ sưu tập hoặc công nghệ nào.",
        columns: [
            { key: "id", title: "Mã", width: "70px" },
            {
                key: "name",
                title: "Tên bộ sưu tập / Công nghệ",
                render: row => `<strong>${escapeHtml(row.name ?? "")}</strong>`
            },
            {
                key: "type",
                title: "Phân loại",
                width: "160px",
                render: row => (row.type === "Technology" || row.type === "Công nghệ")
                    ? `<span class="badge badge-info">Công nghệ</span>`
                    : `<span class="badge badge-primary">Bộ sưu tập</span>`
            }
        ],
        actions: [
            {
                label: "Gỡ bỏ",
                danger: true,
                onClick: row => detachCollection(row)
            }
        ]
    });
}

function openAttachCollectionForm() {
    // Chỉ lấy các bộ sưu tập chưa được gắn vào sản phẩm này
    const attachedIds = new Set(attachedCollections.map(c => c.id));
    const available = allCollections.filter(c => !attachedIds.has(c.id));

    if (available.length === 0) {
        toast.warning("Sản phẩm đã được gắn vào tất cả các bộ sưu tập hiện có.");
        return;
    }

    const options = available.map(c => ({
        value: c.id,
        label: `[${c.type ?? "BST"}] ${c.name}`
    }));

    formModal({
        title: "Gắn vào bộ sưu tập / công nghệ",
        fields: [
            {
                name: "collectionId",
                label: "Chọn bộ sưu tập / công nghệ",
                type: "select",
                valueType: "number",
                required: true,
                emptyOption: "-- Chọn mục cần gắn --",
                options
            }
        ],
        submitText: "Gắn vào sản phẩm",
        onSubmit: async data => {
            const colId = Number(data.collectionId);
            await productApi.addToCollection(productId, colId);
            toast.success("Đã gắn bộ sưu tập vào sản phẩm!");
            await loadProductDetail();
        }
    });
}

async function detachCollection(row) {
    const ok = await confirmDialog({
        title: "Gỡ bộ sưu tập",
        message: `Bạn có chắc muốn gỡ sản phẩm khỏi "${row.name}" không?`,
        confirmText: "Gỡ bỏ",
        danger: true
    });
    if (!ok) return;

    try {
        await productApi.removeFromCollection(productId, row.id);
        toast.success("Đã gỡ thành công.");
        await loadProductDetail();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 4. THƯ VIỆN HÌNH ẢNH SẢN PHẨM
 * ------------------------------------------------------------------------- */
async function loadImages() {
    try {
        const allImages = await productImageApi.getAll();
        images = (Array.isArray(allImages) ? allImages : []).filter(img => img.productId === productId);
        $("statImages").textContent = formatNumber(images.length);

        renderTable($("imageTable"), {
            rows: images,
            emptyText: "Chưa có hình ảnh nào cho sản phẩm này.",
            columns: [
                { key: "imageId", title: "Mã", width: "70px" },
                {
                    key: "imageUrl",
                    title: "Hình ảnh xem trước",
                    width: "120px",
                    render: row => row.imageUrl
                        ? `<img src="${escapeHtml(row.imageUrl)}" alt="Ảnh SP" style="width: 56px; height: 56px; object-fit: cover; border-radius: 6px; border: 1px solid var(--color-border);">`
                        : `<span class="text-muted text-sm">Chưa có</span>`
                },
                {
                    key: "imageUrl",
                    title: "Đường dẫn URL ảnh",
                    render: row => `<a href="${escapeHtml(row.imageUrl ?? "")}" target="_blank" class="text-sm text-primary" style="word-break: break-all;">${escapeHtml(row.imageUrl ?? "")}</a>`
                }
            ],
            actions: [
                {
                    label: "Xóa ảnh",
                    danger: true,
                    onClick: row => removeImage(row)
                }
            ]
        });
    } catch (err) {
        console.error("Lỗi tải hình ảnh:", err);
    }
}

function openAddImageForm() {
    formModal({
        title: "Thêm hình ảnh cho sản phẩm",
        fields: [
            {
                name: "imageUrl",
                label: "Đường dẫn URL hình ảnh",
                required: true,
                placeholder: "Ví dụ: https://images.unsplash.com/... hoặc /assets/images/sp1.jpg"
            }
        ],
        submitText: "Thêm ảnh",
        onSubmit: async data => {
            await productImageApi.create({
                productId: productId,
                imageUrl: data.imageUrl.trim()
            });
            toast.success("Thêm hình ảnh thành công!");
            await loadProductDetail();
        }
    });
}

async function removeImage(row) {
    const ok = await confirmDialog({
        title: "Xóa ảnh",
        message: "Bạn có chắc chắn muốn xóa ảnh này không?",
        confirmText: "Xóa",
        danger: true
    });
    if (!ok) return;

    try {
        await productImageApi.remove(row.imageId);
        toast.success("Đã xóa ảnh thành công.");
        await loadProductDetail();
    } catch (err) {
        toast.error(err.message);
    }
}

/* ---------------------------------------------------------------------------
 * 5. SỬA THÔNG TIN SẢN PHẨM GỐC
 * ------------------------------------------------------------------------- */
function openEditProductForm() {
    const categoryOptions = categories.map(c => ({
        value: c.id,
        label: c.name
    }));

    formModal({
        title: `Sửa sản phẩm #${product.id}`,
        values: {
            name: product.name,
            categoryId: product.categoryId
        },
        fields: [
            {
                name: "name",
                label: "Tên sản phẩm",
                required: true,
                placeholder: "Tên sản phẩm"
            },
            {
                name: "categoryId",
                label: "Danh mục",
                type: "select",
                valueType: "number",
                required: true,
                options: categoryOptions
            }
        ],
        submitText: "Lưu thay đổi",
        onSubmit: async data => {
            await productApi.update(product.id, data);
            toast.success("Đã cập nhật thông tin sản phẩm!");
            await loadProductDetail();
        }
    });
}

/* ---------------------------------------------------------------------------
 * 6. KHỞI ĐỘNG TRANG
 * ------------------------------------------------------------------------- */
if (await initAdminLayout({ title: "Chi tiết sản phẩm" })) {
    if (!productId) {
        pageError.style.display = "block";
        pageError.textContent = "Không tìm thấy mã sản phẩm hợp lệ trên đường dẫn (ví dụ: product-detail.html?id=1).";
    } else {
        try {
            // Tải danh mục và danh sách bộ sưu tập để dùng cho các form popup
            const [cats, cols] = await Promise.all([
                categoryApi.getAll(),
                collectionTechApi.getAll()
            ]);
            categories = Array.isArray(cats) ? cats : [];
            allCollections = Array.isArray(cols) ? cols : [];

            // Tải chi tiết sản phẩm
            await loadProductDetail();
            pageContent.classList.remove("hidden");

            // Gắn sự kiện các nút
            $("btnEditProduct")?.addEventListener("click", () => openEditProductForm());
            $("btnAddVariant")?.addEventListener("click", () => openVariantForm());
            $("btnAttachCollection")?.addEventListener("click", () => openAttachCollectionForm());
            $("btnAddImage")?.addEventListener("click", () => openAddImageForm());
        } catch (err) {
            pageError.style.display = "block";
            pageError.textContent = err.message;
        }
    }
}
