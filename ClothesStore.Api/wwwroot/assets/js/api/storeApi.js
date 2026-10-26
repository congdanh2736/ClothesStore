import { get, post, put, del } from "../core/http.js";

const BASE = "/Store";
const STOCK_BASE = "/StoreStock";

export const storeApi = {
    getAll: () => get(BASE),
    getById: id => get(`${BASE}/${id}`),
    create: data => post(BASE, data),
    update: (id, data) => put(`${BASE}/${id}`, data),
    remove: id => del(`${BASE}/${id}`)
};

export const storeStockApi = {
    getAll: () => get(STOCK_BASE),
    getByStore: storeId => get(`${STOCK_BASE}/store/${storeId}`),
    getById: id => get(`${STOCK_BASE}/${id}`),
    adjust: data => post(`${STOCK_BASE}/adjust`, data),
    create: data => post(STOCK_BASE, data),
    update: (id, data) => put(`${STOCK_BASE}/${id}`, data),
    remove: id => del(`${STOCK_BASE}/${id}`)
};
