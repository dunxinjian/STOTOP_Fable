<script setup lang="ts">
import { ref, watch, onMounted, toRef } from 'vue'
import { getUserList } from '@/api/system'
import type { UserItem } from '@/api/system'

const props = withDefaults(defineProps<{
  value?: number | string
  disabled?: boolean
  placeholder?: string
  orgId?: number | string
  deptId?: number | string
  status?: number | string
}>(), {
  value: undefined,
  disabled: false,
  placeholder: '请选择员工',
  orgId: '',
  deptId: '',
  status: '',
})

const emit = defineEmits<{
  'update:value': [val: number | string | undefined]
}>()

interface EmployeeOption {
  id: number
  name: string
  account: string
  phone: string
  orgName: string
}

const options = ref<EmployeeOption[]>([])
const loading = ref(false)
const searchText = ref('')

async function fetchEmployees(keyword?: string) {
  loading.value = true
  try {
    const params: Record<string, any> = { pageSize: 200 }
    if (keyword) params.keyword = keyword
    if (props.orgId) params.orgId = props.orgId
    if (props.deptId) params.orgId = props.deptId
    if (props.status !== '' && props.status !== undefined && props.status !== null) params.status = props.status
    const res = await getUserList(params) as any
    const list: UserItem[] = Array.isArray(res) ? res : (res?.items || [])
    options.value = list.map(u => ({
      id: u.id,
      name: u.name,
      account: u.account,
      phone: u.phone || '',
      orgName: u.orgName || '',
    }))
  } catch {
    options.value = []
  } finally {
    loading.value = false
  }
}

function handleSearch(val: string) {
  searchText.value = val
  fetchEmployees(val)
}

function handleChange(val: number | string | undefined) {
  emit('update:value', val)
}

onMounted(() => {
  fetchEmployees()
})

// 范围配置变化时重新加载
watch(
  [toRef(props, 'orgId'), toRef(props, 'deptId'), toRef(props, 'status')],
  () => {
    fetchEmployees(searchText.value || undefined)
  },
)
</script>

<template>
  <a-select
    :value="value"
    :disabled="disabled"
    :placeholder="placeholder"
    :loading="loading"
    show-search
    :filter-option="false"
    option-filter-prop="label"
    allow-clear
    style="width: 100%"
    @search="handleSearch"
    @change="handleChange"
  >
    <a-select-option
      v-for="emp in options"
      :key="emp.id"
      :value="emp.id"
      :label="`${emp.name}${emp.orgName ? ' /' + emp.orgName : ''}${emp.phone ? '（' + emp.phone + '）' : ''}`"
    >
      {{ emp.name }}{{ emp.orgName ? ' /' + emp.orgName : '' }}{{ emp.phone ? '（' + emp.phone + '）' : '' }}
    </a-select-option>
  </a-select>
</template>
