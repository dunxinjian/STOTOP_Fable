<template>
  <div
    class="work-item-card"
    :class="[
      `source-${item.source}`,
      `priority-bar-${item.priority}`,
      { clickable: !!item.detailRoute || hub.isMultiSelectMode.value },
      { 'work-item-card--multi-selected': hub.isMultiSelectMode.value && hub.selectedItemIds.value.has(item.id) },
    ]"
    @click.self="handleCardClick"
  >
    <!-- 多选复选框（仅多选模式下可见） -->
    <div v-if="hub.isMultiSelectMode.value" class="multi-select-checkbox-wrap" @click.stop>
      <a-checkbox
        :checked="hub.selectedItemIds.value.has(item.id)"
        class="multi-select-checkbox"
        @change="hub.toggleSelectItem(item.id)"
      />
    </div>

    <!-- 左侧优先级色条 -->
    <div class="priority-bar" :style="{ background: priorityColor }"></div>

    <!-- 来源图标区域 -->
    <div class="source-icon-wrap" :style="{ background: sourceColor + '15' }">
      <component :is="sourceIcon" class="source-icon" :style="{ color: sourceColor }" />
    </div>

    <!-- 卡片内容 -->
    <div class="card-content" @click="handleCardClick">
      <!-- 标题行：来源标签 + 标题 + 时间 -->
      <div class="card-header">
        <span class="source-tag" :style="{ background: sourceColor + '18', color: sourceColor }">
          {{ sourceLabel }}
        </span>
        <a-tag
          :color="priorityTagColor"
          class="priority-tag"
        >{{ priorityLabel }}</a-tag>
        <span v-if="item.source === 'oa' && item.metadata?.processNo" class="process-no">[{{ item.metadata.processNo }}]</span>
        <span class="card-title" :title="item.title">{{ isDataImport ? diTitle : item.title }}</span>
        <span class="card-time">{{ relativeTime }}</span>
      </div>

      <!-- 关联链接行 - 仅 relatedLinks 非空时渲染 -->
      <div class="related-links-row" v-if="item.relatedLinks && item.relatedLinks.length > 0">
        <template v-for="(link, idx) in item.relatedLinks" :key="link.route">
          <a-popover
            v-if="link.summary"
            placement="top"
            :mouse-enter-delay="0.3"
            :mouse-leave-delay="0.2"
            :overlay-style="{ maxWidth: '320px' }"
          >
            <template #content>
              <div class="related-link-summary">{{ link.summary }}</div>
            </template>
            <span
              class="related-link"
              :class="{ disabled: !hasPermission(link) }"
              @click.stop="handleLinkClick(link)"
            >
              <component :is="getIconComponent(link.icon)" class="link-icon" />
              {{ link.label }}
            </span>
          </a-popover>
          <span
            v-else
            class="related-link"
            :class="{ disabled: !hasPermission(link) }"
            @click.stop="handleLinkClick(link)"
          >
            <component :is="getIconComponent(link.icon)" class="link-icon" />
            {{ link.label }}
          </span>
          <span v-if="idx < item.relatedLinks!.length - 1" class="link-separator">·</span>
        </template>
      </div>

      <!-- ===== OA 审批增强渲染 ===== -->
      <template v-if="item.source === 'oa'">
        <!-- 当前审批节点 -->
        <div v-if="item.metadata?.currentNodeName" class="oa-node-row">
          <a-tag color="blue">当前节点: {{ item.metadata.currentNodeName }}</a-tag>
        </div>

        <!-- 摘要行 -->
        <div class="card-summary">{{ item.summary }}</div>

        <!-- OA 增强信息区 -->
        <div class="oa-enhanced">
          <span v-if="item.deadline" class="deadline" :class="{ overdue: isOverdue }">
            <ClockCircleOutlined class="deadline-icon" />
            {{ formattedDeadline }}
          </span>
          <a-tag v-if="item.metadata?.approvalMode" :color="approvalModeColor">
            {{ approvalModeLabel }}
          </a-tag>
        </div>

        <!-- 底部操作按钮 -->
        <div class="card-footer">
          <div class="card-meta"></div>
          <div class="card-actions" @click.stop>
            <a-button
              v-for="(action, idx) in primaryActions"
              :key="action.key"
              size="small"
              :type="idx === 0 ? 'primary' : 'default'"
              @click="handleAction(action)"
            >
              {{ action.label }}
            </a-button>
            <a-dropdown v-if="secondaryActions.length" :trigger="['click']" placement="bottomRight">
              <a-button size="small" class="more-actions-btn" @click.stop>
                <EllipsisOutlined />
              </a-button>
              <template #overlay>
                <a-menu>
                  <a-menu-item
                    v-for="action in secondaryActions"
                    :key="action.key"
                    @click="handleAction(action)"
                  >
                    {{ action.label }}
                  </a-menu-item>
                  <a-menu-divider />
                  <a-menu-item key="__defer" @click="handleDefer">
                    <ClockCircleOutlined /> 稍后处理
                  </a-menu-item>
                </a-menu>
              </template>
            </a-dropdown>
            <a-button
              v-if="item.metadata?.canWithdraw"
              size="small"
              type="link"
              danger
              @click="handleWithdraw"
            >
              <template #icon><RollbackOutlined /></template>
              撤回
            </a-button>
          </div>
        </div>
      </template>

      <!-- ===== 数据导入增强渲染 ===== -->
      <template v-if="isDataImport && diSubType">
        <!-- batch_completed：批次完成 -->
        <template v-if="diSubType === 'batch_completed'">
          <div class="di-stats">
            <a-progress
              :percent="diSuccessPercent"
              :stroke-color="'#52c41a'"
              size="small"
              :format="() => `${meta.successRows ?? 0}/${meta.totalRows ?? 0}`"
            />
            <span class="di-label">成功率</span>
          </div>
          <div v-if="meta.stagingTable" class="di-extra">
            <DatabaseOutlined class="di-extra-icon" />
            <span>暂存表：{{ meta.stagingTable }}</span>
          </div>
        </template>

        <!-- batch_failed：导入失败 -->
        <template v-else-if="diSubType === 'batch_failed'">
          <div class="di-error">
            <ExclamationCircleOutlined class="di-error-icon" />
            <span>{{ meta.errorMessage || item.summary || '导入失败' }}</span>
          </div>
          <div class="card-footer">
            <div class="card-meta"></div>
            <div class="card-actions" @click.stop>
              <a-button size="small" type="link" danger @click="router.push('/cardflow/upload-center')">
                查看详情
              </a-button>
            </div>
          </div>
        </template>

        <!-- classification_alert：分类告警 -->
        <template v-else-if="diSubType === 'classification_alert'">
          <div class="di-alert-tags">
            <a-tag
              :color="meta.severity === 'Critical' ? 'error' : meta.severity === 'Error' ? 'error' : 'warning'"
              :class="{ 'di-tag-bold': meta.severity === 'Critical' }"
            >
              {{ meta.severity || 'Warning' }}
            </a-tag>
            <a-tag v-for="cls in (meta.classificationTypes || [])" :key="cls">
              {{ cls }}
            </a-tag>
          </div>
          <div class="di-hit-rows" v-if="meta.hitRows">
            命中 <strong>{{ meta.hitRows }}</strong> 行
          </div>
          <div class="card-footer">
            <div class="card-meta"></div>
            <div class="card-actions" @click.stop>
              <a-button size="small" type="link" @click="router.push('/cardflow/upload-center')">
                查看分类结果
              </a-button>
            </div>
          </div>
        </template>

        <!-- error_dispatched：异常已派发 -->
        <template v-else-if="diSubType === 'error_dispatched'">
          <div class="di-dispatch-info">
            <div v-if="meta.batchName" class="di-dispatch-row">
              <span class="di-dispatch-label">来源批次：</span>
              <span>{{ meta.batchName }}</span>
            </div>
            <div v-if="meta.requirement" class="di-dispatch-row">
              <span class="di-dispatch-label">处理要求：</span>
              <span>{{ meta.requirement }}</span>
            </div>
          </div>
          <div class="card-footer">
            <div class="card-meta"></div>
            <div class="card-actions" @click.stop>
              <a-button
                v-for="(action, idx) in primaryActions"
                :key="action.key"
                size="small"
                :type="idx === 0 ? 'primary' : 'default'"
                @click="handleAction(action)"
              >
                {{ action.label }}
              </a-button>
              <a-dropdown v-if="secondaryActions.length" :trigger="['click']" placement="bottomRight">
                <a-button size="small" class="more-actions-btn" @click.stop>
                  <EllipsisOutlined />
                </a-button>
                <template #overlay>
                  <a-menu>
                    <a-menu-item
                      v-for="action in secondaryActions"
                      :key="action.key"
                      @click="handleAction(action)"
                    >
                      {{ action.label }}
                    </a-menu-item>
                    <a-menu-divider />
                    <a-menu-item key="__defer" @click="handleDefer">
                      <ClockCircleOutlined /> 稍后处理
                    </a-menu-item>
                  </a-menu>
                </template>
              </a-dropdown>
              <a-button v-if="!item.actions.length" size="small" type="primary" @click="handleCardClick">
                开始处理
              </a-button>
            </div>
          </div>
        </template>

        <!-- 未知 subType：降级为默认渲染 -->
        <template v-else>
          <div class="card-summary">{{ item.summary }}</div>
          <div class="card-footer">
            <div class="card-meta">
              <span v-if="item.deadline" class="deadline" :class="{ overdue: isOverdue }">
                <ClockCircleOutlined class="deadline-icon" />
                {{ formattedDeadline }}
              </span>
            </div>
            <div class="card-actions" @click.stop>
              <a-button
                v-for="(action, idx) in primaryActions"
                :key="action.key"
                size="small"
                :type="idx === 0 ? 'primary' : 'default'"
                @click="handleAction(action)"
              >
                {{ action.label }}
              </a-button>
              <a-dropdown v-if="secondaryActions.length" :trigger="['click']" placement="bottomRight">
                <a-button size="small" class="more-actions-btn" @click.stop>
                  <EllipsisOutlined />
                </a-button>
                <template #overlay>
                  <a-menu>
                    <a-menu-item
                      v-for="action in secondaryActions"
                      :key="action.key"
                      @click="handleAction(action)"
                    >
                      {{ action.label }}
                    </a-menu-item>
                    <a-menu-divider />
                    <a-menu-item key="__defer" @click="handleDefer">
                      <ClockCircleOutlined /> 稍后处理
                    </a-menu-item>
                  </a-menu>
                </template>
              </a-dropdown>
            </div>
          </div>
        </template>
      </template>

      <!-- ===== 默认渲染（非 datacenter 或无 subType） ===== -->
      <template v-else>
        <!-- 摘要行 -->
        <div class="card-summary">{{ item.summary }}</div>

        <!-- 底部行：截止日期 + 操作按钮 -->
        <div class="card-footer">
          <div class="card-meta">
            <span v-if="item.deadline" class="deadline" :class="{ overdue: isOverdue }">
              <ClockCircleOutlined class="deadline-icon" />
              {{ formattedDeadline }}
            </span>
          </div>
          <div class="card-actions" @click.stop>
            <a-button
              v-for="(action, idx) in primaryActions"
              :key="action.key"
              size="small"
              :type="idx === 0 ? 'primary' : 'default'"
              @click="handleAction(action)"
            >
              {{ action.label }}
            </a-button>
            <a-dropdown v-if="secondaryActions.length" :trigger="['click']" placement="bottomRight">
              <a-button size="small" class="more-actions-btn" @click.stop>
                <EllipsisOutlined />
              </a-button>
              <template #overlay>
                <a-menu>
                  <a-menu-item
                    v-for="action in secondaryActions"
                    :key="action.key"
                    @click="handleAction(action)"
                  >
                    {{ action.label }}
                  </a-menu-item>
                  <a-menu-divider />
                  <a-menu-item key="__defer" @click="handleDefer">
                    <ClockCircleOutlined /> 稍后处理
                  </a-menu-item>
                </a-menu>
              </template>
            </a-dropdown>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, h } from 'vue'
import { useRouter } from 'vue-router'
import { Modal, message } from 'ant-design-vue'
import {
  ClockCircleOutlined,
  AuditOutlined,
  WarningOutlined,
  CheckSquareOutlined,
  ImportOutlined,
  FileTextOutlined,
  TrophyOutlined,
  DollarOutlined,
  SettingOutlined,
  DatabaseOutlined,
  ExclamationCircleOutlined,
  RollbackOutlined,
  EllipsisOutlined,
  LinkOutlined,
} from '@ant-design/icons-vue'
import dayjs from 'dayjs'
import relativeTimePlugin from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/zh-cn'
import type { WorkItem, WorkItemAction, RelatedLink } from '@/api/workhub'
import { executeWorkItemAction } from '@/api/workhub'
import { useWorkHub } from '@/composables/useWorkHub'

const hub = useWorkHub()

dayjs.extend(relativeTimePlugin)
dayjs.locale('zh-cn')

const props = defineProps<{
  item: WorkItem
}>()

const emit = defineEmits<{
  action: [itemId: string, actionKey: string]
  refresh: []
}>()

const router = useRouter()

// ===== 来源配置 =====
type SourceEntry = { label: string; color: string; icon: ReturnType<typeof h> }

const sourceConfig: Record<WorkItem['source'], { label: string; color: string; icon: any }> = {
  oa: { label: 'OA审批', color: '#1677ff', icon: AuditOutlined },
  quality: { label: '质量异常', color: '#fa541c', icon: WarningOutlined },
  task: { label: '任务', color: '#52c41a', icon: CheckSquareOutlined },
  datacenter: { label: 'CardFlow', color: '#722ed1', icon: ImportOutlined },
  cardflow: { label: 'CardFlow审批', color: '#1677ff', icon: AuditOutlined },
  contract: { label: '合同', color: '#7B5B3A', icon: FileTextOutlined },
  points: { label: '积分', color: '#d4b106', icon: TrophyOutlined },
  finance: { label: '财务', color: '#faad14', icon: DollarOutlined },
  system: { label: '系统', color: '#595959', icon: SettingOutlined },
  workflow: { label: '工作流', color: '#13c2c2', icon: CheckSquareOutlined },
}

const sourceLabel = computed(() => sourceConfig[props.item.source]?.label ?? props.item.source)
const sourceColor = computed(() => sourceConfig[props.item.source]?.color ?? '#595959')
const sourceIcon = computed(() => sourceConfig[props.item.source]?.icon ?? SettingOutlined)

// ===== 优先级配置 =====
const priorityConfig = {
  urgent: { label: '紧急', color: '#ff4d4f', tagColor: 'error' },
  high: { label: '高', color: '#fa8c16', tagColor: 'warning' },
  normal: { label: '普通', color: '#1890ff', tagColor: 'processing' },
  low: { label: '低', color: '#8c8c8c', tagColor: 'default' },
} as const

type PriorityKey = keyof typeof priorityConfig

const priorityColor = computed(() => priorityConfig[props.item.priority as PriorityKey]?.color ?? '#8c8c8c')
const priorityLabel = computed(() => priorityConfig[props.item.priority as PriorityKey]?.label ?? String(props.item.priority))
const priorityTagColor = computed(() => priorityConfig[props.item.priority as PriorityKey]?.tagColor ?? 'default')

// ===== 时间处理 =====
const relativeTime = computed(() => {
  const ts = props.item.timestamp || (props.item as any).createdAt
  if (!ts) return ''
  return dayjs(ts).fromNow()
})

const formattedDeadline = computed(() => {
  if (!props.item.deadline) return ''
  const d = dayjs(props.item.deadline)
  const now = dayjs()
  const today = now.startOf('day')
  const tomorrow = now.add(1, 'day').startOf('day')
  if (d.isSame(today, 'day')) return '今天截止'
  if (d.isSame(tomorrow, 'day')) return '明天截止'
  return d.format('MM/DD 截止')
})

const isOverdue = computed(() => {
  if (!props.item.deadline) return false
  return dayjs(props.item.deadline).isBefore(dayjs())
})

// ===== 数据导入增强 =====
const isDataImport = computed(() => props.item.source === 'datacenter')
const meta = computed(() => props.item.metadata || {})
const diSubType = computed(() => meta.value.subType as string | undefined)

const diTitle = computed(() => {
  if (!isDataImport.value) return props.item.title
  const sub = diSubType.value
  if (sub === 'batch_completed' || sub === 'batch_failed') {
    return meta.value.fileName || props.item.title
  }
  if (sub === 'classification_alert') {
    return (meta.value.fileName || props.item.title) + ' 数据异常'
  }
  if (sub === 'error_dispatched') {
    return meta.value.errorDescription || props.item.title
  }
  return props.item.title
})

const diSuccessPercent = computed(() => {
  const total = meta.value.totalRows
  const success = meta.value.successRows
  if (!total || total === 0) return 0
  return Math.round((success / total) * 100)
})

// ===== 事件处理 =====
function handleCardClick() {
  // 多选模式：点击卡片等于勾选/取消勾选
  if (hub.isMultiSelectMode.value) {
    hub.toggleSelectItem(props.item.id)
    return
  }
  if (props.item.detailRoute) {
    router.push(props.item.detailRoute)
  }
}

function handleAction(action: WorkItemAction) {
  if (action.route) {
    router.push(action.route)
  } else {
    // 通过可逆性分级统一入口处理：
    // - needsConfirm=true → 二次确认（不可逆）
    // - 否则 → 乐观更新 + 5 秒 Undo
    hub.executeAction(props.item, action)
  }
}

// 稍后处理：将本卡片移入「已延后」区
function handleDefer() {
  hub.deferItem(props.item.id)
}

// ===== 操作按钮分层 =====
// primary：始终可见（最多 2 个）；secondary：收纳到“...”下拉菜单。
// 若 action 显式声明了 type='primary'/'secondary'，按声明分组；
// 否则按 source 默认分类规则降级（OA 前 2 个为 primary，其余 source 第 1 个为 primary）。
function getDefaultPrimaryCount(source: WorkItem['source']): number {
  return source === 'oa' ? 2 : 1
}

const primaryActions = computed<WorkItemAction[]>(() => {
  const actions = props.item.actions || []
  if (actions.length === 0) return []
  const hasExplicit = actions.some(a => a.type === 'primary' || a.type === 'secondary')
  if (hasExplicit) {
    return actions.filter(a => a.type === 'primary').slice(0, 2)
  }
  return actions.slice(0, getDefaultPrimaryCount(props.item.source))
})

const secondaryActions = computed<WorkItemAction[]>(() => {
  const actions = props.item.actions || []
  if (actions.length === 0) return []
  const hasExplicit = actions.some(a => a.type === 'primary' || a.type === 'secondary')
  if (hasExplicit) {
    return actions.filter(a => a.type === 'secondary')
  }
  return actions.slice(getDefaultPrimaryCount(props.item.source))
})

// ===== OA 审批增强 =====
const approvalModeLabel = computed(() => {
  const mode = props.item.metadata?.approvalMode
  const map: Record<string, string> = { countersign: '会签', or_sign: '或签', sequential: '依次' }
  return map[mode] || mode || ''
})

const approvalModeColor = computed(() => {
  const mode = props.item.metadata?.approvalMode
  const map: Record<string, string> = { countersign: 'purple', or_sign: 'cyan', sequential: 'geekblue' }
  return map[mode] || 'default'
})

// ===== 关联链接 =====
function hasPermission(link: RelatedLink): boolean {
  if (!link.permission) return true
  // TODO: 对接实际权限系统，当前默认放行
  return true
}

function handleLinkClick(link: RelatedLink) {
  if (!hasPermission(link)) return
  router.push(link.route)
}

// 简化实现：所有关联链接统一使用 LinkOutlined 图标
function getIconComponent(_iconName?: string) {
  return LinkOutlined
}

function handleWithdraw() {
  Modal.confirm({
    title: '确认撤回',
    content: '撤回后流程将终止，确认继续？',
    okText: '确认',
    cancelText: '取消',
    okButtonProps: { danger: true },
    async onOk() {
      try {
        await executeWorkItemAction(props.item.id, 'withdraw')
        message.success('撤回成功')
        emit('refresh')
      } catch {
        message.error('撤回失败，请稍后重试')
      }
    },
  })
}
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.work-item-card {
  background: $bg-card;
  border-radius: 8px;
  box-shadow: 0 1px 2px rgba(18, 31, 53, 0.04);
  display: flex;
  overflow: hidden;
  transition: box-shadow 0.18s ease, transform 0.15s ease, background-color 0.15s ease, border-color 0.18s ease;
  border: 1px solid rgba(18, 31, 53, 0.07);

  &:hover {
    border-color: rgba(255, 103, 0, 0.18);
    box-shadow: 0 8px 18px rgba(18, 31, 53, 0.08);
    transform: translateY(-1px);
  }

  &.clickable {
    cursor: pointer;
  }

  &.work-item-card--multi-selected {
    background-color: #e6f4ff;
    border-color: #91caff;
  }
}

// ===== 多选复选框 =====
.multi-select-checkbox-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 8px 0 12px;
  flex-shrink: 0;
}

// ===== 左侧优先级色条 =====
.priority-bar {
  width: 5px;
  flex-shrink: 0;
}

// ===== 来源图标 =====
.source-icon-wrap {
  width: 48px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 4px;
  border-right: 1px solid rgba(18, 31, 53, 0.04);
}

.source-icon {
  font-size: 19px;
}

// ===== 卡片内容 =====
.card-content {
  flex: 1;
  padding: 14px 18px 14px 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}

// ===== 标题行 =====
.card-header {
  display: flex;
  align-items: center;
  gap: 7px;
  min-width: 0;
}

.source-tag {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 600;
  white-space: nowrap;
  letter-spacing: 0;
}

.priority-tag {
  flex-shrink: 0;
  font-size: 11px;
  margin: 0;
  line-height: 18px;
  padding: 0 6px;
  border-radius: 999px;
}

.card-title {
  flex: 1;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.card-time {
  flex-shrink: 0;
  font-size: 12px;
  color: $text-placeholder;
  white-space: nowrap;
}

// ===== 摘要 =====
.card-summary {
  font-size: 13px;
  color: rgba(0, 0, 0, 0.56);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  line-height: 1.5;
}

// ===== 底部行 =====
.card-footer {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 4px;
}

.card-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
  min-width: 0;
}

.deadline {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-size: 12px;
  color: $text-secondary;
  white-space: nowrap;

  &.overdue {
    color: #ff4d4f;
    font-weight: 500;
  }
}

.deadline-icon {
  font-size: 11px;
}

.card-actions {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;

  :deep(.ant-btn-sm) {
    height: 28px;
    font-size: 12px;
    padding: 0 12px;
    border-radius: 6px;
  }

  :deep(.ant-btn-primary) {
    background: #ff6700;
    border-color: #ff6700;

    &:hover,
    &:focus {
      background: #ff8533;
      border-color: #ff8533;
    }
  }

  .more-actions-btn {
    padding: 0 6px;
    color: $text-secondary;

    &:hover {
      color: $text-primary;
    }
  }
}

// ===== 流程编号 =====
.process-no {
  flex-shrink: 0;
  font-size: 12px;
  color: $text-secondary;
  white-space: nowrap;
}

// ===== OA 审批增强样式 =====
.oa-node-row {
  display: flex;
  align-items: center;
  gap: 6px;

  :deep(.ant-tag) {
    margin: 0;
    font-size: 12px;
  }
}

.oa-enhanced {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;

  :deep(.ant-tag) {
    margin: 0;
  }
}

// ===== 数据导入增强样式 =====
.di-stats {
  display: flex;
  align-items: center;
  gap: 8px;

  :deep(.ant-progress) {
    flex: 1;
    margin-bottom: 0;
  }
}

.di-label {
  font-size: 12px;
  color: $text-secondary;
  white-space: nowrap;
}

.di-extra {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: $text-secondary;
}

.di-extra-icon {
  font-size: 12px;
  color: #52c41a;
}

.di-error {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  font-size: 13px;
  color: #ff4d4f;
  line-height: 1.5;
}

.di-error-icon {
  flex-shrink: 0;
  margin-top: 2px;
}

.di-alert-tags {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-wrap: wrap;

  :deep(.ant-tag) {
    margin: 0;
  }
}

.di-tag-bold {
  font-weight: 700;
}

.di-hit-rows {
  font-size: 12px;
  color: $text-regular;
}

.di-dispatch-info {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.di-dispatch-row {
  font-size: 12px;
  color: $text-regular;
  line-height: 1.5;
}

.di-dispatch-label {
  color: $text-secondary;
  font-weight: 500;
}

// ===== 关联链接行 =====
.related-links-row {
  display: flex;
  align-items: center;
  padding: 2px 0;
  margin-bottom: 2px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.related-link {
  font-size: 13px;
  color: rgba(0, 0, 0, 0.45);
  cursor: pointer;
  transition: color 0.2s;

  &:hover {
    color: #1677ff;
  }

  &.disabled {
    color: rgba(0, 0, 0, 0.25);
    cursor: not-allowed;
  }
}

.link-icon {
  font-size: 12px;
  margin-right: 3px;
}

.link-separator {
  margin: 0 6px;
  color: rgba(0, 0, 0, 0.25);
}

.related-link-summary {
  white-space: pre-wrap;
  font-size: 13px;
  color: rgba(0, 0, 0, 0.65);
  max-width: 280px;
  line-height: 1.5;
}
</style>
