import http from "@/api";

/**
 * @description 商场退款单模块
 */
export const mallRefundApi = {
  getRefundList: (params: any) => {
    return http.get<any>("/admin/mall/refunds", params);
  },
  simulateRefund: (id: string, params: any) => {
    return http.post<any>(`/admin/mall/refunds/${id}/simulate`, params);
  }
};

export default mallRefundApi;