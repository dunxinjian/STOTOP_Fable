import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import { theme } from 'ant-design-vue'
import { getThemeSettings, updateThemeSettings } from '@/api/theme'

/** 表格行高密度选项 */
export type TableRowDensity = 'compact' | 'standard' | 'relaxed' | 'comfortable'

/** 密度选项对应的 padding 值映射 */
export const TABLE_DENSITY_MAP: Record<TableRowDensity, { block: number; inline: number; blockSM: number; inlineSM: number }> = {
  compact:     { block: 4,  inline: 6,  blockSM: 2,  inlineSM: 4 },
  standard:    { block: 6,  inline: 8,  blockSM: 4,  inlineSM: 6 },
  relaxed:     { block: 8,  inline: 10, blockSM: 6,  inlineSM: 8 },
  comfortable: { block: 12, inline: 14, blockSM: 8,  inlineSM: 10 },
}

export const TABLE_DENSITY_LABELS: Record<TableRowDensity, string> = {
  compact: '紧凑',
  standard: '标准',
  relaxed: '宽松',
  comfortable: '舒适',
}

export interface ThemeConfig {
  colorPrimary: string
  colorSuccess: string
  colorWarning: string
  colorError: string
  colorInfo: string
  borderRadius: number
  fontSize: number
  sizeStep: number
  sizeUnit: number
  wireframe: boolean
  compactMode: boolean
  darkMode: boolean
  marginXS: number
  marginSM: number
  margin: number
  marginMD: number
  marginLG: number
  tableRowDensity: TableRowDensity
  pagePaddingX: number
  pagePaddingY: number
  sidebarExpandedWidth: number
  sidebarCollapsedWidth: number
  sidebarBgColor: string
  sidebarActiveBgColor: string
  sidebarMaxTabs: number
}

const defaultThemeConfig: ThemeConfig = {
  colorPrimary: '#E85E00',
  colorSuccess: '#3E9E6E',
  colorWarning: '#D49A2E',
  colorError: '#D6584E',
  colorInfo: '#5B7290',
  borderRadius: 6,
  fontSize: 14,
  sizeStep: 4,
  sizeUnit: 4,
  wireframe: false,
  compactMode: false,
  darkMode: false,
  marginXS: 8,   // = $spacing-sm  = --space-sm8
  marginSM: 12,  // = $spacing-md12 = --space-md12
  margin: 16,    // = $spacing-md  = --space-lg16
  marginMD: 20,  // antd 内部刻度，不在 spacing 双轨（文档已登记）
  marginLG: 24,  // = $spacing-lg  = --space-xl24
  tableRowDensity: 'standard',
  pagePaddingX: 16,  // = --page-pad-x（替代旧 0 的拥挤，给内容区合理留白）
  pagePaddingY: 12,  // = --page-pad-y
  sidebarExpandedWidth: 180,
  sidebarCollapsedWidth: 48,
  sidebarBgColor: '#EDEEF1',
  sidebarActiveBgColor: '#F1F3F6',
  sidebarMaxTabs: 12,
}

export const useThemeStore = defineStore('theme', () => {
  const themeConfig = ref<ThemeConfig>({ ...defaultThemeConfig })
  const loading = ref(false)

  /** 转换为 Ant Design ConfigProvider 的 theme prop 格式 */
  const antdTheme = computed(() => {
    const algorithms: any[] = []
    if (themeConfig.value.darkMode) {
      algorithms.push(theme.darkAlgorithm)
    } else {
      algorithms.push(theme.defaultAlgorithm)
    }
    if (themeConfig.value.compactMode) {
      algorithms.push(theme.compactAlgorithm)
    }

    const density = TABLE_DENSITY_MAP[themeConfig.value.tableRowDensity] || TABLE_DENSITY_MAP.standard

    return {
      token: {
        colorPrimary: themeConfig.value.colorPrimary,
        colorSuccess: themeConfig.value.colorSuccess,
        colorWarning: themeConfig.value.colorWarning,
        colorError: themeConfig.value.colorError,
        colorInfo: themeConfig.value.colorInfo,
        colorLink: '#2C3340',
        colorLinkHover: themeConfig.value.colorPrimary,
        colorLinkActive: '#C94E00',
        borderRadius: themeConfig.value.borderRadius,
        fontSize: themeConfig.value.fontSize,
        sizeStep: themeConfig.value.sizeStep,
        sizeUnit: themeConfig.value.sizeUnit,
        wireframe: themeConfig.value.wireframe,
        marginXS: themeConfig.value.marginXS,
        marginSM: themeConfig.value.marginSM,
        margin: themeConfig.value.margin,
        marginMD: themeConfig.value.marginMD,
        marginLG: themeConfig.value.marginLG,
      },
      components: {
        Table: {
          headerBg: '#fafafa',          // 对齐 --bg-muted 口径（antd token 不吃 var，保持字面量）
          headerColor: 'rgba(0, 0, 0, 0.85)',
          rowHoverBg: '#f5f7fa',        // 对齐 --color-primary-light 口径（ant-override 行 hover 已用浅橙令牌覆盖）
          cellPaddingBlock: density.block,
          cellPaddingBlockSM: density.blockSM,
          cellPaddingBlockMD: density.block,
          cellPaddingInline: density.inline,
          cellPaddingInlineSM: density.inlineSM,
          cellPaddingInlineMD: density.inline,
        },
        Button: {
          controlHeight: 32,
          controlHeightSM: 24,
        },
        Tabs: {
          itemColor: '#5A6068',
          itemSelectedColor: '#1F2329',   // 激活页签文字中性；下划线(inkBar)留橙作标记
          itemHoverColor: themeConfig.value.colorPrimary,
          inkBarColor: themeConfig.value.colorPrimary,
        },
        Menu: {
          itemSelectedColor: '#1F2329',    // 激活菜单文字中性
          itemSelectedBg: '#F1F3F6',       // 激活菜单底中性
        },
        Segmented: {
          itemSelectedBg: '#FFFFFF',       // 分段控件选中段：白底浮起（灰轨上），中性
          itemSelectedColor: '#1F2329',
          itemColor: '#5A6068',
          itemHoverColor: '#1F2329',
        },
        Radio: {
          buttonSolidCheckedBg: '#5A6068',        // 实心 radio 选中底改中性深灰（原橙）
          buttonSolidCheckedHoverBg: '#41464D',
          buttonSolidCheckedActiveBg: '#41464D',
        },
      },
      algorithm: algorithms,
      cssVar: { prefix: 'sto' },
      hashed: false,
    }
  })

  /** 从 API 加载主题配置 */
  async function loadTheme() {
    try {
      const json = await getThemeSettings()
      if (json) {
        const parsed = typeof json === 'string' ? JSON.parse(json) : json
        themeConfig.value = { ...defaultThemeConfig, ...parsed }
      } else {
        // API 返回空数据，使用默认配置
        themeConfig.value = { ...defaultThemeConfig }
      }
    } catch (error: unknown) {
      // API 返回 404/500 或网络异常，优雅降级为默认配置
      console.warn('Failed to load theme settings, using defaults:', error)
      themeConfig.value = { ...defaultThemeConfig }
    }
  }

  /** 保存主题配置到 API */
  async function saveTheme() {
    loading.value = true
    try {
      await updateThemeSettings(JSON.stringify(themeConfig.value))
    } finally {
      loading.value = false
    }
  }

  /** 恢复默认主题 */
  function resetTheme() {
    themeConfig.value = { ...defaultThemeConfig }
  }

  /** 动态注入表格行高 CSS 覆盖（同步 ant-override.scss 中的 !important 规则） */
  function applyTableDensityCSS(density: TableRowDensity) {
    const id = '__theme-table-density__'
    let style = document.getElementById(id) as HTMLStyleElement | null
    if (!style) {
      style = document.createElement('style')
      style.id = id
      document.head.appendChild(style)
    }
    const d = TABLE_DENSITY_MAP[density] || TABLE_DENSITY_MAP.standard
    style.textContent = `
.ant-table .ant-table-thead > tr > th { padding: ${d.block}px ${d.inline}px !important; }
.ant-table .ant-table-tbody > tr > td { padding: ${d.block}px ${d.inline}px !important; }
.ant-table.ant-table-small .ant-table-thead > tr > th,
.ant-table.ant-table-small .ant-table-tbody > tr > td { padding: ${d.blockSM}px ${d.inlineSM}px !important; }
.ant-table.ant-table-middle .ant-table-thead > tr > th,
.ant-table.ant-table-middle .ant-table-tbody > tr > td { padding: ${d.block}px ${d.inline}px !important; }
`
  }

  /** 动态注入页面间距：写 CSS 变量而非注入 !important 规则（与 index.scss 的 .page-container 单一规则配合，收口双轨） */
  function applyPagePaddingCSS(paddingY: number, paddingX: number) {
    const s = document.documentElement.style
    s.setProperty('--page-pad-x', paddingX + 'px')
    s.setProperty('--page-pad-y', paddingY + 'px')
  }

  // 监听 tableRowDensity 变化，实时注入 CSS
  watch(() => themeConfig.value.tableRowDensity, (val) => {
    applyTableDensityCSS(val)
  }, { immediate: true })

  // 监听页面间距变化，实时注入 CSS
  watch(
    () => [themeConfig.value.pagePaddingX, themeConfig.value.pagePaddingY],
    ([x, y]) => {
      applyPagePaddingCSS(y, x)
    },
    { immediate: true }
  )

  /** 动态注入侧栏 CSS 变量 */
  function applySidebarCSS() {
    const style = document.documentElement.style
    style.setProperty('--sidebar-expanded-width', themeConfig.value.sidebarExpandedWidth + 'px')
    style.setProperty('--sidebar-collapsed-width', themeConfig.value.sidebarCollapsedWidth + 'px')
    style.setProperty('--sidebar-bg', themeConfig.value.sidebarBgColor)
    style.setProperty('--sidebar-item-active-bg', themeConfig.value.sidebarActiveBgColor)
    style.setProperty('--sidebar-active-indicator', themeConfig.value.colorPrimary || '#E85E00')
  }

  // 监听侧栏配置变化，实时注入 CSS
  watch(
    () => [
      themeConfig.value.sidebarExpandedWidth,
      themeConfig.value.sidebarCollapsedWidth,
      themeConfig.value.sidebarBgColor,
      themeConfig.value.sidebarActiveBgColor,
      themeConfig.value.colorPrimary,
    ],
    () => {
      applySidebarCSS()
    },
    { immediate: true }
  )

  /** 动态注入完整设计令牌集到 :root（静态令牌为常量，动态主色/状态色由 themeConfig 派生） */
  function applyDesignTokensCSS() {
    const s = document.documentElement.style
    const c = themeConfig.value
    // —— 动态：主色（派生 hover/active/light/border 由 antd 算法吃 colorPrimary，这里仅暴露权威色阶常量）
    s.setProperty('--color-primary', c.colorPrimary || '#E85E00')
    s.setProperty('--color-primary-hover', '#FF6700')
    s.setProperty('--color-primary-active', '#C94E00')
    s.setProperty('--color-primary-light', '#FFF3EA')
    s.setProperty('--color-primary-border', 'rgba(232,94,0,0.30)')
    // —— 动态：状态主色由 themeConfig 派生；浅底/文字为常量
    s.setProperty('--color-success', c.colorSuccess || '#3E9E6E')
    s.setProperty('--color-success-light', '#EAF4EF')
    s.setProperty('--color-success-text', '#2A6B4C')
    s.setProperty('--color-warning', c.colorWarning || '#D49A2E')
    s.setProperty('--color-warning-light', '#FAF1DD')
    s.setProperty('--color-warning-text', '#8A6212')
    s.setProperty('--color-danger', c.colorError || '#D6584E')
    s.setProperty('--color-danger-light', '#FBEEEC')
    s.setProperty('--color-danger-text', '#9E332B')
    s.setProperty('--color-danger-border', 'rgba(214, 88, 78, 0.30)') // 危险态焦点环/描边(对齐 --color-primary-border .30 口径)
    s.setProperty('--color-info', c.colorInfo || '#5B7290')
    s.setProperty('--color-info-light', '#EBEFF4')
    s.setProperty('--color-info-text', '#34455A')
    // —— 静态：文字
    s.setProperty('--text-1', '#1F2329')
    s.setProperty('--text-2', '#5A6068')
    s.setProperty('--text-3', '#8A9099')
    s.setProperty('--text-disabled', '#BFC3C9')
    s.setProperty('--text-on-accent', '#FFFFFF') // 强调色块(橙/红/危险)之上的文字/图标色
    // —— 静态：表面/边框
    s.setProperty('--bg-page', '#F7F8FA')
    s.setProperty('--bg-card', '#FFFFFF')
    s.setProperty('--bg-muted', '#F1F3F6')
    s.setProperty('--border', '#ECEEF1')
    s.setProperty('--border-strong', '#DDE0E4')
    s.setProperty('--border-faint', '#F2F4F6') // 比 --border 更浅一档：表头/行间等“耳语级”分隔线，避免白对白时细线被读成缝
    // —— 静态：外壳
    s.setProperty('--topbar-ink', '#232834')
    s.setProperty('--topbar-ink-admin', '#171A22')
    s.setProperty('--topbar-border', 'rgba(255,255,255,0.10)')
    // 注：--sidebar-bg / --sidebar-item-active-bg 由 applySidebarCSS 按 themeConfig 注入，此处补静态项
    s.setProperty('--sidebar-item-hover', 'rgba(0,0,0,0.05)')
    s.setProperty('--sidebar-item-active-text', 'var(--text-1)')
    // —— 静态：业务色
    s.setProperty('--biz-waybill', '#6B4FB0')
    s.setProperty('--biz-contract', '#8A6D3B')
    s.setProperty('--biz-quality', '#D9603A')
    s.setProperty('--biz-approval', '#5B7290')
    s.setProperty('--biz-points', '#C99A2E')
    s.setProperty('--biz-finance', '#B8860B')
    // —— 静态：圆角
    s.setProperty('--radius-sm', '4px')
    s.setProperty('--radius-md', '6px')
    s.setProperty('--radius-lg', '8px')
    s.setProperty('--radius-modal', '12px')
    s.setProperty('--radius-pill', '999px')
    // —— 静态：阴影
    s.setProperty('--shadow-sm', '0 1px 2px rgba(18,31,53,0.05)')
    s.setProperty('--shadow-md', '0 4px 12px rgba(18,31,53,0.08)')
    s.setProperty('--shadow-lg', '0 8px 24px rgba(18,31,53,0.10)')
    // —— 静态：字号刻度
    s.setProperty('--font-xs', '11px')
    s.setProperty('--font-sm', '12px')
    s.setProperty('--font-sm2', '13px')
    s.setProperty('--font-base', '14px')
    s.setProperty('--font-lg', '16px')
    s.setProperty('--font-xl', '18px')
    s.setProperty('--font-2xl', '24px')
    // —— 静态：间距 4 基数
    s.setProperty('--space-2xs2', '2px')
    s.setProperty('--space-xs4', '4px')
    s.setProperty('--space-sm8', '8px')
    s.setProperty('--space-md12', '12px')
    s.setProperty('--space-lg16', '16px')
    s.setProperty('--space-xl24', '24px')
    s.setProperty('--space-2xl32', '32px')
    // —— 静态：布局范式（工具栏高度；页面内边距初值由 applyPagePaddingCSS 按 themeConfig 覆盖）
    s.setProperty('--toolbar-height', '40px')
  }

  // 监听动态主色/状态色变化，实时重注入令牌集（静态项每次一并写入，幂等）
  watch(
    () => [
      themeConfig.value.colorPrimary,
      themeConfig.value.colorSuccess,
      themeConfig.value.colorWarning,
      themeConfig.value.colorError,
      themeConfig.value.colorInfo,
    ],
    () => {
      applyDesignTokensCSS()
    },
    { immediate: true }
  )

  return {
    themeConfig,
    loading,
    antdTheme,
    loadTheme,
    saveTheme,
    resetTheme,
    applyTableDensityCSS,
    applyPagePaddingCSS,
    applySidebarCSS,
    applyDesignTokensCSS,
  }
})
