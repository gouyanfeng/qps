import { computed } from "vue";
import { useRoute } from "vue-router";
import { useAuthStore } from "@/stores/modules/auth";

interface AuthButtonItem {
  action: string;
  permissionCode?: string;
}

/**
 * @description 页面按钮权限
 * */
export const useAuthButtons = () => {
  const route = useRoute();
  const authStore = useAuthStore();
  const authButtons: AuthButtonItem[] = authStore.authButtonListGet[route.name as string] || [];
  const userPermissions = authStore.userPermissions;

  const BUTTONS = computed(() => {
    let currentPageAuthButton: { [key: string]: boolean } = {};
    authButtons.forEach(item => {
      // 未设置 permissionCode → 默认显示（兼容旧数据）
      if (!item.permissionCode) {
        currentPageAuthButton[item.action] = true;
        return;
      }
      // 有 permissionCode → 检查用户是否拥有该权限
      currentPageAuthButton[item.action] = userPermissions.includes(item.permissionCode);
    });
    return currentPageAuthButton;
  });

  return {
    BUTTONS
  };
};
