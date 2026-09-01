import http from "@/api";

export const crmPurchaseDemandApi = {
  getList: (params: any) => http.get<any>("/admin/crm/purchase-demands", params),
  getDetail: (id: string) => http.get<any>(`/admin/crm/purchase-demands/${id}`),
  create: (data: any) => http.post<any>("/admin/crm/purchase-demands", data),
  update: (id: string, data: any) => http.put<any>(`/admin/crm/purchase-demands/${id}`, data),
  remove: (id: string) => http.delete<any>(`/admin/crm/purchase-demands/${id}`),
  changeStatus: (id: string, data: any) => http.patch<any>(`/admin/crm/purchase-demands/${id}/status`, data),
};

export default crmPurchaseDemandApi;
