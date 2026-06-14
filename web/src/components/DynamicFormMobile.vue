<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'

const props = withDefaults(defineProps<{
  rule: any[]
  option?: Record<string, any>
  value?: Record<string, any>
  readonly?: boolean
}>(), {
  rule: () => [],
  option: () => ({}),
  value: () => ({}),
  readonly: false,
})

const fApi = ref<any>(null)
const innerRule = ref<any[]>([])
const innerOption = ref<Record<string, any>>({})

/** 合并 option，移动端强制单列并隐藏按钮 */
function buildOption(): Record<string, any> {
  const base: Record<string, any> = {
    submitBtn: false,
    resetBtn: false,
    ...props.option,
    // 移动端强制单列：覆盖全局 col 配置
    global: {
      ...(props.option?.global || {}),
      '*': {
        ...(props.option?.global?.['*'] || {}),
        col: { span: 24 },
      },
    },
    // form 级别的布局：垂直单列
    form: {
      ...(props.option?.form || {}),
      layout: 'vertical',
      labelCol: { span: 24 },
      wrapperCol: { span: 24 },
    },
  }
  if (props.readonly) {
    base.submitBtn = false
    base.resetBtn = false
    base.formData = { ...props.value }
  }
  return base
}

/** 深拷贝 rule，移动端强制 col=24 并处理只读 */
function buildRules(): any[] {
  if (!props.rule || props.rule.length === 0) return []
  try {
    const rules = JSON.parse(JSON.stringify(props.rule)) as any[]
    forceSingleColumn(rules)
    if (props.readonly) {
      setDisabledRecursive(rules)
    }
    return rules
  } catch (e) {
    console.error('[DynamicFormMobile] rule parse error:', e)
    return []
  }
}

/** 强制所有组件单列布局 */
function forceSingleColumn(rules: any[]) {
  for (const r of rules) {
    // 覆盖每个字段的 col 配置为满宽
    if (r.col) {
      r.col = { span: 24 }
    } else {
      r.col = { span: 24 }
    }
    if (Array.isArray(r.children)) {
      forceSingleColumn(r.children)
    }
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

watch(() => [props.rule, props.readonly], rebuild, { immediate: true, deep: true })

// value 变化时同步到表单
watch(() => props.value, (val) => {
  if (fApi.value && val && Object.keys(val).length > 0) {
    nextTick(() => {
      try {
        fApi.value.setValue(val)
      } catch (e) {
        console.warn('[DynamicFormMobile] setValue error:', e)
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
        console.warn('[DynamicFormMobile] initial setValue error:', e)
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
    console.warn('[DynamicFormMobile] setFormData: fApi not ready')
    return
  }
  fApi.value.setValue(data)
}

defineExpose({ validate, getFormData, setFormData })
</script>

<template>
  <div class="dynamic-form-mobile">
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
.dynamic-form-mobile {
  width: 100%;
  padding: 0 4px;
}

/* 移动端适配：增大字体和间距 */
.dynamic-form-mobile :deep(.ant-form-item-label > label) {
  font-size: 15px;
}

.dynamic-form-mobile :deep(.ant-input),
.dynamic-form-mobile :deep(.ant-select-selector),
.dynamic-form-mobile :deep(.ant-input-number) {
  font-size: 15px;
  min-height: 40px;
}

.dynamic-form-mobile :deep(.ant-form-item) {
  margin-bottom: 16px;
}
</style>
