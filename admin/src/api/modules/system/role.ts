import http from "@/api";

/**
 * @description 角色管理模块
 */
export const roleApi = {
  // 获取角色列表
  getRoleList: (params: any) => {
    return http.get<any>("/admin/roles", params);
  },
  // 新增角色
  addRole: (params: any) => {
    return http.post<any>("/admin/roles", params);
  },
  // 更新角色
  updateRole: (value: string, params: any) => {
    return http.put<any>(`/admin/roles/${value}`, params);
  },
  // 删除角色
  deleteRole: (value: string) => {
    return http.delete(`/admin/roles/${value}`);
  },
};

export default roleApi;


