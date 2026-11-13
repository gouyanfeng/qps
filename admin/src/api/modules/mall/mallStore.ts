import http from "@/api";

/**
 * @description 商场门店模块
 */
export const mallStoreApi = {
  // 分页查询门店
  getStoreList: (params: any) => {
    return http.get<any>("/admin/mall/stores", params);
  },
  // 获取门店详情
  getStore: (id: string) => {
    return http.get<any>(`/admin/mall/stores/${id}`);
  },
  // 新增门店
  addStore: (params: any) => {
    return http.post<any>("/admin/mall/stores", params);
  },
  // 编辑门店
  updateStore: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/stores/${id}`, params);
  },
  // 启用或停用门店
  changeStoreStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/stores/${id}/status`, { isActive });
  },
  // 删除门店
  deleteStore: (id: string) => {
    return http.delete(`/admin/mall/stores/${id}`);
  }
};

export default mallStoreApi;
