import { get } from "../core/http.js";

export const statisticApi = {
    getDailyStats: () => get("/StoreDailyStat"),
    getDailyStatById: id => get(`/StoreDailyStat/${id}`),
    getItemStats: () => get("/StoreItemStat"),
    getItemStatById: id => get(`/StoreItemStat/${id}`)
};
