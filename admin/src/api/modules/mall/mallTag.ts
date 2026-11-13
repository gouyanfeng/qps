import http from "@/api";

export const mallTagApi = {
  getTags: (params: any = {}) => http.get<any>("/admin/mall/tags", params),
  addTag: (params: any) => http.post<any>("/admin/mall/tags", params),
  updateTag: (id: string, params: any) => http.put<any>(`/admin/mall/tags/${id}`, params),
  changeTagStatus: (id: string, isActive: boolean) =>
    http.patch<any>(`/admin/mall/tags/${id}/status`, { isActive }),
  deleteTag: (id: string) => http.delete(`/admin/mall/tags/${id}`)
};

export default mallTagApi;
