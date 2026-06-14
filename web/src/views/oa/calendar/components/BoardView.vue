<script setup lang="ts">
import { computed } from 'vue'
import { Badge, Card, Tag, Space, Row, Col } from 'ant-design-vue'
import { TeamOutlined, PlusOutlined } from '@ant-design/icons-vue'
import type { CalendarBoardData, CalendarEventDto } from '@/types/calendar'
import {
  CalendarEventStatus,
  CalendarEventPriority,
  STATUS_COLOR_MAP,
  STATUS_LABEL_MAP,
  PRIORITY_LABEL_MAP,
} from '@/types/calendar'

const props = defineProps<{
  boardData: CalendarBoardData
}>()

const emit = defineEmits<{
  (e: 'select', eventId: number): void
  (e: 'create'): void
}>()

// 看板列配置
const columns = [
  { key: 'pending', status: CalendarEventStatus.Pending, title: '未召开' },
  { key: 'inProgress', status: CalendarEventStatus.InProgress, title: '进行中' },
  { key: 'completed', status: CalendarEventStatus.Completed, title: '已召开' },
  { key: 'early', status: CalendarEventStatus.Early, title: '提前' },
  { key: 'delayed', status: CalendarEventStatus.Delayed, title: '延后' },
  { key: 'cancelled', status: CalendarEventStatus.Cancelled, title: '取消' },
] as const

// 获取优先级标签颜色
function getPriorityColor(priority: CalendarEventPriority): string {
  switch (priority) {
    case CalendarEventPriority.Urgent:
      return 'red'
    case CalendarEventPriority.Important:
      return 'orange'
    default:
      return 'default'
  }
}

// 格式化时间范围
function formatTimeRange(event: CalendarEventDto): string {
  if (event.isAllDay) {
    return '全天'
  }
  const start = new Date(event.startTime)
  const end = new Date(event.endTime)
  const dateStr = start.toLocaleDateString('zh-CN', { month: 'short', day: 'numeric' })
  const startTime = start.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  const endTime = end.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  return `${dateStr} ${startTime}-${endTime}`
}

// 处理卡片点击
function handleCardClick(eventId: number) {
  emit('select', eventId)
}

// 处理新建
function handleCreate() {
  emit('create')
}
</script>

<template>
  <div class="board-view">
    <a-row :gutter="12">
      <a-col v-for="col in columns" :key="col.key" :span="4">
        <div class="board-column">
          <!-- 列标题 -->
          <div class="column-header" :style="{ borderLeftColor: STATUS_COLOR_MAP[col.status] }">
            <span class="status-dot" :style="{ backgroundColor: STATUS_COLOR_MAP[col.status] }"></span>
            <span class="column-title">{{ col.title }}</span>
            <a-badge
              :count="(boardData as any)[col.key]?.length || 0"
              :style="{ backgroundColor: STATUS_COLOR_MAP[col.status] }"
            />
          </div>
          
          <!-- 列内容 -->
          <div class="column-content">
            <div
              v-for="event in (boardData as any)[col.key]"
              :key="event.id"
              class="event-card-wrapper"
            >
              <a-card
                size="small"
                class="event-card"
                :hoverable="true"
                @click="handleCardClick(event.id)"
              >
                <div class="event-title">{{ event.title }}</div>
                <div class="event-time">{{ formatTimeRange(event) }}</div>
                <div class="event-organizer">组织者: {{ event.organizerName }}</div>
                <div class="event-footer">
                  <span class="attendee-count">
                    <TeamOutlined /> {{ event.attendeeCount }}
                  </span>
                  <a-tag
                    :color="getPriorityColor(event.priority)"
                    size="small"
                  >
                    {{ PRIORITY_LABEL_MAP[event.priority as CalendarEventPriority] }}
                  </a-tag>
                </div>
              </a-card>
            </div>
            
            <!-- 空状态 -->
            <div v-if="!(boardData as any)[col.key]?.length" class="empty-column">
              暂无会议
            </div>
          </div>
        </div>
      </a-col>
    </a-row>
    
    <!-- 新建按钮 -->
    <a-button
      type="primary"
      shape="circle"
      size="large"
      class="fab-button"
      @click="handleCreate"
    >
      <PlusOutlined />
    </a-button>
  </div>
</template>

<style scoped lang="scss">
.board-view {
  padding: 16px;
  height: 100%;
  overflow-x: auto;
  position: relative;

  .board-column {
    display: flex;
    flex-direction: column;
    height: calc(100vh - 180px);
    background: #f5f5f5;
    border-radius: 8px;
    overflow: hidden;
  }

  .column-header {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 12px 16px;
    background: #fff;
    border-bottom: 1px solid #e8e8e8;
    border-left: 4px solid;

    .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
    }

    .column-title {
      flex: 1;
      font-weight: 500;
      font-size: 14px;
    }
  }

  .column-content {
    flex: 1;
    overflow-y: auto;
    padding: 12px;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .event-card-wrapper {
    cursor: pointer;
  }

  .event-card {
    transition: all 0.2s;

    &:hover {
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      transform: translateY(-2px);
    }

    :deep(.ant-card-body) {
      padding: 12px;
    }

    .event-title {
      font-weight: 600;
      font-size: 14px;
      margin-bottom: 8px;
      color: #262626;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .event-time {
      font-size: 12px;
      color: #8c8c8c;
      margin-bottom: 4px;
    }

    .event-organizer {
      font-size: 12px;
      color: #595959;
      margin-bottom: 8px;
    }

    .event-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;

      .attendee-count {
        font-size: 12px;
        color: #8c8c8c;
        display: flex;
        align-items: center;
        gap: 4px;
      }
    }
  }

  .empty-column {
    text-align: center;
    color: #bfbfbf;
    font-size: 12px;
    padding: 24px 0;
  }

  .fab-button {
    position: fixed;
    right: 32px;
    bottom: 32px;
    width: 56px;
    height: 56px;
    font-size: 24px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    z-index: 100;
  }
}
</style>
