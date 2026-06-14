<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'

const props = defineProps<{
  modelValue: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'update:endDate', value: string | undefined): void
}>()

// 频率选项
const frequencyOptions = [
  { label: '每天', value: 'DAILY' },
  { label: '每周', value: 'WEEKLY' },
  { label: '每月', value: 'MONTHLY' },
]

// 星期选项
const weekDayOptions = [
  { label: '周一', value: 'MO' },
  { label: '周二', value: 'TU' },
  { label: '周三', value: 'WE' },
  { label: '周四', value: 'TH' },
  { label: '周五', value: 'FR' },
  { label: '周六', value: 'SA' },
  { label: '周日', value: 'SU' },
]

// 结束方式
const endTypeOptions = [
  { label: '永不结束', value: 'never' },
  { label: '指定次数后结束', value: 'count' },
  { label: '指定日期结束', value: 'until' },
]

// 内部状态
const frequency = ref('DAILY')
const interval = ref(1)
const byDay = ref<string[]>(['MO', 'TU', 'WE', 'TH', 'FR'])
const endType = ref('never')
const count = ref(10)
const untilDate = ref<Dayjs>()

// 解析 RRULE
function parseRRule(rrule: string) {
  if (!rrule || !rrule.startsWith('FREQ=')) return
  
  const parts = rrule.split(';')
  const map: Record<string, string> = {}
  
  parts.forEach(part => {
    const [key, value] = part.split('=')
    if (key && value) {
      map[key] = value
    }
  })
  
  // 频率
  if (map.FREQ) {
    frequency.value = map.FREQ
  }
  
  // 间隔
  if (map.INTERVAL) {
    interval.value = parseInt(map.INTERVAL, 10)
  }
  
  // 星期
  if (map.BYDAY) {
    byDay.value = map.BYDAY.split(',')
  }
  
  // 结束方式
  if (map.COUNT) {
    endType.value = 'count'
    count.value = parseInt(map.COUNT, 10)
  } else if (map.UNTIL) {
    endType.value = 'until'
    // 解析日期格式 YYYYMMDD
    const untilStr = map.UNTIL
    if (untilStr.length >= 8) {
      const year = untilStr.substring(0, 4)
      const month = untilStr.substring(4, 6)
      const day = untilStr.substring(6, 8)
      untilDate.value = dayjs(`${year}-${month}-${day}`)
    }
  } else {
    endType.value = 'never'
  }
}

// 生成 RRULE
function generateRRule(): string {
  const parts: string[] = [`FREQ=${frequency.value}`]
  
  if (interval.value > 1) {
    parts.push(`INTERVAL=${interval.value}`)
  }
  
  if (frequency.value === 'WEEKLY' && byDay.value.length > 0) {
    parts.push(`BYDAY=${byDay.value.join(',')}`)
  }
  
  if (endType.value === 'count') {
    parts.push(`COUNT=${count.value}`)
  } else if (endType.value === 'until' && untilDate.value) {
    const formatted = untilDate.value.format('YYYYMMDD')
    parts.push(`UNTIL=${formatted}`)
  }
  
  return parts.join(';')
}

// 监听变化，生成新的 RRULE
watch([frequency, interval, byDay, endType, count, untilDate], () => {
  const rrule = generateRRule()
  emit('update:modelValue', rrule)
  
  // 计算结束日期
  let endDate: string | undefined
  if (endType.value === 'until' && untilDate.value) {
    endDate = untilDate.value.format('YYYY-MM-DD')
  }
  emit('update:endDate', endDate)
}, { immediate: true })

// 监听外部值变化
watch(() => props.modelValue, (newVal) => {
  if (newVal) {
    parseRRule(newVal)
  }
}, { immediate: true })

// 频率文本
const intervalLabel = computed(() => {
  switch (frequency.value) {
    case 'DAILY':
      return '天'
    case 'WEEKLY':
      return '周'
    case 'MONTHLY':
      return '月'
    default:
      return ''
  }
})
</script>

<template>
  <div class="recurrence-editor">
    <a-space direction="vertical" style="width: 100%">
      <!-- 频率选择 -->
      <a-form-item label="重复频率" style="margin-bottom: 12px">
        <a-select v-model:value="frequency" :options="frequencyOptions" style="width: 120px" />
      </a-form-item>
      
      <!-- 间隔 -->
      <a-form-item label="每" style="margin-bottom: 12px">
        <a-input-number v-model:value="interval" :min="1" :max="99" style="width: 80px" />
        <span class="interval-label">{{ intervalLabel }}</span>
      </a-form-item>
      
      <!-- 星期选择（仅每周） -->
      <a-form-item v-if="frequency === 'WEEKLY'" label="重复于" style="margin-bottom: 12px">
        <a-checkbox-group v-model:value="byDay" :options="weekDayOptions" />
      </a-form-item>
      
      <!-- 结束方式 -->
      <a-form-item label="结束方式" style="margin-bottom: 0">
        <a-radio-group v-model:value="endType" :options="endTypeOptions" />
      </a-form-item>
      
      <!-- 指定次数 -->
      <a-form-item v-if="endType === 'count'" style="margin-bottom: 0; margin-left: 24px">
        <a-input-number v-model:value="count" :min="1" :max="999" style="width: 100px" />
        <span class="suffix-text">次后结束</span>
      </a-form-item>
      
      <!-- 指定日期 -->
      <a-form-item v-if="endType === 'until'" style="margin-bottom: 0; margin-left: 24px">
        <a-date-picker v-model:value="untilDate" placeholder="选择结束日期" />
      </a-form-item>
    </a-space>
  </div>
</template>

<style scoped lang="scss">
.recurrence-editor {
  padding: 8px 0;

  .interval-label {
    margin-left: 8px;
    color: #595959;
  }

  .suffix-text {
    margin-left: 8px;
    color: #595959;
  }

  :deep(.ant-checkbox-group) {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
  }

  :deep(.ant-radio-group) {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
}
</style>
