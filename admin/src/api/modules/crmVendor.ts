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
  changeVendorOwner: (id: string, data: any) => {
    return http.patch<any>(`/admin/crm/vendors/${id}/owner`, data);
  },
  getVendorContacts: (id: string) => {
    return http.get<any>(`/admin/crm/vendors/${id}/contacts`);
  },
  createVendorContact: (id: string, data: any) => {
    return http.post<any>(`/admin/crm/vendors/${id}/contacts`, data);
  },
  updateVendorContact: (id: string, contactId: string, data: any) => {
    return http.put<any>(`/admin/crm/vendors/${id}/contacts/${contactId}`, data);
  },
  setPrimaryVendorContact: (id: string, contactId: string) => {
    return http.patch<any>(`/admin/crm/vendors/${id}/contacts/${contactId}/primary`);
  },
  updateVendorContactStatus: (id: string, contactId: string, data: any) => {
    return http.patch<any>(`/admin/crm/vendors/${id}/contacts/${contactId}/status`, data);
  },
  getVendorFollowRecords: (id: string) => {
    return http.get<any>(`/admin/crm/vendors/${id}/follow-records`);
  },
  createVendorFollowRecord: (id: string, data: any) => {
    return http.post<any>(`/admin/crm/vendors/${id}/follow-records`, data);
  },
  getVendorTransferRecords: (id: string) => {
    return http.get<any>(`/admin/crm/vendors/${id}/transfer-records`);
  },
  getHerbProductOptions: (params: any) => {
    return http.get<any>("/admin/crm/vendors/herb-product-options", params, { loading: false, cancel: false });
  },
  getVendorPurchaseDemands: (id: string, params: any) => {
    return http.get<any>(`/admin/crm/vendors/${id}/purchase-demands`, params);
  },
};

export default crmVendorApi;


