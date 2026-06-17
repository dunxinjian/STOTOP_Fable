<template>
  <div class="topbar">
    <!-- 上下文区：侧栏折叠按钮 + 品牌标识(Logo+简称)，与侧栏右对齐 -->
    <div class="topbar-context">
      <!-- 侧栏折叠/展开按钮 -->
      <button
        class="topbar-icon-btn topbar-sidebar-toggle"
        :title="sidebarStore.collapsed ? '展开侧栏' : '收起侧栏'"
        @click="sidebarStore.toggleCollapse()"
      >
        <MenuUnfoldOutlined v-if="sidebarStore.collapsed" />
        <MenuFoldOutlined v-else />
      </button>
      <!-- 品牌标识：Logo + 企业简称 -->
      <div class="topbar-brand" title="回首页" @click="goWorkhub">
        <img v-if="enterpriseInfoStore.hasLogo" :src="enterpriseInfoStore.logoUrl" class="brand-logo" alt="logo" />
        <span class="brand-name">{{ enterpriseInfoStore.displayName }}</span>
      </div>
    </div>

    <span class="topbar-divider" />

    <!-- 信息区：组织切换器 | 工作台 | 回退/前进 | 搜索 | 公告 -->
    <div class="topbar-info">
      <!-- 组织切换器 -->
      <OrgSwitcher class="topbar-org" />
      <!-- 竖线 -->
      <span class="topbar-info-divider" />
      <!-- 工作台入口 -->
      <button
        class="topbar-icon-btn topbar-workhub-btn"
        :class="{ active: route.path === '/workhub' || route.path.startsWith('/workhub/') }"
        title="工作台"
        @click="goWorkhub"
      >
        <HomeOutlined />
        <span>工作台</span>
        <span v-if="todoCount > 0" class="workhub-todo-count">{{ todoCount }}</span>
      </button>
      <!-- 回退按钮 -->
      <button
        class="topbar-icon-btn topbar-back-btn"
        :disabled="!canGoBack"
        title="返回上一级"
        @click="goBack"
      >
        <ArrowLeftOutlined />
        <span>返回</span>
      </button>
      <!-- 前进按钮 -->
      <button
        class="topbar-icon-btn topbar-forward-btn"
        :disabled="!canGoForward"
        title="前进"
        @click="goForward"
      >
        <ArrowRightOutlined />
      </button>
      <!-- 当前页面标签（与内容区底色一致并连接） -->
      <div v-if="!isWorkhub" class="page-tab">
        <FileOutlined class="page-tab-icon" />
        <span class="page-tab-title">{{ currentPageTitle }}</span>
        <button class="page-tab-close" title="关闭" @click="closePageTab">
          <CloseOutlined />
        </button>
      </div>
      <!-- 弹性间距 -->
      <div class="topbar-info-spacer" />
      <!-- 搜索框：加宽 -->
      <div
        class="topbar-search"
        role="button"
        tabindex="0"
        @click="openGlobalSearch"
        @keydown.enter="openGlobalSearch"
      >
        <SearchOutlined />
        <span class="topbar-search-placeholder" :class="{ 'fade-out': placeholderFading }">{{ currentPlaceholder }}</span>
        <kbd>{{ shortcutLabel }}</kbd>
      </div>
      <!-- 公告：弹性充满搜索框到右侧竖线之间的剩余空间 -->
      <SystemAnnouncement class="topbar-announcement" />
    </div>

    <!-- 个人区 -->
    <div class="topbar-personal">
      <button class="topbar-icon-btn topbar-refresh-btn" @click="refreshPage" title="刷新当前页面">
        <ReloadOutlined />
      </button>
      <button class="topbar-icon-btn topbar-fullscreen-btn" @click="toggleFullscreen" :title="isFullscreen ? '退出全屏' : '全屏'">
        <FullscreenExitOutlined v-if="isFullscreen" />
        <FullscreenOutlined v-else />
      </button>
      <a-dropdown>
        <div
          class="user-identity"
          role="button"
          tabindex="0"
          :title="displayName"
          :aria-label="`用户菜单: ${displayName}`"
        >
          <span class="user-name">{{ displayName }}</span>
        </div>
        <template #overlay>
          <a-menu>
            <a-menu-item @click="goPersonalSettings">个人设置</a-menu-item>
            <a-menu-item v-if="hasAdminPermission" @click="goAdmin">
              <SettingOutlined /> 系统管理
            </a-menu-item>
            <a-menu-divider />
            <a-menu-item @click="logout">退出登录</a-menu-item>
          </a-menu>
        </template>
      </a-dropdown>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import {
  HomeOutlined,
  SearchOutlined,
  FullscreenOutlined,
  FullscreenExitOutlined,
  SettingOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  ArrowLeftOutlined,
  ArrowRightOutlined,
  CloseOutlined,
  FileOutlined,
  ReloadOutlined,
} from '@ant-design/icons-vue'
import { useUserStore } from '@/stores/user'
import { usePermissionStore } from '@/stores/permission'
import { useNotificationStore } from '@/stores/notification'
import { useOrgContextStore } from '@/stores/orgContext'
import { useSidebarStore } from '@/stores/sidebar'
import { useEnterpriseInfoStore } from '@/stores/enterpriseInfo'
import { resetRouter } from '@/router'
import { useCommandPalette } from '@/composables/useCommandPalette'
import OrgSwitcher from '@/components/OrgSwitcher.vue'
import SystemAnnouncement from '@/components/SystemAnnouncement.vue'
import { markNavSource } from '@/stores/navChain'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const permissionStore = usePermissionStore()
const notificationStore = useNotificationStore()
const orgContextStore = useOrgContextStore()
const sidebarStore = useSidebarStore()
const enterpriseInfoStore = useEnterpriseInfoStore()

// 用户信息
const displayName = computed(
  () => userStore.userInfo?.realName || userStore.userInfo?.username || '用户'
)
// 头像为固定 SVG 人形图案，无需文字计算属性

// 待办计数
const todoCount = computed(() => notificationStore.todoCount?.total ?? 0)

// 权限：管理后台入口
const hasAdminPermission = computed(() => permissionStore.hasAdminAccess)

// 平台感知快捷键
const isMac = typeof navigator !== 'undefined' && navigator.platform.toUpperCase().includes('MAC')
const shortcutLabel = computed(() => (isMac ? '⌘K' : 'Ctrl+K'))

// 全屏：使用 document.fullscreenElement API 直接实现
const isFullscreen = ref(false)

function toggleFullscreen() {
  if (!document.fullscreenElement) {
    document.documentElement.requestFullscreen?.()
  } else {
    document.exitFullscreen?.()
  }
}

function onFullscreenChange() {
  isFullscreen.value = !!document.fullscreenElement
}

// 全局搜索：直接通过共享 composable 打开命令面板
// 避免中间层 keyboardManager.trigger 合成 KeyboardEvent / 注册时序 / 闭包引用等任何不确定性
const { open: openCommandPalette } = useCommandPalette()

function openGlobalSearch() {
  openCommandPalette()
}

function goWorkhub() {
  markNavSource('menu')
  router.push('/workhub')
}

// 回退 / 前进按钮
const canGoBack = computed(() => window.history.length > 1)

function goBack() {
  if (window.history.length > 1) {
    router.back()
  }
}

const canGoForward = computed(() => window.history.length > 1 && window.history.state?.forward != null)

function goForward() {
  router.forward()
}

// 刷新当前页面：通过递增版本号强制组件重载，不会退出全屏
function refreshPage() {
  orgContextStore.triggerPageRefresh()
}

// 当前页面标签
const currentPageTitle = computed(() => {
  const meta = route.meta as Record<string, any> | undefined
  return meta?.title || meta?.label || '当前页面'
})

const isWorkhub = computed(() => route.path === '/workhub' || route.path.startsWith('/workhub/'))

function closePageTab() {
  markNavSource('menu')
  router.push('/workhub')
}

function goPersonalSettings() {
  router.push('/profile')
}

function goAdmin() {
  router.push('/admin')
}

async function logout() {
  await userStore.logout()
  permissionStore.reset()
  orgContextStore.clearOrgContext()
  sidebarStore.reset?.()
  resetRouter()
  router.push('/login')
}

// 搜索框 Placeholder 轮播
const placeholderHints = [
  '搜索菜单...',
  '搜索数据...',
  '搜索人员...',
  '搜索功能...',
]
const placeholderIndex = ref(0)
const placeholderFading = ref(false)
let placeholderTimer: ReturnType<typeof setInterval> | null = null

const currentPlaceholder = computed(() => placeholderHints[placeholderIndex.value])

function startPlaceholderRotation() {
  placeholderTimer = setInterval(() => {
    placeholderFading.value = true
    setTimeout(() => {
      placeholderIndex.value = (placeholderIndex.value + 1) % placeholderHints.length
      placeholderFading.value = false
    }, 300) // 淡出动画时长
  }, 3000)
}

function stopPlaceholderRotation() {
  if (placeholderTimer) {
    clearInterval(placeholderTimer)
    placeholderTimer = null
  }
}

onMounted(() => {
  notificationStore.startPolling?.()
  startPlaceholderRotation()
  document.addEventListener('fullscreenchange', onFullscreenChange)
})

onBeforeUnmount(() => {
  notificationStore.stopPolling?.()
  stopPlaceholderRotation()
  document.removeEventListener('fullscreenchange', onFullscreenChange)
})
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

// 顶栏组件级样式（基础布局已在 layout.scss 中定义，此处针对视觉细节做精确覆盖）

// 1. 顶栏整体高度与内边距
.topbar {
  height: 40px;
  padding: 0 12px;
}

// 上下文区间距
.topbar-context {
  gap: 0;
}

// 个人区間距
.topbar-personal {
  gap: 2px;
}

// 信息区使用稍宽间距
.topbar-info {
  gap: 6px;
}

// 品牌标识（Logo + 企业简称）
.topbar-brand {
  display: flex;
  align-items: center;
  gap: 6px;
  flex: 1;
  min-width: 0;
  padding: 0 6px;
  height: 28px;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: background 0.15s ease;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
  }

  .brand-logo {
    height: 22px;
    max-width: 28px;
    object-fit: contain;
    flex-shrink: 0;
  }

  .brand-name {
    font-size: 14px;
    font-weight: 600;
    color: rgba(255, 255, 255, 0.92);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    line-height: 1;
  }
}

// 弹性间距：固定 15px，让 Tab 与搜索框保持距离
.topbar-info-spacer {
  width: 15px;
  min-width: 15px;
  flex-shrink: 0;
}

// 信息区内部竖线（比顶层 divider 更轻量）
.topbar-info-divider {
  display: block;
  width: 1px;
  height: 16px;
  background: rgba(255, 255, 255, 0.10);
  margin: 0 8px;
  flex-shrink: 0;
  align-self: center;
}

// 2. 组织切换器触发按钮（深色顶栏样式覆盖）
:deep(.topbar-org.org-switcher--dark) {
  flex-shrink: 0;
  .org-current {
    max-width: 180px;
    padding: 0 6px;
    height: 28px;
    border-radius: var(--radius-md);
    background: transparent;
    border: none;
    color: rgba(255, 255, 255, 0.85);
    font-size: 13px;

    &:hover {
      background: rgba(255, 255, 255, 0.08);
    }

    // 图标直接内联，无圆圈底色
    .org-icon-circle {
      background: none;
      color: rgba(255, 255, 255, 0.55);
    }

    .org-name {
      max-width: 140px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  }
}

// 3. 工作台按钮（顶栏 info 区内联按钮）
.topbar-workhub-btn {
  display: inline-flex;
  align-items: center;
  padding: 0 8px !important;
  height: 28px;
  min-width: 0;
  min-height: 28px;
  border-radius: var(--radius-md);
  font-size: 13px;
  gap: 5px;
  color: rgba(255, 255, 255, 0.65);
  white-space: nowrap;
  transition: all 0.15s ease;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    color: rgba(255, 255, 255, 0.9);
  }

  &.active {
    background: var(--color-primary-light);
    color: var(--color-primary);
    font-weight: 600;
  }
}

// 回退按钮
.topbar-back-btn,
.topbar-forward-btn {
  display: inline-flex;
  align-items: center;
  padding: 0 8px !important;
  height: 28px;
  min-height: 28px;
  border-radius: var(--radius-md);
  font-size: 13px;
  gap: 5px;
  color: rgba(255, 255, 255, 0.55);
  white-space: nowrap;
  transition: all 0.15s ease;

  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.08);
    color: rgba(255, 255, 255, 0.85);
  }

  &:disabled {
    opacity: 0.35;
    cursor: not-allowed;
  }
}

// 前进按钮：无文字标签，更紧凑
.topbar-forward-btn {
  padding: 0 6px !important;
}

// 当前页面标签（白色卡片式，与内容区连接）
.page-tab {
  align-self: flex-end;
  width: 200px;
  min-width: 200px;
  max-width: 200px;
  height: 35px;
  padding: 0 10px;
  margin-bottom: -1px;
  background: #ffffff;
  border-radius: var(--radius-lg) var(--radius-lg) 0 0;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  position: relative;
  z-index: 2;
  box-shadow: 0 -1px 3px rgba(0, 0, 0, 0.04);
  flex-shrink: 0;

  margin-left: 9px;

  // 底部外凸圆角：向左右两侧延伸
  &::before,
  &::after {
    content: '';
    position: absolute;
    bottom: 0;
    width: 6px;
    height: 6px;
    background: transparent;
    z-index: -1;
  }

  &::before {
    left: -6px;
    border-bottom-right-radius: 6px;
    box-shadow: 2px 2px 0 0 #fff;
  }

  &::after {
    right: -6px;
    border-bottom-left-radius: 6px;
    box-shadow: -2px 2px 0 0 #fff;
  }

  .page-tab-icon {
    font-size: 14px;
    color: var(--color-primary);
    flex-shrink: 0;
  }

  .page-tab-title {
    flex: 1;
    font-size: 14px;
    font-weight: 600;
    color: rgba(0, 0, 0, 0.85);
    min-width: 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    line-height: 1;
  }

  .page-tab-close {
    width: 16px;
    height: 16px;
    padding: 0;
    border: none;
    background: transparent;
    color: $text-secondary;
    border-radius: 3px;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 11px;
    flex-shrink: 0;
    transition: background 0.15s ease, color 0.15s ease;

    &:hover {
      background: rgba(0, 0, 0, 0.06);
      color: $text-primary;
    }
  }
}

// 工作台待办计数徽章
.workhub-todo-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 8px;
  background: var(--color-primary);
  color: #fff;
  font-size: 11px;
  font-weight: 600;
  line-height: 1;
  flex-shrink: 0;
}

// 3. 搜索框（命令面板触发器）
.topbar-search {
  width: 200px;
  flex-shrink: 0;
  height: 28px;
  padding: 0 12px;
  border-radius: var(--radius-pill); // 全圆角
  background: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.20);
  color: rgba(255, 255, 255, 0.65);
  font-size: 12px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 6px;
  box-shadow: inset 0 1px 2px rgba(0, 0, 0, 0.15);

  // Placeholder 轮播文字
  .topbar-search-placeholder {
    flex: 1;
    min-width: 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    transition: opacity 0.3s ease;
    opacity: 1;

    &.fade-out {
      opacity: 0;
    }
  }

  // 快捷键标签（模板中使用 <kbd>），保持与设计一致
  kbd,
  .shortcut-label {
    margin-left: auto;
    font-size: 10px;
    padding: 1px 5px;
    background: rgba(255, 255, 255, 0.15);
    border-radius: var(--radius-sm);
    color: rgba(255, 255, 255, 0.70);
  }

  &:hover {
    background: rgba(255, 255, 255, 0.18);
    border-color: var(--color-primary-border);
    color: rgba(255, 255, 255, 0.9);
  }
}

// 5. 系统公告：在前置位置追加橙色脉冲圆点
.topbar-announcement {
  flex: 1;
  min-width: 0;
  margin-left: 16px;
  display: flex;
  align-items: center;
  gap: 6px;
  // 颜色由 layout.scss 统一控制，避免 scoped 优先级冲突

  &::before {
    content: '';
    width: 5px;
    height: 5px;
    border-radius: 50%;
    background: var(--color-primary);
    flex-shrink: 0;
    animation: pulse-dot 2s infinite;
  }
}

@keyframes pulse-dot {
  0%, 100% { opacity: 1; transform: scale(1); }
  50%      { opacity: 0.4; transform: scale(0.8); }
}

// 6. 用户姓名标签
.user-identity {
  display: flex;
  align-items: center;
  padding: 0 10px;
  height: 28px;
  border-radius: 14px;
  cursor: pointer;
  transition: background 0.15s ease;

  &:hover {
    background: rgba(255, 255, 255, 0.10);
  }
}

// 姓名文字标签
.user-name {
  font-size: 13px;
  font-weight: 500;
  color: rgba(255, 255, 255, 0.85);
  max-width: 100px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  line-height: 1;
}

// ========== 响应式窄屏收缩策略 ==========

// 1200px 以下：缩短组织切换器
@media (max-width: 1200px) {
  :deep(.topbar-org.org-switcher--dark) {
    .org-current {
      max-width: 120px;
      .org-name {
        max-width: 80px;
      }
    }
  }
}

// 960px 以下：搜索框只显示图标，隐藏文字与快捷键；回退按钮隐藏文字
@media (max-width: 960px) {
  .topbar-search {
    width: 36px;
    padding: 0;
    justify-content: center;
    border-radius: 50%;
    span,
    kbd {
      display: none;
    }
  }
  .topbar-back-btn span {
    display: none;
  }
  // 页面标签只保留图标和关闭按钮
  .page-tab {
    padding: 0 6px 2px;
    .page-tab-title {
      display: none;
    }
  }
  // 品牌名称隐藏，仅保留 Logo
  .topbar-brand .brand-name {
    display: none;
  }
  // 组织切换器仅显示图标
  :deep(.topbar-org.org-switcher--dark) {
    .org-current {
      max-width: 36px;
      padding: 0 6px;
      .org-name,
      .org-arrow {
        display: none;
      }
    }
  }
}

// 768px 以下：隐藏系统公告
@media (max-width: 768px) {
  .topbar-announcement {
    display: none;
  }
}

</style>
