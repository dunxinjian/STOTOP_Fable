<script setup lang="ts">
/**
 * QualitySummaryCard.vue — 数据质量概况卡片
 *
 * 在 WorkHub 右栏默认态显示，为管理角色提供快速质量概览。
 * 点击可跳转到质量管理看板。
 */
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  WarningOutlined,
  PlusCircleOutlined,
  ClockCircleOutlined,
  RightOutlined,
} from '@ant-design/icons-vue'
import { getWorkHubQualitySummary, type WorkHubQualitySummaryDto } from '@/api/qualityCenter'

const router = useRouter()
const loading = ref(false)
const data = ref<WorkHubQualitySummaryDto | null>(null)

async function fetchData() {
  loading.value = true
  try {
    const res = await getWorkHubQualitySummary() as any
    data.value = res ?? null
  } catch {
    // silent
  } finally {
    loading.value = false
  }
}

function goToDashboard() {
  router.push('/express/quality-center/dashboard')
}

let timer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  fetchData()
  timer = setInterval(fetchData, 5 * 60 * 1000)
})

onUnmounted(() => {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
})
</script>

<template>
  <div class="quality-summary-card" @click="goToDashboard">
    <div class="card-header">
      <WarningOutlined class="card-header__icon" />
      <span class="card-header__title">运单与仓储质量</span>
      <RightOutlined class="card-header__arrow" />
    </div>

    <a-spin :spinning="loading" size="small">
      <div v-if="data" class="card-body">
        <div class="metric-item">
          <ClockCircleOutlined class="metric-icon pending" />
          <div class="metric-info">
            <span class="metric-value">{{ data.pendingTotal }}</span>
            <span class="metric-label">待处理</span>
          </div>
        </div>
        <div class="metric-item">
          <PlusCircleOutlined class="metric-icon new" />
          <div class="metric-info">
            <span class="metric-value">{{ data.todayNew }}</span>
            <span class="metric-label">今日新增</span>
          </div>
        </div>
        <div class="metric-item">
          <WarningOutlined class="metric-icon overdue" />
          <div class="metric-info">
            <span class="metric-value" :class="{ 'has-warning': data.overdueWarning > 0 }">
              {{ data.overdueWarning }}
            </span>
            <span class="metric-label">超时预警</span>
          </div>
        </div>
      </div>
      <div v-else class="card-body card-empty">
        <span class="empty-text">暂无待处理质量问题</span>
      </div>
    </a-spin>
  </div>
</template>

<style scoped>
.quality-summary-card {
  border-bottom: 1px solid var(--border);
  cursor: pointer;
  transition: background 0.2s, box-shadow 0.2s;
  background: var(--bg-card);
}

.quality-summary-card:hover {
  background: var(--bg-muted);
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
  flex: 1;
}

.card-header__arrow {
  font-size: 11px;
  color: var(--text-3);
}

.card-body {
  display: flex;
  gap: 10px;
  padding: 2px 20px 16px;
}

.card-empty {
  justify-content: center;
  min-height: 40px;
  align-items: center;
}

.empty-text {
  font-size: 12px;
  color: var(--text-3);
}

.metric-item {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
  padding: 10px 12px;
  border-radius: var(--radius-lg);
  background: var(--bg-card);
  border: 1px solid var(--border);
  box-shadow: var(--shadow-sm);
}

.metric-icon {
  font-size: 18px;
  flex-shrink: 0;
}

.metric-icon.pending {
  color: var(--color-warning);
}

.metric-icon.new {
  color: var(--color-info);
}

.metric-icon.overdue {
  color: var(--color-danger);
}

.metric-info {
  display: flex;
  flex-direction: column;
}

.metric-value {
  font-size: 22px;
  font-weight: 700;
  line-height: 1.2;
  color: var(--text-1);
}

.metric-value.has-warning {
  color: var(--color-danger);
}

.metric-label {
  font-size: 12px;
  color: var(--text-3);
  line-height: 1.3;
  white-space: nowrap;
}
</style>
