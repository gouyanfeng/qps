import http from "@/api";

const statisticsApi = {
  getOverview(params?: { timeRange?: string }) {
    return http.get("/admin/statistics/overview", params);
  },

  getRealtime() {
    return http.get("/admin/statistics/realtime");
  },
};

export { statisticsApi };
