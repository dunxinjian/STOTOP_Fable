<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue'
import WorkHubCenter from './WorkHubCenter.vue'
import WorkHubDetail from './WorkHubDetail.vue'
import WorkHubRecentVisits from './WorkHubRecentVisits.vue'
import TriggerActionPanel from './TriggerActionPanel.vue'
import QualitySummaryCard from './QualitySummaryCard.vue'
import CardFlowPanel from '@/components/cardflow/CardFlowPanel.vue'
import { useWorkHub } from '@/composables/useWorkHub'

// WorkHub 共享状态（单例）——主要用于键盘导航与中栏选中项同步
const hub = useWorkHub()

type SelectedItem = { type: 'workitem' | 'notification'; id: string; data: any }

// 右栏选中项
const selectedItem = ref<SelectedItem | null>(null)

// 右栏浮窗模式
const detailExpanded = ref(false)

// CardFlow 面板（发起卡片填写）
const cardPanelVisible = ref(false)
const cardPanelCardId = ref<number | null>(null)

function handleStartCardflow(payload: { cardId: number; flowName: string }) {
  cardPanelCardId.value = payload.cardId
  cardPanelVisible.value = true
}

function handleCardFlowClosed() {
  cardPanelCardId.value = null
}

// 处理列表项选中
function handleSelectItem(item: SelectedItem) {
  selectedItem.value = item
  detailExpanded.value = false
  // 同步到 hub.selectedItemId（仅 workitem 才能参与上/下条导航，其他类型清空）
  hub.selectedItemId.value = item.type === 'workitem' ? item.id : null
}

// 反向同步：键盘导航改变 hub.selectedItemId 时，更新右栏 selectedItem
watch(
  () => hub.selectedItemId.value,
  (newId) => {
    if (!newId) {
      // selectedItemId 被置空：若当前右栏是工作项（被稍后处理/执行/移除而离开列表），关闭右栏。
      // 注意：选中通知项也会把 selectedItemId 置空，但右栏应保留该通知，故仅清工作项。
      if (selectedItem.value?.type === 'workitem') {
        selectedItem.value = null
        detailExpanded.value = false
      }
      return
    }
    // 如果当前选中项已为该 ID，跳过
    if (selectedItem.value?.type === 'workitem' && selectedItem.value.id === newId) return
    const target = hub.items.value.find((i) => i.id === newId)
    if (target) {
      selectedItem.value = { type: 'workitem', id: target.id, data: target }
      detailExpanded.value = false
    }
  }
)

// 右栏关闭时同步清空 hub.selectedItemId
function handleDetailClose() {
  selectedItem.value = null
  hub.selectedItemId.value = null
}

// ===== 全局键盘监听（J/K/Enter） =====
function handleKeydown(e: KeyboardEvent) {
  // 焦点在输入控件中时不处理
  const target = e.target as HTMLElement | null
  const tag = target?.tagName?.toLowerCase()
  if (tag === 'input' || tag === 'textarea' || tag === 'select') return
  if (target?.isContentEditable) return

  // 仅当有选中项（右栏处于预览态）时才生效
  if (!hub.selectedItemId.value) return

  if (e.key === 'j' || e.key === 'J') {
    e.preventDefault()
    hub.navigateNext()
  } else if (e.key === 'k' || e.key === 'K') {
    e.preventDefault()
    hub.navigatePrev()
  } else if (e.key === 'Enter') {
    // Enter 触发当前选中项的主操作
    // TODO: Task 5 实现 executePrimaryAction 后在此处对接
    // e.preventDefault()
    // hub.executePrimaryAction()
  }
}

onMounted(() => {
  document.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <div class="workhub-three-col">
    <!-- 左栏：CardFlow 发起入口 -->
    <aside class="workhub-left">
      <TriggerActionPanel @start-cardflow="handleStartCardflow" />
    </aside>

    <!-- 中栏：工作项/对话/通知 -->
    <WorkHubCenter
      class="workhub-center"
      @select-item="handleSelectItem"
    />

    <!-- 右栏：详情预览 / 快速导航 -->
    <div class="workhub-right">
      <WorkHubDetail
        v-if="selectedItem"
        :selected-item="selectedItem"
        @close="handleDetailClose"
        @expand="detailExpanded = !detailExpanded"
      />
      <div v-else class="workhub-right__default">
        <QualitySummaryCard />
        <WorkHubRecentVisits />
      </div>
    </div>

    <!-- CardFlow 卡片发起面板（initiator/fill） -->
    <CardFlowPanel
      v-model:visible="cardPanelVisible"
      :card-id="cardPanelCardId"
      mode="fill"
      @closed="handleCardFlowClosed"
      @submitted="handleCardFlowClosed"
      @saved="handleCardFlowClosed"
    />
  </div>
</template>

<style scoped>
.workhub-three-col {
  display: flex;
  height: calc(100vh - 48px);
  overflow: hidden;
  background: var(--bg-page);
}

.workhub-left {
  width: 238px;
  flex-shrink: 0;
  background: var(--bg-page);
  border-right: 1px solid var(--border);
  transition: width 0.2s ease;
}

.workhub-left.collapsed {
  width: 50px;
}

.workhub-center {
  flex: 1 1 auto;
  min-width: 0;
  overflow: hidden;
}

.workhub-right {
  width: clamp(380px, 29vw, 460px);
  min-width: 380px;
  flex: 0 0 clamp(380px, 29vw, 460px);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  background: var(--bg-card);
  border-left: 1px solid var(--border);
}

.workhub-right__default {
  min-height: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

@media (max-width: 1280px) {
  .workhub-left {
    width: 218px;
  }

  .workhub-right {
    width: 380px;
    min-width: 380px;
    flex-basis: 380px;
  }
}
</style>
