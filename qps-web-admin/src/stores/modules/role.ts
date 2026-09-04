import { defineStore } from "pinia";
import { RoleType } from "@/stores/interface";

const defaultRoles: RoleType[] = [
  { id: "1", name: "Administrator", code: "admin" },
  { id: "2", name: "User", code: "user" },
];

export const useRoleStore = defineStore("qps-role", {
  state: () => ({
    roles: defaultRoles,
  }),
  getters: {
    rolesGet: (state) => state.roles,
  },
  actions: {
    async getRoles() {
      return this.roles;
    },
    addRole(role: RoleType) {
      this.roles.push(role);
    },
    removeRole(code: string) {
      this.roles = this.roles.filter((role) => role.code !== code);
    },
    updateRole(role: RoleType) {
      const index = this.roles.findIndex((r) => r.code === role.code);
      if (index !== -1) {
        this.roles[index] = role;
      }
    },
  },
});


