import { Theme } from "@/hooks/interface";

export const asideTheme: Record<Theme.ThemeType, { [key: string]: string }> = {
  light: {
    "--el-aside-logo-text-color": "#f5f7ff",
    "--el-aside-border-color": "#e4e7ed"
  },
  inverted: {
    "--el-aside-logo-text-color": "#dadada",
    "--el-aside-border-color": "#414243"
  },
  dark: {
    "--el-aside-logo-text-color": "#dadada",
    "--el-aside-border-color": "#414243"
  }
};


