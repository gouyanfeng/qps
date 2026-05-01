import http from "@/api";

/**
 * @description 优惠券管理模块
 */
export const couponApi = {
  // 获取优惠券列表
  getCouponList: (params: any) => {
    return http.get<any>("/admin/coupons", params);
  },
  // 获取优惠券详情
  getCouponDetail: (id: string) => {
    return http.get<any>(`/admin/coupons/${id}`);
  },
  // 新增优惠券
  addCoupon: (params: any) => {
    return http.post<any>("/admin/coupons", params);
  },
  // 更新优惠券
  updateCoupon: (id: string, params: any) => {
    return http.put<any>(`/admin/coupons/${id}`, params);
  },
  // 删除优惠券
  deleteCoupon: (id: string) => {
    return http.delete(`/admin/coupons/${id}`);
  },
};

export default couponApi;
