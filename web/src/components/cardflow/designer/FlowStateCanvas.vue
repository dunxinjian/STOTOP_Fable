<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Background } from '@vue-flow/background'
import { Controls } from '@vue-flow/controls'
import { MiniMap } from '@vue-flow/minimap'
import { Handle, Position, VueFlow } from '@vue-flow/core'
import type { Edge, Node } from '@vue-flow/core'
import '@vue-flow/core/dist/style.css'
import '@vue-flow/core/dist/theme-default.css'
import type { DynamicStagePolicyRequest, StageRouteRuleRequest } from '@/types/cardflow'
import type { StageDefinition } from '../StageDefinitionEditor.vue'

const props = defineProps<{
  stages: StageDefinition[]
  routes: StageRouteRuleRequest[]
  dynamicPolicies: DynamicStagePolicyRequest[]
  selectedType?: 'node' | 'edge' | 'blank'
  selectedKey?: string | null
}>()

const emit = defineEmits<{
  (e: 'select-node', stageKey: string): void
  (e: 'select-edge', edgeKey: string): void
  (e: 'select-blank'): void
  (e: 'create-route', fromStageKey?: string): void
  (e: 'connect-route', payload: { fromStageKey: string; toStageKey: string }): void
  (e: 'reorder-stages', orderedStageKeys: string[]): void
}>()

const flowNodes = ref<Node[]>([])

const stageByKey = computed(() => {
  const map = new Map<string, StageDefinition>()
  props.stages.forEach(stage => map.set(stage.id, stage))
  return map
})

const routeCountBySource = computed(() => {
  const map = new Map<string, number>()
  props.routes.forEach(route => {
    if (!route.fromStageKey || route.status === 'disabled') return
    map.set(route.fromStageKey, (map.get(route.fromStageKey) || 0) + 1)
  })
  return map
})

const linearEdges = computed(() =>
  props.stages.slice(0, -1).map((stage, index) => {
    const target = props.stages[index + 1]
    return {
      id: `linear_${stage.id}_${target.id}`,
      source: stage.id,
      target: target.id,
      sourceHandle: 'next',
      targetHandle: 'in',
      type: 'smoothstep',
      label: '默认顺序',
      data: { kind: 'linear' },
      class: 'cf-flow-graph-edge cf-flow-graph-edge--linear',
      style: { stroke: '#9aa7a1', strokeWidth: 2 },
      labelBgStyle: { fill: '#ffffff', fillOpacity: 0.9 },
      labelStyle: { fill: '#66736d', fontWeight: 600, fontSize: 12 },
    } satisfies Edge
  }),
)

const routeEdges = computed(() =>
  props.routes
    .filter(route => route.fromStageKey && route.toStageKey && stageByKey.value.has(route.fromStageKey) && stageByKey.value.has(route.toStageKey))
    .map(route => {
      const selected = props.selectedType === 'edge' && props.selectedKey === route.edgeKey
      const isDefault = route.isDefault
      const stroke = selected ? '#1f6f5f' : isDefault ? '#475569' : '#2878a8'
      return {
        id: route.edgeKey,
        source: route.fromStageKey,
        target: route.toStageKey,
        sourceHandle: isDefault ? 'next' : 'branch',
        targetHandle: 'in',
        type: 'smoothstep',
        animated: !isDefault,
        label: `${route.routeName || (isDefault ? '默认分支' : '条件分支')} · ${isDefault ? '默认分支' : `优先级 ${route.priority}`}`,
        data: { kind: isDefault ? 'default' : 'conditional' },
        class: [
          'cf-flow-graph-edge',
          isDefault ? 'cf-flow-graph-edge--default' : 'cf-flow-graph-edge--conditional',
          selected ? 'cf-flow-graph-edge--selected' : '',
        ].filter(Boolean).join(' '),
        style: { stroke, strokeWidth: selected ? 3 : 2.5 },
        labelBgStyle: { fill: '#ffffff', fillOpacity: 0.94 },
        labelStyle: { fill: stroke, fontWeight: 700, fontSize: 12 },
      } satisfies Edge
    }),
)

const flowEdges = computed<Edge[]>(() => [...linearEdges.value, ...routeEdges.value])

const flowLayoutSignature = computed(() => [
  props.stages.map(stage => [
    stage.id,
    stage.name,
    stage.type,
    stage.processingGranularity || '',
    stage.sortOrder || '',
  ].join(':')).join('|'),
  props.dynamicPolicies.map(policy => [
    policy.policyKey,
    policy.sourceStageKey,
    policy.status || '',
  ].join(':')).join('|'),
  [...routeCountBySource.value.entries()].map(([stageKey, count]) => `${stageKey}:${count}`).join('|'),
].join('::'))

watch(
  () => flowLayoutSignature.value,
  () => syncFlowNodeLayout(),
  { immediate: true },
)

watch(
  () => [props.selectedType, props.selectedKey] as const,
  () => syncFlowNodeSelection(),
)

function syncFlowNodeLayout() {
  const previousById = new Map(flowNodes.value.map(node => [node.id, node]))
  flowNodes.value = props.stages.map((stage, index) => buildFlowNode(stage, index, previousById.get(stage.id)))
}

function syncFlowNodeSelection() {
  flowNodes.value = flowNodes.value.map(node => {
    const stage = stageByKey.value.get(node.id)
    if (!stage) return node
    return {
      ...node,
      data: buildFlowNodeData(stage, Number(node.data?.index || 0)),
    }
  })
}

function buildFlowNode(stage: StageDefinition, index: number, previous?: Node): Node {
  const preservedPosition = previous?.position || {
    x: 72 + (routeCountBySource.value.get(stage.id) ? 16 : 0),
    y: 48 + index * 142,
  }
  return {
    id: stage.id,
    type: 'stage',
    position: preservedPosition,
    draggable: true,
    selectable: true,
    data: buildFlowNodeData(stage, index),
  }
}

function buildFlowNodeData(stage: StageDefinition, index: number) {
  return {
    stage,
    index,
    stageTypeLabel: stageTypeLabel(stage),
    policyCount: stagePolicyCount(stage.id),
    routeCount: routeCountBySource.value.get(stage.id) || 0,
    selected: props.selectedType === 'node' && props.selectedKey === stage.id,
  }
}

function stageTypeLabel(stage: StageDefinition) {
  if (stage.type === 'manual') return '人工审批'
  return stage.processingGranularity === 'batch' ? '批次自动' : '自动处理'
}

function stagePolicyCount(stageKey: string) {
  return props.dynamicPolicies.filter(policy => policy.sourceStageKey === stageKey && policy.status !== 'disabled').length
}

function handleConnect(connection: { source?: string | null; target?: string | null }) {
  const source = connection.source
  const target = connection.target
  if (!source || !target || source === target) return
  emit('connect-route', { fromStageKey: source, toStageKey: target })
}

function handleNodeDragStop() {
  const orderedStageKeys = [...flowNodes.value]
    .filter(node => stageByKey.value.has(node.id))
    .sort((left, right) => {
      const yDelta = (left.position?.y || 0) - (right.position?.y || 0)
      if (Math.abs(yDelta) > 8) return yDelta
      return (left.position?.x || 0) - (right.position?.x || 0)
    })
    .map(node => node.id)
  const current = props.stages.map(stage => stage.id).join(',')
  const next = orderedStageKeys.join(',')
  if (orderedStageKeys.length === props.stages.length && next !== current) {
    emit('reorder-stages', orderedStageKeys)
  }
}

function handleNodeClick(...args: any[]) {
  const node = resolveFlowPayload(args, 'node')
  if (node?.id && stageByKey.value.has(node.id)) {
    emit('select-node', node.id)
  }
}

function handleEdgeClick(...args: any[]) {
  const edge = resolveFlowPayload(args, 'edge')
  if (!edge?.id) return
  if (String(edge.id).startsWith('linear_')) {
    emit('create-route', edge.source)
    return
  }
  emit('select-edge', edge.id)
}

function resolveFlowPayload(args: any[], key: 'node' | 'edge') {
  const wrapped = args.find(arg => arg?.[key])?.[key]
  if (wrapped) return wrapped
  return args.find(arg => arg?.id)
}
</script>

<template>
  <section class="cf-flow-canvas">
    <header class="cf-flow-canvas__head">
      <div>
        <strong>审批状态机画布</strong>
        <span>{{ stages.length }} 个节点 · {{ routes.length }} 条条件边 · {{ dynamicPolicies.length }} 个动态审批策略</span>
      </div>
      <div class="cf-flow-canvas__actions">
        <span>拖动节点调整主链顺序 · 从节点连接点拖出条件分支</span>
        <a-button size="small" type="primary" ghost @click="emit('create-route', stages[0]?.id)">
          添加条件边
        </a-button>
      </div>
    </header>

    <div v-if="stages.length === 0" class="cf-flow-canvas__empty">
      先在节点链中添加人工或自动节点
    </div>

    <div v-else class="cf-flow-canvas__graph">
      <VueFlow
        v-model:nodes="flowNodes"
        :edges="flowEdges"
        :default-viewport="{ zoom: 1, x: 0, y: 0 }"
        :min-zoom="0.55"
        :max-zoom="1.35"
        :snap-to-grid="true"
        :snap-grid="[12, 12]"
        fit-view-on-init
        class="cf-flow-vue"
        @connect="handleConnect"
        @node-click="handleNodeClick"
        @edge-click="handleEdgeClick"
        @node-drag-stop="handleNodeDragStop"
        @pane-click="emit('select-blank')"
      >
        <Background />
        <Controls />
        <MiniMap />

        <template #node-stage="nodeProps">
          <article
            class="cf-flow-node"
            :class="[
              `cf-flow-node--${nodeProps.data.stage.type}`,
              nodeProps.data.selected ? 'cf-flow-node--selected' : '',
              nodeProps.data.routeCount ? 'cf-flow-node--branch-source' : '',
            ]"
            role="button"
            tabindex="0"
            @click.stop="emit('select-node', nodeProps.id)"
            @keyup.enter.stop="emit('select-node', nodeProps.id)"
          >
            <Handle id="in" type="target" :position="Position.Top" />
            <div class="cf-flow-node__stripe" />
            <div class="cf-flow-node__body">
              <span class="cf-flow-node__index">{{ nodeProps.data.index + 1 }}</span>
              <div class="cf-flow-node__main">
                <strong>{{ nodeProps.data.stage.name || '未命名节点' }}</strong>
                <small>{{ nodeProps.data.stageTypeLabel }}</small>
              </div>
              <div class="cf-flow-node__badges">
                <span v-if="nodeProps.data.policyCount" class="cf-flow-node__badge cf-flow-node__badge--policy">
                  动态审批 {{ nodeProps.data.policyCount }}
                </span>
                <span v-if="nodeProps.data.routeCount" class="cf-flow-node__badge cf-flow-node__badge--route">
                  条件分支 {{ nodeProps.data.routeCount }}
                </span>
              </div>
            </div>
            <span class="cf-flow-node__drag">拖动排序</span>
            <Handle id="next" type="source" :position="Position.Bottom" />
            <Handle id="branch" class="cf-flow-node__branch-handle" type="source" :position="Position.Right" />
            <span class="cf-flow-node__branch-label">条件</span>
          </article>
        </template>
      </VueFlow>

      <aside class="cf-flow-canvas__legend" aria-label="流程图图例">
        <span><i class="cf-flow-canvas__dot cf-flow-canvas__dot--linear" />默认顺序</span>
        <span><i class="cf-flow-canvas__dot cf-flow-canvas__dot--conditional" />条件分支</span>
        <span><i class="cf-flow-canvas__dot cf-flow-canvas__dot--policy" />动态审批</span>
      </aside>
    </div>
  </section>
</template>

<style scoped lang="scss">
.cf-flow-canvas {
  display: flex;
  flex-direction: column;
  min-height: 620px;
  overflow: hidden;
  border: 1px solid #dfe4e8;
  background: #f8faf9;
}

.cf-flow-canvas__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  padding: 14px 16px;
  border-bottom: 1px solid #e4e8e6;
  background: rgba(255, 255, 255, .96);

  strong {
    display: block;
    color: #17251f;
    font-size: 15px;
  }

  span {
    display: block;
    margin-top: 4px;
    color: #6b7771;
    font-size: 12px;
  }
}

.cf-flow-canvas__actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;

  span {
    color: #6e7a74;
    font-size: 12px;
    white-space: nowrap;
  }
}

.cf-flow-canvas__empty {
  display: grid;
  place-items: center;
  min-height: 520px;
  color: #7b8781;
  font-size: 13px;
}

.cf-flow-canvas__graph {
  position: relative;
  flex: 1;
  min-height: 560px;
}

.cf-flow-vue {
  width: 100%;
  height: 100%;
  min-height: 560px;
  background:
    linear-gradient(90deg, rgba(38, 70, 83, .06) 1px, transparent 1px),
    linear-gradient(180deg, rgba(38, 70, 83, .06) 1px, transparent 1px),
    #fbfcfb;
  background-size: 28px 28px;
}

.cf-flow-node {
  position: relative;
  width: 340px;
  min-height: 96px;
  border: 1px solid #dce5e1;
  border-radius: 8px;
  background: #fff;
  box-shadow: 0 10px 26px rgba(23, 37, 31, .08);
  cursor: grab;
  transition: border-color .16s ease, box-shadow .16s ease, transform .16s ease;

  &:focus-visible,
  &:hover {
    border-color: #1f6f5f;
    box-shadow: 0 14px 32px rgba(31, 111, 95, .14);
    outline: none;
  }

  &:active {
    cursor: grabbing;
  }
}

.cf-flow-node--selected {
  border-color: #1f6f5f;
  box-shadow: 0 0 0 3px rgba(31, 111, 95, .16), 0 14px 32px rgba(31, 111, 95, .12);
}

.cf-flow-node__stripe {
  position: absolute;
  inset: 0 auto 0 0;
  width: 5px;
  border-radius: 8px 0 0 8px;
  background: #2c6e9f;
}

.cf-flow-node--auto .cf-flow-node__stripe {
  background: #9a6a16;
}

.cf-flow-node__body {
  display: grid;
  grid-template-columns: 38px minmax(0, 1fr);
  gap: 12px;
  padding: 16px 18px 12px 20px;
}

.cf-flow-node__index {
  display: inline-grid;
  place-items: center;
  width: 34px;
  height: 34px;
  border-radius: 50%;
  background: #edf4f2;
  color: #1f6f5f;
  font-size: 13px;
  font-weight: 800;
}

.cf-flow-node__main {
  min-width: 0;

  strong,
  small {
    display: block;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  strong {
    color: #1d2b25;
    font-size: 15px;
    line-height: 22px;
  }

  small {
    margin-top: 3px;
    color: #75827c;
    font-size: 12px;
  }
}

.cf-flow-node__badges {
  grid-column: 2;
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  min-height: 24px;
}

.cf-flow-node__badge {
  display: inline-flex;
  align-items: center;
  max-width: 100%;
  height: 22px;
  padding: 0 8px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 22px;
  white-space: nowrap;
}

.cf-flow-node__badge--policy {
  background: #fff4d8;
  color: #8a5a00;
}

.cf-flow-node__badge--route {
  background: #eaf4fb;
  color: #216b98;
}

.cf-flow-node__drag {
  position: absolute;
  right: 14px;
  bottom: 10px;
  color: #9aa7a1;
  font-size: 12px;
}

.cf-flow-node__branch-label {
  position: absolute;
  top: 34px;
  right: -34px;
  color: #2878a8;
  font-size: 12px;
  font-weight: 700;
}

:deep(.cf-flow-node__branch-handle) {
  width: 11px;
  height: 11px;
  border: 2px solid #fff;
  background: #2878a8;
  box-shadow: 0 0 0 2px rgba(40, 120, 168, .28);
}

:deep(.vue-flow__handle) {
  width: 10px;
  height: 10px;
  border: 2px solid #fff;
  background: #1f6f5f;
  box-shadow: 0 0 0 2px rgba(31, 111, 95, .22);
}

:deep(.cf-flow-graph-edge--linear .vue-flow__edge-path) {
  stroke-dasharray: 6 5;
}

:deep(.cf-flow-graph-edge--conditional .vue-flow__edge-path) {
  stroke: #2878a8;
}

:deep(.cf-flow-graph-edge--default .vue-flow__edge-path) {
  stroke: #475569;
}

:deep(.cf-flow-graph-edge--selected .vue-flow__edge-path) {
  filter: drop-shadow(0 3px 5px rgba(31, 111, 95, .25));
}

:deep(.vue-flow__edge-textbg) {
  rx: 5px;
  ry: 5px;
}

.cf-flow-canvas__legend {
  position: absolute;
  right: 14px;
  bottom: 12px;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 10px;
  border: 1px solid rgba(220, 229, 225, .9);
  border-radius: 8px;
  background: rgba(255, 255, 255, .92);
  box-shadow: 0 8px 22px rgba(23, 37, 31, .08);
  color: #52615a;
  font-size: 12px;
}

.cf-flow-canvas__legend span {
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.cf-flow-canvas__dot {
  display: inline-block;
  width: 9px;
  height: 9px;
  border-radius: 50%;
}

.cf-flow-canvas__dot--linear {
  background: #9aa7a1;
}

.cf-flow-canvas__dot--conditional {
  background: #2878a8;
}

.cf-flow-canvas__dot--policy {
  background: #c48813;
}

@media (max-width: 900px) {
  .cf-flow-canvas__head,
  .cf-flow-canvas__actions {
    align-items: flex-start;
    flex-direction: column;
  }

  .cf-flow-node {
    width: 300px;
  }

  .cf-flow-canvas__legend {
    left: 12px;
    right: auto;
    flex-wrap: wrap;
  }
}
</style>
