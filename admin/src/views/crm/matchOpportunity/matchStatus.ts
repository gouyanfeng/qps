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

export const candidateStatusLabel = (status?: string | null) =>
  CANDIDATE_STATUS_LABELS[status || ""] || status || "-";

export const candidateStatusTagType = (status?: string | null) =>
  TAG_TYPES[candidateStatusLabel(status)] || "";
