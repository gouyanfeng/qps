import http from "@/api";

export const mallNoticeApi = {
  getList: (params: any) => {
    return http.get<any>("/admin/mall/notices", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/mall/notices/${id}`);
  },
  add: (params: any) => {
    return http.post<any>("/admin/mall/notices", params);
  },
  update: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/notices/${id}`, params);
  },
  remove: (id: string) => {
    return http.delete(`/admin/mall/notices/${id}`);
  },
  changeStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/notices/${id}/status`, null, { params: { isActive } });
  },
  setTop: (id: string, isTop: boolean) => {
    return http.patch<any>(`/admin/mall/notices/${id}/top`, null, { params: { isTop } });
  }
};

export default mallNoticeApi;