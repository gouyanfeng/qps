 import http from "@/api";

export const crmMatchOpportunityApi = {
  getCandidates: (demandItemId: string, params: any) => http.get<any>(`/admin/crm/vendor-demand-items/${demandItemId}/match-candidates`, params),
  getProgressOptions: () => http.get<any>("/admin/crm/match-opportunity-progress-options"),
  create: (demandItemId: string, data: { supplyIds: string[]; nextAction: string; nextActionRemark: string; nextActionAt: string }) => http.post<any>(`/admin/crm/vendor-demand-items/${demandItemId}/match-opportunities`, data),
  addCandidates: (opportunityId: string, data: { supplyIds: string[] }) => http.post<any>(`/admin/crm/match-opportunities/${opportunityId}/candidates`, data),
  advance: (opportunityId: string, data: { stage: string; currentResult: string; nextAction: string; nextActionRemark: string; nextActionAt: string }) => http.patch<any>(`/admin/crm/match-opportunities/${opportunityId}/progress`, data),
};

export default crmMatchOpportunityApi;
