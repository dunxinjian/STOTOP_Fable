<template>
  <div class="admin-layout">
    <!-- 顶栏 -->
    <header class="admin-topbar">
      <div class="topbar-left" @click="goBack">
        <ArrowLeftOutlined />
        <span class="back-text">返回前台</span>
      </div>
      <div class="topbar-center">
        <ShieldOutlined class="admin-shield-icon" />
        系统管理后台
      </div>
      <div class="topbar-right">
        <a-avatar v-if="userInfo?.avatar" :src="userInfo.avatar" :size="24" />
        <a-avatar v-else :size="24" :style="{ background: 'var(--color-primary)', fontSize: '12px' }">
          {{ userInfo?.realName?.charAt(0) || 'U' }}
        </a-avatar>
        <span class="user-name">{{ userInfo?.realName || userInfo?.username || '用户' }}</span>
      </div>
    </header>

    <div class="admin-body">
      <!-- 左侧菜单 -->
      <aside class="admin-sidebar">
        <template v-for="(group, gi) in menuGroups" :key="gi">
          <div class="menu-group">
            <div class="group-title">{{ group.groupName }}</div>
            <div
              v-for="item in group.items"
              :key="item.id"
              class="menu-item"
              :class="{ active: isActive(item) }"
              @click="handleMenuClick(item)"
            >
              <component
                v-if="item.icon && iconMap[item.icon]"
                :is="iconMap[item.icon]"
                class="menu-icon"
              />
              <span class="menu-text">{{ item.name }}</span>
            </div>
          </div>
        </template>
      </aside>

      <!-- 主内容区 -->
      <main class="admin-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowLeftOutlined, SafetyCertificateOutlined } from '@ant-design/icons-vue'
import * as AntIcons from '@ant-design/icons-vue'
import { usePermissionStore } from '@/stores/permission'
import { useUserStore } from '@/stores/user'
import type { MenuItem } from '@/api/auth'

const router = useRouter()
const route = useRoute()
const permissionStore = usePermissionStore()
const userStore = useUserStore()

const userInfo = computed(() => userStore.userInfo)

const menuGroups = computed(() => permissionStore.getModuleMenuGroups('system'))

// 图标名称 → 组件映射
const iconMap = AntIcons as unknown as Record<string, any>

function handleMenuClick(item: MenuItem) {
  if (!item.route) return
  const adminPath = item.route.replace(/^\/system/, '/admin')
  router.push(adminPath)
}

function isActive(item: MenuItem): boolean {
  if (!item.route) return false
  const adminPath = item.route.replace(/^\/system/, '/admin')
  return route.path === adminPath || route.path.startsWith(adminPath + '/')
}

function goBack() {
  const returnPath = sessionStorage.getItem('admin_return_path') || '/'
  router.push(returnPath)
}
</script>

<style scoped>
.admin-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

/* ── 顶栏 ─────────────────────────────────── */
.admin-topbar {
  height: 48px;
  min-height: 48px;
  background: var(--topbar-ink-admin);
  border-bottom: 1px solid var(--topbar-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  z-index: 100;
  position: relative;
}

.topbar-left {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  color: rgba(255, 255, 255, 0.72);
  font-size: 13px;
  padding: 5px 10px;
  border-radius: 4px;
  border: 1px solid rgba(255, 255, 255, 0.15);
  transition: all 0.2s;
}

.topbar-left:hover {
  color: #fff;
  background: rgba(255, 255, 255, 0.08);
  border-color: rgba(255, 255, 255, 0.28);
}

.back-text {
  font-size: 13px;
}

.topbar-center {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.95);
  letter-spacing: 0.5px;
}

.admin-shield-icon {
  font-size: 15px;
  color: var(--text-3);
}

.topbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: rgba(255, 255, 255, 0.75);
}

.user-name {
  white-space: nowrap;
}

/* ── 主体 ─────────────────────────────────── */
.admin-body {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* ── 左侧菜单 ─────────────────────────────── */
.admin-sidebar {
  width: 220px;
  flex-shrink: 0;
  background: var(--sidebar-bg);
  overflow-y: auto;
  padding-top: 4px;
}

.menu-group {
  margin-bottom: 8px;
}

.group-title {
  color: var(--text-3);
  font-size: var(--font-sm);
  letter-spacing: 0.5px;
  padding: var(--space-lg16) var(--space-xl24) var(--space-sm8);
  user-select: none;
}

.menu-item {
  display: flex;
  align-items: center;
  height: 36px;
  padding: 0 var(--space-md12);
  margin: 1px var(--space-sm8);
  border-radius: var(--radius-md);
  cursor: pointer;
  color: var(--text-2);
  font-size: var(--font-sm2);
  transition: all 0.15s ease;
  position: relative;
}

.menu-item:hover {
  color: var(--text-1);
  background: var(--sidebar-item-hover);
}

.menu-item.active {
  color: var(--sidebar-item-active-text);
  background: var(--sidebar-item-active-bg);
  font-weight: 600;
}

.menu-item.active::before {
  content: '';
  position: absolute;
  left: 0;
  top: 10px;
  bottom: 10px;
  width: 3px;
  border-radius: 0 2px 2px 0;
  background: var(--sidebar-active-indicator);
}

.menu-icon {
  margin-right: 10px;
  font-size: 15px;
  opacity: 1;
  color: var(--text-3);
}

.menu-item.active .menu-icon {
  color: var(--sidebar-item-active-text);
}

.menu-text {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ── 主内容区 ─────────────────────────────── */
.admin-content {
  flex: 1;
  background: var(--bg-page);
  overflow-y: auto;
  padding: var(--space-lg16);
}
</style>
