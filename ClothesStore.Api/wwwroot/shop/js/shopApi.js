/*
 * ============================================================================
 *  shopApi.js  -  MỌI LỆNH LẤY / GỬI DỮ LIỆU CỦA TRANG MUA SẮM (shop)
 * ============================================================================
 *
 *  Các trang shop KHÔNG gọi fetch trực tiếp mà chỉ gọi hàm trong file này, ví dụ:
 *      import { shopApi } from "./shopApi.js";
 *      const products = await shopApi.products.getAll();
 *
 *  CÓ 2 CHẾ ĐỘ (đổi ở biến USE_MOCK ngay bên dưới):
 *    - USE_MOCK = true : dùng dữ liệu mẫu trong mock-data.js (không cần chạy backend).
 *                        Thay đổi (giỏ hàng, đặt đơn, yêu thích...) được lưu tạm trong
 *                        trình duyệt (localStorage) để bấm thử như thật.
 *                        Đăng nhập: khachhang015@clothesstore.vn / Khach@123
 *    - USE_MOCK = false: gọi API thật của ClothesStore.Api. Mỗi hàm ghi rõ endpoint.
 *
 *  Dữ liệu trả về ở 2 chế độ CÓ CÙNG HÌNH DẠNG (giống các DTO của backend),
 *  nên đổi chế độ thì các trang không phải sửa gì.
 * ============================================================================
 */

import { get, post, put, patch, del } from "../../assets/js/core/http.js";
import { getCustomerId, saveLogin } from "../../assets/js/core/auth.js";

/** true = dữ liệu mẫu, false = API thật */
export const USE_MOCK = true;

/** Tài khoản mẫu (chỉ dùng ở chế độ mẫu) */
export const DEMO_ACCOUNT = { email: "khachhang015@clothesstore.vn", password: "Khach@123" };

/* ============================================================================
 *  PHẦN 1: "BACKEND GIẢ" CHO CHẾ ĐỘ MẪU
 *  Làm đúng các luật của Services/ trong backend (giảm giá, hủy đơn, trả hàng...)
 * ========================================================================== */

const STATE_KEY = "aura-mock-state-v1";

/** Dữ liệu mẫu được nạp khi cần lần đầu (file khá lớn nên không nạp sẵn). @type {any} */
let mock = null;
/** Phần dữ liệu của khách có thể thay đổi (giỏ, đơn, yêu thích...). @type {any} */
let me = null;

/** Tên các trường ngày giờ cần dời cho khớp hôm nay */
const DATE_FIELDS = ["orderDate", "requestDate", "createdAt", "txnDate", "shippedDate", "deliveredDate", "paidAt", "startDate", "endDate"];

/**
 * Dời mọi ngày giờ trong dữ liệu mẫu đi "days" ngày (dữ liệu tạo quanh ngày anchorDate,
 * dời để ngày cuối là hôm nay - giống SampleDataSeeder ở backend).
 * @param {any} value @param {number} days
 */
function shiftDates(value, days) {
    if (Array.isArray(value)) { value.forEach(v => shiftDates(v, days)); return; }
    if (!value || typeof value !== "object") return;
    for (const key of Object.keys(value)) {
        const v = value[key];
        if (DATE_FIELDS.includes(key) && typeof v === "string") {
            value[key] = new Date(new Date(v).getTime() + days * 86400000).toISOString().slice(0, 19) + "Z";
        } else if (typeof v === "object") {
            shiftDates(v, days);
        }
    }
}

/** Nạp dữ liệu mẫu (1 lần) + phần đã lưu trong trình duyệt */
async function loadMock() {
    if (mock) return;
    const module = await import("./mock-data.js");
    mock = structuredClone(module.MOCK);
    const anchor = new Date(`${mock.anchorDate}T00:00:00`);
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const days = Math.round((today.getTime() - anchor.getTime()) / 86400000);
    shiftDates(mock, days);

    try {
        const saved = JSON.parse(localStorage.getItem(STATE_KEY) ?? "null");
        me = saved && saved.anchorShift === days ? saved : { ...structuredClone(mock.me), anchorShift: days, nextId: 900000 };
    } catch {
        me = { ...structuredClone(mock.me), anchorShift: days, nextId: 900000 };
    }
}

/** Lưu phần dữ liệu khách vào trình duyệt */
function saveMe() {
    try { localStorage.setItem(STATE_KEY, JSON.stringify(me)); } catch { /* trình duyệt chặn lưu: bỏ qua */ }
}

/** Xóa mọi thay đổi ở chế độ mẫu, quay về dữ liệu ban đầu */
export function resetMockData() {
    try { localStorage.removeItem(STATE_KEY); } catch { /* bỏ qua */ }
    me = null; mock = null;
}

const nextId = () => ++me.nextId;
const nowIso = () => new Date().toISOString().slice(0, 19) + "Z";

/** Lỗi giống ApiError của http.js (trang chỉ cần đọc err.message) */
function fail(message) { throw new Error(message); }

/** Trả kết quả sau một chút để giao diện "đang tải" có dịp hiện ra */
function reply(value) {
    return new Promise(resolve => setTimeout(() => resolve(structuredClone(value)), 120));
}

const findProduct = (/** @type {number} */ id) => mock.products.find((/** @type {any} */ p) => p.id === id);
const findVariant = (/** @type {number} */ id) => {
    for (const p of mock.products) {
        const v = p.variants.find((/** @type {any} */ x) => x.id === id);
        if (v) return { product: p, variant: v };
    }
    return null;
};
const categoryName = (/** @type {number} */ id) => mock.categories.find((/** @type {any} */ c) => c.id === id)?.name ?? "";
const minPrice = (/** @type {any} */ p) => Math.min(...p.variants.map((/** @type {any} */ v) => v.price));

/** ProductDto (danh sách) */
const productDto = (/** @type {any} */ p) => ({
    id: p.id, name: p.name, categoryId: p.categoryId, categoryName: categoryName(p.categoryId),
    mainProductImageUrl: p.imageUrls[0] ?? "", minPrice: minPrice(p)
});

/** CartResponseDto */
function cartDto() {
    const items = me.cart.map((/** @type {any} */ i) => {
        const found = findVariant(i.variantId);
        const p = found?.product, v = found?.variant;
        return {
            cartItemId: i.cartItemId, variantId: i.variantId, productId: p?.id, productName: p?.name,
            productImageUrl: p?.imageUrls[0], color: v?.color, size: v?.size, price: v?.price ?? 0,
            quantity: i.quantity, totalPrice: (v?.price ?? 0) * i.quantity
        };
    });
    return {
        cartId: me.customer.id, customerId: me.customer.id, items,
        totalItems: items.reduce((/** @type {number} */ s, /** @type {any} */ i) => s + i.quantity, 0),
        totalAmount: items.reduce((/** @type {number} */ s, /** @type {any} */ i) => s + i.totalPrice, 0)
    };
}

/** Tổng tồn kho của một biến thể ở mọi cửa hàng */
const totalStock = (/** @type {number} */ variantId) =>
    (mock.stock[variantId] ?? []).reduce((/** @type {number} */ s, /** @type {any} */ r) => s + r.quantity, 0);

/** Khuyến mãi còn hiệu lực theo mã (giống OrderService.GetActivePromotionAsync) */
function activePromotion(/** @type {string} */ code) {
    const p = mock.promotions.find((/** @type {any} */ x) => x.code.toUpperCase() === code.trim().toUpperCase());
    if (!p) fail("Mã khuyến mãi không tồn tại.");
    const now = Date.now();
    if (new Date(p.startDate).getTime() > now || new Date(p.endDate).getTime() < now)
        fail("Mã khuyến mãi đã hết hạn hoặc chưa đến thời gian áp dụng.");
    return p;
}

/* ============================================================================
 *  PHẦN 2: CÁC HÀM DÙNG TRONG TRANG
 *  Mỗi hàm: nếu USE_MOCK -> dùng backend giả ở trên, ngược lại gọi API thật.
 * ========================================================================== */

/** Id khách đang đăng nhập (chế độ mẫu: luôn là khách mẫu) */
async function customerId() {
    if (USE_MOCK) { await loadMock(); return me.customer.id; }
    const id = getCustomerId();
    if (!id) fail("Vui lòng đăng nhập bằng tài khoản khách hàng.");
    return id;
}

export const shopApi = {
    /* ---------------- Đăng nhập / đăng ký ---------------- */
    auth: {
        /** POST /api/Auth/login  -> { token, email, roles, customerId, fullName } (đã lưu sẵn vào trình duyệt) */
        async login(/** @type {{email:string,password:string}} */ data) {
            let result;
            if (USE_MOCK) {
                await loadMock();
                if (data.email.trim().toLowerCase() !== DEMO_ACCOUNT.email || data.password !== DEMO_ACCOUNT.password)
                    fail(`Email hoặc mật khẩu không đúng. Tài khoản mẫu: ${DEMO_ACCOUNT.email} / ${DEMO_ACCOUNT.password}`);
                // thẻ giả có hạn 1 ngày (auth.js đọc phần "exp")
                const payload = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 86400 })).replace(/=+$/, "");
                result = { token: `mock.${payload}.sig`, email: me.customer.email, roles: ["Customer"], customerId: me.customer.id, fullName: me.customer.fullName };
            } else {
                result = await post("/Auth/login", data);
            }
            saveLogin(result);
            return result;
        },
        /** POST /api/Auth/register  { email, password, firstName, lastName } */
        async register(/** @type {any} */ data) {
            if (USE_MOCK) {
                await loadMock();
                fail(`Chế độ dữ liệu mẫu chưa tạo được tài khoản mới. Hãy đăng nhập bằng ${DEMO_ACCOUNT.email} / ${DEMO_ACCOUNT.password}`);
            }
            return post("/Auth/register", data);
        }
    },

    /* ---------------- Danh mục, bộ sưu tập, hạng, cửa hàng, khuyến mãi ---------------- */
    /** GET /api/Category -> [{ id, name, parentCategoryId }] */
    async getCategories() { if (USE_MOCK) { await loadMock(); return reply(mock.categories); } return get("/Category"); },
    /** GET /api/CollectionTech -> [{ id, type: "Collection" | "Technology", name }] */
    async getCollections() { if (USE_MOCK) { await loadMock(); return reply(mock.collections); } return get("/CollectionTech"); },
    /** GET /api/MembershipTier -> [{ id, tierName, minPoints }] */
    async getTiers() { if (USE_MOCK) { await loadMock(); return reply(mock.tiers); } return get("/MembershipTier"); },
    /** GET /api/Store -> [{ id, name, location }] */
    async getStores() { if (USE_MOCK) { await loadMock(); return reply(mock.stores); } return get("/Store"); },
    /** GET /api/Promotion -> [{ id, code, discountValue, startDate, endDate }] */
    async getPromotions() { if (USE_MOCK) { await loadMock(); return reply(mock.promotions); } return get("/Promotion"); },
    /** GET /api/SizeChart/category/{id} -> [{ sizeLabel, measurements, categoryName }] */
    async getSizeChart(/** @type {number} */ categoryId) {
        if (USE_MOCK) { await loadMock(); return reply(mock.sizeCharts.filter((/** @type {any} */ s) => s.categoryId === categoryId)); }
        return get(`/SizeChart/category/${categoryId}`);
    },

    /* ---------------- Sản phẩm ---------------- */
    products: {
        /** GET /api/Product -> [{ id, name, categoryId, categoryName, mainProductImageUrl, minPrice }] */
        async getAll() { if (USE_MOCK) { await loadMock(); return reply(mock.products.map(productDto)); } return get("/Product"); },
        /** GET /api/Product/{id}/detail -> { id, name, categoryId, categoryName, imageUrls, variants, collections } */
        async getDetail(/** @type {number} */ id) {
            if (USE_MOCK) {
                await loadMock();
                const p = findProduct(id);
                if (!p) fail("Không tìm thấy sản phẩm.");
                return reply({
                    id: p.id, name: p.name, categoryId: p.categoryId, categoryName: categoryName(p.categoryId),
                    imageUrls: p.imageUrls, variants: p.variants,
                    collections: mock.collections.filter((/** @type {any} */ c) => p.collectionIds.includes(c.id))
                });
            }
            return get(`/Product/${id}/detail`);
        },
        /** GET /api/StoreStock/variant/{id} -> [{ storeId, storeName, quantity }] (còn hàng ở cửa hàng nào) */
        async getStock(/** @type {number} */ variantId) {
            if (USE_MOCK) {
                await loadMock();
                return reply((mock.stock[variantId] ?? []).map((/** @type {any} */ r) => ({
                    ...r, variantId, storeName: mock.stores.find((/** @type {any} */ s) => s.id === r.storeId)?.name
                })));
            }
            return get(`/StoreStock/variant/${variantId}`);
        }
    },

    /* ---------------- Đánh giá ---------------- */
    reviews: {
        /** GET /api/Review/product/{id} */
        async getByProduct(/** @type {number} */ productId) {
            if (USE_MOCK) { await loadMock(); return reply(allReviews().filter(r => r.productId === productId)); }
            return get(`/Review/product/${productId}`);
        },
        /** GET /api/Review/product/{id}/summary -> { averageRating, totalReviews, ratingDistribution } */
        async getSummary(/** @type {number} */ productId) {
            if (USE_MOCK) {
                await loadMock();
                const list = allReviews().filter(r => r.productId === productId);
                /** @type {Record<number, number>} */
                const dist = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 };
                list.forEach(r => { dist[r.rating]++; });
                return reply({ productId, totalReviews: list.length, ratingDistribution: dist,
                    averageRating: list.length ? list.reduce((s, r) => s + r.rating, 0) / list.length : 0 });
            }
            return get(`/Review/product/${productId}/summary`);
        },
        /** GET /api/Review/customer/{id} */
        async getMine() {
            const id = await customerId();
            if (USE_MOCK) return reply(allReviews().filter(r => r.customerId === id));
            return get(`/Review/customer/${id}`);
        },
        /** POST /api/Review  { customerId, productId, rating, comment, imageUrls (tối đa 3) } */
        async create(/** @type {{productId:number, rating:number, comment:string, imageUrls:string[]}} */ data) {
            const id = await customerId();
            if (USE_MOCK) {
                const bought = me.orders.some((/** @type {any} */ o) => ["Completed", "Returned"].includes(o.status)
                    && o.orderItems.some((/** @type {any} */ i) => i.productId === data.productId));
                if (!bought) fail("Khách hàng chỉ có thể đánh giá sản phẩm sau khi đã mua hàng thành công.");
                if (allReviews().some(r => r.customerId === id && r.productId === data.productId)) fail("Khách hàng đã đánh giá sản phẩm này rồi.");
                const review = { id: nextId(), customerId: id, customerName: me.customer.fullName, productId: data.productId,
                    productName: findProduct(data.productId)?.name, rating: data.rating, comment: data.comment,
                    imageUrls: (data.imageUrls ?? []).slice(0, 3), createdAt: nowIso() };
                (me.myReviews ??= []).push(review); saveMe();
                return reply(review);
            }
            return post("/Review", { ...data, customerId: id });
        },
        /** PUT /api/Review/{id}  { rating, comment, imageUrls } */
        async update(/** @type {number} */ reviewId, /** @type {any} */ data) {
            if (USE_MOCK) {
                await loadMock();
                const r = myReviewList().find(x => x.id === reviewId);
                if (!r) fail("Không tìm thấy đánh giá.");
                Object.assign(r, { rating: data.rating, comment: data.comment, imageUrls: (data.imageUrls ?? r.imageUrls).slice(0, 3) });
                saveMe(); return reply(null);
            }
            return put(`/Review/${reviewId}`, data);
        },
        /** DELETE /api/Review/{id} */
        async remove(/** @type {number} */ reviewId) {
            if (USE_MOCK) {
                await loadMock();
                (me.deletedReviews ??= []).push(reviewId);
                me.myReviews = (me.myReviews ?? []).filter((/** @type {any} */ r) => r.id !== reviewId);
                saveMe(); return reply(null);
            }
            return del(`/Review/${reviewId}`);
        }
    },

    /* ---------------- Yêu thích ---------------- */
    wishlist: {
        /** GET /api/Wishlist/customer/{id} -> [{ id, productId, productName, productImageUrl, price, inStock, createdAt }] */
        async getMine() { const id = await customerId(); if (USE_MOCK) return reply(me.wishlist); return get(`/Wishlist/customer/${id}`); },
        /** POST /api/Wishlist/toggle { customerId, productId } -> trả về true nếu sau khi bấm đang được yêu thích */
        async toggle(/** @type {number} */ productId) {
            const id = await customerId();
            if (USE_MOCK) {
                const i = me.wishlist.findIndex((/** @type {any} */ w) => w.productId === productId);
                if (i >= 0) { me.wishlist.splice(i, 1); saveMe(); return reply(false); }
                const p = findProduct(productId);
                me.wishlist.unshift({ id: nextId(), customerId: id, productId, productName: p?.name, productImageUrl: p?.imageUrls[0],
                    price: p ? minPrice(p) : 0, inStock: true, createdAt: nowIso() });
                saveMe(); return reply(true);
            }
            const result = await post("/Wishlist/toggle", { customerId: id, productId });
            // backend có thể trả object hoặc boolean -> hỏi lại cho chắc
            if (typeof result === "boolean") return result;
            const list = await get(`/Wishlist/customer/${id}`);
            return list.some((/** @type {any} */ w) => w.productId === productId);
        }
    },

    /* ---------------- Giỏ hàng ---------------- */
    cart: {
        /** GET /api/Cart/{customerId} -> { cartId, items: [...], totalItems, totalAmount } */
        async get() { const id = await customerId(); if (USE_MOCK) return reply(cartDto()); return get(`/Cart/${id}`); },
        /** POST /api/Cart/items { customerId, variantId, quantity } */
        async add(/** @type {number} */ variantId, /** @type {number} */ quantity) {
            const id = await customerId();
            if (USE_MOCK) {
                const line = me.cart.find((/** @type {any} */ i) => i.variantId === variantId);
                const newQty = (line?.quantity ?? 0) + quantity;
                if (newQty > totalStock(variantId)) fail(`Sản phẩm chỉ còn ${totalStock(variantId)} chiếc.`);
                if (line) line.quantity = newQty; else me.cart.push({ cartItemId: nextId(), variantId, quantity });
                saveMe(); return reply(cartDto());
            }
            return post("/Cart/items", { customerId: id, variantId, quantity });
        },
        /** PUT /api/Cart/items { cartItemId, quantity } */
        async update(/** @type {number} */ cartItemId, /** @type {number} */ quantity) {
            await customerId();
            if (USE_MOCK) {
                const line = me.cart.find((/** @type {any} */ i) => i.cartItemId === cartItemId);
                if (!line) fail("Không tìm thấy sản phẩm trong giỏ.");
                if (quantity > totalStock(line.variantId)) fail(`Sản phẩm chỉ còn ${totalStock(line.variantId)} chiếc.`);
                line.quantity = quantity; saveMe(); return reply(cartDto());
            }
            return put("/Cart/items", { cartItemId, quantity });
        },
        /** DELETE /api/Cart/items/{cartItemId} */
        async remove(/** @type {number} */ cartItemId) {
            await customerId();
            if (USE_MOCK) { me.cart = me.cart.filter((/** @type {any} */ i) => i.cartItemId !== cartItemId); saveMe(); return reply(cartDto()); }
            return del(`/Cart/items/${cartItemId}`);
        }
    },

    /* ---------------- Địa chỉ ---------------- */
    addresses: {
        /** GET /api/Address/customer/{id} -> [{ id, street, district, city, country, orderCount }] */
        async getMine() { const id = await customerId(); if (USE_MOCK) return reply(me.addresses); return get(`/Address/customer/${id}`); },
        /** POST /api/Address { street, district, city, country, customerId } */
        async create(/** @type {any} */ data) {
            const id = await customerId();
            if (USE_MOCK) { const a = { ...data, id: nextId(), customerId: id, orderCount: 0 }; me.addresses.push(a); saveMe(); return reply(a); }
            return post("/Address", { ...data, customerId: id });
        },
        /** PUT /api/Address/{id} */
        async update(/** @type {number} */ addressId, /** @type {any} */ data) {
            const id = await customerId();
            if (USE_MOCK) { Object.assign(me.addresses.find((/** @type {any} */ a) => a.id === addressId), data); saveMe(); return reply(null); }
            return put(`/Address/${addressId}`, { ...data, customerId: id });
        },
        /** DELETE /api/Address/{id}  (địa chỉ đã dùng cho đơn hàng thì backend không cho xóa) */
        async remove(/** @type {number} */ addressId) {
            await customerId();
            if (USE_MOCK) {
                if (me.orders.some((/** @type {any} */ o) => o.addressId === addressId)) fail("Địa chỉ đã được dùng cho đơn hàng, không thể xóa.");
                me.addresses = me.addresses.filter((/** @type {any} */ a) => a.id !== addressId); saveMe(); return reply(null);
            }
            return del(`/Address/${addressId}`);
        }
    },

    /* ---------------- Đơn hàng ---------------- */
    orders: {
        /** GET /api/Order/customer/{id} -> [OrderDetailDTO] */
        async getMine() { const id = await customerId(); if (USE_MOCK) return reply(me.orders); return get(`/Order/customer/${id}`); },
        /** GET /api/Order/{id} */
        async getById(/** @type {number} */ orderId) {
            if (USE_MOCK) {
                await loadMock();
                const o = me.orders.find((/** @type {any} */ x) => x.id === orderId);
                if (!o) fail("Không tìm thấy đơn hàng.");
                return reply(o);
            }
            return get(`/Order/${orderId}`);
        },
        /**
         * POST /api/Order/checkout  { customerId, addressId, promotionCode, storeId, paymentMethod }
         * Đặt toàn bộ giỏ hàng thành 1 đơn (giống OrderService.CheckoutAsync).
         */
        async checkout(/** @type {{addressId:number, promotionCode?:string, storeId?:number|null, paymentMethod:string}} */ data) {
            const id = await customerId();
            if (USE_MOCK) {
                const cart = cartDto();
                if (!cart.items.length) fail("Giỏ hàng đang trống, không thể đặt hàng.");
                const address = me.addresses.find((/** @type {any} */ a) => a.id === data.addressId);
                if (!address) fail("Địa chỉ không thuộc về khách hàng này.");
                const promo = data.promotionCode ? activePromotion(data.promotionCode) : null;
                const subtotal = cart.totalAmount;
                const discount = promo ? Math.min(promo.discountValue, subtotal) : 0;
                const store = mock.stores.find((/** @type {any} */ s) => s.id === (data.storeId ?? 1));
                const orderId = nextId();
                const order = {
                    id: orderId, orderDate: nowIso(), customerId: id, customerName: me.customer.fullName, status: "Pending",
                    addressId: address.id, address: `${address.street}, ${address.district}, ${address.city}`,
                    promotionId: promo?.id ?? null, promotionCode: promo?.code ?? null, storeId: store.id, storeName: store.name,
                    paymentMethod: data.paymentMethod, paymentStatus: "Pending", shipmentStatus: null, trackingNumber: null,
                    subtotal, discountAmount: discount, totalAmount: subtotal - discount,
                    orderItems: cart.items.map((/** @type {any} */ i) => ({
                        orderItemId: nextId(), orderId, variantId: i.variantId, productId: i.productId, variantName: i.productName,
                        color: i.color, size: i.size, quantity: i.quantity, price: i.price, lineTotal: i.totalPrice
                    }))
                };
                me.orders.unshift(order); me.cart = []; saveMe();
                return reply(order);
            }
            return post("/Order/checkout", { ...data, customerId: id });
        },
        /** PATCH /api/Order/{id}/cancel  (chỉ khi Pending / Confirmed) */
        async cancel(/** @type {number} */ orderId) {
            if (USE_MOCK) {
                await loadMock();
                const o = me.orders.find((/** @type {any} */ x) => x.id === orderId);
                if (!o || !["Pending", "Confirmed"].includes(o.status)) fail(`Không thể hủy đơn ở trạng thái ${o?.status}.`);
                o.status = "Cancelled";
                o.paymentStatus = o.paymentStatus === "Paid" ? "Refunded" : "Cancelled";
                saveMe(); return reply(o);
            }
            return patch(`/Order/${orderId}/cancel`);
        }
    },

    /** GET /api/Shipment/order/{orderId} -> { carrier, trackingNumber, shippedDate, deliveredDate, status } hoặc null */
    async getShipment(/** @type {number} */ orderId) {
        if (USE_MOCK) { await loadMock(); return reply(me.shipments.find((/** @type {any} */ s) => s.orderId === orderId) ?? null); }
        try { return await get(`/Shipment/order/${orderId}`); } catch { return null; }
    },

    /* ---------------- Đổi / trả hàng ---------------- */
    returns: {
        /** GET /api/ReturnRequest/customer/{id} */
        async getMine() { const id = await customerId(); if (USE_MOCK) return reply(me.returns); return get(`/ReturnRequest/customer/${id}`); },
        /**
         * POST /api/ReturnRequest  { orderId, reason, items: [{ orderItemId, quantity, condition }] }
         * Luật (giống ReturnRequestService): đơn Completed, trong 30 ngày, không trả quá số đã mua.
         */
        async create(/** @type {any} */ data) {
            const id = await customerId();
            if (USE_MOCK) {
                const o = me.orders.find((/** @type {any} */ x) => x.id === data.orderId);
                if (!o || o.status !== "Completed") fail("Chỉ được yêu cầu đổi/trả với đơn hàng đã giao thành công (Completed).");
                if (Date.now() - new Date(o.orderDate).getTime() > 30 * 86400000) fail("Đơn hàng đã quá thời hạn đổi/trả (30 ngày).");
                const ratio = o.totalAmount / (o.totalAmount + o.discountAmount);
                const items = data.items.map((/** @type {any} */ line) => {
                    const it = o.orderItems.find((/** @type {any} */ x) => x.orderItemId === line.orderItemId);
                    if (line.quantity > returnableQty(o, it)) fail(`Sản phẩm "${it.variantName}" chỉ còn có thể trả tối đa ${returnableQty(o, it)} chiếc.`);
                    return { id: nextId(), orderItemId: it.orderItemId, variantId: it.variantId, productName: it.variantName, color: it.color,
                        size: it.size, quantity: line.quantity, condition: line.condition, refundAmount: Math.round(it.price * line.quantity * ratio * 100) / 100 };
                });
                const r = { id: nextId(), orderId: o.id, customerId: id, requestDate: nowIso(), reason: data.reason, status: "Pending",
                    totalRefund: items.reduce((/** @type {number} */ s, /** @type {any} */ i) => s + i.refundAmount, 0), items };
                me.returns.unshift(r); saveMe(); return reply(r);
            }
            return post("/ReturnRequest", data);
        },
        /** DELETE /api/ReturnRequest/{id}  (chỉ khi đang chờ duyệt / bị từ chối) */
        async remove(/** @type {number} */ returnId) {
            if (USE_MOCK) {
                await loadMock();
                const r = me.returns.find((/** @type {any} */ x) => x.id === returnId);
                if (!r || !["Pending", "Rejected"].includes(r.status)) fail("Chỉ được xóa yêu cầu đang chờ xử lý hoặc đã bị từ chối.");
                me.returns = me.returns.filter((/** @type {any} */ x) => x.id !== returnId); saveMe(); return reply(null);
            }
            return del(`/ReturnRequest/${returnId}`);
        }
    },

    /* ---------------- Khách hàng + điểm thưởng ---------------- */
    me: {
        /** GET /api/Customer/{id} -> { id, fullName, firstName, lastName, email, membershipTierName, totalPoints, ... } */
        async get() {
            const id = await customerId();
            if (USE_MOCK) {
                const points = me.loyalty.reduce((/** @type {number} */ s, /** @type {any} */ x) => s + x.pointsChange, 0);
                const tier = [...mock.tiers].reverse().find((/** @type {any} */ t) => t.minPoints <= points);
                return reply({ ...me.customer, totalPoints: points, membershipTierId: tier?.id, membershipTierName: tier?.tierName,
                    addressCount: me.addresses.length, wishlistCount: me.wishlist.length });
            }
            return get(`/Customer/${id}`);
        },
        /** PUT /api/Customer/{id} { firstName, lastName } */
        async update(/** @type {{firstName:string,lastName:string}} */ data) {
            const id = await customerId();
            if (USE_MOCK) {
                Object.assign(me.customer, data, { fullName: `${data.firstName} ${data.lastName}` }); saveMe(); return reply(null);
            }
            return put(`/Customer/${id}`, data);
        },
        /** GET /api/LoyaltyTransaction/customer/{id} -> [{ txnDate, pointsChange, reason }] */
        async getLoyalty() { const id = await customerId(); if (USE_MOCK) return reply(me.loyalty); return get(`/LoyaltyTransaction/customer/${id}`); }
    }
};

/* ---------------- hàm phụ cho chế độ mẫu ---------------- */

/** Danh sách của khách trong chế độ mẫu */
function myReviewList() { return /** @type {any[]} */ (me.myReviews ?? []); }

/** Tất cả đánh giá (dữ liệu mẫu + khách vừa viết, trừ cái đã xóa) */
function allReviews() {
    const deleted = new Set(me.deletedReviews ?? []);
    const edited = new Map(myReviewList().map(r => [r.id, r]));
    return /** @type {any[]} */ ([...mock.reviews.filter((/** @type {any} */ r) => !deleted.has(r.id) && !edited.has(r.id)), ...myReviewList()]);
}

/** Số lượng còn có thể trả của 1 dòng đơn (đã trừ yêu cầu chưa bị từ chối) */
function returnableQty(/** @type {any} */ order, /** @type {any} */ item) {
    const used = me.returns
        .filter((/** @type {any} */ r) => r.orderId === order.id && r.status !== "Rejected")
        .flatMap((/** @type {any} */ r) => r.items)
        .filter((/** @type {any} */ i) => i.orderItemId === item.orderItemId)
        .reduce((/** @type {number} */ s, /** @type {any} */ i) => s + i.quantity, 0);
    return item.quantity - used;
}

/**
 * Số lượng còn trả được của từng dòng đơn (dùng cho trang tạo yêu cầu trả hàng).
 * @param {any} order @param {any[]} returns - danh sách yêu cầu của khách
 */
export function remainingReturnQty(order, returns) {
    /** @type {Record<number, number>} */
    const result = {};
    for (const item of order.orderItems) {
        const used = returns
            .filter(r => r.orderId === order.id && r.status !== "Rejected")
            .flatMap(r => r.items)
            .filter(i => i.orderItemId === item.orderItemId)
            .reduce((s, i) => s + i.quantity, 0);
        result[item.orderItemId] = item.quantity - used;
    }
    return result;
}
