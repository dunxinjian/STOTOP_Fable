<template>
  <div class="power-layer">
    <button class="todo-cta" @click="goWorkhub">
      <div class="todo-cta__main">今天要清 <b>{{ total }}</b> 条</div>
      <div class="todo-cta__sub">进入处理 →</div>
    </button>

    <section>
      <div class="power-title">待办流</div>
      <div v-if="items.length" class="todo-list">
        <button
          v-for="it in items"
          :key="it.id"
          class="todo-item"
          @click="openItem(it)"
        >
          <span class="todo-item__title">{{ it.title }}</span>
          <span class="todo-item__type">{{ it.bizTypeLabel }}</span>
        </button>
      </div>
      <a-empty v-else description="今天没有待办，已清零" />
    </section>

    <WorkHubRecentVisits />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import WorkHubRecentVisits from '@/views/workhub/WorkHubRecentVisits.vue'
import { getWorkItemsWithStats, type WorkItem } from '@/api/workhub'

const router = useRouter()
const total = ref(0)
const items = ref<WorkItem[]>([])

onMounted(async () => {
  try {
    const res = await getWorkItemsWithStats({ pageSize: 5, page: 1 })
    total.value = res.stats.total
    items.value = res.items.items
  } catch {
    /* 容错 */
  }
})

function goWorkhub() {
  router.push('/workhub')
}
function openItem(it: WorkItem) {
  router.push(it.detailRoute || '/workhub')
}
</script>

<style scoped>
.power-layer {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.todo-cta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  border: 1px solid var(--color-primary);
  border-radius: var(--radius-lg);
  background: var(--bg-card);
  cursor: pointer;
  text-align: left;
}

.todo-cta__main {
  font-size: 15px;
  color: var(--text-1);
}

.todo-cta__sub {
  font-size: 13px;
  color: var(--color-primary);
}

.power-title {
  font-size: 13px;
  color: var(--text-2);
  margin-bottom: 8px;
}

.todo-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.todo-item {
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

.todo-item:hover {
  border-color: var(--color-primary);
  background: var(--bg-muted);
}

.todo-item__title {
  font-size: 13px;
  color: var(--text-1);
}

.todo-item__type {
  font-size: 12px;
  color: var(--text-3);
}
</style>
