import { defineStore } from "pinia";
import { AuthState } from "@/stores/interface";
import { getAuthMenuListApi, getUserPermissionsApi } from "@/api/modules/system/login";
import { getFlatMenuList, getShowMenuList, getMenuPermissionMap, getAllBreadcrumbList } from "@/utils";

export const useAuthStore = defineStore("qps-auth",{
  state: (): AuthState => ({
    // 菜单权限列表
    authMenuList: [],
    // 当前登录用户权限代码列表
    userPermissions: []
  }),
  getters: {
    // 菜单权限列表 ==> 这里的菜单没有经过任何处理
    authMenuListGet: state => state.authMenuList,
    // 菜单权限列表 ==> 左侧菜单栏渲染，剔除 isHide + 按 permissionCode 过滤
    showMenuListGet: state => getShowMenuList(state.authMenuList, state.userPermissions),
    // 菜单权限列表 ==> 扁平化之后的一维数组菜单（已按权限过滤），用来添加动态路由
    flatMenuListGet: state => getFlatMenuList(getShowMenuList(state.authMenuList, state.userPermissions)),
    // 当前用户可见菜单路径
    visibleRoutePathsGet: state =>
      getFlatMenuList(getShowMenuList(state.authMenuList, state.userPermissions)).map(item => item.path),
    // 菜单路径 -> 需要具备的权限码链（含父级模块）
    routePermissionMapGet: state => getMenuPermissionMap(state.authMenuList),
    // 判断当前用户是否具备某个权限码
    hasPermissionGet: state => (code: string) => state.userPermissions.includes(code),
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
    // Get AuthMenuList
    async getAuthMenuList() {
      const { data } = await getAuthMenuListApi();
      this.authMenuList = data;
    }
  }
});


