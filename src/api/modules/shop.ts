import http from "@/api";

/**
 * @description 门店管理模块
 */
export const shopApi = {
  // 获取门店列表
  getShopList: (params: any) => {
    return http.get<any>("/admin/shops", params);
  },
  // 获取门店详情
  getShopDetail: (id: string) => {
    return http.get<any>(`/admin/shops/${id}`);
  },
  // 新增门店
  addShop: (params: any) => {
    return http.post<any>("/admin/shops", params);
  },
  // 更新门店
  updateShop: (id: string, params: any) => {
    return http.put<any>(`/admin/shops/${id}`, params);
  },
  // 删除门店
  deleteShop: (id: string) => {
    return http.delete(`/admin/shops/${id}`);
  },
};

export default shopApi;
