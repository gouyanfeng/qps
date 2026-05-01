import http from "@/api";

/**
 * @description 套餐管理模块
 */
export const planApi = {
  // 获取套餐列表
  getPlanList: (params: any) => {
    return http.get<any>("/admin/plans", params);
  },
  // 获取套餐详情
  getPlanDetail: (id: string) => {
    return http.get<any>(`/admin/plans/${id}`);
  },
  // 新增套餐
  addPlan: (params: any) => {
    return http.post<any>("/admin/plans", params);
  },
  // 更新套餐
  updatePlan: (id: string, params: any) => {
    return http.put<any>(`/admin/plans/${id}`, params);
  },
  // 删除套餐
  deletePlan: (id: string) => {
    return http.delete(`/admin/plans/${id}`);
  },
  // 切换套餐状态
  togglePlanStatus: (id: string, isActive: boolean) => {
    return http.post<any>(`/admin/plans/${id}/toggle-status`, isActive);
  },
};

export default planApi;
