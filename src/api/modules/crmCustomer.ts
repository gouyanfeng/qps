import http from "@/api";

/**
 * @description 客户管理模块
 */
export const crmCustomerApi = {
  // 获取客户列表
  getCustomerList: (params: any) => {
    return http.get<any>("/admin/crm/customers", params);
  },
  // 获取客户详情
  getCustomer: (id: string) => {
    return http.get<any>(`/admin/crm/customers/${id}`);
  },
  // 创建客户
  createCustomer: (params: any) => {
    return http.post<any>("/admin/crm/customers", params);
  },
  // 更新客户
  updateCustomer: (id: string, params: any) => {
    return http.put<any>(`/admin/crm/customers/${id}`, params);
  },
  // 删除客户
  deleteCustomer: (id: string) => {
    return http.delete(`/admin/crm/customers/${id}`);
  },
};

export default crmCustomerApi;
