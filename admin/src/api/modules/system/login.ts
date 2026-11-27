import { Login } from "@/api/interface/index";
import authMenuList from "@/assets/json/authMenuList.json";
import http from "@/api";

export const loginApi = (params: Login.ReqLoginForm) => {
  return http.post<Login.ResLogin>("admin/auth/login", params, {
    loading: false,
  });
};

export const getAuthMenuListApi = () => {
  return authMenuList;
};

/**
 * 用户权限实时从后端拉取，不做本地缓存。
 * 权限是强敏感且会变动的数据，缓存会导致「后端已授权但前端看不到」、
 * 退出重登也不生效（旧缓存命中）等问题。接口很轻，实时拉取更可靠。
 * 同时清理历史版本缓存，避免遗留脏数据。
 */
export const getUserPermissionsApi = async (): Promise<string[]> => {
  clearUserPermissionsCache();
  const { data } = await http.get<{ permissions: string[] }>("/admin/auth/user-permissions", {}, { loading: false, cancel: false });
  return data.permissions || [];
};

export const clearUserPermissionsCache = (_userId?: string) => {
  Object.keys(window.localStorage)
    .filter(key => key.indexOf("qps-user-permissions") === 0)
    .forEach(key => window.localStorage.removeItem(key));
};

export const logoutApi = () => {
  return http.post("admin/auth/logout", {});
};

export const changePasswordApi = (oldPassword: string, newPassword: string) => {
  return http.post<boolean>("admin/auth/change-password", {
    oldPassword,
    newPassword
  });
};