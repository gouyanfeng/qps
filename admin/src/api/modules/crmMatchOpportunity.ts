import http from "@/api";

export const crmMatchOpportunityApi = {
  getCandidates: (demandItemId: string, params: any) => http.get<any>(`/admin/crm/vendor-demand-items/${demandItemId}/match-candidates`, params),
  create: (demandItemId: string, data: { supplyIds: string[] }) => http.post<any>(`/admin/crm/vendor-demand-items/${demandItemId}/match-opportunities`, data),
};

export default crmMatchOpportunityApi;
