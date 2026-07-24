import { Login } from "@/api/interface/index";
import authMenuList from "@/assets/json/authMenuList.json";
import authButtonList from "@/assets/json/authButtonList.json";
import permissionList from "@/assets/json/permissionList.json";
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

export const getAuthButtonListApi = () => {
  return authButtonList;
};

export const getUserPermissionsApi = async (): Promise<string[]> => {
  const { useUserStore } = await import("@/stores/modules/user");
  const userStore = useUserStore();
  const userId = userStore.userInfo.userId || "anonymous";
  const role = userStore.userInfo.role || "user";
  const cacheKey = `qps-user-permissions-${userId}`;
  const ttlMinutes = 60;

  const cached = cacheGet(cacheKey, ttlMinutes) as string[] | null;
  if (cached) {
    return cached;
  }

  const permissionsByRole = permissionList.data as Record<string, { permissions: string[] }>;
  const permissions = permissionsByRole[role]?.permissions || permissionsByRole.user.permissions;

  cacheSet(cacheKey, permissions, ttlMinutes);

  return permissions;
};

export const logoutApi = () => {
  return http.post("admin/auth/logout");
};
