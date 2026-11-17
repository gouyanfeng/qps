// 候选状态归一：基地候选存中文，药厂候选存英文枚举，展示与配色统一走这里
export const CANDIDATE_STATUS_LABELS: Record<string, string> = {
  保留: "保留",
  kept: "保留",
  中选: "中选",
  selected: "中选",
  淘汰: "淘汰",
  eliminated: "淘汰",
  待评估: "待评估",
  pending: "待评估"
};

const TAG_TYPES: Record<string, string> = {
  保留: "success",
  中选: "primary",
  淘汰: "danger",
  待评估: "warning"
};

const COLORS: Record<string, string> = {
  保留: "var(--el-color-success)",
  中选: "var(--el-color-primary)",
  淘汰: "var(--el-color-danger)",
  待评估: "var(--el-color-warning)"
};

export const candidateStatusLabel = (status?: string | null) =>
  CANDIDATE_STATUS_LABELS[status || ""] || status || "-";

export const candidateStatusTagType = (status?: string | null) =>
  TAG_TYPES[candidateStatusLabel(status)] || "";

export const candidateStatusColor = (status?: string | null) =>
  COLORS[candidateStatusLabel(status)] || "var(--el-border-color-lighter)";

// 发起方卡片按撮合机会整体状态上色：推进中绿、暂停橙、已关闭灰/成交绿
export const opportunityStatusColor = (status?: string | null, matched = false) => {
  if (status === "已关闭") return matched ? "var(--el-color-success)" : "var(--el-color-info)";
  return ({ 推进中: "var(--el-color-success)", 暂停: "var(--el-color-warning)" } as Record<string, string>)[status || ""]
    || "var(--el-border-color-lighter)";
};
