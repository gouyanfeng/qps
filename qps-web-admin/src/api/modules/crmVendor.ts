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
  changeOwner: (data: any) => {
    return http.patch<any>("/admin/crm/vendors/owner", data);
  },
  createContact: (id: string, data: any) => {
    return http.post<any>(`/admin/crm/vendors/${id}/contacts`, data);
  },
  updateContact: (id: string, data: any) => {
    return http.put<any>(`/admin/crm/contacts/${id}`, data);
  },
  setPrimaryContact: (id: string) => {
    return http.patch<any>(`/admin/crm/contacts/${id}/primary`);
  },
  updateContactStatus: (id: string, data: any) => {
    return http.patch<any>(`/admin/crm/contacts/${id}/status`, data);
  },
  getFollowRecords: (id: string) => {
    return http.get<any>(`/admin/crm/vendors/${id}/follow-records`);
  },
  createFollowRecord: (id: string, data: any) => {
    return http.post<any>(`/admin/crm/vendors/${id}/follow-records`, data);
  },
  getHerbProductOptions: (params: any) => {
    return http.get<any>("/admin/crm/vendors/herb-product-options", params, { loading: false, cancel: false });
  },
  getVendorPurchaseDemands: (id: string, params: any) => {
    return http.get<any>(`/admin/crm/vendors/${id}/purchase-demands`, params);
  },
};

export default crmVendorApi;


