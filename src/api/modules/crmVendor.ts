import http from "@/api";

/**
 * @description CRM 厂商管理模块
 */
export const crmVendorApi = {
  getVendorList: (params: any) => {
    return http.get<any>("/admin/crm/vendors", params);
  },
  getVendor: (id: string) => {
    return http.get<any>(`/admin/crm/vendors/${id}`);
  },
  createVendor: (data: any) => {
    return http.post<any>("/admin/crm/vendors", data);
  },
  updateVendor: (id: string, data: any) => {
    return http.put<any>(`/admin/crm/vendors/${id}`, data);
  },
  assignOwner: (data: any) => {
    return http.patch<any>("/admin/crm/vendors/assign-owner", data);
  },
  getVendorPurchasePlans: (id: string, params: any) => {
    return http.get<any>(`/admin/crm/vendors/${id}/purchase-plans`, params);
  },
};

export default crmVendorApi;


