import http from "@/api";

export const crmMatchOpportunityApi = {
  getCandidates: (demandItemId: string, params: any) => http.get<any>(`/admin/crm/vendor-demand-items/${demandItemId}/match-candidates`, params),
  create: (demandItemId: string, data: { supplyIds: string[]; nextAction: string; nextActionAt: string }) => http.post<any>(`/admin/crm/vendor-demand-items/${demandItemId}/match-opportunities`, data),
  addCandidates: (opportunityId: string, data: { supplyIds: string[] }) => http.post<any>(`/admin/crm/match-opportunities/${opportunityId}/candidates`, data),
};

export default crmMatchOpportunityApi;
