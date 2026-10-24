import http from "@/api";

/**
 * @description 商场商品分类模块
 */
export const mallCategoryApi = {
  // 分页查询分类
  getCategoryList: (params: any) => {
    return http.get<any>("/admin/mall/categories", params);
  },
  // 获取分类详情
  getCategory: (id: string) => {
    return http.get<any>(`/admin/mall/categories/${id}`);
  },
  // 新增分类
  addCategory: (params: any) => {
    return http.post<any>("/admin/mall/categories", params);
  },
  // 编辑分类
  updateCategory: (id: string, params: any) => {
    return http.put<any>(`/admin/mall/categories/${id}`, params);
  },
  // 分类启用停用
  changeCategoryStatus: (id: string, isActive: boolean) => {
    return http.patch<any>(`/admin/mall/categories/${id}/status`, { isActive });
  },
  // 删除分类
  deleteCategory: (id: string) => {
    return http.delete(`/admin/mall/categories/${id}`);
  }
};

export default mallCategoryApi;
