import { defineStore } from "pinia";
import { RoleType } from "@/stores/interface";

// 默认角色列表
const defaultRoles: RoleType[] = [
  { id: "1", name: "管理员", code: "admin" },
  { id: "2", name: "商户", code: "merchant" },
  { id: "3", name: "用户", code: "user" },
];

export const useRoleStore = defineStore("qps-role", {
  state: () => ({
    roles: defaultRoles,
  }),
  getters: {
    rolesGet: (state) => state.roles,
  },
  actions: {
    // 获取角色列表
    async getRoles() {
      // 这里可以从 API 获取角色列表
      // const { data } = await getRolesApi();
      // this.roles = data;
      // 暂时使用默认角色列表
      return this.roles;
    },
    // 添加角色
    addRole(role: RoleType) {
      this.roles.push(role);
    },
    // 删除角色
    removeRole(code: string) {
      this.roles = this.roles.filter((role) => role.code !== code);
    },
    // 更新角色
    updateRole(role: RoleType) {
      const index = this.roles.findIndex((r) => r.code === role.code);
      if (index !== -1) {
        this.roles[index] = role;
      }
    },
  },
});
