import http from "@/api";

/**
 * @description 商场商品评价模块
 */
export const mallReviewApi = {
  // 分页查询评价
  getReviewList: (params: any) => {
    return http.get<any>("/admin/mall/reviews", params);
  },
  // 审核状态变更（0待审核 / 1已通过 / 2已隐藏）
  changeReviewStatus: (id: string, auditStatus: number) => {
    return http.patch<any>(`/admin/mall/reviews/${id}/status`, { auditStatus });
  },
  // 回复评价
  replyReview: (id: string, replyContent: string) => {
    return http.put<any>(`/admin/mall/reviews/${id}/reply`, { replyContent });
  },
  // 详情展示设置（仅已通过评价可设置）
  changeReviewFeatured: (id: string, params: { isFeatured: boolean; sortOrder: number }) => {
    return http.patch<any>(`/admin/mall/reviews/${id}/featured`, params);
  },
  // 删除评价
  deleteReview: (id: string) => {
    return http.delete(`/admin/mall/reviews/${id}`);
  }
};

export default mallReviewApi;
