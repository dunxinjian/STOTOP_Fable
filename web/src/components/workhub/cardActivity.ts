import type {
  StageInstanceDto,
  CardFlowRuntimeAuditDto,
  ActionLogDto,
} from '@/types/cardflow'

export type ActivityKind = 'op' | 'decision' | 'node-enter' | 'system'

export interface ActivityEvent {
  time: string
  kind: ActivityKind
  title: string
  actor?: string
  node?: string
  opinion?: string
  status?: string
}

export interface PendingStage {
  stageName: string
  assignees: string[]
}

// 操作日志中属于"审批决策"的动作类型——决策统一由审批节点出，故在操作日志里过滤掉以去重。
// 若后端新增其它决策类动作，加到此集合即可。
export const DECISION_ACTION_TYPES = new Set(['approve', 'reject'])

const ACTION_TEXT: Record<string, string> = {
  create: '创建草稿',
  submit: '提交审批',
  approve: '审批通过',
  reject: '退回',
  withdraw: '撤回',
  resubmit: '重新提交',
  void: '废除',
  resume: '恢复',
  countersign: '加签',
  transfer: '转交',
  cc: '抄送',
  urge: '催办',
  autoComplete: '自动完成',
}

export function actionText(action: string): string {
  return ACTION_TEXT[action] || action
}

const STATUS_LABEL: Record<string, string> = {
  approved: '已通过',
  rejected: '已退回',
  processing: '处理中',
  pending: '待处理',
  skipped: '已跳过',
}

export function statusLabel(status: string): string {
  return STATUS_LABEL[status] || status
}

function ts(v?: string | null): number {
  if (!v) return NaN
  const t = new Date(v).getTime()
  return Number.isNaN(t) ? NaN : t
}

/** 合并三源为去重的活动事件，按时间升序（发起在前、最新在后） */
export function buildActivityEvents(
  stageInstances: StageInstanceDto[] = [],
  auditTrail: CardFlowRuntimeAuditDto[] = [],
  logs: ActionLogDto[] = [],
): ActivityEvent[] {
  const events: ActivityEvent[] = []

  // 1. 操作事件（排除 approve/reject，避免与节点决策重复）
  for (const log of logs) {
    if (DECISION_ACTION_TYPES.has(log.actionType)) continue
    events.push({
      time: log.operationTime,
      kind: 'op',
      title: actionText(log.actionType),
      actor: log.operatorName || '系统',
      opinion: log.opinion ?? undefined,
    })
  }

  // 2/3. 节点进入 + 审批决策
  for (const s of stageInstances) {
    if (s.activatedTime) {
      events.push({
        time: s.activatedTime,
        kind: 'node-enter',
        title: `进入「${s.stageName}」`,
        node: s.stageName,
      })
    }
    const decided = (s.assignees || []).filter((a) => a.completedTime)
    if (decided.length > 0) {
      for (const a of decided) {
        events.push({
          time: a.completedTime as string,
          kind: 'decision',
          title: statusLabel(a.status),
          actor: a.userName,
          node: s.stageName,
          status: a.status,
          opinion: a.opinion ?? undefined,
        })
      }
    } else if (s.completedTime) {
      // 自动节点（无 assignees）：以节点完成兜底，不丢自动完成
      events.push({
        time: s.completedTime,
        kind: 'decision',
        title: s.status === 'approved' ? '自动完成' : statusLabel(s.status),
        node: s.stageName,
        status: s.status,
        opinion: s.opinion ?? undefined,
      })
    }
  }

  // 4. 系统轨迹（条件流转 / 动态审批）
  for (const a of auditTrail) {
    events.push({
      time: a.eventTime,
      kind: 'system',
      title: a.title || a.reason,
      node: a.stageName ?? undefined,
    })
  }

  // 5. 升序排序，丢弃无有效时间
  return events
    .filter((e) => !Number.isNaN(ts(e.time)))
    .sort((a, b) => ts(a.time) - ts(b.time))
}

/** 当前待审节点（pending/processing），用于底部高亮 footer */
export function buildPendingStages(stageInstances: StageInstanceDto[] = []): PendingStage[] {
  return stageInstances
    .filter((s) => s.status === 'pending' || s.status === 'processing')
    .map((s) => ({
      stageName: s.stageName,
      assignees: (s.assignees || [])
        .filter((a) => !a.completedTime)
        .map((a) => a.userName)
        .filter(Boolean),
    }))
}
