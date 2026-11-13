import http from "@/api";

export const mallPageApi = {
  getList: (params: any) => {
    return http.get<any>("/admin/mall/pages", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/mall/pages/${id}`);
  },
  add: (params: any) => {
    return http.post<any>("/admin/mall/pages", params);
  },
  update: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/pages/${id}`, params);
  },
  remove: (id: string) => {
    return http.delete(`/admin/mall/pages/${id}`);
  },
  changeStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/pages/${id}/status`, null, { params: { isActive } });
  }
};

export default mallPageApi;