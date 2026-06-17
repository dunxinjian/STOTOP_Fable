<script setup lang="ts">
/**
 * QualitySummaryCard.vue — 工作台右栏「质量」卡
 *
 * 按域两行展示，数据来自共享的 useQualitySummary 单一真源：
 *   - 异常处理 → /quality/exceptions
 *   - 运单与仓储数据 → /express/quality-center/dashboard
 */
import { useRouter } from 'vue-router'
import {
  WarningOutlined,
  AlertOutlined,
  DatabaseOutlined,
  RightOutlined,
} from '@ant-design/icons-vue'
import { useQualitySummary } from '@/composables/useQualitySummary'

const router = useRouter()
const { exception, dataQuality, loading } = useQualitySummary()

function goExceptions() {
  router.push('/quality/exceptions')
}
function goDataQuality() {
  router.push('/express/quality-center/dashboard')
}
</script>

<template>
  <a-spin :spinning="loading" size="small">
    <div class="quality-card">
      <div class="card-header">
        <WarningOutlined class="card-header__icon" />
        <span class="card-header__title">质量</span>
      </div>

      <div class="quality-row" @click="goExceptions">
        <AlertOutlined class="row-icon" />
        <div class="row-main">
          <div class="row-title">异常处理</div>
          <div class="row-sub">通用异常框架</div>
        </div>
        <div class="row-metrics">
          <span class="metric" :class="{ muted: exception.pending === 0 }">待处理 {{ exception.pending }}</span>
          <span class="metric metric--minor" :class="{ danger: exception.overdue > 0 }">· 超时 {{ exception.overdue }}</span>
        </div>
        <RightOutlined class="row-arrow" />
      </div>

      <div class="quality-row" @click="goDataQuality">
        <DatabaseOutlined class="row-icon" />
        <div class="row-main">
          <div class="row-title">运单与仓储数据</div>
          <div class="row-sub">数据质量中心</div>
        </div>
        <div class="row-metrics">
          <span class="metric" :class="{ muted: dataQuality.pending === 0 }">待处理 {{ dataQuality.pending }}</span>
          <span class="metric metric--minor" :class="{ danger: dataQuality.overdue > 0 }">· 超时 {{ dataQuality.overdue }}</span>
        </div>
        <RightOutlined class="row-arrow" />
      </div>
    </div>
  </a-spin>
</template>

<style scoped>
.quality-card {
  background: var(--bg-card);
  border-bottom: 1px solid var(--border);
}
.card-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 14px 20px 10px;
}
.card-header__icon {
  font-size: 14px;
  color: var(--biz-quality);
}
.card-header__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-1);
}
.quality-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 20px;
  border-top: 1px solid var(--border);
  cursor: pointer;
  transition: background 0.2s;
}
.quality-row:hover {
  background: var(--bg-muted);
}
.row-icon {
  font-size: 18px;
  color: var(--text-3);
  flex-shrink: 0;
}
.row-main {
  flex: 1;
  min-width: 0;
}
.row-title {
  font-size: 13px;
  color: var(--text-1);
}
.row-sub {
  font-size: 12px;
  color: var(--text-3);
}
.row-metrics {
  display: flex;
  align-items: baseline;
  gap: 4px;
  flex-shrink: 0;
}
.metric {
  font-size: 13px;
  color: var(--text-1);
}
.metric.muted {
  color: var(--text-3);
}
.metric--minor {
  font-size: 12px;
  color: var(--text-3);
}
.metric--minor.danger {
  color: var(--color-danger);
}
.row-arrow {
  font-size: 11px;
  color: var(--text-3);
  flex-shrink: 0;
}
</style>
