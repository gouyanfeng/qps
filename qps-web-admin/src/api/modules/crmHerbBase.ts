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
  updateSubject: (id: string, params: any) => {
    return http.put<any>(`/admin/crm/herb-base-subjects/${id}`, params);
  },
  changeOwner: (params: any) => {
    return http.patch<any>("/admin/crm/herb-base-subjects/owner", params);
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
  getHerbBaseList: (params: any) => {
    return http.get<any>("/admin/crm/herb-bases", params);
  },
  getHerbBase: (id: string) => {
    return http.get<any>(`/admin/crm/herb-bases/${id}`);
  },
  createHerbBase: (params: any) => {
    return http.post<any>("/admin/crm/herb-bases", params);
  },
  updateHerbBase: (id: string, params: any) => {
    return http.put<any>(`/admin/crm/herb-bases/${id}`, params);
  },
  deleteHerbBase: (id: string) => {
    return http.delete(`/admin/crm/herb-bases/${id}`);
  },
  getSupplies: (herbBaseId: string) => {
    return http.get<any>(`/admin/crm/herb-bases/${herbBaseId}/supplies`);
  },
  createSupply: (herbBaseId: string, params: any) => {
    return http.post<any>(`/admin/crm/herb-bases/${herbBaseId}/supplies`, params);
  },
  updateSupply: (id: string, params: any) => {
    return http.put<any>(`/admin/crm/herb-base-supplies/${id}`, params);
  },
  deleteSupply: (id: string) => {
    return http.delete(`/admin/crm/herb-base-supplies/${id}`);
  },
  changeSupplyStatus: (id: string, params: any) => {
    return http.patch<any>(`/admin/crm/herb-base-supplies/${id}/status`, params);
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
};

export default crmHerbBaseApi;



