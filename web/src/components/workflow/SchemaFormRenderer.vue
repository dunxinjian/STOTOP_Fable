<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import formCreate from '@form-create/ant-design-vue'

const FcFormCreate = formCreate.$form()

const props = defineProps<{
  /** Schema JSON 字符串（直接传入时优先使用） */
  schemaJson?: string
  /** Schema 标识，用于从 API 加载 */
  formRef?: string
  /** 填写 / 查看模式 */
  mode?: 'fill' | 'view'
  /** 初始表单数据 */
  initialData?: Record<string, any>
}>()

const emit = defineEmits<{
  submit: [data: Record<string, any>]
}>()

const formApi = ref<any>(null)
const formRules = ref<any[]>([])
const formOptions = ref<any>({
  submitBtn: true,
  resetBtn: false,
})
const loading = ref(false)

/** 解析 Schema JSON 字符串为 rule + option */
function parseSchemaJson(json: string) {
  try {
    const parsed = JSON.parse(json)
    // form-create 导出格式可能是 { rule, option } 或直接是 rule 数组
    if (Array.isArray(parsed)) {
      formRules.value = parsed
    } else if (parsed && Array.isArray(parsed.rule)) {
      formRules.value = parsed.rule
      if (parsed.option) {
        formOptions.value = { ...formOptions.value, ...parsed.option }
      }
    } else {
      formRules.value = []
    }
  } catch {
    console.warn('[SchemaFormRenderer] Schema JSON 解析失败')
    formRules.value = []
  }
}

/** 从 API 加载 Schema */
async function loadSchemaFromApi(formRefId: string) {
  loading.value = true
  try {
    console.warn('[SchemaFormRenderer] BPM表单接口已废除，忽略远端 Schema 加载:', formRefId)
    formRules.value = []
  } finally {
    loading.value = false
  }
}

/** 应用查看模式（禁用所有字段） */
function applyMode() {
  if (props.mode === 'view') {
    formOptions.value = {
      ...formOptions.value,
      submitBtn: false,
      resetBtn: false,
      formData: {
        ...formOptions.value.formData,
      },
    }
    // 为 form-create 设置全局 disabled
    formOptions.value.form = {
      ...formOptions.value.form,
      disabled: true,
    }
  }
}

/** 应用初始数据 */
function applyInitialData() {
  if (props.initialData) {
    formOptions.value = {
      ...formOptions.value,
      formData: { ...props.initialData },
    }
  }
}

/** 提交处理 */
function handleSubmit(formData: Record<string, any>) {
  emit('submit', formData)
}

// 监听 schemaJson 变化
watch(
  () => props.schemaJson,
  (val) => {
    if (val) {
      parseSchemaJson(val)
      applyInitialData()
      applyMode()
    }
  },
  { immediate: true },
)

// 监听 formRef 变化（仅在没有 schemaJson 时从 API 加载）
watch(
  () => props.formRef,
  (val) => {
    if (val && !props.schemaJson) {
      loadSchemaFromApi(val).then(() => {
        applyInitialData()
        applyMode()
      })
    }
  },
  { immediate: true },
)

// 监听 mode 变化
watch(() => props.mode, () => {
  applyMode()
})

// formApi 就绪后设置初始数据
watch(formApi, (api) => {
  if (api && props.initialData) {
    api.setValue(props.initialData)
  }
})

defineExpose({
  /** 获取 form-create api 实例 */
  getFormApi: () => formApi.value,
  /** 手动获取表单数据 */
  getFormData: () => formApi.value?.formData?.() ?? {},
  /** 手动触发表单验证 */
  validate: () => formApi.value?.validate?.(),
})
</script>

<template>
  <div class="schema-form-renderer">
    <a-spin :spinning="loading">
      <FcFormCreate
        v-model:api="formApi"
        :rule="formRules"
        :option="formOptions"
        @submit="handleSubmit"
      />
    </a-spin>
  </div>
</template>

<style scoped>
.schema-form-renderer {
  width: 100%;
}
</style>
