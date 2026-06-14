<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import type { TableColumnsType } from 'ant-design-vue'
import { PlusOutlined, StopOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import { getDelegationList, createDelegation, revokeDelegation } from '@/api/oa'
import { useOrgContextStore } from '@/stores/orgContext'

const orgStore = useOrgContextStore()
const loading = ref(false)
const dataSource = ref<any[]>([])
const modalVisible = ref(false)

const formData = reactive({
  delegateToUserId: undefined as number | undefined,
  delegateToUserName: '',
  organizationId: undefined as number | undefined,
  processType: undefined as string | undefined,
  startDate: '',
  endDate: '',
})

const processTypeOptions = [
  { label: '全部流程', value: undefined },
  { label: '费用请款', value: 'ExpenseRequest' },
  { label: '费用报销', value: 'ExpenseReimburse' },
  { label: '对外付款', value: 'ExternalPayment' },
  { label: '备用金申请', value: 'PettyCashApply' },
  { label: '预支工资', value: 'SalaryAdvance' },
  { label: '借款申请', value: 'LoanApply' },
]

const columns: TableColumnsType = [
  { title: '受托人', dataIndex: 'delegateToUserName', key: 'delegateToUserName', width: 120 },
  { title: '组织', dataIndex: 'organizationName', key: 'organizationName', width: 120 },
  { title: '流程类型', dataIndex: 'processTypeName', key: 'processTypeName', width: 120 },
  { title: '开始日期', dataIndex: 'startDate', key: 'startDate', width: 120 },
  { title: '结束日期', dataIndex: 'endDate', key: 'endDate', width: 120 },
  { title: '状态', dataIndex: 'status', key: 'status', width: 100, align: 'center' },
  { title: '操作', key: 'action', width: 100, align: 'center' },
]

async function loadData() {
  loading.value = true
  try { dataSource.value = (await getDelegationList() as any) || [] }
  catch {} finally { loading.value = false }
}

function handleAdd() {
  Object.assign(formData, { delegateToUserId: undefined, delegateToUserName: '', organizationId: undefined, processType: undefined, startDate: '', endDate: '' })
  modalVisible.value = true
}

async function handleOk() {
  if (!formData.delegateToUserName) { message.warning('请输入受托人'); return }
  if (!formData.startDate || !formData.endDate) { message.warning('请选择有效期'); return }
  try {
    await createDelegation(formData)
    message.success('委托创建成功')
    modalVisible.value = false
    loadData()
  } catch { message.error('创建失败') }
}

async function handleRevoke(record: any) {
  Modal.confirm({
    title: '撤销委托', content: `确定要撤销对「${record.delegateToUserName}」的委托吗？`,
    okType: 'danger',
    async onOk() {
      try { await revokeDelegation(record.id); message.success('已撤销'); loadData() }
      catch { message.error('撤销失败') }
    }
  })
}

function formatDate(val: string) {
  if (!val) return '-'
  return val.substring(0, 10)
}

onMounted(() => loadData())
</script>

<template>
  <div class="page-container">
    <PageHeader title="委托办理配置">
      <template #right>
        <a-space>
          <a-button type="primary" @click="handleAdd"><template #icon><PlusOutlined /></template>新增委托</a-button>
        </a-space>
      </template>
    </PageHeader>

    <a-alert message="委托期间，您的审批任务将由受托人代为处理。" type="info" show-icon style="margin-bottom: 16px;" />

    <a-table :columns="columns" :data-source="dataSource" :loading="loading" row-key="id" :pagination="false">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'startDate'">{{ formatDate(record.startDate) }}</template>
        <template v-if="column.key === 'endDate'">{{ formatDate(record.endDate) }}</template>
        <template v-if="column.key === 'status'">
          <a-tag :color="record.status === 'Active' ? 'green' : 'default'">{{ record.status === 'Active' ? '生效中' : '已失效' }}</a-tag>
        </template>
        <template v-if="column.key === 'action'">
          <a-button v-if="record.status === 'Active'" type="link" danger size="small" @click="handleRevoke(record)">
            <template #icon><StopOutlined /></template>撤销
          </a-button>
          <span v-else style="color: #999;">-</span>
        </template>
      </template>
    </a-table>

    <a-modal v-model:open="modalVisible" title="新增委托" @ok="handleOk" :width="500">
      <a-form layout="vertical" style="margin-top: 16px;">
        <a-form-item label="受托人" required>
          <a-input v-model:value="formData.delegateToUserName" placeholder="请输入受托人姓名" />
        </a-form-item>
        <a-form-item label="组织">
          <a-select v-model:value="formData.organizationId" placeholder="全部组织（可选）" allow-clear>
            <a-select-option v-for="org in orgStore.organizations" :key="org.orgId" :value="org.orgId">{{ org.orgName }}</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="流程类型">
          <a-select v-model:value="formData.processType" placeholder="全部流程（可选）" allow-clear>
            <a-select-option v-for="opt in processTypeOptions" :key="String(opt.value)" :value="opt.value">{{ opt.label }}</a-select-option>
          </a-select>
        </a-form-item>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="开始日期" required>
              <a-date-picker v-model:value="formData.startDate" style="width: 100%" format="YYYY-MM-DD" value-format="YYYY-MM-DD" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="结束日期" required>
              <a-date-picker v-model:value="formData.endDate" style="width: 100%" format="YYYY-MM-DD" value-format="YYYY-MM-DD" />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>
    </a-modal>
  </div>
</template>

<style scoped lang="scss">
.page-container { padding: 16px; }
</style>
