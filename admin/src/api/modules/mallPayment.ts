import http from "@/api";

/**
 * @description 商场支付单模块
 */
export const mallPaymentApi = {
  getPaymentList: (params: any) => {
    return http.get<any>("/admin/mall/payments", params);
  },
  simulatePayment: (id: string, params: any) => {
    return http.post<any>(`/admin/mall/payments/${id}/simulate`, params);
  }
};

export default mallPaymentApi;