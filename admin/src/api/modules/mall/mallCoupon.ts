import http from "@/api";

/**
 * @description 商场优惠券管理模块
 */
export const mallCouponApi = {
  // 分页查询优惠券
  getCouponList: (params: any) => {
    return http.get<any>("/admin/mall/coupons", params);
  },
  // 查询优惠券详情
  getCoupon: (id: string) => {
    return http.get<any>(`/admin/mall/coupons/${id}`);
  },
  // 新增优惠券
  addCoupon: (params: any) => {
    return http.post<any>("/admin/mall/coupons", params);
  },
  // 编辑优惠券
  updateCoupon: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/coupons/${id}`, params);
  },
  // 启用/停用
  changeCouponStatus: (id: string, isActive: boolean) => {
    return http.put<any>(`/admin/mall/coupons/${id}/status`, null, { params: { isActive } });
  },
  // 补库存
  replenishCoupon: (id: string, count: number) => {
    return http.put<any>(`/admin/mall/coupons/${id}/replenish`, null, { params: { count } });
  },
  // 删除优惠券
  deleteCoupon: (id: string) => {
    return http.delete(`/admin/mall/coupons/${id}`);
  }
};

export default mallCouponApi;
