import http from "@/api";

export const mallReturnApi = {
  getList: (params: any) => {
    return http.get<any>("/admin/mall/returns", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/mall/returns/${id}`);
  },
  approve: (id: string, params: any) => {
    return http.patch<any>(`/admin/mall/returns/${id}/approve`, params);
  },
  reject: (id: string, reason: string) => {
    return http.patch<any>(`/admin/mall/returns/${id}/reject`, { reason });
  },
  confirmReceive: (id: string) => {
    return http.patch<any>(`/admin/mall/returns/${id}/confirm-receive`);
  },
  refund: (id: string) => {
    return http.patch<any>(`/admin/mall/returns/${id}/refund`);
  }
};

export default mallReturnApi;