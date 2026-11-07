import http from "@/api";

export const mallReturnApi = {
  getList: (params: any) => {
    return http.get<any>("/admin/mall/returns", params);
  },
  get: (id: string) => {
    return http.get<any>(`/admin/mall/returns/${id}`);
  },
  // 后端以退货单号定位，POST /returns/{returnNo}/approve，body 通过 approve 区分通过与拒绝
  approve: (returnNo: string, params?: { approve?: boolean; rejectReason?: string }) => {
    return http.post<any>(`/admin/mall/returns/${returnNo}/approve`, {
      approve: true,
      ...params
    });
  },
  reject: (returnNo: string, reason: string) => {
    return http.post<any>(`/admin/mall/returns/${returnNo}/approve`, {
      approve: false,
      rejectReason: reason
    });
  },
  // 确认收货后后端同步完成退货退款并回补库存，无需再单独调用退款
  confirmReceive: (returnNo: string) => {
    return http.post<any>(`/admin/mall/returns/${returnNo}/confirm-receipt`);
  }
};

export default mallReturnApi;
