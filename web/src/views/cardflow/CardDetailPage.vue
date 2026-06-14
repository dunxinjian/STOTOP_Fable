<script setup lang="ts">
import { computed, h, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message, Modal } from 'ant-design-vue'
import {
  ArrowLeftOutlined,
  MoreOutlined,
  PrinterOutlined,
  FilePdfOutlined,
  RollbackOutlined,
  BellOutlined,
  StopOutlined,
  LinkOutlined,
  ThunderboltFilled,
} from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import SchemaRenderer from '@/components/cardflow/SchemaRenderer.vue'
import CardDetailTable from '@/components/cardflow/CardDetailTable.vue'
import CardTimeline from '@/components/cardflow/CardTimeline.vue'
import {
  getCard,
  getCardLogs,
  getCardRelations,
  getFlowVersionDetail,
  withdrawCard,
  urgeCard,
  voidCard,
} from '@/api/cardflow'
import { useUserStore } from '@/stores/user'
import type {
  CardDetailDto,
  ActionLogDto,
  CardRelationDto,
  SchemaFieldDefinition,
} from '@/types/cardflow'
import { parseCardSchemaFields, parseDetailSchemaFields } from '@/utils/cardflowSchema'

// ==================== Route & Store ====================

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const cardId = computed(() => Number(route.params.id))

// ==================== State ====================

const loading = ref(false)
const card = ref<CardDetailDto | null>(null)
const logs = ref<ActionLogDto[]>([])
const relations = ref<CardRelationDto[]>([])
const cardSchema = ref<SchemaFieldDefinition[]>([])
const detailSchema = ref<SchemaFieldDefinition[]>([])

// ==================== Status config ====================

interface StatusMeta {
  text: string
  color: string
  tone: 'info' | 'success' | 'warn' | 'mute' | 'draft'
}

const statusConfig: Record<string, StatusMeta> = {
  draft: { text: '草稿', color: 'default', tone: 'draft' },
  active: { text: '进行中', color: 'processing', tone: 'info' },
  completed: { text: '已完成', color: 'success', tone: 'success' },
  returned: { text: '已退回', color: 'warning', tone: 'warn' },
  voided: { text: '已作废', color: 'default', tone: 'mute' },
}

// ==================== Computed ====================

const cardData = computed<Record<string, unknown>>(() => {
  if (!card.value?.dataJson) return {}
  try {
    return JSON.parse(card.value.dataJson) as Record<string, unknown>
  } catch {
    return {}
  }
})

const detailRows = computed(() => {
  if (!card.value?.details) return []
  return card.value.details.map((d) => {
    let parsed: Record<string, unknown> = {}
    try {
      parsed = d.dataJson ? (JSON.parse(d.dataJson) as Record<string, unknown>) : {}
    } catch {
      parsed = {}
    }
    return { _id: String(d.id), sortOrder: d.sortOrder, ...parsed }
  })
})

const statusMeta = computed<StatusMeta>(() => {
  if (!card.value) return statusConfig.draft
  return statusConfig[card.value.status] || statusConfig.draft
})

const isInitiator = computed(
  () => !!card.value && card.value.initiatorId === userStore.userInfo?.id
)

const isAdmin = computed(
  () =>
    userStore.roles.includes('admin') ||
    userStore.roles.includes('Admin') ||
    userStore.hasPermission?.('cardflow:admin') === true
)

const currentStage = computed(() => {
  if (!card.value || !card.value.currentStageInstanceId) return null
  return card.value.stageInstances.find(
    (s) => s.id === card.value!.currentStageInstanceId
  ) || null
})

const isFirstStage = computed(() => {
  if (!card.value || !currentStage.value) return false
  if (card.value.stageInstances.length === 0) return false
  const sorted = [...card.value.stageInstances].sort((a, b) => a.id - b.id)
  return sorted[0].id === currentStage.value.id
})

const hasAnyApproval = computed(() => {
  if (!currentStage.value) return false
  return currentStage.value.assignees.some(
    (a) =>
      !!a.opinion ||
      !!a.completedTime ||
      a.status === 'approved' ||
      a.status === 'rejected'
  )
})

const canWithdraw = computed(
  () =>
    isInitiator.value &&
    card.value?.status === 'active' &&
    isFirstStage.value &&
    !hasAnyApproval.value
)

const canUrge = computed(() => card.value?.status === 'active')
const canVoid = computed(
  () =>
    !!card.value &&
    card.value.status !== 'voided' &&
    card.value.status !== 'draft'
)

const showToolbarUrge = computed(
  () => (isAdmin.value || isInitiator.value) && canUrge.value
)
const showToolbarVoid = computed(
  () => (isAdmin.value || isInitiator.value) && canVoid.value
)
const showToolbarWithdraw = computed(() => isInitiator.value)

const triggerRelations = computed(() =>
  relations.value.filter(
    (r) => r.sourceCardId === cardId.value && (r.relationType === 'trigger' || r.relationType === 'auto-trigger')
  )
)

const inboundRelations = computed(() =>
  relations.value.filter((r) => r.targetCardId === cardId.value)
)

const peerRelations = computed(() =>
  relations.value.filter(
    (r) =>
      r.sourceCardId === cardId.value &&
      r.relationType !== 'trigger' &&
      r.relationType !== 'auto-trigger'
  )
)

const allRelatedDisplay = computed(() => {
  // Combine for sidebar/relation list (excluding pure trigger which has dedicated banner)
  return [...peerRelations.value, ...inboundRelations.value]
})

const formatTime = (val: string | null | undefined) => {
  if (!val) return '-'
  const d = new Date(val)
  if (isNaN(d.getTime())) return String(val)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

const safeParseSchema = (json: string | null | undefined): SchemaFieldDefinition[] => {
  return parseCardSchemaFields(json)
}

// Fallback: build a schema from a record's keys when version schema is missing
const fallbackSchema = (data: Record<string, unknown>): SchemaFieldDefinition[] => {
  return Object.keys(data).map((k) => ({
    key: k,
    label: k,
    type: typeof data[k] === 'number' ? 'money' : 'text',
  })) as SchemaFieldDefinition[]
}

const effectiveCardSchema = computed(() =>
  cardSchema.value.length > 0 ? cardSchema.value : fallbackSchema(cardData.value)
)

const effectiveDetailSchema = computed(() => {
  if (detailSchema.value.length > 0) return detailSchema.value
  if (detailRows.value.length === 0) return []
  const first = detailRows.value[0] as Record<string, unknown>
  const keys = Object.keys(first).filter((k) => k !== '_id' && k !== 'sortOrder')
  return keys.map((k) => ({
    key: k,
    label: k,
    type: typeof first[k] === 'number' ? 'money' : 'text',
  })) as SchemaFieldDefinition[]
})

// ==================== Loaders ====================

async function loadCard() {
  loading.value = true
  try {
    const [cardRes, logsRes, relRes] = await Promise.all([
      getCard(cardId.value),
      getCardLogs(cardId.value).catch(() => [] as ActionLogDto[]),
      getCardRelations(cardId.value).catch(() => [] as CardRelationDto[]),
    ])
    card.value = cardRes as CardDetailDto
    logs.value = (logsRes as ActionLogDto[]) || []
    relations.value = (relRes as CardRelationDto[]) || []
    if (card.value) {
      try {
        const ver = await getFlowVersionDetail(
          card.value.flowDefinitionId,
          card.value.flowVersionId
        )
        cardSchema.value = safeParseSchema(ver.cardSchemaJson)
        detailSchema.value = parseDetailSchemaFields(ver.detailSchemaJson)
      } catch {
        cardSchema.value = []
        detailSchema.value = []
      }
    }
  } catch {
    message.error('加载卡片详情失败')
  } finally {
    loading.value = false
  }
}

// ==================== Actions ====================

function handleBack() {
  if (window.history.length > 1) router.back()
  else router.push('/cardflow/cards')
}

async function handleWithdraw() {
  if (!card.value) return
  try {
    await withdrawCard(card.value.id)
    message.success('已撤回')
    await loadCard()
  } catch {
    message.error('撤回失败')
  }
}

async function handleUrge() {
  if (!card.value) return
  try {
    await urgeCard(card.value.id)
    message.success('催办通知已发送')
  } catch {
    message.error('催办失败')
  }
}

function handleVoid() {
  if (!card.value) return
  const c = card.value
  const completed = c.status === 'completed'
  const impact = relations.value.length

  if (completed) {
    Modal.confirm({
      title: '⚠ 警告：废除已完成卡片',
      content: () =>
        h('div', { class: 'void-modal-body' }, [
          h(
            'p',
            { class: 'void-modal-warn' },
            `该卡片已完成，废除可能影响 ${impact} 张关联卡片。`
          ),
          impact > 0
            ? h(
                'ul',
                { class: 'void-modal-impact' },
                relations.value.slice(0, 6).map((r) =>
                  h('li', null, [
                    r.sourceCardId === c.id
                      ? `→ 触发：${r.targetCardNumber || '未知'}`
                      : `← 来源：${r.sourceCardNumber || '未知'}`,
                    r.relationType ? ` (${r.relationType})` : '',
                  ])
                )
              )
            : null,
        ]),
      okText: '确认废除',
      okType: 'danger',
      cancelText: '取消',
      width: 480,
      onOk: doVoid,
    })
  } else {
    Modal.confirm({
      title: '确认强制终止？',
      content: '相关待办将被取消，此操作不可恢复。',
      okText: '强制终止',
      okType: 'danger',
      cancelText: '取消',
      onOk: doVoid,
    })
  }
}

async function doVoid() {
  if (!card.value) return
  try {
    await voidCard(card.value.id)
    message.success('已废除')
    await loadCard()
  } catch {
    message.error('废除失败')
  }
}

function gotoCard(id: number) {
  router.push(`/cardflow/cards/${id}`)
}

function handlePrint() {
  // Trigger native print; @media print styles will hide non-content
  window.print()
}

// ==================== Helpers ====================

function relationTypeLabel(t: string): string {
  const map: Record<string, string> = {
    trigger: '触发',
    'auto-trigger': '自动触发',
    offset: '冲抵',
    reference: '引用',
    prerequisite: '前置',
  }
  return map[t] || t
}

function relationCardLabel(r: CardRelationDto): string {
  if (r.sourceCardId === cardId.value) {
    return r.targetCardNumber || `卡片#${r.targetCardId}`
  }
  return r.sourceCardNumber || `卡片#${r.sourceCardId}`
}

function relationCardId(r: CardRelationDto): number {
  return r.sourceCardId === cardId.value ? r.targetCardId : r.sourceCardId
}

function logActionText(action: string): string {
  const map: Record<string, string> = {
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
  return map[action] || action
}

onMounted(() => {
  loadCard()
})
</script>

<template>
  <PageHeader>
    <template #left>
      <a-button type="text" class="ribbon-back" @click="handleBack">
        <template #icon><ArrowLeftOutlined /></template>
        返回
      </a-button>
    </template>

    <template #center>
      <div v-if="card" class="ribbon-title">
        <span class="ribbon-title__num">{{ card.cardNumber || '—' }}</span>
        <span
          class="ribbon-title__pill"
          :class="`pill--${statusMeta.tone}`"
        >
          <span class="pill__dot" />
          {{ statusMeta.text }}
        </span>
        <span v-if="card.title" class="ribbon-title__sub">{{ card.title }}</span>
      </div>
    </template>

    <template #actions>
      <template v-if="card">
        <a-tooltip
          v-if="showToolbarWithdraw"
          :title="canWithdraw ? '' : '当前节点已开始处理，无法撤回'"
        >
          <a-button :disabled="!canWithdraw" @click="handleWithdraw">
            <template #icon><RollbackOutlined /></template>
            撤回
          </a-button>
        </a-tooltip>

        <a-button v-if="showToolbarUrge" @click="handleUrge">
          <template #icon><BellOutlined /></template>
          催办
        </a-button>

        <a-button v-if="showToolbarVoid" danger @click="handleVoid">
          <template #icon><StopOutlined /></template>
          {{ card.status === 'completed' ? '废除' : '强制终止' }}
        </a-button>

        <a-dropdown :trigger="['click']">
          <a-button>
            <template #icon><MoreOutlined /></template>
            更多
          </a-button>
          <template #overlay>
            <a-menu>
              <a-menu-item key="pdf" @click="handlePrint">
                <FilePdfOutlined /> 导出 PDF
              </a-menu-item>
              <a-menu-item key="print" @click="handlePrint">
                <PrinterOutlined /> 打印
              </a-menu-item>
            </a-menu>
          </template>
        </a-dropdown>
      </template>
    </template>
  </PageHeader>

  <div class="cd-page" :data-tone="statusMeta.tone">
    <a-spin :spinning="loading">
      <a-row :gutter="16" class="cd-row">
        <!-- ==================== LEFT 14 ==================== -->
        <a-col :span="14" class="cd-col cd-col--left">
          <!-- Trigger result banner (only after completion) -->
          <div
            v-if="card && card.status === 'completed' && triggerRelations.length > 0"
            class="cd-trigger-banner"
          >
            <ThunderboltFilled class="cd-trigger-banner__icon" />
            <div class="cd-trigger-banner__text">
              <span class="cd-trigger-banner__lead">已自动触发：</span>
              <template v-for="(r, idx) in triggerRelations" :key="r.id">
                <a class="cd-trigger-banner__link" @click="gotoCard(r.targetCardId)">
                  {{ r.targetCardNumber || `卡片#${r.targetCardId}` }}
                </a>
                <span v-if="idx < triggerRelations.length - 1" class="cd-trigger-banner__sep">、</span>
              </template>
            </div>
          </div>

          <!-- 卡片信息 -->
          <section class="cd-section">
            <header class="cd-section__head">
              <span class="cd-section__index">01</span>
              <h2 class="cd-section__title">卡片信息</h2>
              <span class="cd-section__line" />
            </header>
            <div class="cd-section__body">
              <SchemaRenderer
                v-if="effectiveCardSchema.length > 0"
                mode="view"
                platform="pc"
                :schema="effectiveCardSchema"
                :model-value="cardData"
              />
              <a-empty v-else description="暂无卡片字段" />
            </div>
          </section>

          <!-- 明细行 -->
          <section v-if="detailRows.length > 0" class="cd-section">
            <header class="cd-section__head">
              <span class="cd-section__index">02</span>
              <h2 class="cd-section__title">明细行</h2>
              <span class="cd-section__line" />
            </header>
            <div class="cd-section__body">
              <CardDetailTable
                mode="view"
                platform="pc"
                :schema="effectiveDetailSchema"
                :model-value="detailRows"
              />
            </div>
          </section>

          <!-- 关联卡片 -->
          <section v-if="allRelatedDisplay.length > 0" class="cd-section">
            <header class="cd-section__head">
              <span class="cd-section__index">03</span>
              <h2 class="cd-section__title">关联卡片</h2>
              <span class="cd-section__line" />
              <span class="cd-section__count">{{ allRelatedDisplay.length }}</span>
            </header>
            <div class="cd-section__body">
              <ul class="cd-relations">
                <li
                  v-for="r in allRelatedDisplay"
                  :key="r.id"
                  class="cd-relation"
                  @click="gotoCard(relationCardId(r))"
                >
                  <LinkOutlined class="cd-relation__icon" />
                  <span class="cd-relation__num">{{ relationCardLabel(r) }}</span>
                  <span class="cd-relation__type">{{ relationTypeLabel(r.relationType) }}</span>
                  <span v-if="r.description" class="cd-relation__desc">{{ r.description }}</span>
                  <span class="cd-relation__arrow">→</span>
                </li>
              </ul>
            </div>
          </section>
        </a-col>

        <!-- ==================== RIGHT 10 ==================== -->
        <a-col :span="10" class="cd-col cd-col--right">
          <!-- 审批进度 -->
          <section class="cd-section cd-section--right">
            <header class="cd-section__head">
              <span class="cd-section__index">A</span>
              <h2 class="cd-section__title">审批进度</h2>
              <span class="cd-section__line" />
            </header>
            <div class="cd-section__body">
              <CardTimeline
                v-if="card"
                mode="full"
                :stages="card.stageInstances"
                :audit-trail="card.auditTrail"
                :current-round="card.currentRound"
              />
              <a-empty v-else description="—" />
            </div>
          </section>

          <!-- 操作日志 -->
          <section class="cd-section cd-section--right cd-logs">
            <header class="cd-section__head">
              <span class="cd-section__index">B</span>
              <h2 class="cd-section__title">操作日志</h2>
              <span class="cd-section__line" />
              <span class="cd-section__count">{{ logs.length }}</span>
            </header>
            <div class="cd-section__body">
              <ol v-if="logs.length > 0" class="cd-log-list">
                <li v-for="log in logs" :key="log.id" class="cd-log-item">
                  <span class="cd-log-item__time">{{ formatTime(log.operationTime) }}</span>
                  <span class="cd-log-item__op">{{ log.operatorName || '系统' }}</span>
                  <span class="cd-log-item__action">{{ logActionText(log.actionType) }}</span>
                  <span v-if="log.opinion" class="cd-log-item__opinion">「{{ log.opinion }}」</span>
                </li>
              </ol>
              <a-empty v-else description="暂无操作日志" />
            </div>
          </section>
        </a-col>
      </a-row>
    </a-spin>
  </div>
</template>

<style lang="scss" scoped>
// ===== Tonal palette per status =====
$tone-info-fg: #1677ff;
$tone-info-bg: rgba(22, 119, 255, 0.08);
$tone-success-fg: #389e0d;
$tone-success-bg: rgba(56, 158, 13, 0.08);
$tone-warn-fg: #d4651a;
$tone-warn-bg: rgba(212, 101, 26, 0.10);
$tone-mute-fg: #8c8c8c;
$tone-mute-bg: rgba(140, 140, 140, 0.10);
$tone-draft-fg: #595959;
$tone-draft-bg: rgba(89, 89, 89, 0.08);

.cd-page {
  padding: 16px;
  background: linear-gradient(180deg, #fafbfc 0%, #f5f6f8 220px, #f5f6f8 100%);
  min-height: calc(100vh - 96px);
}

.cd-row {
  margin: 0 !important;
}

.cd-col {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

// ===== Toolbar title (center slot) =====
.ribbon-back {
  font-size: 13px;
  color: #1677ff;
  padding: 0 8px;
}

.ribbon-title {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;

  &__num {
    font-family: 'JetBrains Mono', 'SF Mono', 'Menlo', 'Consolas', monospace;
    font-size: 14px;
    font-weight: 600;
    letter-spacing: 0.04em;
    color: #1f1f1f;
    padding: 2px 8px;
    background: #fff;
    border: 1px solid #e8e8eb;
    border-radius: 4px;
    line-height: 22px;
  }

  &__sub {
    font-size: 13px;
    font-weight: 400;
    color: #595959;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    max-width: 280px;
  }

  &__pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    height: 22px;
    padding: 0 10px;
    border-radius: 11px;
    font-size: 12px;
    font-weight: 500;
    line-height: 22px;
    letter-spacing: 0.02em;

    .pill__dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: currentColor;
    }
  }
}

.pill--info {
  color: $tone-info-fg;
  background: $tone-info-bg;
  box-shadow: inset 0 0 0 1px rgba(22, 119, 255, 0.18);
  .pill__dot { animation: pulse 1.6s ease-in-out infinite; }
}
.pill--success {
  color: $tone-success-fg;
  background: $tone-success-bg;
  box-shadow: inset 0 0 0 1px rgba(56, 158, 13, 0.18);
}
.pill--warn {
  color: $tone-warn-fg;
  background: $tone-warn-bg;
  box-shadow: inset 0 0 0 1px rgba(212, 101, 26, 0.20);
}
.pill--mute {
  color: $tone-mute-fg;
  background: $tone-mute-bg;
  box-shadow: inset 0 0 0 1px rgba(140, 140, 140, 0.20);
}
.pill--draft {
  color: $tone-draft-fg;
  background: $tone-draft-bg;
  box-shadow: inset 0 0 0 1px rgba(89, 89, 89, 0.18);
}

@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.5; transform: scale(1.4); }
}

// ===== Section blocks =====
.cd-section {
  background: #fff;
  border: 1px solid #ececef;
  border-radius: 6px;
  position: relative;

  &__head {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 14px 18px 8px;
  }

  &__index {
    font-family: 'JetBrains Mono', 'SF Mono', 'Menlo', 'Consolas', monospace;
    font-size: 11px;
    font-weight: 600;
    color: #b3b3b8;
    letter-spacing: 0.1em;
  }

  &__title {
    margin: 0;
    font-size: 14px;
    font-weight: 600;
    color: #1f1f1f;
    letter-spacing: 0.01em;
  }

  &__line {
    flex: 1;
    height: 1px;
    background: linear-gradient(90deg, #e6e6ea 0%, transparent 100%);
  }

  &__count {
    font-family: 'JetBrains Mono', 'SF Mono', monospace;
    font-size: 11px;
    color: #8c8c8c;
    padding: 1px 8px;
    background: #f5f5f7;
    border-radius: 8px;
  }

  &__body {
    padding: 4px 18px 18px;
  }

  &--right {
    .cd-section__body {
      padding-top: 0;
    }
  }
}

// Top accent ribbon based on status
.cd-page[data-tone='info'] .cd-col--left .cd-section:first-of-type::before,
.cd-page[data-tone='success'] .cd-col--left .cd-section:first-of-type::before,
.cd-page[data-tone='warn'] .cd-col--left .cd-section:first-of-type::before,
.cd-page[data-tone='mute'] .cd-col--left .cd-section:first-of-type::before,
.cd-page[data-tone='draft'] .cd-col--left .cd-section:first-of-type::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  border-radius: 6px 6px 0 0;
}
.cd-page[data-tone='info']    .cd-col--left .cd-section:first-of-type::before { background: $tone-info-fg; }
.cd-page[data-tone='success'] .cd-col--left .cd-section:first-of-type::before { background: $tone-success-fg; }
.cd-page[data-tone='warn']    .cd-col--left .cd-section:first-of-type::before { background: $tone-warn-fg; }
.cd-page[data-tone='mute']    .cd-col--left .cd-section:first-of-type::before { background: $tone-mute-fg; }
.cd-page[data-tone='draft']   .cd-col--left .cd-section:first-of-type::before { background: $tone-draft-fg; }

// ===== Trigger banner =====
.cd-trigger-banner {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  background: linear-gradient(
    135deg,
    rgba(22, 119, 255, 0.06) 0%,
    rgba(22, 119, 255, 0.02) 100%
  );
  border: 1px solid rgba(22, 119, 255, 0.18);
  border-left: 3px solid #1677ff;
  border-radius: 6px;
  position: relative;
  overflow: hidden;

  &::after {
    content: '';
    position: absolute;
    top: 0; right: 0; bottom: 0;
    width: 80px;
    background: repeating-linear-gradient(
      -45deg,
      transparent 0,
      transparent 6px,
      rgba(22, 119, 255, 0.04) 6px,
      rgba(22, 119, 255, 0.04) 7px
    );
    pointer-events: none;
  }

  &__icon {
    color: #1677ff;
    font-size: 16px;
    flex-shrink: 0;
  }

  &__text {
    font-size: 13px;
    color: #1f1f1f;
    flex: 1;
    z-index: 1;
  }

  &__lead {
    color: #595959;
  }

  &__link {
    font-family: 'JetBrains Mono', 'SF Mono', monospace;
    color: #1677ff;
    font-weight: 500;
    cursor: pointer;
    letter-spacing: 0.02em;

    &:hover {
      text-decoration: underline;
    }
  }

  &__sep {
    color: #8c8c8c;
    margin: 0 2px;
  }
}

// ===== Relations list =====
.cd-relations {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.cd-relation {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  background: #fafbfc;
  border: 1px solid transparent;
  border-radius: 4px;
  cursor: pointer;
  transition: all 160ms ease;

  &:hover {
    background: #fff;
    border-color: rgba(22, 119, 255, 0.30);
    transform: translateX(2px);

    .cd-relation__arrow {
      color: #1677ff;
      transform: translateX(2px);
    }
  }

  &__icon {
    color: #8c8c8c;
    font-size: 13px;
  }

  &__num {
    font-family: 'JetBrains Mono', 'SF Mono', monospace;
    font-weight: 600;
    color: #1f1f1f;
    font-size: 13px;
    letter-spacing: 0.02em;
  }

  &__type {
    padding: 1px 6px;
    background: #fff;
    border: 1px solid #e8e8eb;
    border-radius: 3px;
    font-size: 11px;
    color: #595959;
  }

  &__desc {
    flex: 1;
    color: #8c8c8c;
    font-size: 12px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__arrow {
    margin-left: auto;
    color: #b3b3b8;
    font-size: 14px;
    transition: all 160ms ease;
  }
}

// ===== Operation log =====
.cd-logs {
  .cd-section__body {
    max-height: 360px;
    overflow-y: auto;

    &::-webkit-scrollbar { width: 4px; }
    &::-webkit-scrollbar-thumb {
      background: #d9d9d9;
      border-radius: 2px;
    }
  }
}

.cd-log-list {
  list-style: none;
  margin: 0;
  padding: 0;
  position: relative;

  &::before {
    content: '';
    position: absolute;
    left: 96px;
    top: 8px;
    bottom: 8px;
    width: 1px;
    background: #f0f0f0;
  }
}

.cd-log-item {
  display: grid;
  grid-template-columns: 92px auto auto 1fr;
  gap: 8px;
  padding: 7px 0;
  font-size: 12px;
  line-height: 1.55;
  position: relative;

  &::before {
    content: '';
    position: absolute;
    left: 95px;
    top: 14px;
    width: 5px;
    height: 5px;
    background: #fff;
    border: 1px solid #bfbfbf;
    border-radius: 50%;
  }

  &:hover::before {
    border-color: #1677ff;
    background: #1677ff;
  }

  &__time {
    color: #8c8c8c;
    font-family: 'JetBrains Mono', 'SF Mono', monospace;
    font-size: 11px;
    text-align: right;
    padding-right: 16px;
  }

  &__op {
    color: #1f1f1f;
    font-weight: 500;
    padding-left: 12px;
  }

  &__action {
    color: #1677ff;
  }

  &__opinion {
    color: #8c8c8c;
    font-style: italic;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}
</style>

<style lang="scss">
// ==================== Print styles (global, non-scoped) ====================
@media print {
  /* Hide chrome */
  .app-toolbar,
  .toolbar-back-btn,
  .breadcrumb-toolbar,
  .sidebar,
  .sidebar-container,
  .top-nav,
  .app-tabs,
  .ant-back-top,
  .cd-logs {
    display: none !important;
  }

  /* Full width left content */
  .cd-page {
    background: #fff !important;
    padding: 0 !important;
  }

  .cd-row {
    display: block !important;
  }

  .cd-col--left {
    width: 100% !important;
    max-width: 100% !important;
    flex: 0 0 100% !important;
  }

  .cd-col--right {
    page-break-before: always;
    width: 100% !important;
    max-width: 100% !important;
    flex: 0 0 100% !important;
  }

  .cd-section {
    border: 1px solid #d9d9d9 !important;
    box-shadow: none !important;
    page-break-inside: avoid;
  }

  .cd-trigger-banner {
    background: #f5f5f7 !important;
  }
}

// ==================== Modal helpers ====================
.void-modal-body {
  .void-modal-warn {
    color: #d4651a;
    margin: 0 0 8px;
    font-weight: 500;
  }

  .void-modal-impact {
    margin: 0;
    padding-left: 18px;
    font-size: 13px;
    color: #595959;

    li {
      line-height: 1.8;
    }
  }
}
</style>
