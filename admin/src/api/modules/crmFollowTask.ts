import http from "@/api";

export const crmFollowTaskApi = {
  getList: (params: any) => http.get<any>("/admin/crm/follow-tasks", params)
};
