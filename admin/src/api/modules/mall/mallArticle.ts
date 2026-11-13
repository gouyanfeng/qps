import http from "@/api";

export const mallArticleApi = {
  getList: (params: any) => {
    return http.get<any>("/admin/mall/articles", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/mall/articles/${id}`);
  },
  add: (params: any) => {
    return http.post<any>("/admin/mall/articles", params);
  },
  update: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/articles/${id}`, params);
  },
  remove: (id: string) => {
    return http.delete(`/admin/mall/articles/${id}`);
  },
  publish: (id: string) => {
    return http.patch<any>(`/admin/mall/articles/${id}/publish`);
  },
  unpublish: (id: string) => {
    return http.patch<any>(`/admin/mall/articles/${id}/unpublish`);
  },
  setTop: (id: string, isTop: boolean) => {
    return http.patch<any>(`/admin/mall/articles/${id}/top`, null, { params: { isTop } });
  }
};

export default mallArticleApi;