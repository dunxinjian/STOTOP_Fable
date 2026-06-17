<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import timeGridPlugin from '@fullcalendar/timegrid'
import interactionPlugin from '@fullcalendar/interaction'
import type { CalendarOptions, EventInput, DateSelectArg, EventClickArg, EventDropArg } from '@fullcalendar/core'
import zhCnLocale from '@fullcalendar/core/locales/zh-cn'
import type { CalendarEventDto } from '@/types/calendar'
import { STATUS_COLOR_MAP } from '@/types/calendar'

const props = defineProps<{
  events: CalendarEventDto[]
}>()

const emit = defineEmits<{
  (e: 'create', data: { startTime: string; endTime: string }): void
  (e: 'select', eventId: number): void
  (e: 'update', data: { id: number; startTime: string; endTime: string }): void
}>()

const calendarRef = ref<InstanceType<typeof FullCalendar>>()

// 将 CalendarEventDto 转换为 FullCalendar 的 EventInput
const calendarEvents = computed<EventInput[]>(() => {
  return props.events.map(event => ({
    id: String(event.id),
    title: event.title,
    start: event.startTime,
    end: event.endTime,
    allDay: event.isAllDay,
    backgroundColor: STATUS_COLOR_MAP[event.status],
    borderColor: STATUS_COLOR_MAP[event.status],
    textColor: '#fff',
    extendedProps: {
      ...event
    }
  }))
})

// 日历配置选项
const calendarOptions = computed<CalendarOptions>(() => ({
  plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
  initialView: 'dayGridMonth',
  locale: zhCnLocale,
  headerToolbar: {
    left: 'prev,next today',
    center: 'title',
    right: 'dayGridMonth,timeGridWeek,timeGridDay'
  },
  buttonText: {
    today: '今天',
    month: '月',
    week: '周',
    day: '日'
  },
  events: calendarEvents.value,
  editable: true,
  selectable: true,
  selectMirror: true,
  dayMaxEvents: true,
  weekends: true,
  height: 'auto',
  // 点击空白日期/时间创建新事件
  select: (selectInfo: DateSelectArg) => {
    emit('create', {
      startTime: selectInfo.startStr,
      endTime: selectInfo.endStr
    })
    selectInfo.view.calendar.unselect()
  },
  // 点击事件查看详情
  eventClick: (clickInfo: EventClickArg) => {
    const eventId = Number(clickInfo.event.id)
    emit('select', eventId)
  },
  // 拖拽调整时间
  eventDrop: (dropInfo: EventDropArg) => {
    emit('update', {
      id: Number(dropInfo.event.id),
      startTime: dropInfo.event.start?.toISOString() || '',
      endTime: dropInfo.event.end?.toISOString() || ''
    })
  },
  // 调整事件大小（改变结束时间）
  eventResize: (resizeInfo: any) => {
    emit('update', {
      id: Number(resizeInfo.event.id),
      startTime: resizeInfo.event.start?.toISOString() || '',
      endTime: resizeInfo.event.end?.toISOString() || ''
    })
  }
}))

// 监听事件数据变化，更新日历
watch(() => props.events, () => {
  const calendarApi = calendarRef.value?.getApi()
  if (calendarApi) {
    calendarApi.removeAllEvents()
    calendarApi.addEventSource(calendarEvents.value)
  }
}, { deep: true })
</script>

<template>
  <div class="calendar-view">
    <FullCalendar ref="calendarRef" :options="calendarOptions" />
  </div>
</template>

<style scoped lang="scss">
.calendar-view {
  :deep(.fc) {
    // FullCalendar 样式调整
    .fc-toolbar-title {
      font-size: 18px;
      font-weight: 600;
    }

    .fc-button {
      background-color: var(--color-primary);
      border-color: var(--color-primary);

      &:hover {
        background-color: var(--color-primary-hover);
        border-color: var(--color-primary-hover);
      }

      &:disabled {
        background-color: #f5f5f5;
        border-color: #d9d9d9;
        color: #999;
      }
    }

    .fc-button-primary:not(:disabled).fc-button-active,
    .fc-button-primary:not(:disabled):active {
      background-color: var(--color-primary-active);
      border-color: var(--color-primary-active);
    }

    .fc-day-today {
      background-color: var(--color-primary-light);
    }

    .fc-event {
      cursor: pointer;
      border-radius: 4px;
      padding: 2px 4px;
      font-size: 12px;

      &:hover {
        opacity: 0.9;
      }
    }

    .fc-timegrid-slot {
      height: 40px;
    }

    .fc-col-header-cell {
      padding: 8px 0;
      font-weight: 600;
      background-color: #fafafa;
    }
  }
}
</style>
