import http from "@/api";

/**
 * @description 商场商品与规格模块
 */
export const mallProductApi = {
  // 分页查询商品
  getProductList: (params: any) => {
    return http.get<any>("/admin/mall/products", params);
  },
  // 获取商品详情（含规格）
  getProduct: (id: string) => {
    return http.get<any>(`/admin/mall/products/${id}`);
  },
  // 新增商品
  addProduct: (params: any) => {
    return http.post<any>("/admin/mall/products", params);
  },
  // 编辑商品
  updateProduct: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/products/${id}`, params);
  },
  // 商品上下架
  changeProductStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/products/${id}/status`, { isActive });
  },
  // 删除商品
  deleteProduct: (id: string) => {
    return http.delete(`/admin/mall/products/${id}`);
  },
  // 新增规格
  addSku: (productId: string, params: any) => {
    return http.post<any>(`/admin/mall/products/${productId}/skus`, params);
  },
  // 编辑规格
  updateSku: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/skus/${id}`, params);
  },
  // 规格启用停用
  changeSkuStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/skus/${id}/status`, { isActive });
  },
  // 删除规格
  deleteSku: (id: string) => {
    return http.delete(`/admin/mall/skus/${id}`);
  }
};

export default mallProductApi;
