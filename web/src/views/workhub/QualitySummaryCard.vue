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
  border-bottom: 1px solid #edf0f4;
  cursor: pointer;
  transition: background 0.2s, box-shadow 0.2s;
  background:
    linear-gradient(135deg, rgba(255, 103, 0, 0.08), rgba(255, 255, 255, 0) 42%),
    #fff;
}

.quality-summary-card:hover {
  background:
    linear-gradient(135deg, rgba(255, 103, 0, 0.11), rgba(255, 255, 255, 0) 42%),
    #fff;
  box-shadow: inset 0 -1px 0 rgba(255, 103, 0, 0.12);
}

.card-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 14px 20px 10px;
}

.card-header__icon {
  font-size: 14px;
  color: #fa541c;
}

.card-header__title {
  font-size: 15px;
  font-weight: 600;
  color: #262626;
  flex: 1;
}

.card-header__arrow {
  font-size: 11px;
  color: #bfbfbf;
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
  color: #999;
}

.metric-item {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
  padding: 10px 12px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.78);
  border: 1px solid rgba(18, 31, 53, 0.06);
  box-shadow: 0 1px 2px rgba(18, 31, 53, 0.04);
}

.metric-icon {
  font-size: 18px;
  flex-shrink: 0;
}

.metric-icon.pending {
  color: #faad14;
}

.metric-icon.new {
  color: #1890ff;
}

.metric-icon.overdue {
  color: #ff4d4f;
}

.metric-info {
  display: flex;
  flex-direction: column;
}

.metric-value {
  font-size: 22px;
  font-weight: 700;
  line-height: 1.2;
  color: #262626;
}

.metric-value.has-warning {
  color: #ff4d4f;
}

.metric-label {
  font-size: 12px;
  color: #8c8c8c;
  line-height: 1.3;
  white-space: nowrap;
}
</style>
