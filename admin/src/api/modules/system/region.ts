import http from "@/api";

/**
 * @description 地址区域模块
 */
export const regionApi = {
  getChinaRegionList: (params?: any) => {
    return http.get<any>("/admin/china-regions", params);
  }
};

export default regionApi;


