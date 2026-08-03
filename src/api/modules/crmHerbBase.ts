import http from "@/api";

/**
 * @description CRM 药材基地管理模块
 */
export const crmHerbBaseApi = {
  getSubjectList: (params: any) => {
    return http.get<any>("/admin/crm/herb-base-subjects", params);
  },
  getSubject: (id: string) => {
    return http.get<any>(`/admin/crm/herb-base-subjects/${id}`);
  },
  assignSubjectOwner: (params: any) => {
    return http.patch<any>("/admin/crm/herb-base-subjects/assign-owner", params);
  },
  getSubjectContacts: (herbBaseSubjectId: string) => {
    return http.get<any>(`/admin/crm/herb-base-subjects/${herbBaseSubjectId}/contacts`);
  },
  createSubjectContact: (herbBaseSubjectId: string, params: any) => {
    return http.post<any>(`/admin/crm/herb-base-subjects/${herbBaseSubjectId}/contacts`, params);
  },
  getSubjectFollowRecords: (herbBaseSubjectId: string) => {
    return http.get<any>(`/admin/crm/herb-base-subjects/${herbBaseSubjectId}/follow-records`);
  },
  createSubjectFollowRecord: (herbBaseSubjectId: string, params: any) => {
    return http.post<any>(`/admin/crm/herb-base-subjects/${herbBaseSubjectId}/follow-records`, params);
  },
  getCustomerList: (params: any) => {
    return http.get<any>("/admin/crm/herb-bases", params);
  },
  getCustomer: (id: string) => {
    return http.get<any>(`/admin/crm/herb-bases/${id}`);
  },
  createCustomer: (params: any) => {
    return http.post<any>("/admin/crm/herb-bases", params);
  },
  updateCustomer: (id: string, params: any) => {
    return http.put<any>(`/admin/crm/herb-bases/${id}`, params);
  },
  assignOwner: (params: any) => {
    return http.patch<any>("/admin/crm/herb-bases/assign-owner", params);
  },
  deleteCustomer: (id: string) => {
    return http.delete(`/admin/crm/herb-bases/${id}`);
  },
  getContacts: (herbBaseId: string) => {
    return http.get<any>(`/admin/crm/herb-bases/${herbBaseId}/contacts`);
  },
  createContact: (herbBaseId: string, params: any) => {
    return http.post<any>(`/admin/crm/herb-bases/${herbBaseId}/contacts`, params);
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
  getFollowRecords: (herbBaseId: string) => {
    return http.get<any>(`/admin/crm/herb-bases/${herbBaseId}/follow-records`);
  },
  getTransferRecords: (herbBaseId: string) => {
    return http.get<any>(`/admin/crm/herb-bases/${herbBaseId}/owner-transfers`);
  },
  createFollowRecord: (herbBaseId: string, params: any) => {
    return http.post<any>(`/admin/crm/herb-bases/${herbBaseId}/follow-records`, params);
  },
};

export default crmHerbBaseApi;



