<template>
  <div
    class="stat-card"
    :class="{ 'stat-card--clickable': clickable }"
    @click="handleClick"
  >
    <div class="stat-card__title">{{ title }}</div>
    <div class="stat-card__value" :style="{ color: color }">
      {{ value }}<span v-if="suffix" class="stat-card__suffix">{{ suffix }}</span>
    </div>
    <div v-if="subValue" class="stat-card__sub">{{ subValue }}</div>
    <a-progress
      v-if="progress !== undefined"
      :percent="progress"
      :stroke-color="computedProgressColor"
      :show-info="false"
      size="small"
      class="stat-card__progress"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  /** 指标标题 */
  title: string
  /** 主数值 */
  value: string | number
  /** 数值后缀 */
  suffix?: string
  /** 附加展示 */
  subValue?: string
  /** 进度百分比 0-100 */
  progress?: number
  /** 进度条颜色 */
  progressColor?: string
  /** 是否可点击 */
  clickable?: boolean
  /** 数值颜色 */
  color?: string
}

const props = withDefaults(defineProps<Props>(), {
  suffix: undefined,
  subValue: undefined,
  progress: undefined,
  progressColor: undefined,
  clickable: true,
  color: '#1677ff',
})

const emit = defineEmits<{
  click: [e: MouseEvent]
}>()

const computedProgressColor = computed(() => {
  if (props.progressColor) return props.progressColor
  if (props.progress === undefined) return '#1677ff'
  if (props.progress >= 100) return '#52c41a'
  if (props.progress >= 50) return '#1677ff'
  return '#fa8c16'
})

function handleClick(e: MouseEvent) {
  if (props.clickable) {
    emit('click', e)
  }
}
</script>

<style scoped lang="scss">
.stat-card {
  background: #fff;
  border-radius: 8px;
  padding: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  height: 100%;
  transition: box-shadow 0.3s, transform 0.3s;

  &--clickable {
    cursor: pointer;

    &:hover {
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
      transform: translateY(-1px);
    }
  }

  &__title {
    color: #8c8c8c;
    font-size: 14px;
    margin-bottom: 8px;
    line-height: 1.4;
  }

  &__value {
    font-size: 24px;
    font-weight: 600;
    line-height: 1.3;
  }

  &__suffix {
    font-size: 14px;
    font-weight: 400;
    margin-left: 2px;
  }

  &__sub {
    color: #8c8c8c;
    font-size: 13px;
    margin-top: 4px;
  }

  &__progress {
    margin-top: 8px;
  }
}
</style>
