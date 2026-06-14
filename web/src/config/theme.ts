import type { ThemeConfig } from 'ant-design-vue/es/config-provider/context'

export const antThemeConfig: ThemeConfig = {
  token: {
    colorPrimary: '#409eff',
    colorSuccess: '#52c41a',
    colorWarning: '#fa8c16',
    colorError: '#f5222d',
    colorInfo: '#909399',
    borderRadius: 4,
    fontSize: 14,
  },
  components: {
    Table: {
      headerBg: '#fafafa',
      headerColor: 'rgba(0, 0, 0, 0.85)',
      rowHoverBg: '#f5f7fa',
      cellPaddingBlock: 6,
      cellPaddingBlockSM: 4,
      cellPaddingBlockMD: 6,
      cellPaddingInline: 8,
      cellPaddingInlineSM: 6,
      cellPaddingInlineMD: 8,
    },
    Button: {
      controlHeight: 32,
      controlHeightSM: 24,
    },
  },
}
