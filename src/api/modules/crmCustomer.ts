import http from "@/api";

/**
 * @description CRM 客户管理模块
 */
export const crmCustomerApi = {
  getCustomerList: (params: any) => {
    return http.get<any>("/admin/crm/customers", params);
  },
  getCustomer: (id: string) => {
    return http.get<any>(`/admin/crm/customers/${id}`);
  },
  createCustomer: (params: any) => {
    return http.post<any>("/admin/crm/customers", params);
  },
  updateCustomer: (id: string, params: any) => {
    return http.put<any>(`/admin/crm/customers/${id}`, params);
  },
  deleteCustomer: (id: string) => {
    return http.delete(`/admin/crm/customers/${id}`);
  },
  getContacts: (customerId: string) => {
    return http.get<any>(`/admin/crm/customers/${customerId}/contacts`);
  },
  createContact: (customerId: string, params: any) => {
    return http.post<any>(`/admin/crm/customers/${customerId}/contacts`, params);
  },
  updateContact: (id: string, params: any) => {
    return http.put<any>(`/admin/crm/contacts/${id}`, params);
  },
  setPrimaryContact: (id: string) => {
    return http.patch<any>(`/admin/crm/contacts/${id}/primary`);
  },
  updateContactStatus: (id: string, params: any) => {
    return http.patch<any>(`/admin/crm/contacts/${id}/status`, params);
  },
  getFollowRecords: (customerId: string) => {
    return http.get<any>(`/admin/crm/customers/${customerId}/follow-records`);
  },
  createFollowRecord: (customerId: string, params: any) => {
    return http.post<any>(`/admin/crm/customers/${customerId}/follow-records`, params);
  },
};

export default crmCustomerApi;
