import http from "@/api";

export interface CrmDashboardChartItem {
  code: string;
  name: string;
  value: number;
}

export interface CrmDashboardTrendItem {
  date: string;
  followCount: number;
  effectiveFollowCount: number;
}

export interface CrmDashboardNewBaseTrendItem {
  date: string;
  newBaseCount: number;
}

export interface CrmDashboardNewPurchaseDemandTrendItem {
  date: string;
  newPurchaseDemandCount: number;
}

export const dashboardApi = {
  getFollowFunnel: () => http.get<CrmDashboardChartItem[]>("/admin/dashboard/crm/follow-funnel", undefined, { loading: false }),
  getSupplyProductDistribution: () => http.get<CrmDashboardChartItem[]>("/admin/dashboard/crm/supply-product-distribution", undefined, { loading: false }),
  getFollowTrend: () => http.get<CrmDashboardTrendItem[]>("/admin/dashboard/crm/follow-trend", undefined, { loading: false }),
  getNewBaseTrend: () => http.get<CrmDashboardNewBaseTrendItem[]>("/admin/dashboard/crm/new-base-trend", undefined, { loading: false }),
  getVendorPriorityDistribution: () => http.get<CrmDashboardChartItem[]>("/admin/dashboard/crm/vendor-priority-distribution", undefined, { loading: false }),
  getVendorFollowTrend: () => http.get<CrmDashboardTrendItem[]>("/admin/dashboard/crm/vendor-follow-trend", undefined, { loading: false }),
  getNewPurchaseDemandTrend: () => http.get<CrmDashboardNewPurchaseDemandTrendItem[]>("/admin/dashboard/crm/new-purchase-demand-trend", undefined, { loading: false }),
  getVendorPurchaseProductDistribution: () => http.get<CrmDashboardChartItem[]>("/admin/dashboard/crm/vendor-purchase-product-distribution", undefined, { loading: false }),
};

export default dashboardApi;
