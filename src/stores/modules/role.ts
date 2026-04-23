import { defineStore } from "pinia";
import { RoleType } from "@/stores/interface";

// 默认角色列表
const defaultRoles: RoleType[] = [
  { label: "管理员", value: "admin" },
  { label: "商户", value: "merchant" },
  { label: "用户", value: "user" },
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
    removeRole(value: string) {
      this.roles = this.roles.filter((role) => role.value !== value);
    },
    // 更新角色
    updateRole(role: RoleType) {
      const index = this.roles.findIndex((r) => r.value === role.value);
      if (index !== -1) {
        this.roles[index] = role;
      }
    },
  },
});
