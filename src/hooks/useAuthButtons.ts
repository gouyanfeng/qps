import { computed } from "vue";
import { useRoute } from "vue-router";
import { useAuthStore } from "@/stores/modules/auth";

/**
 * @description 页面按钮权限
 * */
export const useAuthButtons = () => {
  const route = useRoute();
  const authStore = useAuthStore();
  const actions = ["add", "edit", "delete", "assign"];

  const BUTTONS = computed(() => {
    let currentPageAuthButton: { [key: string]: boolean } = {};
    const pagePermissionCode = route.meta?.permissionCode as string | undefined;

    actions.forEach(action => {
      currentPageAuthButton[action] = pagePermissionCode
        ? authStore.userPermissions.includes(`${pagePermissionCode}_${action.toUpperCase()}`)
        : true;
    });
    return currentPageAuthButton;
  });

  return {
    BUTTONS
  };
};


