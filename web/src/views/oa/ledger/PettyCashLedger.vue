<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import type { TableColumnsType } from 'ant-design-vue'
import PageHeader from '@/components/PageHeader.vue'
import { getPettyCashLedger } from '@/api/oa'
import { useOrgContextStore } from '@/stores/orgContext'

const orgStore = useOrgContextStore()
const loading = ref(false)
const dataSource = ref<any[]>([])
const searchParams = reactive({ orgId: orgStore.currentOrgId as number | undefined })

const columns: TableColumnsType = [
  { title: '申请人', dataIndex: 'applicantName', key: 'applicantName', width: 120 },
  { title: '组织', dataIndex: 'organizationName', key: 'organizationName', width: 120 },
  { title: '备用金原额', dataIndex: 'totalApplied', key: 'totalApplied', width: 140, align: 'right' },
  { title: '已报销', dataIndex: 'totalReimbursed', key: 'totalReimbursed', width: 140, align: 'right' },
  { title: '已还款', dataIndex: 'totalReturned', key: 'totalReturned', width: 140, align: 'right' },
  { title: '未核销余额', dataIndex: 'balance', key: 'balance', width: 140, align: 'right' },
]

const expandColumns: TableColumnsType = [
  { title: '单据编号', dataIndex: 'docNumber', key: 'docNumber', width: 160 },
  { title: '类型', dataIndex: 'docType', key: 'docType', width: 100 },
  { title: '金额', dataIndex: 'amount', key: 'amount', width: 130, align: 'right' },
  { title: '状态', dataIndex: 'status', key: 'status', width: 100 },
  { title: '日期', dataIndex: 'createdAt', key: 'createdAt', width: 130 },
]

async function loadData() {
  loading.value = true
  try {
    const res = await getPettyCashLedger(searchParams) as any
    dataSource.value = res || []
  } catch {} finally { loading.value = false }
}

function formatAmount(val: number | undefined | null) {
  if (!val) return '-'
  return val.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(val: string) {
  if (!val) return '-'
  return val.substring(0, 10)
}

onMounted(() => loadData())
</script>

<template>
  <div class="page-container">
    <PageHeader title="备用金台账">
      <template #right>
        <a-space>
          <a-select v-model:value="searchParams.orgId" placeholder="全部组织" style="width: 150px" allow-clear @change="loadData">
            <a-select-option v-for="org in orgStore.organizations" :key="org.orgId" :value="org.orgId">{{ org.orgName }}</a-select-option>
          </a-select>
        </a-space>
      </template>
    </PageHeader>

    <a-table
      :columns="columns" :data-source="dataSource" :loading="loading" row-key="applicantId"
      :pagination="{ pageSize: 20, showTotal: (t: number) => `共 ${t} 条` }"
      :expandedRowRender="undefined"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'totalApplied'">{{ formatAmount(record.totalApplied) }}</template>
        <template v-if="column.key === 'totalReimbursed'">{{ formatAmount(record.totalReimbursed) }}</template>
        <template v-if="column.key === 'totalReturned'">{{ formatAmount(record.totalReturned) }}</template>
        <template v-if="column.key === 'balance'">
          <span :style="{ color: record.balance > 0 ? 'var(--color-danger)' : 'var(--color-success)', fontWeight: 600 }">{{ formatAmount(record.balance) }}</span>
        </template>
      </template>
      <template #expandedRowRender="{ record }">
        <a-table :columns="expandColumns" :data-source="record.details || []" :pagination="false" size="small">
          <template #bodyCell="{ column, record: detail }">
            <template v-if="column.key === 'amount'">{{ formatAmount(detail.amount) }}</template>
            <template v-if="column.key === 'status'"><a-tag :color="detail.status === 'Approved' ? 'green' : 'default'">{{ detail.statusText || detail.status }}</a-tag></template>
            <template v-if="column.key === 'createdAt'">{{ formatDate(detail.createdAt) }}</template>
          </template>
        </a-table>
      </template>
    </a-table>
  </div>
</template>

<style scoped lang="scss">
.page-container { padding: 16px; }
</style>
