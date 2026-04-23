import { Merchant } from "@/api/interface/index";
import http from "@/api";

/**
 * @description 商户管理模块
 */
export const merchantApi = {
  // 获取商户列表
  getMerchantList: (params: Merchant.ReqMerchantParams) => {
    return http.get<Merchant.ResMerchantPagination>("/admin/merchants", params);
  },
  // 获取商户详情
  getMerchantDetail: (id: string) => {
    return http.get<Merchant.ResMerchantList>(`/admin/merchants/${id}`);
  },
  // 新增商户
  addMerchant: (params: Merchant.ReqMerchantForm) => {
    return http.post<Merchant.ResMerchantList>("/admin/merchants", params);
  },
  // 更新商户
  updateMerchant: (id: string, params: Merchant.ReqMerchantUpdate) => {
    return http.put<Merchant.ResMerchantList>(`/admin/merchants/${id}`, params);
  },
  // 删除商户
  deleteMerchant: (id: string) => {
    return http.delete(`/admin/merchants/${id}`);
  },
};

export default merchantApi;
