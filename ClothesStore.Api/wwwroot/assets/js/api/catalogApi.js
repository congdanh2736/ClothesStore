import {get, post, put, del} from '../core/http.js';

// tạo đường dẫn gốc(root) để code nhanh
const CATEGORY = '/Category';

export const categoryApi = {
    getAll: () => get(`${CATEGORY}`),
    getById: id => get(`${CATEGORY}/${id}`),
    create: data => post(`${CATEGORY}`, data),
    update: (id, data) => put(`${CATEGORY}/${id}`,data),
    remove: id => del(`${CATEGORY}/${id}`)
};

const COLLECTION_TECH = '/CollectionTech';

export const collectionTechApi = {
    getAll: () => get(`${COLLECTION_TECH}`),
    getById: id => get(`${COLLECTION_TECH}/${id}`),
    create: data => post(`${COLLECTION_TECH}`,data),
    update: (id, data) => put(`${COLLECTION_TECH}/${id}`,data),
    remove: (id) => del(`${COLLECTION_TECH}/${id}`)
};

const SIZECHART = '/SizeChart';

export const sizeChartApi = {
    getAll: () => get(`${SIZECHART}`),
    getById: id => get(`${SIZECHART}/${id}`),
    create: data => post(`${SIZECHART}`,data),
    update: (id,data) => put(`${SIZECHART}/${id}`,data),
    remove: (id) => del(`${SIZECHART}/${id}`)
};