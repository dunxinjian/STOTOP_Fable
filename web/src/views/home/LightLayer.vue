<template>
  <div class="light-layer">
    <div class="action-grid">
      <button
        v-for="a in actions"
        :key="a.label"
        class="action-card"
        @click="router.push(a.route)"
      >
        <component :is="a.icon" class="action-card__icon" />
        <span class="action-card__label">{{ a.label }}</span>
      </button>
    </div>

    <section>
      <div class="light-title">我交的单</div>
      <div v-if="mine.length" class="mine-list">
        <button
          v-for="it in mine"
          :key="it.id"
          class="mine-item"
          @click="router.push(it.detailRoute || '/workhub')"
        >
          <span class="mine-item__title">{{ it.title }}</span>
          <span class="mine-item__type">{{ it.bizTypeLabel }}</span>
        </button>
      </div>
      <a-empty v-else description="你还没有提交中的单据" />
    </section>

    <WorkHubRecentVisits />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, markRaw } from 'vue'
import { useRouter } from 'vue-router'
import { UploadOutlined, FileAddOutlined, FileSearchOutlined, ShopOutlined } from '@ant-design/icons-vue'
import WorkHubRecentVisits from '@/views/workhub/WorkHubRecentVisits.vue'
import { getWorkItems, type WorkItem } from '@/api/workhub'

const router = useRouter()
const actions = [
  { label: '上传数据', icon: markRaw(UploadOutlined), route: '/cardflow/upload-center' },
  { label: '发起申请', icon: markRaw(FileAddOutlined), route: '/cardflow/home' },
  { label: '查我的单', icon: markRaw(FileSearchOutlined), route: '/workhub' },
  { label: '我的网点', icon: markRaw(ShopOutlined), route: '/quality/dashboard' },
]

const mine = ref<WorkItem[]>([])
onMounted(async () => {
  try {
    const res = await getWorkItems({ category: 'initiated', pageSize: 5, page: 1 })
    mine.value = res.items
  } catch {
    /* 容错 */
  }
})
</script>

<style scoped>
.light-layer {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.action-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 12px;
}

.action-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 20px 12px;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--bg-card);
  cursor: pointer;
}

.action-card:hover {
  border-color: var(--color-primary);
  background: var(--bg-muted);
}

.action-card__icon {
  font-size: 24px;
  color: var(--color-primary);
}

.action-card__label {
  font-size: 14px;
  color: var(--text-1);
}

.light-title {
  font-size: 13px;
  color: var(--text-2);
  margin-bottom: 8px;
}

.mine-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.mine-item {
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

.mine-item:hover {
  border-color: var(--color-primary);
  background: var(--bg-muted);
}

.mine-item__title {
  font-size: 13px;
  color: var(--text-1);
}

.mine-item__type {
  font-size: 12px;
  color: var(--text-3);
}
</style>
