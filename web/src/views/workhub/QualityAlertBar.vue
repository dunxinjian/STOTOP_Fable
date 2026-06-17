<script setup lang="ts">
/**
 * QualityAlertBar.vue — 中栏全宽紧急条
 *
 * 仅当任一质量域存在超时项时显示（真正紧急），数据来自共享 useQualitySummary。
 * 点击「查看」跳转异常列表。
 */
import { useRouter } from 'vue-router'
import { WarningOutlined } from '@ant-design/icons-vue'
import { useQualitySummary } from '@/composables/useQualitySummary'

const router = useRouter()
const { exception, dataQuality, hasOverdue } = useQualitySummary()

function goToExceptions() {
  router.push('/quality/exceptions')
}
</script>

<template>
  <div v-if="hasOverdue" class="quality-alert-bar">
    <div class="alert-left">
      <WarningOutlined class="alert-icon" />
      <span class="alert-title">质量预警</span>
    </div>
    <div class="alert-center">
      <span v-if="exception.overdue > 0">异常 <span class="alert-num">{{ exception.overdue }}</span> 项已超时</span>
      <span v-if="exception.overdue > 0 && dataQuality.overdue > 0" class="alert-dot">·</span>
      <span v-if="dataQuality.overdue > 0">运单数据 <span class="alert-num">{{ dataQuality.overdue }}</span> 项已超时</span>
    </div>
    <div class="alert-right" @click="goToExceptions">
      查看 →
    </div>
  </div>
</template>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.quality-alert-bar {
  display: flex;
  align-items: center;
  height: 40px;
  background: color-mix(in srgb, var(--biz-quality) 8%, transparent);
  border-left: 3px solid var(--biz-quality);
  border-radius: 0 var(--radius-lg) var(--radius-lg) 0;
  margin: var(--space-sm8) var(--space-lg16) 0 var(--space-lg16);
  padding: 0 var(--space-md12);
  flex-shrink: 0;
  font-size: var(--font-sm2);
  color: var(--text-2);
  box-shadow: var(--shadow-sm);
}

.alert-left {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.alert-icon {
  color: var(--biz-quality);
  font-size: 14px;
}

.alert-title {
  font-weight: 600;
  font-size: 14px;
  color: var(--text-1);
}

.alert-center {
  flex: 1;
  text-align: center;
  font-size: 13px;
  color: var(--text-2);
}

.alert-num {
  font-weight: 600;
  color: var(--biz-quality);
}

.alert-dot {
  margin: 0 6px;
  color: var(--text-disabled);
}

.alert-right {
  font-size: 13px;
  color: var(--text-1);
  cursor: pointer;
  flex-shrink: 0;
  white-space: nowrap;

  &:hover {
    color: var(--color-primary);
    opacity: 0.8;
  }
}
</style>
