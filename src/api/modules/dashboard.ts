import http from "@/api";

export interface CrmDashboardMetrics {
  todayFollowCount: number;
  overdueFollowCount: number;
  myCustomerCount: number;
  highIntentCustomerCount: number;
}

export interface CrmDashboardFollowCustomer {
  id: string;
  baseName: string;
  subjectName: string;
  mainProduct: string;
  mainProducts: string[];
  grade: string;
  province: string;
  city: string;
  area: string;
  primaryContactName: string;
  primaryContactPhone: string;
  lastFollowResult: string;
  nextFollowAt?: string | null;
}

export interface CrmDashboardRecentFollowRecord {
  id: string;
  customerId: string;
  baseName: string;
  followType: string;
  followResult: string;
  intentLevel: string;
  content: string;
  nextFollowAt?: string | null;
  createdAt: string;
}

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

export interface CrmDashboardData {
  metrics: CrmDashboardMetrics;
  todayFollowCustomers: CrmDashboardFollowCustomer[];
  recentFollowRecords: CrmDashboardRecentFollowRecord[];
  followFunnel: CrmDashboardChartItem[];
  mainProductDistribution: CrmDashboardChartItem[];
  followTrend: CrmDashboardTrendItem[];
}

export const dashboardApi = {
  getCrmDashboard: () => {
    return http.get<CrmDashboardData>("/admin/dashboard/crm", undefined, { loading: false });
  },
};

export default dashboardApi;
