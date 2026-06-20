<template>
  <nav class="module-rail" aria-label="模块导航">
    <a-tooltip
      v-for="mod in visibleModules"
      :key="mod.code"
      :title="mod.name"
      placement="right"
    >
      <button
        type="button"
        class="module-rail__item"
        :class="{ 'is-active': mod.code === appStore.currentModule }"
        @click="goModule(mod)"
      >
        <component :is="iconOf(mod)" class="module-rail__icon" />
      </button>
    </a-tooltip>
  </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import {
  HomeOutlined, AccountBookOutlined, ApartmentOutlined, UserOutlined,
  TeamOutlined, CarOutlined, FileTextOutlined, ShopOutlined, TrophyOutlined,
  CheckSquareOutlined, SendOutlined, SafetyOutlined, SafetyCertificateOutlined,
  ScheduleOutlined, BarChartOutlined, SettingOutlined, AppstoreOutlined,
} from '@ant-design/icons-vue'
import { useAppStore, MODULE_TABS, type ModuleTab } from '@/stores/app'
import { usePermissionStore } from '@/stores/permission'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const appStore = useAppStore()
const permissionStore = usePermissionStore()
const userStore = useUserStore()

const iconMap: Record<string, any> = {
  HomeOutlined, AccountBookOutlined, ApartmentOutlined, UserOutlined,
  TeamOutlined, CarOutlined, FileTextOutlined, ShopOutlined, TrophyOutlined,
  CheckSquareOutlined, SendOutlined, SafetyOutlined, SafetyCertificateOutlined,
  ScheduleOutlined, BarChartOutlined, SettingOutlined,
}

function iconOf(mod: ModuleTab) {
  return (mod.icon && iconMap[mod.icon]) || AppstoreOutlined
}

const visibleModules = computed(() => {
  const vis = permissionStore.getModuleVisibility(userStore.permissions) as Record<string, boolean>
  return MODULE_TABS.filter((m) => m.code === 'workhub' || m.alwaysShow || vis[m.code])
})

function goModule(mod: ModuleTab) {
  appStore.setCurrentModule(mod.code)
  router.push(mod.route)
}
</script>

<style scoped>
.module-rail {
  flex: 0 0 var(--rail-width, 46px);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 8px 0;
  background: var(--sidebar-bg);
  border-right: 1px solid var(--border);
  overflow-y: auto;
}

.module-rail__item {
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: var(--radius-md);
  background: transparent;
  color: var(--text-2);
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}

.module-rail__item:hover {
  background: var(--sidebar-item-hover);
  color: var(--text-1);
}

.module-rail__item.is-active {
  color: var(--color-primary);
  background: var(--bg-muted);
}

.module-rail__icon {
  font-size: 18px;
}
</style>
