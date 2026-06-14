<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { PlusOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import dayjs from 'dayjs'
import { getCalendarEvents, getCalendarBoard } from '@/api/oa'
import type { CalendarEventDto, CalendarBoardData } from '@/types/calendar'
import CalendarView from './components/CalendarView.vue'
import BoardView from './components/BoardView.vue'
import EventForm from './components/EventForm.vue'
import EventDetail from './components/EventDetail.vue'

// 视图模式：calendar | board
const viewMode = ref<'calendar' | 'board'>('calendar')

// 日历事件数据
const events = ref<CalendarEventDto[]>([])
const boardData = ref<CalendarBoardData>({
  pending: [],
  inProgress: [],
  completed: [],
  early: [],
  delayed: [],
  cancelled: []
})
const loading = ref(false)

// 日期范围（用于查询）
const dateRange = computed(() => {
  const now = dayjs()
  return {
    startDate: now.startOf('month').format('YYYY-MM-DD'),
    endDate: now.endOf('month').format('YYYY-MM-DD')
  }
})

// 抽屉控制
const formDrawerVisible = ref(false)
const detailDrawerVisible = ref(false)
const selectedEventId = ref<number | null>(null)
const editingEventId = ref<number | undefined>(undefined)
const formInitialData = ref<{ startTime?: string; endTime?: string }>({})

// 加载日历事件
async function loadEvents(startDate?: string, endDate?: string) {
  loading.value = true
  try {
    const params: any = {
      startDate: startDate || dateRange.value.startDate,
      endDate: endDate || dateRange.value.endDate
    }
    const res = await getCalendarEvents(params) as any
    events.value = res || []
  } catch {
    message.error('加载会议数据失败')
  } finally {
    loading.value = false
  }
}

// 加载看板数据
async function loadBoardData() {
  loading.value = true
  try {
    const res = await getCalendarBoard({
      startDate: dateRange.value.startDate,
      endDate: dateRange.value.endDate
    }) as any
    boardData.value = res || {
      pending: [],
      inProgress: [],
      completed: [],
      early: [],
      delayed: [],
      cancelled: []
    }
  } catch {
    message.error('加载看板数据失败')
  } finally {
    loading.value = false
  }
}

// 刷新当前视图数据
async function refreshData() {
  if (viewMode.value === 'calendar') {
    await loadEvents()
  } else {
    await loadBoardData()
  }
}

// 创建新事件
function handleCreate(data: { startTime: string; endTime: string }) {
  formInitialData.value = data
  formDrawerVisible.value = true
}

// 查看事件详情
function handleSelect(eventId: number) {
  selectedEventId.value = eventId
  detailDrawerVisible.value = true
}

// 更新事件时间（拖拽调整）
async function handleUpdate(data: { id: number; startTime: string; endTime: string }) {
  // 这里可以调用更新API，或者由父组件处理
  message.success('会议时间已更新')
  // 刷新数据
  loadEvents()
}

// 打开新建表单
function handleOpenForm() {
  editingEventId.value = undefined
  formInitialData.value = {}
  formDrawerVisible.value = true
}

// 编辑事件
function handleEdit() {
  editingEventId.value = selectedEventId.value ?? undefined
  detailDrawerVisible.value = false
  formDrawerVisible.value = true
}

// 关闭表单抽屉
function handleFormClose() {
  formDrawerVisible.value = false
  formInitialData.value = {}
}

// 关闭详情抽屉
function handleDetailClose() {
  detailDrawerVisible.value = false
  selectedEventId.value = null
}

// 表单保存成功
function handleFormSaved() {
  formDrawerVisible.value = false
  formInitialData.value = {}
  editingEventId.value = undefined
  refreshData()
}

// 状态变更后刷新
function handleStatusChanged() {
  refreshData()
}

onMounted(() => {
  loadEvents()
})
</script>

<template>
  <div class="page-container">
    <!-- 顶部工具栏 -->
    <PageHeader title="会议日程">
      <template #left>
        <a-radio-group v-model:value="viewMode" button-style="solid">
          <a-radio-button value="calendar">日历视图</a-radio-button>
          <a-radio-button value="board">看板视图</a-radio-button>
        </a-radio-group>
      </template>
      <template #right>
        <a-button type="primary" @click="handleOpenForm">
          <template #icon><PlusOutlined /></template>
          新建会议
        </a-button>
      </template>
    </PageHeader>

    <!-- 内容区 -->
    <a-card :loading="loading" class="calendar-card">
      <CalendarView
        v-if="viewMode === 'calendar'"
        :events="events"
        @create="handleCreate"
        @select="handleSelect"
        @update="handleUpdate"
      />
      <BoardView
        v-else
        :board-data="boardData"
        @select="handleSelect"
        @create="handleOpenForm"
      />
    </a-card>

    <!-- 会议表单抽屉 -->
    <EventForm
      v-model:visible="formDrawerVisible"
      :event-id="editingEventId"
      :default-start-time="formInitialData.startTime"
      :default-end-time="formInitialData.endTime"
      @saved="handleFormSaved"
    />

    <!-- 会议详情抽屉 -->
    <EventDetail
      v-model:visible="detailDrawerVisible"
      :event-id="selectedEventId ?? 0"
      @edit="handleEdit"
      @status-changed="handleStatusChanged"
    />
  </div>
</template>

<style scoped lang="scss">
.page-container {
  padding: 16px;
}

.calendar-card {
  min-height: calc(100vh - 140px);
}

</style>
