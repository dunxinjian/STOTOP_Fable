<template>
  <div class="kpi-bar">
    <template v-for="(item, idx) in kpiItems" :key="idx">
      <div v-if="idx > 0" class="kpi-divider"></div>
      <div class="kpi-item">
        <span class="kpi-dot" :class="item.dot"></span>
        <span class="kpi-value">{{ item.value }}</span>
        <span class="kpi-label">{{ item.label }}</span>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { KpiItem } from '../composables/useUploadCenter'

defineProps<{
  kpiItems: KpiItem[]
}>()
</script>

<style scoped lang="scss">
.kpi-bar {
  display: flex;
  align-items: center;
  gap: 0;
  padding: 0;
  margin-bottom: 0;
}

.kpi-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 24px;

  &:first-child {
    padding-left: 0;
  }
}

.kpi-divider {
  width: 1px;
  height: 20px;
  background: #e8e8e8;
  flex-shrink: 0;
}

.kpi-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;

  &.blue { background: #1677ff; }
  &.blue-pulse { background: #1677ff; animation: pulse 1.5s infinite; }
  &.red { background: #ff4d4f; }
  &.orange { background: #faad14; }
  &.green { background: #52c41a; }
  &.purple { background: #722ed1; }
}

.kpi-value {
  font-size: 16px;
  font-weight: 600;
  color: #262626;
}

.kpi-label {
  font-size: 13px;
  color: rgba(0, 0, 0, 0.45);
  margin-left: 4px;
  white-space: nowrap;
}

@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.5; transform: scale(1.2); }
}
</style>
