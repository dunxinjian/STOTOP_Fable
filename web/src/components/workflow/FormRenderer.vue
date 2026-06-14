<script setup lang="ts">
import { ref, computed, watch, type Component } from 'vue'
import SchemaFormRenderer from './SchemaFormRenderer.vue'
import { getHardcodedForm } from '@/forms'

// Props
const props = defineProps<{
  /** 表单类型：schema（form-create 渲染） | hardcoded（硬编码组件） */
  formType: 'schema' | 'hardcoded'
  /** Schema 标识或硬编码组件名 */
  formRef?: string
  /** 流程实例 ID */
  instanceId?: number | string
  /** 节点 ID */
  nodeId?: string
  /** 填写 / 查看模式 */
  mode?: 'fill' | 'view'
  /** 初始数据 */
  initialData?: Record<string, any>
  /** Schema JSON 字符串（直接传入，schema 模式下优先使用） */
  schemaJson?: string
}>()

// Emits
const emit = defineEmits<{
  submit: [data: Record<string, any>]
  cancel: []
}>()

// Schema 渲染器引用
const schemaRendererRef = ref<InstanceType<typeof SchemaFormRenderer> | null>(null)

// 硬编码组件解析
const hardcodedComponent = computed<Component | undefined>(() => {
  if (props.formType === 'hardcoded' && props.formRef) {
    return getHardcodedForm(props.formRef)
  }
  return undefined
})

// 硬编码组件未找到
const hardcodedNotFound = computed(() => {
  return props.formType === 'hardcoded' && props.formRef && !hardcodedComponent.value
})

// 传递给硬编码组件的统一 props
const hardcodedProps = computed(() => ({
  instanceId: props.instanceId,
  nodeId: props.nodeId,
  mode: props.mode ?? 'fill',
  initialData: props.initialData,
}))

// 事件处理
function handleSchemaSubmit(data: Record<string, any>) {
  emit('submit', data)
}

function handleHardcodedSubmit(data: Record<string, any>) {
  emit('submit', data)
}

function handleCancel() {
  emit('cancel')
}

// 暴露方法
defineExpose({
  /** 获取表单数据（仅 schema 模式） */
  getFormData: () => schemaRendererRef.value?.getFormData?.() ?? {},
  /** 触发表单验证（仅 schema 模式） */
  validate: () => schemaRendererRef.value?.validate?.(),
  /** 获取 form-create API 实例（仅 schema 模式） */
  getFormApi: () => schemaRendererRef.value?.getFormApi?.(),
})
</script>

<template>
  <div class="form-renderer">
    <!-- Schema 模式：使用 form-create 渲染 -->
    <template v-if="formType === 'schema'">
      <SchemaFormRenderer
        ref="schemaRendererRef"
        :schema-json="schemaJson"
        :form-ref="formRef"
        :mode="mode"
        :initial-data="initialData"
        @submit="handleSchemaSubmit"
      />
    </template>

    <!-- 硬编码模式：动态组件渲染 -->
    <template v-else-if="formType === 'hardcoded'">
      <template v-if="hardcodedComponent">
        <component
          :is="hardcodedComponent"
          v-bind="hardcodedProps"
          @submit="handleHardcodedSubmit"
          @cancel="handleCancel"
        />
      </template>
      <a-result
        v-else-if="hardcodedNotFound"
        status="warning"
        :title="`未找到表单组件：${formRef}`"
        sub-title="请检查表单组件是否已在 forms/index.ts 中注册"
      />
    </template>
  </div>
</template>

<style scoped>
.form-renderer {
  width: 100%;
}
</style>
