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
  colorPrimary: '#FF6700',
  colorSuccess: '#52C41A',
  colorWarning: '#E6A700',
  colorError: '#FF4D4F',
  colorInfo: '#13C2C2',
  borderRadius: 6,
  fontSize: 14,
  sizeStep: 4,
  sizeUnit: 4,
  wireframe: false,
  compactMode: false,
  darkMode: false,
  marginXS: 8,
  marginSM: 12,
  margin: 16,
  marginMD: 20,
  marginLG: 24,
  tableRowDensity: 'standard',
  pagePaddingX: 0,
  pagePaddingY: 0,
  sidebarExpandedWidth: 180,
  sidebarCollapsedWidth: 48,
  sidebarBgColor: '#e4e7ef',
  sidebarActiveBgColor: 'rgba(255, 103, 0, 0.06)',
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
          headerBg: '#fafafa',
          headerColor: 'rgba(0, 0, 0, 0.85)',
          rowHoverBg: '#f5f7fa',
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
      },
      algorithm: algorithms,
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

  /** 动态注入页面间距 CSS */
  function applyPagePaddingCSS(paddingY: number, paddingX: number) {
    const id = '__theme-page-padding__'
    let style = document.getElementById(id) as HTMLStyleElement | null
    if (!style) {
      style = document.createElement('style')
      style.id = id
      document.head.appendChild(style)
    }
    style.textContent = `.page-container { padding: ${paddingY}px ${paddingX}px !important; }`
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
    style.setProperty('--sidebar-active-bg', themeConfig.value.sidebarActiveBgColor)
    style.setProperty('--sidebar-active-indicator', themeConfig.value.colorPrimary || '#FF6700')
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
  }
})
