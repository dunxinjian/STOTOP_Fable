<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'

const props = withDefaults(defineProps<{
  rule: any[]
  option?: Record<string, any>
  value?: Record<string, any>
  readonly?: boolean
  /** 字段权限配置（来自流程节点） */
  fieldPermissions?: { field: string; state: 'editable' | 'readonly' | 'hidden' }[]
}>(), {
  rule: () => [],
  option: () => ({}),
  value: () => ({}),
  readonly: false,
  fieldPermissions: undefined,
})

const fApi = ref<any>(null)
const innerRule = ref<any[]>([])
const innerOption = ref<Record<string, any>>({})

/** 合并 option，只读模式下隐藏提交/重置按钮并全局 disabled */
function buildOption(): Record<string, any> {
  const base: Record<string, any> = {
    submitBtn: false,
    resetBtn: false,
    ...props.option,
  }
  if (props.readonly) {
    base.submitBtn = false
    base.resetBtn = false
    // form-create 全局配置 disabled
    base.formData = { ...props.value }
  }
  return base
}

/** 根据节点字段权限配置，递归应用 readonly / hidden 状态 */
function applyFieldPermissions(rules: any[], permissions?: { field: string; state: string }[]) {
  if (!permissions?.length) return
  for (const rule of rules) {
    const perm = permissions.find(p => p.field === rule.field)
    if (perm) {
      if (perm.state === 'readonly') {
        if (!rule.props) rule.props = {}
        rule.props.disabled = true
      } else if (perm.state === 'hidden') {
        rule.hidden = true
      }
      // 'editable' 不做处理，保持原始状态
    }
    if (Array.isArray(rule.children)) {
      applyFieldPermissions(rule.children, permissions)
    }
  }
}

/** 深拷贝 rule，先应用字段权限，再在只读模式下给每个组件加 disabled */
function buildRules(): any[] {
  if (!props.rule || props.rule.length === 0) return []
  try {
    const rules = JSON.parse(JSON.stringify(props.rule)) as any[]
    // 先应用节点级字段权限（只在非全局只读时有意义，但放在 readonly 前以保证全局 readonly 优先级最高）
    applyFieldPermissions(rules, props.fieldPermissions)
    if (props.readonly) {
      setDisabledRecursive(rules)
    }
    return rules
  } catch (e) {
    console.error('[DynamicForm] rule parse error:', e)
    return []
  }
}

function setDisabledRecursive(rules: any[]) {
  for (const r of rules) {
    if (!r.props) r.props = {}
    r.props.disabled = true
    if (Array.isArray(r.children)) {
      setDisabledRecursive(r.children)
    }
  }
}

/** 初始化 / rule / readonly 变更时重建 */
function rebuild() {
  innerRule.value = buildRules()
  innerOption.value = buildOption()
}

// 监听核心 props 变化（通过 JSON.stringify 比较避免 deep watch 导致的过度重建）
watch(
  () => JSON.stringify([props.rule, props.readonly, props.fieldPermissions]),
  (newVal, oldVal) => {
    if (newVal !== oldVal) rebuild()
  },
  { immediate: true }
)

// value 变化时同步到表单
watch(() => props.value, (val) => {
  if (fApi.value && val && Object.keys(val).length > 0) {
    nextTick(() => {
      try {
        fApi.value.setValue(val)
      } catch (e) {
        console.warn('[DynamicForm] setValue error:', e)
      }
    })
  }
}, { deep: true })

/** fApi 就绪后同步初始值 */
watch(fApi, (api) => {
  if (api && props.value && Object.keys(props.value).length > 0) {
    nextTick(() => {
      try {
        api.setValue(props.value)
      } catch (e) {
        console.warn('[DynamicForm] initial setValue error:', e)
      }
    })
  }
})

/** 暴露方法 - 触发表单校验 */
async function validate(): Promise<any> {
  if (!fApi.value) throw new Error('表单未就绪')
  return new Promise((resolve, reject) => {
    fApi.value.validate((valid: boolean, fail: any) => {
      if (valid) {
        resolve(fApi.value.formData())
      } else {
        reject(fail)
      }
    })
  })
}

/** 暴露方法 - 获取当前表单数据 */
function getFormData(): Record<string, any> {
  if (!fApi.value) return {}
  return fApi.value.formData()
}

/** 暴露方法 - 设置表单数据 */
function setFormData(data: Record<string, any>) {
  if (!fApi.value) {
    console.warn('[DynamicForm] setFormData: fApi not ready')
    return
  }
  fApi.value.setValue(data)
}

defineExpose({ validate, getFormData, setFormData })
</script>

<template>
  <div class="dynamic-form">
    <template v-if="innerRule.length > 0">
      <form-create
        v-model:api="fApi"
        :rule="innerRule"
        :option="innerOption"
      />
    </template>
    <a-empty v-else description="暂无表单配置" />
  </div>
</template>

<style scoped>
.dynamic-form {
  width: 100%;
}
</style>
