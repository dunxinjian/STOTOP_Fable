<script setup lang="ts">
import { computed } from 'vue'
import { Steps as VanSteps, Step as VanStep, Tag as VanTag } from 'vant'
import 'vant/es/steps/style'
import 'vant/es/step/style'
import 'vant/es/tag/style'

export interface TimelineTask {
  id: string
  nodeDisplayName: string
  approverName?: string
  status: string
  statusText: string
  comment?: string
  completedTime?: string
  createdTime?: string
}

const props = defineProps<{
  tasks: TimelineTask[]
}>()

const currentStep = computed(() => {
  const idx = props.tasks.findIndex(t => t.status === 'Pending' || t.status === 'Running')
  return idx >= 0 ? idx : props.tasks.length
})

function getTagType(status: string): 'success' | 'danger' | 'warning' | 'primary' | 'default' {
  switch (status) {
    case 'Completed':
    case 'Approved':
      return 'success'
    case 'Rejected':
      return 'danger'
    case 'Pending':
    case 'Running':
      return 'warning'
    case 'Cancelled':
      return 'default'
    default:
      return 'primary'
  }
}

function formatTime(time?: string): string {
  if (!time) return ''
  const d = new Date(time)
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}
</script>

<template>
  <div class="mobile-timeline">
    <VanSteps direction="vertical" :active="currentStep" active-color="#07c160">
      <VanStep v-for="task in tasks" :key="task.id">
        <div class="step-header">
          <span class="step-title">{{ task.nodeDisplayName }}</span>
          <VanTag :type="getTagType(task.status)" size="medium">{{ task.statusText }}</VanTag>
        </div>
        <div v-if="task.approverName" class="step-meta">
          {{ task.approverName }}
          <span v-if="task.completedTime" class="step-time">· {{ formatTime(task.completedTime) }}</span>
        </div>
        <div v-if="task.comment" class="step-comment">
          "{{ task.comment }}"
        </div>
      </VanStep>
    </VanSteps>
  </div>
</template>

<style scoped>
.mobile-timeline {
  padding: 0 8px;
}
.step-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.step-title {
  font-size: 14px;
  font-weight: 500;
  color: #323233;
}
.step-meta {
  font-size: 12px;
  color: #969799;
  margin-top: 4px;
}
.step-time {
  color: #c8c9cc;
}
.step-comment {
  font-size: 13px;
  color: #646566;
  margin-top: 4px;
  padding: 6px 10px;
  background: #f7f8fa;
  border-radius: 4px;
  font-style: italic;
}
</style>
