import { Login } from "@/api/interface/index";
import authMenuList from "@/assets/json/authMenuList.json";
import http from "@/api";
import { cacheGet, cacheSet } from "@/utils";

export const loginApi = (params: Login.ReqLoginForm) => {
  return http.post<Login.ResLogin>("admin/auth/login", params, {
    loading: false,
  });
};

export const getAuthMenuListApi = () => {
  return authMenuList;
};

const getUserPermissionsCacheKey = (userId: string) => `qps-user-permissions-v4-${userId || "anonymous"}`;

export const getUserPermissionsApi = async (): Promise<string[]> => {
  const { useUserStore } = await import("@/stores/modules/user");
  const userStore = useUserStore();
  const userId = userStore.userInfo.userId || "anonymous";
  const cacheKey = getUserPermissionsCacheKey(userId);
  const ttlMinutes = 60;

  const cached = cacheGet(cacheKey, ttlMinutes) as string[] | null;
  if (cached) {
    return cached;
  }

  const { data } = await http.get<{ permissions: string[] }>("/admin/auth/user-permissions", {}, { loading: false, cancel: false });
  const permissions = data.permissions || [];
  cacheSet(cacheKey, permissions, ttlMinutes);

  return permissions;
};

export const clearUserPermissionsCache = (userId: string) => {
  window.localStorage.removeItem(getUserPermissionsCacheKey(userId));
};

export const logoutApi = () => {
  return http.post("admin/auth/logout");
};

export const changePasswordApi = (oldPassword: string, newPassword: string) => {
  return http.post<boolean>("admin/auth/change-password", {
    oldPassword,
    newPassword
  });
};


