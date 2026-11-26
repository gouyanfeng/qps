import http from "@/api";

/** 订单趋势：近 7 天每日下单量 */
export interface MallOrderTrendItem {
  date: string;
  orderCount: number;
}

/** 销售额趋势：近 7 天每日实收金额 */
export interface MallSalesTrendItem {
  date: string;
  salesAmount: number;
}

/** 订单状态分布 */
export interface MallOrderStatusItem {
  code: string;
  name: string;
  value: number;
}

/** 热销商品 */
export interface MallTopProductItem {
  productId: string;
  productName: string;
  quantity: number;
  salesAmount: number;
}

/** 核心指标概览 */
export interface MallDashboardSummary {
  totalOrders: number;
  paidOrders: number;
  conversionRate: number;
  salesAmount: number;
  averageOrderValue: number;
  itemsSold: number;
  refundAmount: number;
  pendingShipment: number;
}

/** 客单价趋势 */
export interface MallAovTrendItem {
  date: string;
  averageOrderValue: number;
}

/** 转化漏斗 */
export interface MallConversionFunnelItem {
  name: string;
  value: number;
}

/**
 * @description 商城数据看板（固定近 7 天）
 */
export const mallDashboardApi = {
  getOrderTrend: () =>
    http.get<MallOrderTrendItem[]>("/admin/mall/dashboard/order-trend", undefined, { loading: false }),
  getSalesTrend: () =>
    http.get<MallSalesTrendItem[]>("/admin/mall/dashboard/sales-trend", undefined, { loading: false }),
  getOrderStatusDistribution: () =>
    http.get<MallOrderStatusItem[]>("/admin/mall/dashboard/order-status-distribution", undefined, {
      loading: false
    }),
  getTopProducts: () =>
    http.get<MallTopProductItem[]>("/admin/mall/dashboard/top-products", undefined, { loading: false }),
  getSummary: () =>
    http.get<MallDashboardSummary>("/admin/mall/dashboard/summary", undefined, { loading: false }),
  getAovTrend: () =>
    http.get<MallAovTrendItem[]>("/admin/mall/dashboard/aov-trend", undefined, { loading: false }),
  getConversionFunnel: () =>
    http.get<MallConversionFunnelItem[]>("/admin/mall/dashboard/conversion-funnel", undefined, {
      loading: false
    })
};

export default mallDashboardApi;
