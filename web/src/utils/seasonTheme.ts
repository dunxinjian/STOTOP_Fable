/**
 * 季节主题配置
 * 根据当前月份返回对应季节的视觉主题：
 *  - 春（spring） 3-5月：嫩绿 + 品牌橙点缀
 *  - 夏（summer） 6-8月：暖阳金色
 *  - 秋（autumn） 9-11月：金橙暖色（契合申通品牌橙）
 *  - 冬（winter） 12,1,2月：冷蓝银灰 + 品牌橙点缀
 */

export type Season = 'spring' | 'summer' | 'autumn' | 'winter'

export interface SeasonTheme {
  season: Season
  /** 背景渐变三色（用于 linear-gradient(145deg, ...)） */
  bgGradient: [string, string, string]
  /** 右上装饰光晕色（::before radial-gradient） */
  glowPrimary: string
  /** 左下装饰光晕色（::after radial-gradient） */
  glowSecondary: string
  /** 标语 */
  tagline: string
  /** 功能亮点小圆点的渐变色 */
  dotGradient: [string, string]
  /** 粒子动画 CSS 类名 */
  particleClass: string
}

const SEASON_THEMES: Record<Season, SeasonTheme> = {
  spring: {
    season: 'spring',
    bgGradient: ['#1B2A24', '#22332B', '#16201C'],
    glowPrimary: 'rgba(140, 212, 138, 0.22)',
    glowSecondary: 'rgba(255, 154, 68, 0.12)',
    tagline: '新季度，新起点',
    dotGradient: ['#8CD48A', '#FFAA44'],
    particleClass: 'particles-spring',
  },
  summer: {
    season: 'summer',
    bgGradient: ['#2A2118', '#3A2A1A', '#1F180F'],
    glowPrimary: 'rgba(255, 196, 76, 0.26)',
    glowSecondary: 'rgba(255, 103, 0, 0.16)',
    tagline: '高效运转，活力满格',
    dotGradient: ['#FFD166', '#FF8533'],
    particleClass: 'particles-summer',
  },
  autumn: {
    season: 'autumn',
    bgGradient: ['#2D1A0A', '#5C2D0C', '#3D1F08'],
    glowPrimary: 'rgba(255, 103, 0, 0.35)',
    glowSecondary: 'rgba(180, 60, 0, 0.2)',
    tagline: '丰收季，全力以赴',
    dotGradient: ['#FF6700', '#FF9A44'],
    particleClass: 'particles-autumn',
  },
  winter: {
    season: 'winter',
    bgGradient: ['#0F1520', '#1A2540', '#0C1228'],
    glowPrimary: 'rgba(100, 140, 210, 0.3)',
    glowSecondary: 'rgba(60, 90, 160, 0.2)',
    tagline: '岁末收官，稳步前行',
    dotGradient: ['#A8C4E8', '#FFAA44'],
    particleClass: 'particles-winter',
  },
}

/** 根据月份(1-12)推断季节 */
export function getSeasonByMonth(month: number): Season {
  if (month >= 3 && month <= 5) return 'spring'
  if (month >= 6 && month <= 8) return 'summer'
  if (month >= 9 && month <= 11) return 'autumn'
  return 'winter'
}

/** 获取当前季节主题 */
export function getCurrentSeasonTheme(): SeasonTheme {
  const month = new Date().getMonth() + 1
  return SEASON_THEMES[getSeasonByMonth(month)]
}

/** 按季节获取主题（用于预览/调试） */
export function getSeasonTheme(season: Season): SeasonTheme {
  return SEASON_THEMES[season]
}
