import http from "@/api";

/**
 * @description 活动专题（商品推荐型）管理模块
 */
export const mallCampaignApi = {
  // 分页查询活动专题
  getCampaignList: (params: any) => {
    return http.get<any>("/admin/mall/campaigns", params);
  },
  // 查询领券型活动可选券
  getCouponOptions: (params: any) => {
    return http.get<any>("/admin/mall/campaigns/coupons", params);
  },
  // 查询活动专题详情（含选品）
  getCampaign: (id: string) => {
    return http.get<any>(`/admin/mall/campaigns/${id}`);
  },
  // 新增活动专题
  addCampaign: (params: any) => {
    return http.post<any>("/admin/mall/campaigns", params);
  },
  // 编辑活动专题
  updateCampaign: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/campaigns/${id}`, params);
  },
  // 活动专题上下架
  changeCampaignStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/campaigns/${id}/status`, { isActive });
  },
  // 删除活动专题
  deleteCampaign: (id: string) => {
    return http.delete(`/admin/mall/campaigns/${id}`);
  }
};

export default mallCampaignApi;
