/**
 * useQualitySummary — 工作台质量摘要单一真源
 *
 * 聚合两个不同子系统的质量摘要，供右栏「质量」卡与中栏紧急条共享：
 *   - 异常域：getQualityDashboardStats() → 通用异常框架 (/quality/exceptions)
 *   - 数据域：getWorkHubQualitySummary() → 数据质量中心 (/express/quality-center)
 *
 * 模块级单例 + 引用计数：首个消费组件挂载时拉取并启动 5 分钟轮询，
 * 最后一个卸载时停止，避免两组件各自轮询导致口径分叉与重复请求。
 */
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { getQualityDashboardStats } from '@/api/quality'
import { getWorkHubQualitySummary } from '@/api/qualityCenter'

interface DomainSummary {
  pending: number
  overdue: number
}

const exception = ref<DomainSummary>({ pending: 0, overdue: 0 })
const dataQuality = ref<DomainSummary>({ pending: 0, overdue: 0 })
const loading = ref(false)

const hasOverdue = computed(
  () => exception.value.overdue > 0 || dataQuality.value.overdue > 0
)

let timer: ReturnType<typeof setInterval> | null = null
let refCount = 0

async function refresh() {
  loading.value = true
  try {
    const [exRes, dqRes] = await Promise.allSettled([
      getQualityDashboardStats(),
      getWorkHubQualitySummary(),
    ])
    if (exRes.status === 'fulfilled' && exRes.value) {
      const v = exRes.value as any
      exception.value = { pending: v.pendingCount ?? 0, overdue: v.overdueCount ?? 0 }
    }
    if (dqRes.status === 'fulfilled' && dqRes.value) {
      const v = dqRes.value as any
      dataQuality.value = { pending: v.pendingTotal ?? 0, overdue: v.overdueWarning ?? 0 }
    }
  } finally {
    loading.value = false
  }
}

export function useQualitySummary() {
  onMounted(() => {
    refCount++
    if (refCount === 1) {
      refresh()
      timer = setInterval(refresh, 5 * 60 * 1000)
    }
  })
  onUnmounted(() => {
    refCount--
    if (refCount <= 0) {
      refCount = 0
      if (timer) {
        clearInterval(timer)
        timer = null
      }
    }
  })
  return { exception, dataQuality, loading, hasOverdue, refresh }
}
