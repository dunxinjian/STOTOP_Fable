<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import { Drawer, Form, Input, Switch, DatePicker, Radio, Select, Button, Space, message } from 'ant-design-vue'
import type { FormInstance } from 'ant-design-vue'
import { createCalendarEvent, updateCalendarEvent, getCalendarEventDetail } from '@/api/oa'
import { CalendarEventPriority } from '@/types/calendar'
import type { CreateCalendarEventRequest } from '@/types/calendar'
import { useOrgContextStore } from '@/stores/orgContext'
import RecurrenceEditor from './RecurrenceEditor.vue'
import AttendeeSelector from './AttendeeSelector.vue'

const props = defineProps<{
  visible: boolean
  eventId?: number
  defaultStartTime?: string
  defaultEndTime?: string
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'saved'): void
}>()

const orgStore = useOrgContextStore()
const formRef = ref<FormInstance>()
const loading = ref(false)
const isEdit = computed(() => !!props.eventId)

// 颜色预设
const colorPresets = ['#1890ff', '#52c41a', '#faad14', '#f5222d', '#722ed1', '#13c2c2', '#fa541c', '#eb2f96']

// 提醒选项
const remindOptions = [
  { label: '不提醒', value: 0 },
  { label: '5分钟前', value: 5 },
  { label: '15分钟前', value: 15 },
  { label: '30分钟前', value: 30 },
  { label: '1小时前', value: 60 },
]

// 优先级选项
const priorityOptions = [
  { label: '普通', value: CalendarEventPriority.Normal },
  { label: '重要', value: CalendarEventPriority.Important },
  { label: '紧急', value: CalendarEventPriority.Urgent },
]

// 表单数据
const formData = ref<Partial<CreateCalendarEventRequest>>({
  title: '',
  description: '',
  location: '',
  isAllDay: false,
  startTime: '',
  endTime: '',
  priority: CalendarEventPriority.Normal,
  isRecurring: false,
  recurrenceRule: '',
  recurrenceEndDate: undefined,
  orgId: orgStore.currentOrgId || 0,
  attendeeUserIds: [],
  remindMinutes: 15,
  color: '#1890ff',
  syncToDingTalk: false,
})

// 日期选择器值
const dateRange = ref<[import('dayjs').Dayjs, import('dayjs').Dayjs] | undefined>(undefined)

// 加载事件详情
async function loadEventDetail() {
  if (!props.eventId) return
  
  loading.value = true
  try {
    const res = await getCalendarEventDetail(props.eventId) as any
    formData.value = {
      title: res.title,
      description: res.description,
      location: res.location,
      isAllDay: res.isAllDay,
      startTime: res.startTime,
      endTime: res.endTime,
      priority: res.priority,
      isRecurring: res.isRecurring,
      recurrenceRule: res.recurrenceRule || '',
      recurrenceEndDate: res.recurrenceEndDate,
      orgId: res.orgId,
      attendeeUserIds: res.attendees?.map((a: any) => a.userId) || [],
      remindMinutes: res.remindMinutes,
      color: res.color || '#1890ff',
      syncToDingTalk: false,
    }
    
    // 设置日期范围
    if (res.startTime && res.endTime) {
      dateRange.value = [dayjs(res.startTime), dayjs(res.endTime)]
    }
  } catch {
    message.error('加载事件详情失败')
  } finally {
    loading.value = false
  }
}

// 监听 visible 变化
watch(() => props.visible, (visible) => {
  if (visible) {
    if (props.eventId) {
      loadEventDetail()
    } else {
      // 新建模式，使用默认值
      resetForm()
      if (props.defaultStartTime && props.defaultEndTime) {
        formData.value.startTime = props.defaultStartTime
        formData.value.endTime = props.defaultEndTime
        dateRange.value = [dayjs(props.defaultStartTime), dayjs(props.defaultEndTime)]
      } else {
        // 默认今天
        const now = dayjs()
        const start = now.startOf('hour').add(1, 'hour')
        const end = start.add(1, 'hour')
        formData.value.startTime = start.format('YYYY-MM-DD HH:mm:ss')
        formData.value.endTime = end.format('YYYY-MM-DD HH:mm:ss')
        dateRange.value = [start, end]
      }
    }
  }
})

// 重置表单
function resetForm() {
  formData.value = {
    title: '',
    description: '',
    location: '',
    isAllDay: false,
    startTime: '',
    endTime: '',
    priority: CalendarEventPriority.Normal,
    isRecurring: false,
    recurrenceRule: '',
    recurrenceEndDate: undefined,
    orgId: orgStore.currentOrgId || 0,
    attendeeUserIds: [],
    remindMinutes: 15,
    color: '#1890ff',
    syncToDingTalk: false,
  }
  dateRange.value = undefined
}

// 监听日期范围变化
watch(dateRange, (val) => {
  if (val && val[0] && val[1]) {
    if (formData.value.isAllDay) {
      formData.value.startTime = val[0].startOf('day').format('YYYY-MM-DD HH:mm:ss')
      formData.value.endTime = val[1].endOf('day').format('YYYY-MM-DD HH:mm:ss')
    } else {
      formData.value.startTime = val[0].format('YYYY-MM-DD HH:mm:ss')
      formData.value.endTime = val[1].format('YYYY-MM-DD HH:mm:ss')
    }
  }
})

// 监听全天事件变化
watch(() => formData.value.isAllDay, (isAllDay) => {
  if (dateRange.value && dateRange.value[0] && dateRange.value[1]) {
    if (isAllDay) {
      formData.value.startTime = dateRange.value[0].startOf('day').format('YYYY-MM-DD HH:mm:ss')
      formData.value.endTime = dateRange.value[1].endOf('day').format('YYYY-MM-DD HH:mm:ss')
    } else {
      formData.value.startTime = dateRange.value[0].format('YYYY-MM-DD HH:mm:ss')
      formData.value.endTime = dateRange.value[1].format('YYYY-MM-DD HH:mm:ss')
    }
  }
})

// 表单验证规则
const rules: Record<string, any> = {
  title: [{ required: true, message: '请输入会议标题', trigger: 'blur' }],
  startTime: [{ required: true, message: '请选择开始时间', trigger: 'change' }],
  endTime: [{ required: true, message: '请选择结束时间', trigger: 'change' }],
}

// 保存
async function handleSave() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }
  
  if (!formData.value.startTime || !formData.value.endTime) {
    message.error('请选择时间范围')
    return
  }
  
  loading.value = true
  try {
    const data: CreateCalendarEventRequest = {
      title: formData.value.title!,
      description: formData.value.description,
      location: formData.value.location,
      startTime: formData.value.startTime,
      endTime: formData.value.endTime,
      isAllDay: formData.value.isAllDay || false,
      priority: formData.value.priority || CalendarEventPriority.Normal,
      isRecurring: formData.value.isRecurring || false,
      recurrenceRule: formData.value.isRecurring ? formData.value.recurrenceRule : undefined,
      recurrenceEndDate: formData.value.isRecurring ? formData.value.recurrenceEndDate : undefined,
      orgId: formData.value.orgId || orgStore.currentOrgId || 0,
      attendeeUserIds: formData.value.attendeeUserIds || [],
      remindMinutes: formData.value.remindMinutes || 0,
      color: formData.value.color,
      syncToDingTalk: formData.value.syncToDingTalk || false,
    }
    
    if (isEdit.value && props.eventId) {
      await updateCalendarEvent(props.eventId, data)
      message.success('更新成功')
    } else {
      await createCalendarEvent(data)
      message.success('创建成功')
    }
    
    emit('saved')
    emit('update:visible', false)
  } catch {
    message.error(isEdit.value ? '更新失败' : '创建失败')
  } finally {
    loading.value = false
  }
}

// 取消
function handleCancel() {
  emit('update:visible', false)
}

// 监听周期规则结束日期变化
function handleRecurrenceEndDate(date: string | undefined) {
  formData.value.recurrenceEndDate = date
}
</script>

<template>
  <a-drawer
    :open="visible"
    :title="isEdit ? '编辑会议' : '新建会议'"
    :width="560"
    :destroy-on-close="true"
    @close="handleCancel"
  >
    <a-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      layout="vertical"
    >
      <!-- 标题 -->
      <a-form-item label="会议标题" name="title">
        <a-input
          v-model:value="formData.title"
          placeholder="请输入会议标题"
          :maxlength="200"
          show-count
        />
      </a-form-item>
      
      <!-- 全天事件 -->
      <a-form-item>
        <a-space>
          <a-switch v-model:checked="formData.isAllDay" />
          <span>全天事件</span>
        </a-space>
      </a-form-item>
      
      <!-- 时间范围 -->
      <a-form-item label="时间范围" required>
        <a-range-picker
          v-model:value="dateRange"
          :show-time="!formData.isAllDay ? { format: 'HH:mm' } : false"
          format="YYYY-MM-DD HH:mm"
          style="width: 100%"
          :placeholder="['开始时间', '结束时间']"
        />
      </a-form-item>
      
      <!-- 地点 -->
      <a-form-item label="地点">
        <a-input v-model:value="formData.location" placeholder="请输入会议地点" />
      </a-form-item>
      
      <!-- 描述 -->
      <a-form-item label="描述/议程">
        <a-textarea
          v-model:value="formData.description"
          :rows="4"
          placeholder="请输入会议描述或议程"
        />
      </a-form-item>
      
      <!-- 优先级 -->
      <a-form-item label="优先级">
        <a-radio-group v-model:value="formData.priority" :options="priorityOptions" />
      </a-form-item>
      
      <!-- 周期性设置 -->
      <a-form-item>
        <a-space direction="vertical" style="width: 100%">
          <a-space>
            <a-switch v-model:checked="formData.isRecurring" />
            <span>周期性会议</span>
          </a-space>
          <RecurrenceEditor
            v-if="formData.isRecurring"
            v-model="(formData as any).recurrenceRule"
            @update:end-date="handleRecurrenceEndDate"
          />
        </a-space>
      </a-form-item>
      
      <!-- 参与者 -->
      <a-form-item label="参与者">
        <AttendeeSelector v-model="(formData as any).attendeeUserIds" />
      </a-form-item>
      
      <!-- 提醒设置 -->
      <a-form-item label="提醒">
        <a-select v-model:value="formData.remindMinutes" :options="remindOptions" style="width: 150px" />
      </a-form-item>
      
      <!-- 颜色标记 -->
      <a-form-item label="颜色标记">
        <div class="color-picker">
          <div
            v-for="color in colorPresets"
            :key="color"
            class="color-item"
            :style="{ backgroundColor: color }"
            :class="{ active: formData.color === color }"
            @click="formData.color = color"
          />
        </div>
      </a-form-item>
      
      <!-- 同步到钉钉 -->
      <a-form-item>
        <a-space>
          <a-switch v-model:checked="formData.syncToDingTalk" />
          <span>同步到钉钉</span>
        </a-space>
      </a-form-item>
    </a-form>
    
    <!-- 底部按钮 -->
    <template #footer>
      <a-space>
        <a-button @click="handleCancel">取消</a-button>
        <a-button type="primary" :loading="loading" @click="handleSave">保存</a-button>
      </a-space>
    </template>
  </a-drawer>
</template>

<style scoped lang="scss">
.color-picker {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;

  .color-item {
    width: 24px;
    height: 24px;
    border-radius: 50%;
    cursor: pointer;
    transition: all 0.2s;
    border: 2px solid transparent;

    &:hover {
      transform: scale(1.1);
    }

    &.active {
      border-color: #262626;
      transform: scale(1.1);
    }
  }
}
</style>
