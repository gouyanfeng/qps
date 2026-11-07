/**
 * 商品标签颜色解析。
 *
 * 标签颜色统一以十六进制存储，这里把 3 位、6 位、8 位十六进制规整为可直接用于样式的值。
 */

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

/** 解析标签颜色：空值或非法值回退主题绿。 */
export function resolveTagColor(color?: string | null): string {
  const raw = (color || '').trim()
  if (!raw.startsWith('#')) return DEFAULT_COLOR
  return normalizeHex(raw)
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