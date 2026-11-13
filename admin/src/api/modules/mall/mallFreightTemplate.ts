import http from "@/api";

export const mallFreightTemplateApi = {
  getList: (params: any) => {
    return http.get<any>("/admin/mall/freight-templates", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/mall/freight-templates/${id}`);
  },
  add: (params: any) => {
    return http.post<any>("/admin/mall/freight-templates", params);
  },
  update: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/freight-templates/${id}`, params);
  },
  remove: (id: string) => {
    return http.delete(`/admin/mall/freight-templates/${id}`);
  }
};

export default mallFreightTemplateApi;