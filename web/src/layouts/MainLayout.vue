<template>
  <div
    class="app-layout"
    :style="{
      '--sidebar-width': sidebarStore.collapsed ? '0px' : sidebarStore.sidebarWidth + 'px'
    }"
  >
    <!-- 顶栏 -->
    <TopBar />

    <!-- 左栏 -->
    <div class="app-main">
      <!-- 模块轨 -->
      <ModuleRail />
      <!-- 左栏 -->
      <SmartSidebar />

      <!-- 内容区 -->
      <div class="content-area">
        <AppBreadcrumb />
        <div class="content-scroll">
          <router-view v-slot="{ Component, route: viewRoute }">
            <keep-alive :max="20">
              <component :is="Component" :key="`${orgContextStore.orgSwitchVersion}:${orgContextStore.pageRefreshVersion}:${viewRoute.fullPath}`" />
            </keep-alive>
          </router-view>
        </div>
      </div>
    </div>

    <GlobalSearch />
    <ShortcutHelp />
    <FeedbackQuickSubmit />
  </div>
</template>

<script setup lang="ts">
import TopBar from './TopBar.vue'
import ModuleRail from './ModuleRail.vue'
import SmartSidebar from '@/layouts/SmartSidebar.vue'
import GlobalSearch from '@/components/GlobalSearch.vue'
import ShortcutHelp from '@/components/ShortcutHelp.vue'
import FeedbackQuickSubmit from '@/components/FeedbackQuickSubmit.vue'
import AppBreadcrumb from '@/components/AppBreadcrumb.vue'
import { useAppStore, MODULE_TABS } from '@/stores/app'
import { useOrgContextStore } from '@/stores/orgContext'
import { useSidebarStore } from '@/stores/sidebar'
import { onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const appStore = useAppStore()
const orgContextStore = useOrgContextStore()
const sidebarStore = useSidebarStore()

// ── 路由 → 模块检测 ────────────────────────────────────────
function updateCurrentModuleFromRoute() {
  const path = route.path

  if (path === '/' || path === '/home' || path === '/workhub' || path.startsWith('/workhub/')) {
    appStore.setCurrentModule('workhub')
    return
  }

  for (const mod of MODULE_TABS) {
    const routePrefix = mod.route.split('/').slice(0, 2).join('/')
    if (path.startsWith(routePrefix + '/') || path === mod.route) {
      appStore.setCurrentModule(mod.code)
      return
    }
  }
}

// 路由变化 → 同步当前模块标识
watch(() => route.fullPath, () => {
  updateCurrentModuleFromRoute()
}, { immediate: true })

// ── 初始化 ─────────────────────────────────────────────────────
onMounted(() => {
  appStore.fetchVersion()
  updateCurrentModuleFromRoute()
})
</script>
