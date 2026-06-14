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
        <a-avatar v-else :size="24" style="background: linear-gradient(135deg, #722ED1, #9254DE); font-size: 12px;">
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
  background: #434352;
  border-bottom: none;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  z-index: 100;
  position: relative;
}

/* 紫色渐变底线 */
.admin-topbar::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, #722ED1 0%, #B37FEB 60%, transparent 100%);
  pointer-events: none;
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
  color: #B37FEB;
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
  background: #2B2D3A;
  overflow-y: auto;
  padding-top: 4px;
}

.menu-group {
  margin-bottom: 8px;
}

.group-title {
  color: rgba(255, 255, 255, 0.62); /* 提升对比度，达 WCAG AA（原 0.45 不达标） */
  font-size: 13px;                  /* 统一使用 $font-size-sm2 对应山（12px 在深色背景可读性差 */
  letter-spacing: 0.5px;
  padding: 16px 20px 8px;
  user-select: none;
}

.menu-item {
  display: flex;
  align-items: center;
  padding: 10px 20px;
  cursor: pointer;
  color: rgba(255, 255, 255, 0.68);
  font-size: 13px;
  transition: all 0.2s;
  border-left: 3px solid transparent;
}

.menu-item:hover {
  color: rgba(255, 255, 255, 0.92);
  background: rgba(255, 255, 255, 0.05);
}

.menu-item.active {
  color: #fff;
  background: rgba(114, 46, 209, 0.14);
  border-left-color: #722ED1;
}

.menu-icon {
  margin-right: 10px;
  font-size: 15px;
  opacity: 0.85;
}

.menu-text {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ── 主内容区 ─────────────────────────────── */
.admin-content {
  flex: 1;
  background: #F4F5F7;
  overflow-y: auto;
  padding: 16px;
}
</style>
