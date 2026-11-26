/**
 * ECharts 在 canvas 中无法解析 CSS 变量（var(--el-*)），
 * 因此从文档根节点的计算样式里读取主题色，供图表配置使用。
 * 主题切换后需重新进入页面或刷新才会更新（与图表生命周期一致）。
 */

const DEFAULT_THEME = {
  textColor: "#303133",
  textSecondary: "#909399",
  axisLine: "#ebeef5",
  splitLine: "#ebeef5",
  tooltipBg: "#ffffff",
  palette: ["#409eff", "#67c23a", "#e6a23c", "#f56c6c", "#909399", "#9b59b6", "#17a2b8", "#d4a259"]
};

const readVar = (name: string, fallback: string): string => {
  if (typeof window === "undefined") return fallback;
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return value || fallback;
};

export interface ChartTheme {
  textColor: string;
  textSecondary: string;
  axisLine: string;
  splitLine: string;
  tooltipBg: string;
  palette: string[];
}

/** 读取当前 Element Plus 主题下的图表配色（每次调用都读，便于主题切换后重算）。 */
export function getChartTheme(): ChartTheme {
  return {
    textColor: readVar("--el-text-color-primary", DEFAULT_THEME.textColor),
    textSecondary: readVar("--el-text-color-regular", DEFAULT_THEME.textSecondary),
    axisLine: readVar("--el-border-color-light", DEFAULT_THEME.axisLine),
    splitLine: readVar("--el-border-color-lighter", DEFAULT_THEME.splitLine),
    tooltipBg: readVar("--el-bg-color-overlay", DEFAULT_THEME.tooltipBg),
    palette: DEFAULT_THEME.palette
  };
}

/** 金额格式化：¥ + 千分位 + 两位小数。 */
export function formatAmount(value: number): string {
  return `¥${Number(value || 0).toFixed(2)}`;
}

/** 日期格式化：MM-DD。 */
export function formatDay(date: string): string {
  const parsed = new Date(date);
  if (Number.isNaN(parsed.getTime())) return date;
  return `${String(parsed.getMonth() + 1).padStart(2, "0")}-${String(parsed.getDate()).padStart(2, "0")}`;
}
