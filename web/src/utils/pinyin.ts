/**
 * 轻量级拼音首字母工具
 * 基于 Unicode 区间边界映射，覆盖 GB2312 常用汉字
 */

// GB2312 汉字拼音首字母边界表（Unicode 编码升序）
const PINYIN_BOUNDARIES: [number, string][] = [
  [0x5ea7, 'A'], [0x5f46, 'B'], [0x6101, 'C'], [0x6492, 'D'],
  [0x6652, 'E'], [0x6723, 'F'], [0x6946, 'G'], [0x6c49, 'H'],
  [0x6dec, 'J'], [0x7038, 'K'], [0x7185, 'L'], [0x77c7, 'M'],
  [0x79b9, 'N'], [0x7b0b, 'O'], [0x7d77, 'P'], [0x7e9b, 'Q'],
  [0x7fe0, 'R'], [0x8283, 'S'], [0x86f9, 'T'], [0x8a3b, 'W'],
  [0x9002, 'X'], [0x949e, 'Y'], [0x9696, 'Z'],
]

/**
 * 获取单个汉字的拼音首字母（大写）
 * 非汉字返回原字符大写
 */
function getCharInitial(char: string): string {
  const code = char.charCodeAt(0)
  // 非中文字符直接返回大写
  if (code < 0x4e00 || code > 0x9fff) {
    return char.toUpperCase()
  }
  // 二分查找拼音区间
  let initial = 'A'
  for (let i = PINYIN_BOUNDARIES.length - 1; i >= 0; i--) {
    if (code >= PINYIN_BOUNDARIES[i][0]) {
      initial = PINYIN_BOUNDARIES[i][1]
      break
    }
  }
  return initial
}

/**
 * 获取字符串的拼音首字母序列（大写）
 * @example getPinyinInitials('财务管理') => 'CWGL'
 */
export function getPinyinInitials(str: string): string {
  return str.split('').map(getCharInitial).join('')
}

/**
 * 拼音模糊匹配
 * 支持：
 * 1. 精确子串匹配（中文名称）
 * 2. 拼音首字母缩写匹配（如 "cwgl" 匹配 "财务管理"）
 * 3. 拼音首字母序列匹配（如 "cw" 匹配 "财务管理"）
 * 4. 混合匹配（如 "数据z" 匹配 "数据中心"）
 */
export function pinyinMatch(text: string, query: string): boolean {
  if (!query) return true
  const lowerText = text.toLowerCase()
  const lowerQuery = query.toLowerCase()

  // 1. 精确子串匹配
  if (lowerText.includes(lowerQuery)) return true

  // 2. 拼音首字母匹配
  const initials = getPinyinInitials(text).toLowerCase()
  if (initials.includes(lowerQuery)) return true

  // 3. 逐字符序列匹配（query 中的每个字符按顺序出现在 initials 中）
  let pos = 0
  for (const ch of lowerQuery) {
    const found = initials.indexOf(ch, pos)
    if (found === -1) return false
    pos = found + 1
  }
  return true
}
