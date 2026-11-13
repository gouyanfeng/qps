import http from "@/api";

/**
 * @description 商场运费规则模块
 */
export const mallFreightApi = {
  // 查询全部门店运费规则
  getFreightRules: (params?: any) => {
    return http.get<any>("/admin/mall/freight-rules", params);
  },
  // 新增或更新门店运费规则
  upsertFreightRule: (storeId: string, params: any) => {
    return http.put<any>(`/admin/mall/freight-rules/${storeId}`, params);
  }
};

export default mallFreightApi;
