<script setup lang="ts">
/**
 * WorkHubCenter.vue — 工作台中栏组件
 *
 * 整合待办 Feed 流、通知功能区。
 * 通过 emit select-item 通知父组件在右栏展示详情。
 */
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import type { Dayjs } from 'dayjs'
import {
  BellOutlined,
  CheckOutlined,
  AuditOutlined,
  WarningOutlined,
  CheckSquareOutlined,
  ImportOutlined,
  FileTextOutlined,
  TrophyOutlined,
  CloseOutlined,
  CaretRightOutlined,
} from '@ant-design/icons-vue'
import { message } from 'ant-design-vue'
import { useWorkHub } from '@/composables/useWorkHub'
import { useUserStore } from '@/stores/user'
import { getNotifications, getUnreadCount, markNotificationRead, markAllNotificationsRead } from '@/api/task'
import type { NotificationListDto } from '@/types/task'
import type { WorkItem } from '@/api/workhub'
import WorkItemCard from './WorkItemCard.vue'
import WorkItemSkeleton from './WorkItemSkeleton.vue'
import QualityAlertBar from './QualityAlertBar.vue'

// ===== Emits =====
const emit = defineEmits<{
  'select-item': [payload: { type: 'workitem' | 'notification'; id: string; data: any }]
}>()

// ===== Composables / Stores =====
const hub = useWorkHub()
const userStore = useUserStore()

// ===== Tab 状态 =====
type TabKey = 'all' | 'todo' | 'notification'
const activeTab = ref<TabKey>('todo')

// ===== 通知相关状态 =====
const notifications = ref<NotificationListDto[]>([])
const notificationUnreadCount = ref(0)
const notificationsLoading = ref(false)

// ===== 待办 Tab 状态 =====
const activeSource = ref<string>('')
const dateRangeValue = ref<[Dayjs, Dayjs] | undefined>(undefined)

// ===== 来源配置 =====
const statsItems = computed(() => [
  { key: 'oa', label: '审批', count: hub.stats.value.approval, color: '#1677ff', icon: AuditOutlined },
  { key: 'quality', label: '异常', count: hub.stats.value.alert, color: '#fa541c', icon: WarningOutlined },
  { key: 'task', label: '任务', count: hub.stats.value.task, color: '#52c41a', icon: CheckSquareOutlined },
  { key: 'datacenter', label: '运单', count: hub.stats.value.reminder, color: '#722ed1', icon: ImportOutlined },
  { key: 'contract', label: '合同', count: hub.stats.value.notification, color: '#7B5B3A', icon: FileTextOutlined },
  { key: 'points', label: '积分', count: 0, color: '#d4b106', icon: TrophyOutlined },
])

const sourceOptions = [
  { label: 'OA', value: 'oa' },
  { label: '质量', value: 'quality' },
  { label: '任务', value: 'task' },
  { label: '运单', value: 'datacenter' },
  { label: '合同', value: 'contract' },
  { label: '积分', value: 'points' },
]

const hasActiveFilters = computed(() =>
  hub.filters.value.sources.length > 0 ||
  !!hub.filters.value.priority ||
  !!hub.filters.value.dateRange
)

// ===== 紧急/即将超时统计（从 items 派生） =====
const urgentCount = computed(() =>
  hub.items.value.filter(item => item.priority === 'urgent').length
)

const expiringCount = computed(() => {
  const endOfToday = new Date()
  endOfToday.setHours(23, 59, 59, 999)
  const endTs = endOfToday.getTime()
  const nowTs = Date.now()
  return hub.items.value.filter(item => {
    if (!item.deadline) return false
    const t = new Date(item.deadline).getTime()
    if (Number.isNaN(t)) return false
    // 即将超时：截止时间在当前到今日 24:00 之间（不含已过期项）
    return t >= nowTs && t <= endTs
  }).length
})

function filterUrgent() {
  hub.setFilter('priority', 'urgent')
}

function filterExpiring() {
  // 现有筛选器仅支持 sources / priority / dateRange，
  // 这里使用今天作为时间范围作为最接近“即将超时”的可用筛选
  const today = new Date()
  const y = today.getFullYear()
  const m = String(today.getMonth() + 1).padStart(2, '0')
  const d = String(today.getDate()).padStart(2, '0')
  const todayStr = `${y}-${m}-${d}`
  hub.setFilter('dateRange', [todayStr, todayStr])
}

const emptyDesc = computed(() => {
  if (hasActiveFilters.value) return '当前筛选条件下没有工作项，试试调整筛选条件'
  return '可以发起常用流程、查看全部工作，或稍后等待实时推送'
})

// ===== Tab Badge 计算 =====
const todoBadge = computed(() => hub.stats.value.total || 0)
const notificationBadge = computed(() => notificationUnreadCount.value || 0)
const allBadge = computed(() => todoBadge.value + notificationBadge.value)

// ===== 混合列表（全部 Tab）=====
type MixedItem =
  | { type: 'workitem'; data: WorkItem; time: string }
  | { type: 'notification'; data: NotificationListDto; time: string }

const mixedList = computed<MixedItem[]>(() => {
  const items: MixedItem[] = []

  // 工作项
  hub.items.value.forEach(item => {
    items.push({
      type: 'workitem',
      data: item,
      time: (item as any).timestamp || (item as any).createdAt || '',
    })
  })

  // 通知
  notifications.value.forEach(n => {
    items.push({
      type: 'notification',
      data: n,
      time: n.createTime,
    })
  })

  // 按时间倒序
  return items.sort((a, b) => {
    const ta = a.time ? new Date(a.time).getTime() : 0
    const tb = b.time ? new Date(b.time).getTime() : 0
    return tb - ta
  })
})

// ===== 工具函数 =====
function formatTime(val?: string) {
  if (!val) return ''
  const now = new Date()
  const d = new Date(val)
  const diff = now.getTime() - d.getTime()
  if (diff < 60 * 1000) return '刚刚'
  if (diff < 60 * 60 * 1000) return `${Math.floor(diff / 60000)}分钟前`
  if (diff < 24 * 60 * 60 * 1000) return `${Math.floor(diff / 3600000)}小时前`
  if (diff < 7 * 24 * 60 * 60 * 1000) {
    return d.toLocaleDateString('zh-CN', { month: '2-digit', day: '2-digit' })
  }
  return d.toLocaleDateString('zh-CN', { year: '2-digit', month: '2-digit', day: '2-digit' })
}

function getNotificationIcon(type: number) {
  switch (type) {
    case 1: return '📋'
    case 2: return '✅'
    default: return '🔔'
  }
}

// 工作项来源颜色映射（用于混合列表）
const sourceColorMap: Record<string, string> = {
  oa: '#1677ff',
  quality: '#fa541c',
  task: '#52c41a',
  datacenter: '#722ed1',
  contract: '#7B5B3A',
  points: '#d4b106',
  finance: '#faad14',
  system: '#595959',
}
const sourceLabelMap: Record<string, string> = {
  oa: 'OA',
  quality: '异常',
  task: '任务',
  datacenter: '运单',
  contract: '合同',
  points: '积分',
  finance: '财务',
  system: '系统',
}

// ===== 通知 API =====
async function loadNotifications() {
  if (notificationsLoading.value) return
  notificationsLoading.value = true
  try {
    const res = await getNotifications({ pageIndex: 1, pageSize: 50 })
    notifications.value = res?.items || []
  } catch (err) {
    console.error('[WorkHubCenter] 加载通知失败:', err)
  } finally {
    notificationsLoading.value = false
  }
}

async function loadNotificationUnreadCount() {
  try {
    const res = await getUnreadCount()
    notificationUnreadCount.value = res?.total || 0
  } catch {
    // 忽略
  }
}

async function handleMarkRead(notification: NotificationListDto) {
  if (!notification.isRead) {
    try {
      await markNotificationRead(notification.id)
      notification.isRead = true
      notificationUnreadCount.value = Math.max(0, notificationUnreadCount.value - 1)
    } catch (err) {
      console.error('[WorkHubCenter] 标记已读失败:', err)
    }
  }
}

async function handleMarkAllRead() {
  try {
    await markAllNotificationsRead()
    notifications.value.forEach(n => (n.isRead = true))
    notificationUnreadCount.value = 0
  } catch (err) {
    console.error('[WorkHubCenter] 全部标记已读失败:', err)
  }
}

// ===== 通知点击 =====
async function handleNotificationClick(notification: NotificationListDto) {
  await handleMarkRead(notification)
  emit('select-item', {
    type: 'notification',
    id: String(notification.id),
    data: notification,
  })
}

// ===== 待办 Tab 操作 =====
function toggleSourceFilter(key: string) {
  if (activeSource.value === key) {
    activeSource.value = ''
    hub.setFilter('sources', [])
  } else {
    activeSource.value = key
    hub.setFilter('sources', [key as WorkItem['source']])
  }
}

function onSourcesChange() {
  activeSource.value = ''
  hub.fetchItems(true)
  hub.fetchStats()
}

function onPriorityChange() {
  hub.fetchItems(true)
}

function onDateRangeChange(dates: [Dayjs, Dayjs] | [string, string] | null | undefined, dateStrings?: [string, string]) {
  if (dateStrings?.[0] && dateStrings?.[1]) {
    hub.setFilter('dateRange', dateStrings)
  } else if (dates && dates[0] && dates[1] && typeof dates[0] !== 'string' && typeof dates[1] !== 'string') {
    hub.setFilter('dateRange', [
      dates[0].format('YYYY-MM-DD'),
      dates[1].format('YYYY-MM-DD'),
    ])
  } else {
    hub.setFilter('dateRange', null)
  }
}

function handleResetFilters() {
  dateRangeValue.value = undefined
  activeSource.value = ''
  hub.resetFilters()
}

function handleFlushNewItems() {
  hub.flushPendingItems()
}

async function handleAction(itemId: string, actionKey: string) {
  try {
    await hub.handleAction(itemId, actionKey)
    message.success('操作成功')
  } catch {
    message.error('操作失败，请重试')
  }
}

function handleWorkItemCardClick(item: WorkItem) {
  emit('select-item', { type: 'workitem', id: item.id, data: item })
}

// ===== 混合列表点击 =====
async function handleMixedItemClick(item: MixedItem) {
  if (item.type === 'workitem') {
    emit('select-item', { type: 'workitem', id: item.data.id, data: item.data })
  } else {
    await handleNotificationClick(item.data)
  }
}

// ===== Tab 切换 =====
function onTabChange(key: string | number) {
  activeTab.value = key as TabKey
  if (key === 'notification' || key === 'all') {
    loadNotifications()
  }
}

// ===== 刷新 =====
async function handleRefresh() {
  await hub.fetchItems(true)
  await hub.fetchStats()
}

function promptLaunchPanel() {
  message.info('请在左侧「我要发起」选择费用、报价或仓储流程')
}

// ===== 批量操作 =====
function handleBatchApprove() {
  hub.batchExecuteAction({
    key: 'approve',
    label: '批准',
    type: 'primary',
    finalizes: true,
  })
}

function handleBatchReject() {
  hub.batchExecuteAction({
    key: 'reject',
    label: '驳回',
    type: 'danger',
    finalizes: true,
    needsConfirm: true,
    confirmSummary: ['将批量驳回所选项目', '此操作不可撤销'],
  })
}

// ===== 归档/延后区折叠状态 =====
const showDeferred = ref(false)
const showArchived = ref(false)

// ===== 可逆性分级安全机制：二次确认弹窗可见性 =====
const confirmVisible = computed<boolean>({
  get: () => !!hub.confirmDialog.value,
  set: (v: boolean) => {
    if (!v) hub.cancelConfirm()
  },
})

// ===== 生命周期 =====
onMounted(async () => {
  // 创建 init Promise 供多个操作共享
  // connect() 需要 wait init() 完成（initialDataLoaded = true）
  const initPromise = hub.init()

  // 并行执行所有初始化操作：
  // - initPromise 在 init 和 connect 之间共享，确保只执行一次
  // - loadNotificationUnreadCount 与待办初始化互相独立
  await Promise.allSettled([
    // 待办数据初始化
    initPromise,
    // 加载通知未读数（独立）
    loadNotificationUnreadCount(),
    // SignalR 连接：依赖 init 完成后执行
    (async () => {
      await initPromise
      if (userStore.userInfo?.id) {
        hub.connect(userStore.userInfo.id)
      }
    })(),
  ])
})

onUnmounted(() => {
  hub.disconnect()
})
</script>

<template>
  <div class="workhub-center">
    <!-- 质量预警条 -->
    <QualityAlertBar />

    <!-- Tab 栏 -->
    <a-tabs
      v-model:activeKey="activeTab"
      size="small"
      class="center-tabs"
      :tab-bar-style="{ marginBottom: 0, padding: '0 12px', flexShrink: 0 }"
      @change="onTabChange"
    >
      <!-- ===== 全部 Tab ===== -->
      <a-tab-pane key="all">
        <template #tab>
          全部
          <a-badge
            v-if="allBadge > 0"
            :count="allBadge"
            :number-style="{ backgroundColor: '#52c41a', fontSize: '10px', minWidth: '16px', height: '16px', lineHeight: '16px' }"
            class="tab-badge"
          />
        </template>

        <div class="tab-content scrollable">
          <!-- 加载中 -->
          <div v-if="hub.loading.value && mixedList.length === 0" class="loading-state">
            <a-spin size="small" />
            <span>加载中...</span>
          </div>

          <!-- 空状态 -->
          <div v-else-if="mixedList.length === 0" class="empty-state">
            <a-empty :image="null" description="暂无消息">
              <template #image>
                <BellOutlined style="font-size: 40px; color: #d9d9d9;" />
              </template>
            </a-empty>
          </div>

          <!-- 混合列表 -->
          <div v-else class="mixed-list">
            <div
              v-for="(item, index) in mixedList"
              :key="`${item.type}-${item.type === 'workitem' ? item.data.id : item.data.id}-${index}`"
              class="mixed-item"
              :class="{
                'is-unread': item.type === 'notification' && !item.data.isRead,
                [`mixed-${item.type}`]: true,
              }"
              @click="handleMixedItemClick(item)"
            >
              <!-- 工作项 -->
              <template v-if="item.type === 'workitem'">
                <div
                  class="mixed-avatar workitem-avatar"
                  :style="{ backgroundColor: (sourceColorMap[item.data.source] || '#595959') + '18', color: sourceColorMap[item.data.source] || '#595959' }"
                >
                  {{ sourceLabelMap[item.data.source] ?? item.data.source }}
                </div>
                <div class="mixed-body">
                  <div class="mixed-title">{{ item.data.title }}</div>
                  <div class="mixed-desc">{{ item.data.summary }}</div>
                </div>
                <div class="mixed-right">
                  <span class="mixed-time">{{ formatTime(item.time) }}</span>
                  <a-tag
                    size="small"
                    class="priority-chip"
                    :color="item.data.priority === 'urgent' ? 'error' : item.data.priority === 'high' ? 'warning' : 'processing'"
                  >
                    {{ item.data.priority === 'urgent' ? '紧急' : item.data.priority === 'high' ? '高' : item.data.priority === 'normal' ? '普通' : '低' }}
                  </a-tag>
                </div>
              </template>

              <!-- 通知项 -->
              <template v-else>
                <div class="mixed-avatar notification-avatar">
                  {{ getNotificationIcon(item.data.eventType) }}
                </div>
                <div class="mixed-body">
                  <div class="mixed-title">{{ item.data.title }}</div>
                  <div class="mixed-desc">{{ item.data.content }}</div>
                </div>
                <div class="mixed-right">
                  <span class="mixed-time">{{ formatTime(item.time) }}</span>
                  <div v-if="!item.data.isRead" class="unread-dot" />
                </div>
              </template>
            </div>
          </div>
        </div>
      </a-tab-pane>

      <!-- ===== 待办 Tab ===== -->
      <a-tab-pane key="todo">
        <template #tab>
          待办
          <a-badge
            v-if="todoBadge > 0"
            :count="todoBadge"
            :number-style="{ backgroundColor: '#1677ff', fontSize: '10px', minWidth: '16px', height: '16px', lineHeight: '16px' }"
            class="tab-badge"
          />
        </template>

        <div class="tab-content scrollable">
          <!-- 统计栏 -->
          <div class="stats-bar">
            <div
              v-for="stat in statsItems"
              :key="stat.key"
              class="stat-item"
              :class="{ active: activeSource === stat.key }"
              :style="{ '--stat-color': stat.color }"
              @click="toggleSourceFilter(stat.key)"
            >
              <component :is="stat.icon" class="stat-icon" />
              <span class="stat-label">{{ stat.label }}</span>
              <span class="stat-count" v-if="stat.count > 0">{{ stat.count }}</span>
            </div>
          </div>

          <!-- 紧急摘要行 -->
          <div
            v-if="urgentCount > 0 || expiringCount > 0"
            class="urgent-summary-row"
          >
            <span class="urgent-dot"></span>
            <span
              v-if="urgentCount > 0"
              class="urgent-link"
              @click="filterUrgent"
            >
              {{ urgentCount }} 项紧急
            </span>
            <span
              v-if="urgentCount > 0 && expiringCount > 0"
              class="separator"
            > · </span>
            <span
              v-if="expiringCount > 0"
              class="expiring-link"
              @click="filterExpiring"
            >
              {{ expiringCount }} 项即将超时
            </span>
          </div>

          <!-- 筛选器栏（非多选模式下可见） -->
          <div class="filter-bar" v-if="!hub.isMultiSelectMode.value">
            <div class="filter-row">
              <div class="filter-group">
                <span class="filter-label">来源：</span>
                <a-checkbox-group
                  v-model:value="hub.filters.value.sources"
                  :options="sourceOptions"
                  @change="onSourcesChange"
                />
              </div>
              <div class="filter-group">
                <span class="filter-label">优先级：</span>
                <a-select
                  v-model:value="hub.filters.value.priority"
                  style="width: 90px"
                  size="small"
                  @change="onPriorityChange"
                >
                  <a-select-option value="">全部</a-select-option>
                  <a-select-option value="urgent">紧急</a-select-option>
                  <a-select-option value="high">高</a-select-option>
                  <a-select-option value="normal">普通</a-select-option>
                  <a-select-option value="low">低</a-select-option>
                </a-select>
              </div>
              <div class="filter-group">
                <span class="filter-label">时间：</span>
                <a-range-picker
                  v-model:value="dateRangeValue"
                  size="small"
                  :placeholder="['开始', '结束']"
                  style="width: 200px"
                  @change="onDateRangeChange"
                />
              </div>
              <a-button size="small" @click="handleResetFilters" :disabled="!hasActiveFilters">
                重置
              </a-button>
              <a-button
                size="small"
                @click="hub.enterMultiSelect()"
              >
                <template #icon><CheckSquareOutlined /></template>
                多选
              </a-button>
            </div>
          </div>

          <!-- 批量操作栏（多选模式下显示） -->
          <div class="batch-action-bar" v-else>
            <span class="batch-info">
              已选 {{ hub.selectedItemIds.value.size }} 项
            </span>
            <a-button size="small" @click="hub.selectAll()">全选</a-button>
            <a-button size="small" @click="hub.deselectAll()">取消全选</a-button>
            <a-divider type="vertical" />
            <a-button
              size="small"
              type="primary"
              :disabled="!hub.selectedItemIds.value.size"
              @click="handleBatchApprove"
            >
              批量批准
            </a-button>
            <a-button
              size="small"
              danger
              :disabled="!hub.selectedItemIds.value.size"
              @click="handleBatchReject"
            >
              批量驳回
            </a-button>
            <a-divider type="vertical" />
            <a-button size="small" @click="hub.exitMultiSelect()">
              <template #icon><CloseOutlined /></template>
              退出多选
            </a-button>
          </div>

          <!-- 新消息提示条 -->
          <transition name="slide-down">
            <div v-if="hub.newItemsCount.value > 0" class="new-items-banner" @click="handleFlushNewItems">
              <BellOutlined class="banner-icon" />
              有 {{ hub.newItemsCount.value }} 条新消息，点击查看
              <span class="banner-close" @click.stop="hub.dismissPendingItems()">✕</span>
            </div>
          </transition>

          <!-- 骨架屏 -->
          <WorkItemSkeleton v-if="hub.loading.value && hub.items.value.length === 0" :count="5" />

          <!-- 工作项列表 -->
          <template v-else-if="hub.items.value.length > 0">
            <transition-group name="feed-item" tag="div" class="feed-list">
              <WorkItemCard
                v-for="item in hub.items.value"
                :key="item.id"
                :item="item"
                :class="{ 'work-item-card--selected': hub.selectedItemId.value === item.id }"
                @action="handleAction"
                @click.capture="handleWorkItemCardClick(item)"
              />
            </transition-group>

            <!-- 加载更多 -->
            <div class="load-more-wrap">
              <a-button
                v-if="hub.hasMore.value"
                type="text"
                :loading="hub.loading.value"
                @click="hub.loadMore()"
                class="load-more-btn"
              >
                加载更多
              </a-button>
              <span v-else class="no-more-text">已显示全部 {{ hub.totalCount.value }} 项</span>
            </div>
          </template>

          <!-- 空状态 -->
          <div v-else-if="!hub.loading.value" class="empty-state">
            <div class="empty-icon">✓</div>
            <div class="empty-title">当前没有待处理的工作</div>
            <div class="empty-desc">{{ emptyDesc }}</div>
            <div class="empty-actions">
              <a-button type="primary" @click="promptLaunchPanel">发起流程</a-button>
              <a-button @click="handleResetFilters">查看全部工作</a-button>
              <a-button @click="handleRefresh">刷新</a-button>
            </div>
          </div>

          <!-- 稍后处理（延后区） -->
          <div class="deferred-section" v-if="hub.deferredItems.value.length">
            <div class="section-header" @click="showDeferred = !showDeferred">
              <CaretRightOutlined :class="{ rotated: showDeferred }" />
              <span>稍后处理 ({{ hub.deferredItems.value.length }})</span>
            </div>
            <div class="section-items" v-show="showDeferred">
              <div
                class="deferred-item"
                v-for="item in hub.deferredItems.value"
                :key="item.id"
              >
                <span class="deferred-title" :title="item.title">{{ item.title }}</span>
                <a-button type="link" size="small" @click="hub.restoreDeferred(item.id)">恢复</a-button>
              </div>
            </div>
          </div>

          <!-- 已归档（终结性完成项） -->
          <div class="archived-section" v-if="hub.archivedItems.value.length">
            <div class="section-header" @click="showArchived = !showArchived">
              <CaretRightOutlined :class="{ rotated: showArchived }" />
              <span>已归档 ({{ hub.archivedItems.value.length }})</span>
            </div>
            <div class="section-items" v-show="showArchived">
              <div
                class="archived-item"
                v-for="item in hub.archivedItems.value"
                :key="item.id"
              >
                <span class="archived-title" :title="item.title">{{ item.title }}</span>
                <a-tag color="green">已处理</a-tag>
              </div>
            </div>
          </div>
        </div>
      </a-tab-pane>

      <!-- ===== 通知 Tab ===== -->
      <a-tab-pane key="notification">
        <template #tab>
          通知
          <a-badge
            v-if="notificationBadge > 0"
            :count="notificationBadge"
            :number-style="{ backgroundColor: '#ff4d4f', fontSize: '10px', minWidth: '16px', height: '16px', lineHeight: '16px' }"
            class="tab-badge"
          />
        </template>

        <div class="tab-content scrollable">
          <!-- 全部已读按钮 -->
          <div class="notification-actions" v-if="notificationUnreadCount > 0">
            <a-button type="link" size="small" @click="handleMarkAllRead">
              <template #icon><CheckOutlined /></template>
              全部已读
            </a-button>
          </div>

          <!-- 通知列表 -->
          <div v-if="!notificationsLoading">
            <div v-if="notifications.length === 0" class="empty-state">
              <a-empty :image="null" description="暂无通知">
                <template #image>
                  <BellOutlined style="font-size: 40px; color: #d9d9d9;" />
                </template>
              </a-empty>
            </div>

            <div
              v-for="notification in notifications"
              :key="notification.id"
              class="notification-item"
              :class="{ 'is-unread': !notification.isRead }"
              @click="handleNotificationClick(notification)"
            >
              <div class="notification-icon">{{ getNotificationIcon(notification.eventType) }}</div>
              <div class="notification-content">
                <div class="notification-title">{{ notification.title }}</div>
                <div class="notification-desc">{{ notification.content }}</div>
                <div class="notification-time">{{ formatTime(notification.createTime) }}</div>
              </div>
              <div v-if="!notification.isRead" class="unread-dot" />
            </div>
          </div>

          <!-- 加载中 -->
          <div v-else class="loading-state">
            <a-spin size="small" />
            <span>加载中...</span>
          </div>
        </div>
      </a-tab-pane>
    </a-tabs>

    <!-- ===== Undo Toast 列表（可逆操作 5 秒撤销窗口）===== -->
    <div class="undo-toast-container" v-if="hub.pendingActions.value.length">
      <div
        class="undo-toast"
        v-for="pending in hub.pendingActions.value"
        :key="pending.id"
      >
        <span class="undo-message">{{ pending.label }}</span>
        <a-button type="link" size="small" class="undo-btn" @click="hub.undoAction(pending.id)">撤销</a-button>
      </div>
    </div>

    <!-- ===== 二次确认弹窗（不可逆操作）===== -->
    <a-modal
      v-model:open="confirmVisible"
      title="操作确认"
      okText="确认执行"
      cancelText="取消"
      :okButtonProps="{ danger: true }"
      @ok="hub.confirmAction()"
      @cancel="hub.cancelConfirm()"
    >
      <div class="confirm-content" v-if="hub.confirmDialog.value">
        <p>
          确定要对「<strong>{{ hub.confirmDialog.value.item.title }}</strong>」执行「<strong>{{ hub.confirmDialog.value.action.label }}</strong>」？
        </p>
        <ul
          class="confirm-summary"
          v-if="hub.confirmDialog.value.summaryLines && hub.confirmDialog.value.summaryLines.length"
        >
          <li v-for="(line, idx) in hub.confirmDialog.value.summaryLines" :key="idx">{{ line }}</li>
        </ul>
      </div>
    </a-modal>
  </div>
</template>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

// ===== 容器 =====
.workhub-center {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  background: #f4f6f8;
}

// ===== Tab 整体 =====
.center-tabs {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;

  :deep(.ant-tabs-nav) {
    background: rgba(255, 255, 255, 0.92);
    border-bottom: 1px solid #e8edf3;
    margin-bottom: 0;
    flex-shrink: 0;
    backdrop-filter: blur(10px);
  }

  :deep(.ant-tabs-tab) {
    padding: 13px 0;
    font-size: 14px;
  }

  :deep(.ant-tabs-ink-bar) {
    height: 3px;
    border-radius: 999px 999px 0 0;
    background: #ff6700;
  }

  :deep(.ant-tabs-content-holder) {
    flex: 1;
    overflow: hidden;
  }

  :deep(.ant-tabs-content) {
    height: 100%;
  }

  :deep(.ant-tabs-tabpane) {
    height: 100%;
    padding: 0 !important;
  }
}

// ===== Tab 内容区 =====
.tab-content {
  height: 100%;
  display: flex;
  flex-direction: column;

  &.scrollable {
    overflow-y: auto;
    padding: 14px 18px 18px;

    &::-webkit-scrollbar {
      width: 4px;
    }
    &::-webkit-scrollbar-thumb {
      background: rgba(0, 0, 0, 0.12);
      border-radius: 2px;
    }
  }

  &.tab-content--inbox {
    overflow: hidden;
  }
}

// Tab Badge 间距
.tab-badge {
  margin-left: 4px;
}

// ===== 统计栏 =====
.stats-bar {
  display: flex;
  gap: 10px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.stat-item {
  flex: 1;
  min-width: 96px;
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 48px;
  padding: 9px 12px;
  border-radius: 8px;
  background: $bg-card;
  box-shadow: 0 1px 2px rgba(18, 31, 53, 0.04);
  cursor: pointer;
  transition: all 0.18s ease;
  border: 1px solid rgba(18, 31, 53, 0.06);
  user-select: none;

  &:hover {
    box-shadow: 0 8px 18px rgba(18, 31, 53, 0.08);
    border-color: var(--stat-color, $color-primary);
    transform: translateY(-1px);
  }

  &.active {
    background: color-mix(in srgb, var(--stat-color, $color-primary) 8%, white);
    border-color: var(--stat-color, $color-primary);
    box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--stat-color, $color-primary) 24%, transparent);
  }
}

.stat-icon {
  font-size: 16px;
  color: var(--stat-color, $color-primary);
  flex-shrink: 0;
}

.stat-label {
  font-size: 12px;
  color: $text-regular;
  flex: 1;
}

.stat-count {
  font-size: 13px;
  font-weight: 700;
  color: var(--stat-color, $color-primary);
}

// ===== 紧急摘要行 =====
.urgent-summary-row {
  display: flex;
  align-items: center;
  min-height: 34px;
  padding: 0 12px;
  margin-bottom: 10px;
  font-size: 13px;
  color: rgba(0, 0, 0, 0.65);
  border: 1px solid rgba(255, 77, 79, 0.16);
  border-radius: 8px;
  background: rgba(255, 77, 79, 0.045);
}

.urgent-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #ff4d4f;
  margin-right: 8px;
  flex-shrink: 0;
}

.urgent-link,
.expiring-link {
  color: #cf1322;
  cursor: pointer;
  font-weight: 500;

  &:hover {
    color: #ff4d4f;
  }
}

.separator {
  margin: 0 4px;
  color: rgba(0, 0, 0, 0.25);
}

// ===== 筛选器栏 =====
.filter-bar {
  background: rgba(255, 255, 255, 0.78);
  border-radius: 8px;
  padding: 9px 12px;
  margin-bottom: 12px;
  border: 1px solid rgba(18, 31, 53, 0.06);
  box-shadow: 0 1px 2px rgba(18, 31, 53, 0.035);

  :deep(.ant-checkbox-wrapper) {
    color: rgba(0, 0, 0, 0.68);
  }
}

// ===== 批量操作栏 =====
.batch-action-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: #f0f5ff;
  border-radius: 6px;
  margin-bottom: 8px;
  border: 1px solid #adc6ff;
  flex-wrap: wrap;

  :deep(.ant-divider-vertical) {
    height: 18px;
    margin: 0 2px;
  }
}

.batch-info {
  font-weight: 500;
  color: #1677ff;
  font-size: 13px;
  margin-right: 4px;
}

.filter-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 9px 14px;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 4px;
}

.filter-label {
  font-size: 12px;
  color: $text-secondary;
  white-space: nowrap;
}

// ===== 新消息提示条 =====
.new-items-banner {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #fff7f0;
  border: 1px solid rgba(255, 103, 0, 0.24);
  border-radius: 8px;
  padding: 8px 12px;
  margin-bottom: 8px;
  font-size: 13px;
  color: #d94f00;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;

  &:hover {
    background: #fff1e8;
  }
}

.banner-icon {
  font-size: 14px;
}

.banner-close {
  margin-left: auto;
  font-size: 11px;
  opacity: 0.6;
  cursor: pointer;
  padding: 1px 3px;
  border-radius: 3px;

  &:hover {
    opacity: 1;
    background: rgba(22, 119, 255, 0.1);
  }
}

// ===== Feed 列表 =====
.feed-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 8px;
}

// 选中高亮（中栏与右栏联动）
:deep(.work-item-card.work-item-card--selected) {
  background: rgba(255, 103, 0, 0.045);
  box-shadow: inset 3px 0 0 0 #ff6700, 0 8px 18px rgba(18, 31, 53, 0.08);
  border-color: rgba(255, 103, 0, 0.22);
}

// ===== 加载更多 =====
.load-more-wrap {
  display: flex;
  justify-content: center;
  padding: 16px 0 4px;
}

.load-more-btn {
  color: $color-primary;
  font-size: 13px;
}

.no-more-text {
  font-size: 12px;
  color: $text-secondary;
}

// ===== 空状态 =====
.empty-state {
  background: $bg-card;
  border-radius: 8px;
  padding: 56px 16px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  margin-top: 20px;
  border: 1px dashed rgba(18, 31, 53, 0.12);
}

.empty-icon {
  width: 52px;
  height: 52px;
  border-radius: 16px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 26px;
  line-height: 1;
  margin-bottom: 6px;
  color: #ff6700;
  background: rgba(255, 103, 0, 0.08);
}

.empty-title {
  font-size: 18px;
  font-weight: 600;
  color: $text-primary;
}

.empty-desc {
  font-size: 13px;
  color: $text-secondary;
}

.empty-actions {
  display: flex;
  gap: 10px;
  margin-top: 8px;
  flex-wrap: wrap;
  justify-content: center;
}

// ===== 加载中 =====
.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  height: 100px;
  color: $text-secondary;
  font-size: $font-size-sm;
}

// ===== 混合列表 =====
.mixed-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.mixed-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 11px 12px;
  cursor: pointer;
  transition: background 0.15s;
  border-radius: 8px;
  position: relative;
  border: 1px solid rgba(18, 31, 53, 0.05);
  background: #fff;

  &:hover {
    background: #fff7f0;
    border-color: rgba(255, 103, 0, 0.18);
  }

  &.is-unread {
    .mixed-title {
      font-weight: 600;
    }
  }

  &.mixed-workitem {
    background: rgba(255, 255, 255, 0.92);
  }

  &.mixed-conversation {
    background: rgba(82, 196, 26, 0.02);
  }

  &.mixed-notification.is-unread {
    background: rgba(255, 77, 79, 0.04);
  }
}

.mixed-avatar {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 600;
  flex-shrink: 0;

  &.notification-avatar {
    background: #f5f5f5;
    font-size: 18px;
    font-weight: normal;
  }

  &.workitem-avatar {
    font-size: 12px;
    font-weight: 700;
    border-radius: 8px;
  }
}

.mixed-body {
  flex: 1;
  min-width: 0;
}

.mixed-title {
  font-size: 13px;
  font-weight: 500;
  color: $text-primary;
  margin-bottom: 3px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.mixed-desc {
  font-size: 12px;
  color: $text-secondary;
  display: flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.mixed-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
  flex-shrink: 0;
}

.mixed-time {
  font-size: 11px;
  color: $text-placeholder;
}

.priority-chip {
  font-size: 10px;
  line-height: 14px;
  padding: 0 4px;
}

// ===== 通知 =====
.notification-actions {
  display: flex;
  justify-content: flex-end;
  padding: 4px 0 8px;
}

.notification-item {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 11px 12px;
  cursor: pointer;
  transition: background 0.15s;
  border-radius: 8px;
  border: 1px solid rgba(18, 31, 53, 0.05);
  background: #fff;
  margin-bottom: 6px;

  &:hover {
    background: #fff7f0;
  }

  &.is-unread {
    background: rgba(24, 144, 255, 0.04);

    .notification-title {
      font-weight: 600;
    }
  }
}

.notification-icon {
  width: 34px;
  height: 34px;
  border-radius: 8px;
  background: #f5f5f5;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
}

.notification-content {
  flex: 1;
  min-width: 0;
}

.notification-title {
  font-size: 13px;
  font-weight: 500;
  color: $text-primary;
  margin-bottom: 3px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.notification-desc {
  font-size: 12px;
  color: $text-secondary;
  margin-bottom: 3px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.notification-time {
  font-size: 11px;
  color: $text-placeholder;
}

.unread-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #ff4d4f;
  flex-shrink: 0;
  margin-top: 4px;
}

// ===== 动画 =====
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.25s ease;
}

.slide-down-enter-from,
.slide-down-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}

// ===== 可逆性分级安全机制 UI =====
.undo-toast-container {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 1000;
  display: flex;
  flex-direction: column;
  gap: 8px;
  pointer-events: none;
}

.undo-toast {
  background: #323232;
  color: #fff;
  border-radius: 8px;
  padding: 12px 16px;
  display: flex;
  align-items: center;
  gap: 16px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
  font-size: 13px;
  min-width: 280px;
  pointer-events: auto;
  animation: undo-toast-in 0.2s ease-out;
}

.undo-message {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.undo-btn {
  flex-shrink: 0;
  padding: 0 4px;
  height: auto;
  line-height: 1;
  color: #4096ff !important;

  &:hover {
    color: #69b1ff !important;
  }
}

@keyframes undo-toast-in {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.confirm-content {
  font-size: 14px;
  line-height: 1.6;
  color: $text-regular;
}

.confirm-summary {
  margin: 12px 0 0;
  padding: 12px 16px 12px 32px;
  background: #fff7e6;
  border: 1px solid #ffe7ba;
  border-radius: 6px;
  list-style: disc;

  li {
    font-size: 13px;
    color: $text-regular;
    line-height: 1.7;
  }
}

.feed-item-enter-active {
  transition: all 0.3s ease;
}

.feed-item-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

// ===== 归档/延后折叠区 =====
.deferred-section,
.archived-section {
  margin-top: 12px;
  background: $bg-card;
  border-radius: 8px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.06);
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  color: #8c8c8c;
  font-size: 13px;
  cursor: pointer;
  user-select: none;
  transition: background 0.15s ease;

  &:hover {
    background: rgba(0, 0, 0, 0.02);
  }

  :deep(.anticon) {
    font-size: 11px;
    transition: transform 0.2s ease;
  }

  :deep(.anticon.rotated) {
    transform: rotate(90deg);
  }
}

.section-items {
  border-top: 1px solid #f0f0f0;
}

.deferred-item,
.archived-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 12px;
  border-bottom: 1px solid #f0f0f0;

  &:last-child {
    border-bottom: none;
  }
}

.deferred-title,
.archived-title {
  flex: 1;
  min-width: 0;
  font-size: 13px;
  color: #595959;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-right: 8px;
}
</style>
