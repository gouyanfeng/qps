import http from "@/api";
import permissionList from "@/assets/json/permissionList.json";
import permissionTree from "@/assets/json/permissionTree.json";

/**
 * @description 权限管理模块
 */
export const permissionApi = {
  // 获取权限树定义
  getPermissionTree: () => {
    // return http.get<Permission.TreeNode[]>("/admin/permissions/tree");
    return permissionTree;
  },
  // 获取所有角色的权限配置
  getPermissionList: () => {
    // return http.get<Permission.RolePermission[]>("/admin/permissions");
    return permissionList;
  },
  // 获取单个角色的权限
  getRolePermission: (role: string) => {
    // return http.get<Permission.RolePermission>(`/admin/permissions/${role}`);
    const data = permissionList.data as any;
    return { code: 200, data: data[role] || { permissions: [] }, msg: "成功" };
  },
  // 更新角色权限
  updateRolePermission: (params: Permission.ReqRolePermission) => {
    return http.put<any>("/admin/permissions", params);
  },
};

export default permissionApi;
