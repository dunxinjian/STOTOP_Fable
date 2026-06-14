<script setup lang="ts">
/**
 * QualityAlertBar.vue — 质量预警紧凑告警条
 *
 * 仅当有待处理异常时显示，无异常时完全隐藏。
 * 点击"查看全部"跳转到 /quality/exceptions。
 */
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { WarningOutlined } from '@ant-design/icons-vue'
import { getQualityDashboardStats } from '@/api/quality'

interface QualityStats {
  totalExceptions: number
  pendingCount: number
  overdueCount: number
  todayNewCount: number
}

const router = useRouter()
const stats = ref<QualityStats | null>(null)

const hasAlert = computed(() => {
  return stats.value && stats.value.pendingCount > 0
})

async function fetchStats() {
  try {
    const res = (await getQualityDashboardStats()) as any
    stats.value = res ?? null
  } catch (err) {
    console.error('[QualityAlertBar] 获取质量统计失败:', err)
  }
}

function goToExceptions() {
  router.push('/quality/exceptions')
}

let timer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  fetchStats()
  // 每 5 分钟轮询一次
  timer = setInterval(fetchStats, 5 * 60 * 1000)
})

onUnmounted(() => {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
})
</script>

<template>
  <div v-if="hasAlert" class="quality-alert-bar">
    <div class="alert-left">
      <WarningOutlined class="alert-icon" />
      <span class="alert-title">质量预警</span>
    </div>
    <div class="alert-center">
      待处理 <span class="alert-num">{{ stats!.pendingCount }}</span>
      <span class="alert-dot">·</span>
      已超时 <span class="alert-num">{{ stats!.overdueCount }}</span>
      <span class="alert-dot">·</span>
      今日新增 <span class="alert-num">{{ stats!.todayNewCount }}</span>
    </div>
    <div class="alert-right" @click="goToExceptions">
      查看全部 →
    </div>
  </div>
</template>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.quality-alert-bar {
  display: flex;
  align-items: center;
  height: 40px;
  background: rgba(250, 84, 28, 0.075);
  border-left: 3px solid #fa541c;
  border-radius: 0 8px 8px 0;
  margin: 8px 18px 0 18px;
  padding: 0 14px;
  flex-shrink: 0;
  font-size: 13px;
  color: $text-regular;
  box-shadow: 0 1px 2px rgba(250, 84, 28, 0.06);
}

.alert-left {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.alert-icon {
  color: #fa541c;
  font-size: 14px;
}

.alert-title {
  font-weight: 600;
  font-size: 14px;
  color: $text-primary;
}

.alert-center {
  flex: 1;
  text-align: center;
  font-size: 13px;
  color: $text-secondary;
}

.alert-num {
  font-weight: 600;
  color: #fa541c;
}

.alert-dot {
  margin: 0 6px;
  color: $text-placeholder;
}

.alert-right {
  font-size: 13px;
  color: $color-primary;
  cursor: pointer;
  flex-shrink: 0;
  white-space: nowrap;

  &:hover {
    opacity: 0.8;
  }
}
</style>
