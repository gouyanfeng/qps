import http from "@/api";

/**
 * @description 商城活动管理模块
 */
export const mallActivityApi = {
  // 查询活动列表
  getActivityList: (params: any) => {
    return http.get<any>("/admin/mall/activities", params);
  },
  // 查询活动详情
  getActivity: (id: string) => {
    return http.get<any>(`/admin/mall/activities/${id}`);
  },
  // 新增活动
  addActivity: (params: any) => {
    return http.post<any>("/admin/mall/activities", params);
  },
  // 编辑活动
  updateActivity: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/activities/${id}`, params);
  },
  // 活动启用停用
  changeActivityStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/activities/${id}/status`, { isActive });
  },
  // 删除活动
  deleteActivity: (id: string) => {
    return http.delete(`/admin/mall/activities/${id}`);
  }
};

export default mallActivityApi;
