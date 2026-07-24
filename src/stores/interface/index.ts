import { Login } from "@/api/interface";

export type LayoutType = "vertical" | "classic" | "transverse" | "columns";

export type AssemblySizeType = "large" | "default" | "small";

export type LanguageType = "zh" | "en" | null;

/* GlobalState */
export interface GlobalState {
  layout: LayoutType;
  assemblySize: AssemblySizeType;
  language: LanguageType;
  maximize: boolean;
  primary: string;
  isDark: boolean;
  isGrey: boolean;
  isWeak: boolean;
  asideInverted: boolean;
  headerInverted: boolean;
  isCollapse: boolean;
  accordion: boolean;
  watermark: boolean;
  breadcrumb: boolean;
  breadcrumbIcon: boolean;
  tabs: boolean;
  tabsIcon: boolean;
  footer: boolean;
}

/* UserState */
export interface UserState {
  token: string;
  userInfo: { name: string; userId: string; role?: string };
}

/* tabsMenuProps */
export interface TabsMenuProps {
  icon: string;
  title: string;
  path: string;
  name: string;
  close: boolean;
  isKeepAlive: boolean;
}

/* TabsState */
export interface TabsState {
  tabsMenuList: TabsMenuProps[];
}

/* RoleType */
export interface RoleType {
  id: string;
  name: string;
  code: string;
}

/* AuthState */
export interface AuthState {
  authButtonList: {
    [key: string]: Login.AuthButtonItem[];
  };
  authMenuList: Menu.MenuOptions[];
  userPermissions: string[];
}

/* KeepAliveState */
export interface KeepAliveState {
  keepAliveName: string[];
}
