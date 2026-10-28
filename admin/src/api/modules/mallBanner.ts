import http from "@/api";

export const mallBannerApi = {
  getList: (params: any) => {
    return http.get<any>("/admin/mall/banners", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/mall/banners/${id}`);
  },
  add: (params: any) => {
    return http.post<any>("/admin/mall/banners", params);
  },
  update: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/banners/${id}`, params);
  },
  remove: (id: string) => {
    return http.delete(`/admin/mall/banners/${id}`);
  },
  changeStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/banners/${id}/status`, null, { params: { isActive } });
  },
  batchSort: (items: any[]) => {
    return http.post<any>("/admin/mall/banners/batch-sort", { items });
  }
};

export default mallBannerApi;