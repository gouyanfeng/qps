import http from "@/api";

export const mallPromotionApi = {
  getList: (params: any) => {
    return http.get<any>("/admin/mall/promotions", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/mall/promotions/${id}`);
  },
  add: (params: any) => {
    return http.post<any>("/admin/mall/promotions", params);
  },
  update: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/promotions/${id}`, params);
  },
  remove: (id: string) => {
    return http.delete(`/admin/mall/promotions/${id}`);
  },
  changeStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/promotions/${id}/status`, null, { params: { isActive } });
  }
};

export default mallPromotionApi;