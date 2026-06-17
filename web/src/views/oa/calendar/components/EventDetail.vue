<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Drawer, Tag, Descriptions, Button, Space, List, Avatar, message, Popconfirm } from 'ant-design-vue'
import { UserOutlined, EditOutlined, PlayCircleOutlined, StopOutlined, CloseCircleOutlined, LinkOutlined } from '@ant-design/icons-vue'
import { getCalendarEventDetail, startCalendarEvent, endCalendarEvent, cancelCalendarEvent } from '@/api/oa'
import {
  CalendarEventStatus,
  CalendarEventPriority,
  AttendeeResponseStatus,
  AttendeeAttendStatus,
  SyncStatus,
  STATUS_COLOR_MAP,
  STATUS_LABEL_MAP,
  PRIORITY_LABEL_MAP,
} from '@/types/calendar'
import type { CalendarEventDto, CalendarEventAttendeeDto } from '@/types/calendar'

const props = defineProps<{
  visible: boolean
  eventId: number
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'edit'): void
  (e: 'statusChanged'): void
}>()

const loading = ref(false)
const event = ref<CalendarEventDto | null>(null)

// 加载事件详情
async function loadEventDetail() {
  if (!props.eventId) return
  
  loading.value = true
  try {
    const res = await getCalendarEventDetail(props.eventId) as CalendarEventDto
    event.value = res
  } catch {
    message.error('加载会议详情失败')
  } finally {
    loading.value = false
  }
}

// 监听 visible 变化
watch(() => props.visible, (visible) => {
  if (visible && props.eventId) {
    loadEventDetail()
  }
})

// 监听 eventId 变化
watch(() => props.eventId, (id) => {
  if (id && props.visible) {
    loadEventDetail()
  }
})

// 获取优先级标签颜色
const priorityColor = computed(() => {
  if (!event.value) return 'default'
  switch (event.value.priority) {
    case CalendarEventPriority.Urgent:
      return 'red'
    case CalendarEventPriority.Important:
      return 'orange'
    default:
      return 'default'
  }
})

// 格式化时间
function formatTime(timeStr?: string): string {
  if (!timeStr) return '-'
  return new Date(timeStr).toLocaleString('zh-CN', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

// 格式化日期
function formatDate(timeStr?: string): string {
  if (!timeStr) return '-'
  return new Date(timeStr).toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

// 获取回复状态标签
function getResponseStatusTag(status: AttendeeResponseStatus) {
  switch (status) {
    case AttendeeResponseStatus.Accepted:
      return { color: 'success', text: '已接受' }
    case AttendeeResponseStatus.Declined:
      return { color: 'error', text: '已拒绝' }
    case AttendeeResponseStatus.Tentative:
      return { color: 'warning', text: '待定' }
    default:
      return { color: 'default', text: '未回复' }
  }
}

// 获取出席状态标签
function getAttendStatusTag(status: AttendeeAttendStatus) {
  switch (status) {
    case AttendeeAttendStatus.Attended:
      return { color: 'success', text: '已出席' }
    case AttendeeAttendStatus.Absent:
      return { color: 'error', text: '缺席' }
    case AttendeeAttendStatus.Late:
      return { color: 'warning', text: '迟到' }
    default:
      return { color: 'default', text: '未知' }
  }
}

// 获取同步状态标签
const syncStatusTag = computed(() => {
  if (!event.value) return { color: 'default', text: '未知' }
  switch (event.value.syncStatus) {
    case SyncStatus.Synced:
      return { color: 'success', text: '已同步' }
    case SyncStatus.SyncFailed:
      return { color: 'error', text: '同步失败' }
    default:
      return { color: 'default', text: '未同步' }
  }
})

// 是否显示操作按钮
const showEditButton = computed(() => {
  return event.value?.status === CalendarEventStatus.Pending
})

const showStartButton = computed(() => {
  return event.value?.status === CalendarEventStatus.Pending
})

const showEndButton = computed(() => {
  return event.value?.status === CalendarEventStatus.InProgress
})

const showCancelButton = computed(() => {
  return event.value?.status === CalendarEventStatus.Pending
})

// 操作处理
function handleEdit() {
  emit('edit')
  emit('update:visible', false)
}

async function handleStart() {
  if (!event.value) return
  try {
    await startCalendarEvent(event.value.id)
    message.success('会议已开始')
    emit('statusChanged')
    loadEventDetail()
  } catch {
    message.error('操作失败')
  }
}

async function handleEnd() {
  if (!event.value) return
  try {
    await endCalendarEvent(event.value.id)
    message.success('会议已结束')
    emit('statusChanged')
    loadEventDetail()
  } catch {
    message.error('操作失败')
  }
}

async function handleCancel() {
  if (!event.value) return
  try {
    await cancelCalendarEvent(event.value.id)
    message.success('会议已取消')
    emit('statusChanged')
    emit('update:visible', false)
  } catch {
    message.error('操作失败')
  }
}

function handleClose() {
  emit('update:visible', false)
}
</script>

<template>
  <a-drawer
    :open="visible"
    title="会议详情"
    :width="480"
    :destroy-on-close="true"
    @close="handleClose"
  >
    <div v-if="event" class="event-detail">
      <!-- 标题区域 -->
      <div class="detail-header">
        <h3 class="event-title">{{ event.title }}</h3>
        <div class="event-tags">
          <a-tag :color="STATUS_COLOR_MAP[event.status]">
            {{ STATUS_LABEL_MAP[event.status] }}
          </a-tag>
          <a-tag :color="priorityColor">
            {{ PRIORITY_LABEL_MAP[event.priority] }}
          </a-tag>
        </div>
      </div>
      
      <!-- 基本信息 -->
      <a-descriptions :column="1" size="small" bordered class="info-section">
        <a-descriptions-item label="计划时间">
          <div v-if="event.isAllDay">
            {{ formatDate(event.startTime) }} - {{ formatDate(event.endTime) }}
            <a-tag size="small" style="margin-left: 8px">全天</a-tag>
          </div>
          <div v-else>
            {{ formatTime(event.startTime) }} - {{ formatTime(event.endTime) }}
          </div>
        </a-descriptions-item>
        
        <a-descriptions-item v-if="event.actualStartTime" label="实际开始">
          {{ formatTime(event.actualStartTime) }}
        </a-descriptions-item>
        
        <a-descriptions-item v-if="event.actualEndTime" label="实际结束">
          {{ formatTime(event.actualEndTime) }}
        </a-descriptions-item>
        
        <a-descriptions-item label="地点">
          {{ event.location || '-' }}
        </a-descriptions-item>
        
        <a-descriptions-item label="组织者">
          {{ event.organizerName }}
        </a-descriptions-item>
        
        <a-descriptions-item label="所属组织">
          {{ event.orgName }}
        </a-descriptions-item>
        
        <a-descriptions-item v-if="event.isRecurring" label="周期性">
          是
          <span v-if="event.recurrenceEndDate" class="recurrence-end">
            (截止至 {{ formatDate(event.recurrenceEndDate) }})
          </span>
        </a-descriptions-item>
      </a-descriptions>
      
      <!-- 描述 -->
      <div v-if="event.description" class="info-section">
        <div class="section-title">描述/议程</div>
        <div class="description-content">{{ event.description }}</div>
      </div>
      
      <!-- 参与者列表 -->
      <div class="info-section">
        <div class="section-title">
          参与者 ({{ event.attendees.length }})
        </div>
        <a-list
          :data-source="event.attendees"
          size="small"
          class="attendee-list"
        >
          <template #renderItem="{ item }: { item: CalendarEventAttendeeDto }">
            <a-list-item>
              <a-list-item-meta>
                <template #avatar>
                  <a-avatar :size="32">
                    <template #icon><UserOutlined /></template>
                  </a-avatar>
                </template>
                <template #title>
                  <span>{{ item.userName }}</span>
                  <a-tag v-if="item.isRequired" size="small" color="blue" style="margin-left: 8px">必填</a-tag>
                </template>
                <template #description>
                  <a-space size="small">
                    <a-tag :color="getResponseStatusTag(item.responseStatus).color" size="small">
                      {{ getResponseStatusTag(item.responseStatus).text }}
                    </a-tag>
                    <a-tag :color="getAttendStatusTag(item.attendStatus).color" size="small">
                      {{ getAttendStatusTag(item.attendStatus).text }}
                    </a-tag>
                  </a-space>
                </template>
              </a-list-item-meta>
            </a-list-item>
          </template>
        </a-list>
      </div>
      
      <!-- 钉钉同步状态 -->
      <div class="info-section">
        <div class="section-title">同步状态</div>
        <div class="sync-info">
          <a-tag :color="syncStatusTag.color">
            <LinkOutlined v-if="event.syncStatus === SyncStatus.Synced" />
            {{ syncStatusTag.text }}
          </a-tag>
          <span v-if="event.lastSyncTime" class="sync-time">
            上次同步: {{ formatTime(event.lastSyncTime) }}
          </span>
        </div>
      </div>
    </div>
    
    <!-- 底部操作按钮 -->
    <template #footer>
      <a-space v-if="event">
        <a-button v-if="showEditButton" @click="handleEdit">
          <template #icon><EditOutlined /></template>
          编辑
        </a-button>
        
        <a-button
          v-if="showStartButton"
          type="primary"
          @click="handleStart"
        >
          <template #icon><PlayCircleOutlined /></template>
          开始会议
        </a-button>
        
        <a-button
          v-if="showEndButton"
          type="primary"
          @click="handleEnd"
        >
          <template #icon><StopOutlined /></template>
          结束会议
        </a-button>
        
        <a-popconfirm
          v-if="showCancelButton"
          title="确定要取消这个会议吗？"
          ok-text="确定"
          cancel-text="取消"
          @confirm="handleCancel"
        >
          <a-button danger>
            <template #icon><CloseCircleOutlined /></template>
            取消会议
          </a-button>
        </a-popconfirm>
      </a-space>
    </template>
  </a-drawer>
</template>

<style scoped lang="scss">
.event-detail {
  .detail-header {
    margin-bottom: 16px;

    .event-title {
      margin: 0 0 12px 0;
      font-size: 18px;
      font-weight: 600;
      color: #262626;
    }

    .event-tags {
      display: flex;
      gap: 8px;
    }
  }

  .info-section {
    margin-bottom: 20px;

    .section-title {
      font-weight: 500;
      font-size: 14px;
      color: #262626;
      margin-bottom: 12px;
      padding-left: 12px;
      border-left: 3px solid var(--color-info);
    }

    .description-content {
      padding: 12px;
      background: #f5f5f5;
      border-radius: 4px;
      color: #595959;
      line-height: 1.6;
      white-space: pre-wrap;
    }

    .recurrence-end {
      color: #8c8c8c;
      margin-left: 8px;
    }
  }

  .attendee-list {
    :deep(.ant-list-item) {
      padding: 8px 0;
    }

    :deep(.ant-list-item-meta) {
      align-items: center;
    }

    :deep(.ant-list-item-meta-title) {
      margin-bottom: 4px;
      font-size: 14px;
    }

    :deep(.ant-list-item-meta-description) {
      font-size: 12px;
    }
  }

  .sync-info {
    display: flex;
    align-items: center;
    gap: 12px;

    .sync-time {
      color: #8c8c8c;
      font-size: 12px;
    }
  }
}
</style>
