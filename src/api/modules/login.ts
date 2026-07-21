import { Login } from "@/api/interface/index";
import authMenuList from "@/assets/json/authMenuList.json";
import authButtonList from "@/assets/json/authButtonList.json";
import http from "@/api";
import { cacheGet, cacheSet } from "@/utils";

/**
 * @name 登录模块
 */
// 用户登录
export const loginApi = (params: Login.ReqLoginForm) => {
  return http.post<Login.ResLogin>("admin/auth/login", params, {
    loading: false,
  });
};

// 获取菜单列表
export const getAuthMenuListApi = () => {
  return authMenuList;
};

// 获取按钮权限
export const getAuthButtonListApi = () => {
  return authButtonList;
};

// 获取当前登录用户权限代码列表（带 1 小时 localStorage 缓存）
export const getUserPermissionsApi = async (): Promise<string[]> => {
  const { useUserStore } = await import("@/stores/modules/user");
  const userId = useUserStore().userInfo.userId || "anonymous";
  const CACHE_KEY = `qps-user-permissions-${userId}`;
  const TTL = 60; // 1 小时

  // 检查缓存
  const cached = cacheGet<string[]>(CACHE_KEY, TTL);
  if (cached) return cached;

  // 调后端 API
  const res = await http.get<any>("/admin/auth/user-permissions");
  const data = (res as any)?.data?.permissions || (res as any)?.permissions || [];

  // 写入缓存
  cacheSet(CACHE_KEY, data, TTL);

  return data;
};

// 用户退出登录
export const logoutApi = () => {
  return http.post("admin/auth/logout");
};
