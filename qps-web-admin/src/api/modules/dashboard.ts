import http from "@/api";

export interface CrmDashboardMetrics {
  todayFollowCount: number;
  overdueFollowCount: number;
  mySubjectCount: number;
  highIntentSubjectCount: number;
}

export interface CrmDashboardFollowSubject {
  id: string;
  subjectName: string;
  mainProducts: string[];
  grade: string;
  regions: string[];
  primaryContactName: string;
  primaryContactPhone: string;
  lastFollowResult: string;
  nextFollowAt?: string | null;
}

export interface CrmDashboardRecentFollowRecord {
  id: string;
  herbBaseSubjectId: string;
  subjectName: string;
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

export interface CrmDashboardNewBaseTrendItem {
  date: string;
  newBaseCount: number;
}

export interface CrmDashboardNewPurchaseDemandTrendItem {
  date: string;
  newPurchaseDemandCount: number;
}

export interface CrmDashboardData {
  metrics: CrmDashboardMetrics;
  todayFollowSubjects: CrmDashboardFollowSubject[];
  recentFollowRecords: CrmDashboardRecentFollowRecord[];
  followFunnel: CrmDashboardChartItem[];
  mainProductDistribution: CrmDashboardChartItem[];
  followTrend: CrmDashboardTrendItem[];
  newBaseTrend: CrmDashboardNewBaseTrendItem[];
  vendorPriorityDistribution: CrmDashboardChartItem[];
  vendorFollowTrend: CrmDashboardTrendItem[];
  newPurchaseDemandTrend: CrmDashboardNewPurchaseDemandTrendItem[];
  vendorPurchaseProductDistribution: CrmDashboardChartItem[];
}

export const dashboardApi = {
  getCrmDashboard: () => {
    return http.get<CrmDashboardData>("/admin/dashboard/crm", undefined, { loading: false });
  },
};

export default dashboardApi;
