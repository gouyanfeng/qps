import http from "@/api";

/**
 * @description 商场库存管理模块
 */
export const mallStockApi = {
  // 分页查询规格库存
  getSkuList: (params: any) => {
    return http.get<any>("/admin/mall/skus", params);
  },
  // 手动调整库存
  adjustStock: (id: string, params: any) => {
    return http.post<any>(`/admin/mall/skus/${id}/adjust-stock`, params);
  },
  // 批量调整价格与库存
  batchAdjust: (params: any) => {
    return http.post<any>("/admin/mall/skus/batch-adjust", params);
  },
  // 分页查询库存流水
  getStockLogs: (params: any) => {
    return http.get<any>("/admin/mall/stock-logs", params);
  }
};

export default mallStockApi;
