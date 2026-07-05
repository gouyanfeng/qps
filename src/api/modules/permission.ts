import http from "@/api";
import { Permission } from "@/api/interface/index";

/**
 * @description 权限管理模块
 */
export const permissionApi = {
  // 获取权限树定义
  getPermissionTree: () => {
    return http.get<Permission.TreeNode[]>("/admin/permissions/tree");
  },
  // 获取所有角色的权限配置
  getPermissionList: () => {
    return http.get("/admin/permissions");
  },
  // 获取单个角色的权限
  getRolePermission: (role: string) => {
    return http.get(`/admin/permissions/${role}`);
  },
  // 更新角色权限
  updateRolePermission: (params: Permission.ReqRolePermission) => {
    return http.put<any>("/admin/permissions", params);
  },
};

export default permissionApi;
