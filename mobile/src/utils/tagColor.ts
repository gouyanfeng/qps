/**
 * 商品标签颜色解析。
 *
 * 后台历史数据里颜色可能是 Element 色名（success / warning 等），
 * 也可能已经是十六进制；统一转成可直接用于样式的十六进制值。
 */

const COLOR_ALIAS: Record<string, string> = {
  primary: '#2E7D32',
  success: '#4CAF50',
  warning: '#E6A23C',
  danger: '#F44336',
  error: '#F44336',
  info: '#909399'
}

const DEFAULT_COLOR = '#2E7D32'

/** 将 3 位、6 位、8 位十六进制统一成 6 位，非法值返回默认色。 */
function normalizeHex(value: string): string {
  if (/^#[0-9a-f]{6}$/i.test(value)) return value
  if (/^#[0-9a-f]{8}$/i.test(value)) return value.slice(0, 7)
  if (/^#[0-9a-f]{3}$/i.test(value)) {
    const [r, g, b] = [value[1], value[2], value[3]]
    return `#${r}${r}${g}${g}${b}${b}`
  }
  return DEFAULT_COLOR
}

/** 解析标签颜色：支持色名与十六进制，空值/未知值回退主题绿。 */
export function resolveTagColor(color?: string | null): string {
  const raw = (color || '').trim()
  if (!raw) return DEFAULT_COLOR

  const alias = COLOR_ALIAS[raw.toLowerCase()]
  if (alias) return alias

  if (raw.startsWith('#')) return normalizeHex(raw)

  return DEFAULT_COLOR
}

/**
 * 标签胶囊样式：文字与边框用标签色，背景用同色 8% 透明度。
 * 直接绑定到标签元素即可覆盖默认的写死样式。
 */
export function tagChipStyle(color?: string | null) {
  const resolved = resolveTagColor(color)
  return {
    color: resolved,
    borderColor: resolved,
    backgroundColor: `${resolved}14`
  }
}
