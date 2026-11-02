import { Theme } from "@/hooks/interface";

export const menuTheme: Record<Theme.ThemeType, { [key: string]: string }> = {
  light: {
    "--el-menu-bg-color": "#10152a",
    "--el-menu-hover-bg-color": "#1b2138",
    "--el-menu-active-bg-color": "#232b46",
    "--el-menu-text-color": "#c9d0e4",
    "--el-menu-active-color": "#ffffff",
    "--el-menu-hover-text-color": "#ffffff",
    "--el-menu-horizontal-sub-item-height": "50px"
  },
  inverted: {
    "--el-menu-bg-color": "#191a20",
    "--el-menu-hover-bg-color": "#000000",
    "--el-menu-active-bg-color": "#000000",
    "--el-menu-text-color": "#bdbdc0",
    "--el-menu-active-color": "#ffffff",
    "--el-menu-hover-text-color": "#ffffff",
    "--el-menu-horizontal-sub-item-height": "50px"
  },
  dark: {
    "--el-menu-bg-color": "#141414",
    "--el-menu-hover-bg-color": "#000000",
    "--el-menu-active-bg-color": "#000000",
    "--el-menu-text-color": "#bdbdc0",
    "--el-menu-active-color": "#ffffff",
    "--el-menu-hover-text-color": "#ffffff",
    "--el-menu-horizontal-sub-item-height": "50px"
  }
};


