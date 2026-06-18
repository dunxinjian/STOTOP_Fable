# STOTOP 设计令牌（单一真源）

令牌有两条注入轨道，**值必须一致**：
- **运行时**：`web/src/stores/theme.ts` 的 `applyDesignTokensCSS()` 把令牌写入 `:root`（动态主色/状态色由 `themeConfig` 派生，其余为静态常量）。
- **编译期**：`web/src/styles/variables.scss` 的 `$` 变量以 `var(--token, 权威回退值)` 桥接，消费方继续写 `$color-primary` 即自动透传 CSS 变量。

> 阶段0 只建立真源与桥接，**不替换** 组件内 221 处裸值（阶段1）。

## 主色

| 令牌 | 值 | 用途 |
|---|---|---|
| `--color-primary` | `#E85E00` | 品牌主色（动态，随 themeConfig.colorPrimary） |
| `--color-primary-hover` | `#FF6700` | 主色悬停 |
| `--color-primary-active` | `#C94E00` | 主色按下 |
| `--color-primary-light` | `#FFF3EA` | 主色浅底 |
| `--color-primary-border` | `rgba(232,94,0,0.30)` | 主色描边 |
| `--color-danger-border` | `rgba(214,88,78,0.30)` | 危险态焦点环/描边（对齐 --color-primary-border .30 口径） |

## 状态色（成功/警告/危险/信息，各带 -light/-text）

| 令牌 | 值 | | 令牌 | 值 | | 令牌 | 值 |
|---|---|---|---|---|---|---|---|
| `--color-success` | `#2BA471` | | `--color-success-light` | `#E7F5EF` | | `--color-success-text` | `#0F6E56` |
| `--color-warning` | `#E6A700` | | `--color-warning-light` | `#FBF1D8` | | `--color-warning-text` | `#8A6200` |
| `--color-danger` | `#E5484D` | | `--color-danger-light` | `#FCEBEC` | | `--color-danger-text` | `#A3282C` |
| `--color-info` | `#3A6FB0` | | `--color-info-light` | `#E9F0F8` | | `--color-info-text` | `#1C4366` |

> `--color-success` / `--color-warning` / `--color-danger`(=themeConfig.colorError) / `--color-info` 为动态，随 `themeConfig` 派生；各 `-light` / `-text` 为静态常量。

## 文字 / 表面 / 边框

`--text-1 #1F2329` `--text-2 #5A6068` `--text-3 #8A9099` `--text-disabled #BFC3C9` `--text-on-accent #FFFFFF`（强调色块上文字/图标）
`--bg-page #F5F6F8` `--bg-card #FFFFFF` `--bg-muted #EEF0F3` `--border #E6E8EB` `--border-strong #D6D9DD`

## 外壳（topbar / sidebar）

| 令牌 | 值 | 用途 |
|---|---|---|
| `--topbar-ink` | `#1F2430` | 前台顶栏底色 |
| `--topbar-ink-admin` | `#171A22` | 管理后台顶栏底色 |
| `--topbar-border` | `rgba(255,255,255,0.10)` | 顶栏分隔线 |
| `--sidebar-item-hover` | `rgba(0,0,0,0.05)` | 侧栏项悬停底 |
| `--sidebar-item-active-text` | `var(--color-primary)` | 侧栏激活项文字 |

> `--sidebar-bg` / `--sidebar-active-bg` / `--sidebar-item-active-bg` / `--sidebar-active-indicator` 由 `applySidebarCSS()` 按 `themeConfig`（`sidebarBgColor`=`#EDEEF1`、`sidebarActiveBgColor`=`#FFF3EA`、indicator=主色）动态注入，此处仅登记。

## 业务色

| 令牌 | 值 | 用途 |
|---|---|---|
| `--biz-waybill` | `#6B4FB0` | 快递/运单 |
| `--biz-contract` | `#8A6D3B` | 合同 |
| `--biz-quality` | `#D9603A` | 质量 |
| `--biz-approval` | `#3A6FB0` | 审批 |
| `--biz-points` | `#C99A2E` | 积分 |
| `--biz-finance` | `#B8860B` | 财务 |

## 圆角

| 令牌 | 值 |
|---|---|
| `--radius-sm` | `4px` |
| `--radius-md` | `6px` |
| `--radius-lg` | `8px` |
| `--radius-modal` | `12px` |
| `--radius-pill` | `999px` |

## 阴影

| 令牌 | 值 |
|---|---|
| `--shadow-sm` | `0 1px 2px rgba(18,31,53,0.05)` |
| `--shadow-md` | `0 4px 12px rgba(18,31,53,0.08)` |
| `--shadow-lg` | `0 8px 24px rgba(18,31,53,0.10)` |

## 字号刻度

| 令牌 | 值 | SCSS |
|---|---|---|
| `--font-xs` | `11px` | `$font-size-xs` |
| `--font-sm` | `12px` | `$font-size-sm` |
| `--font-sm2` | `13px` | `$font-size-sm2` |
| `--font-base` | `14px` | `$font-size-base` |
| `--font-lg` | `16px` | `$font-size-lg` |
| `--font-xl` | `18px` | `$font-size-xl` |
| `--font-2xl` | `24px` | `$font-size-2xl` |

## 间距（4 基数）

| 令牌 | 值 | SCSS |
|---|---|---|
| `--space-2xs2` | `2px` | `$spacing-2xs` |
| `--space-xs4` | `4px` | `$spacing-xs` |
| `--space-sm8` | `8px` | `$spacing-sm` |
| `--space-md12` | `12px` | `$spacing-md12` |
| `--space-lg16` | `16px` | `$spacing-md` |
| `--space-xl24` | `24px` | `$spacing-lg` |
| `--space-2xl32` | `32px` | `$spacing-xl` |

## 间距双轨对齐（4 基数）

| SCSS | CSS 令牌 | antd token | 值 |
|---|---|---|---|
| `$spacing-sm` | `--space-sm8` | `marginXS` | 8 |
| `$spacing-md12` | `--space-md12` | `marginSM` | 12 |
| `$spacing-md` | `--space-lg16` | `margin` | 16 |
| `$spacing-lg` | `--space-xl24` | `marginLG` | 24 |
| — | — | `marginMD` | 20（antd 内部刻度，不在双轨） |

## 验证

- 静态断言（PowerShell + ripgrep）：`cd web; rg -n "#1890ff" src`，令牌文件外应趋零（阶段1 完成后）。
- 运行时变色：浏览器控制台 `document.documentElement.style.setProperty('--color-primary','#0000ff')`，全局应变蓝。
- 门禁：`npx stylelint "src/styles/variables.scss"` 退出码 0（白名单豁免 `color-no-hex`）；`npm run lint:style` 列出组件内待替换裸 hex（移交阶段1）。
