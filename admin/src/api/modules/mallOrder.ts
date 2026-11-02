import http from "@/api";

/**
 * @description 商场订单模块
 */
export const mallOrderApi = {
  // 分页查询订单
  getOrderList: (params: any) => {
    return http.get<any>("/admin/mall/orders", params);
  },
  // 订单详情
  getOrder: (id: string) => {
    return http.get<any>(`/admin/mall/orders/${id}`);
  },
  // 取消订单
  cancelOrder: (id: string) => {
    return http.post<any>(`/admin/mall/orders/${id}/cancel`);
  },
  // 录入运单并发货
  createShipment: (id: string, params: any) => {
    return http.post<any>(`/admin/mall/orders/${id}/shipments`, params);
  },
  // 确认收货
  confirmReceipt: (id: string) => {
    return http.post<any>(`/admin/mall/orders/${id}/confirm-receipt`);
  },
  // 申请整单退款
  createRefund: (id: string, params: any) => {
    return http.post<any>(`/admin/mall/orders/${id}/refunds`, params);
  },
  // 模拟支付
  simulatePayment: (id: string, params: any) => {
    return http.post<any>(`/admin/mall/payments/${id}/simulate`, params);
  },
  // 模拟退款
  simulateRefund: (id: string, params: any) => {
    return http.post<any>(`/admin/mall/refunds/${id}/simulate`, params);
  }
};

export default mallOrderApi;
