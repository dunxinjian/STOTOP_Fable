<template>
  <div class="decision-layer">
    <div class="metric-grid">
      <StatCard title="待我决策" :value="approvalCount" clickable @click="goWorkhub" />
      <StatCard title="待办合计" :value="todoTotal" clickable @click="goWorkhub" />
      <div class="metric-placeholder">
        <div class="metric-placeholder__label">今日收入</div>
        <div class="metric-placeholder__hint">待接入</div>
      </div>
      <div class="metric-placeholder">
        <div class="metric-placeholder__label">毛利率</div>
        <div class="metric-placeholder__hint">待接入</div>
      </div>
    </div>

    <div class="decision-cols">
      <section class="decision-col">
        <div class="decision-col__title">异常雷达</div>
        <QualitySummaryCard />
      </section>
      <section class="decision-col">
        <div class="decision-col__title">待我决策</div>
        <div v-if="approvals.length" class="approval-list">
          <button
            v-for="it in approvals"
            :key="it.id"
            class="approval-item"
            @click="openItem(it)"
          >
            <span class="approval-item__title">{{ it.title }}</span>
            <span class="approval-item__type">{{ it.bizTypeLabel }}</span>
          </button>
        </div>
        <a-empty v-else description="暂无待决策事项" />
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import StatCard from '@/components/StatCard.vue'
import QualitySummaryCard from '@/views/workhub/QualitySummaryCard.vue'
import { getWorkHubStats, getWorkItems, type WorkItem } from '@/api/workhub'
import { usePermissionStore } from '@/stores/permission'
import { useNotificationStore } from '@/stores/notification'

const router = useRouter()
const permissionStore = usePermissionStore()
const notificationStore = useNotificationStore()

const approvalCount = ref(0)
const approvals = ref<WorkItem[]>([])
const todoTotal = computed(
  () => (notificationStore.todoCount?.total || 0) + (permissionStore.taskCount || 0),
)

onMounted(async () => {
  try {
    const stats = await getWorkHubStats()
    approvalCount.value = stats.approval
    const res = await getWorkItems({ category: 'approval', pageSize: 5, page: 1 })
    approvals.value = res.items
  } catch {
    /* 静默，骨架容错 */
  }
})

function goWorkhub() {
  router.push('/workhub')
}
function openItem(it: WorkItem) {
  if (it.detailRoute) router.push(it.detailRoute)
  else router.push('/workhub')
}
</script>

<style scoped>
.decision-layer {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.metric-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 12px;
}

.metric-placeholder {
  background: var(--bg-card);
  border: 1px dashed var(--border-strong);
  border-radius: var(--radius-lg);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-height: 120px;
  justify-content: center;
}

.metric-placeholder__label {
  font-size: 14px;
  color: var(--text-2);
}

.metric-placeholder__hint {
  font-size: 13px;
  color: var(--text-3);
}

.decision-cols {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.decision-col__title {
  font-size: 13px;
  color: var(--text-2);
  margin-bottom: 8px;
}

.approval-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.approval-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--bg-card);
  cursor: pointer;
  text-align: left;
}

.approval-item:hover {
  border-color: var(--color-primary);
  background: var(--bg-muted);
}

.approval-item__title {
  font-size: 13px;
  color: var(--text-1);
}

.approval-item__type {
  font-size: 12px;
  color: var(--text-3);
}
</style>
