import { defineStore } from "pinia";
import { AuthState } from "@/stores/interface";
import { getAuthButtonListApi, getAuthMenuListApi, getUserPermissionsApi } from "@/api/modules/login";
import { getFlatMenuList, getShowMenuList, getAllBreadcrumbList } from "@/utils";

export const useAuthStore = defineStore("qps-auth",{
  state: (): AuthState => ({
    // 按钮权限列表
    authButtonList: {},
    // 菜单权限列表
    authMenuList: [],
    // 当前登录用户权限代码列表
    userPermissions: []
  }),
  getters: {
    // 按钮权限列表
    authButtonListGet: state => state.authButtonList,
    // 菜单权限列表 ==> 这里的菜单没有经过任何处理
    authMenuListGet: state => state.authMenuList,
    // 菜单权限列表 ==> 左侧菜单栏渲染，剔除 isHide + 按 permissionCode 过滤
    showMenuListGet: state => getShowMenuList(state.authMenuList, state.userPermissions),
    // 菜单权限列表 ==> 扁平化之后的一维数组菜单，主要用来添加动态路由
    flatMenuListGet: state => getFlatMenuList(state.authMenuList),
    // 递归处理后的所有面包屑导航列表
    breadcrumbListGet: state => getAllBreadcrumbList(state.authMenuList)
  },
  actions: {
    // 获取当前登录用户所有权限（含缓存）
    async getUserPermissions() {
      const perms = await getUserPermissionsApi();
      this.userPermissions = perms;
      return perms;
    },
    // Get AuthButtonList
    async getAuthButtonList() {
      const { data } = await getAuthButtonListApi();
      this.authButtonList = data;
    },
    // Get AuthMenuList
    async getAuthMenuList() {
      const { data } = await getAuthMenuListApi();
      this.authMenuList = data;
    }
  }
});