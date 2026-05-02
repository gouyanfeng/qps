import http from "@/api";

const orderApi = {
  // 获取订单列表
  getOrderList(params: any) {
    return http.get("/admin/orders", params);
  },

  // 获取单个订单详情
  getOrderById(id: string) {
    return http.get(`/admin/orders/${id}`);
  },

  // 创建订单
  createOrder(data: {
    roomId: string;
    amount: number;
    durationMinutes: number;
  }) {
    return http.post("/admin/orders", data);
  },

  // 结算订单
  settleOrder(orderId: string) {
    return http.post(`/admin/orders/${orderId}/settle`);
  },
};

export { orderApi };
