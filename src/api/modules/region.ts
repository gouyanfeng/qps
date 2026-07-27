import http from "@/api";

/**
 * @description 地址区域模块
 */
export const regionApi = {
  getRegionList: (params: any) => {
    return http.get<any>("/admin/regions", params);
  },
  getRegion: (id: string) => {
    return http.get<any>(`/admin/regions/${id}`);
  },
  addRegion: (params: any) => {
    return http.post<any>("/admin/regions", params);
  },
  updateRegion: (id: string, params: any) => {
    return http.put<any>(`/admin/regions/${id}`, params);
  },
  deleteRegion: (id: string) => {
    return http.delete(`/admin/regions/${id}`);
  },
  getChinaRegionList: (params?: any) => {
    return http.get<any>("/admin/china-regions", params);
  }
};

export default regionApi;
