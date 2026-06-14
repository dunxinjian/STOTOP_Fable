<template>
  <div class="recent-visits-panel">
    <!-- 头部 -->
    <div class="panel-header">
      <span class="panel-header__title">快速导航</span>
    </div>

    <!-- 内容区 -->
    <div class="panel-body">
      <div class="warehouse-workband">
        <div>
          <strong>仓配工作带</strong>
          <span>运单、入库、异常与结算入口集中处理</span>
        </div>
        <div class="dock-bars" aria-hidden="true">
          <span style="height: 18px"></span>
          <span style="height: 28px"></span>
          <span style="height: 22px"></span>
        </div>
      </div>

      <!-- 最近访问芯片 -->
      <div class="section-label">最近访问</div>
      <div class="pages-chips" v-if="recommendationStore.recentPages.length > 0">
        <div
          v-for="item in recommendationStore.recentPages"
          :key="item.path"
          class="page-chip"
          @click="router.push(item.path)"
        >
          <component :is="getPageIcon(item.path)" class="chip-icon" />
          <span class="chip-name">{{ getDisplayTitle(item.title) }}</span>
          <span class="chip-time">{{ formatRelativeTime(item.lastVisitTime) }}</span>
        </div>
      </div>
      <div class="pages-empty" v-else>
        <span>浏览页面后将自动记录</span>
      </div>

      <!-- 高频功能列表 -->
      <template v-if="recommendationStore.frequentPages.length > 0">
        <div class="section-label section-label--top">常用功能</div>
        <div class="frequent-list">
          <div
            v-for="item in recommendationStore.frequentPages.slice(0, 8)"
            :key="item.path"
            class="frequent-item"
            @click="router.push(item.path)"
          >
            <component :is="getPageIcon(item.path)" class="frequent-icon" />
            <span class="frequent-name">{{ getDisplayTitle(item.title) }}</span>
            <span class="visit-count">{{ item.visitCount }} 次</span>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useRecommendationStore } from '@/stores/recommendation'
import {
  LineChartOutlined, FileOutlined, AuditOutlined, TeamOutlined,
  BarChartOutlined, HomeOutlined, ShopOutlined, SettingOutlined,
} from '@ant-design/icons-vue'

const router = useRouter()
const recommendationStore = useRecommendationStore()

function getPageIcon(path: string) {
  if (path.includes('finance')) return LineChartOutlined
  if (path.includes('oa')) return AuditOutlined
  if (path.includes('hr')) return TeamOutlined
  if (path.includes('quality')) return BarChartOutlined
  if (path.includes('dashboard')) return HomeOutlined
  if (path.includes('shop') || path.includes('crm')) return ShopOutlined
  if (path.includes('setting')) return SettingOutlined
  return FileOutlined
}

function getDisplayTitle(title: string) {
  if (title === '数据质量中心') return '运单质量中心'
  if (title === '快速成本方案') return '快递成本方案'
  return title
}

function formatRelativeTime(timestamp: number): string {
  const diff = Date.now() - timestamp
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) return '刚刚'
  if (minutes < 60) return `${minutes}分钟前`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}小时前`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}天前`
  return new Date(timestamp).toLocaleDateString('zh-CN')
}
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.recent-visits-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #fff;
  border-left: 0;
  overflow: hidden;
}

.panel-header {
  display: flex;
  align-items: center;
  height: 54px;
  padding: 0 20px;
  border-bottom: 1px solid #edf0f4;
  flex-shrink: 0;
  background: linear-gradient(180deg, #fff, #fbfcfd);
}

.panel-header__title {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}

.panel-body {
  flex: 1;
  overflow-y: auto;
  padding: 18px 20px 24px;

  &::-webkit-scrollbar {
    width: 4px;
  }

  &::-webkit-scrollbar-thumb {
    background: rgba(0, 0, 0, 0.12);
    border-radius: 2px;
  }
}

.section-label {
  font-size: 12px;
  color: $text-secondary;
  font-weight: 500;
  margin-bottom: 12px;
  letter-spacing: 0;

  &--top {
    margin-top: 18px;
    padding-top: 18px;
    border-top: 1px solid #edf0f4;
  }
}

.warehouse-workband {
  min-height: 76px;
  margin-bottom: 18px;
  border-radius: 8px;
  border: 1px solid #e5ebf2;
  background:
    linear-gradient(90deg, rgba(255, 103, 0, 0.08), rgba(18, 184, 189, 0.07)),
    repeating-linear-gradient(90deg, rgba(32, 36, 44, 0.06) 0 1px, transparent 1px 28px),
    #fbfcfd;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  padding: 12px 14px;

  strong {
    display: block;
    color: $text-primary;
    font-size: 14px;
    margin-bottom: 5px;
  }

  span {
    color: $text-secondary;
    font-size: 12px;
    line-height: 1.4;
  }
}

.dock-bars {
  display: flex;
  align-items: flex-end;
  gap: 5px;
  flex: 0 0 auto;

  span {
    width: 20px;
    border-radius: 4px 4px 2px 2px;
    background: rgba(255, 103, 0, 0.22);
    border: 1px solid rgba(255, 103, 0, 0.24);
  }
}

.pages-chips {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.page-chip {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 7px 12px;
  background: #f6f9ff;
  border: 1px solid rgba(22, 119, 255, 0.12);
  border-radius: 18px;
  cursor: pointer;
  transition: all 0.2s ease;
  max-width: 220px;

  &:hover {
    background: rgba(22, 119, 255, 0.12);
    border-color: rgba(22, 119, 255, 0.3);
    .chip-name { color: $color-primary; }
    .chip-icon { color: $color-primary; }
  }
}

.chip-icon {
  font-size: 13px;
  color: $text-secondary;
  display: flex;
  align-items: center;
  flex-shrink: 0;
}

.chip-name {
  font-size: 12px;
  color: $text-regular;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 90px;
}

.chip-time {
  font-size: 11px;
  color: $text-placeholder;
  flex-shrink: 0;
}

.pages-empty {
  font-size: 13px;
  color: $text-secondary;
  padding: 12px 0;
  text-align: center;
}

.frequent-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.frequent-item {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 38px;
  padding: 8px 10px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    background: rgba(22, 119, 255, 0.05);
  }
}

.frequent-icon {
  font-size: 14px;
  color: $text-secondary;
  flex-shrink: 0;
}

.frequent-name {
  font-size: 13px;
  color: $text-regular;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.visit-count {
  font-size: 11px;
  background: rgba(255, 103, 0, 0.08);
  color: #ff6700;
  padding: 2px 8px;
  border-radius: 10px;
  font-weight: 500;
  flex-shrink: 0;
}
</style>
