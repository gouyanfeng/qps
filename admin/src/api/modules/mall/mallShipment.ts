import http from "@/api";

/**
 * @description 商城发货管理模块
 */
export const mallShipmentApi = {
  // 分页查询发货单（含已作废记录）
  getShipmentList: (params: any) => {
    return http.get<any>("/admin/mall/shipments", params);
  },
  // 发货单详情
  getShipment: (id: string) => {
    return http.get<any>(`/admin/mall/shipments/${id}`);
  },
  // 作废运单
  voidShipment: (id: string, params: any) => {
    return http.post<any>(`/admin/mall/shipments/${id}/void`, params);
  }
};

export default mallShipmentApi;
