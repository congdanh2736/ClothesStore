import {get, post, put, del} from "../core/http.js";

// tạo đường dẫn gốc(root) để code nhanh
const PRODUCT = "/Product";

export const productApi = {
    //lấy tất cả danh sách tại đường dẫn /{root}
    getAll: () => get(`${PRODUCT}`),
    //lấy đối tượng theo ID tại đường dẫn /{root}/{id}
    getById: id => get(`${PRODUCT}/${id}`),
    // tạo đối tượng tại đường dẫn /{root}
    create: data => post(`${PRODUCT}`,data),
    //chỉnh sửa đối tượng tại đường dẫn /{root}/{id}
    update: (id, data) => put(`${PRODUCT}/${id}`,data),
    //xóa đối tượng tại đường dẫn /{root}/{id}
    remove: id => del(`${PRODUCT}/${id}`),
    // chi tiết sản phẩm (kèm biến thể, ảnh, bộ sưu tập)
    getDetail: id => get(`${PRODUCT}/${id}/detail`),
    // gắn / gỡ bộ sưu tập
    addToCollection: (id, collectionId) => post(`${PRODUCT}/${id}/collections/${collectionId}`),
    removeFromCollection: (id, collectionId) => del(`${PRODUCT}/${id}/collections/${collectionId}`)
};

// tạo đường dẫn gốc(root) để code nhanh
const PRODUCT_VARIANT = '/ProductVariant';

export const productVariantApi = {
    //lấy tất cả danh sách tại đường dẫn /{root}
    getAll: () => get(`${PRODUCT_VARIANT}`),
    //lấy đối tượng theo ID tại đường dẫn /{root}/{id}
    getById: id => get(`${PRODUCT_VARIANT}/${id}`),
    // lấy danh sách biến thể của 1 sản phẩm
    getByProduct: productId => get(`${PRODUCT_VARIANT}/product/${productId}`),
    // tạo đối tượng tại đường dẫn /{root}
    create: data => post(`${PRODUCT_VARIANT}`,data),
    //chỉnh sửa đối tượng tại đường dẫn /{root}/{id}
    update: (id, data) => put(`${PRODUCT_VARIANT}/${id}`,data),
    //xóa đối tượng tại đường dẫn /{root}/{id}
    remove: id => del(`${PRODUCT_VARIANT}/${id}`)
};

const PRODUCT_IMAGE = '/ProductImage';

export const productImageApi = {
    getAll: () => get(`${PRODUCT_IMAGE}`),
    getById: id => get(`${PRODUCT_IMAGE}/${id}`),
    create: data => post(`${PRODUCT_IMAGE}`,data),
    update: (id, data) => put(`${PRODUCT_IMAGE}/${id}`,data),
    remove: id => del(`${PRODUCT_IMAGE}/${id}`)
};
