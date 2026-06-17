<template>
  <div class="page-container">
    <PageHeader title="客户管理">
      <template #actions>
        <a-input
          v-model:value="searchForm.keyword"
          placeholder="简称 / 联系人 / 电话"
          allow-clear
          style="width: 200px"
          @keyup.enter="handleSearch"
        />
        <a-select
          v-model:value="searchForm.orgId"
          placeholder="所属组织"
          allow-clear
          style="width: 130px"
          :options="orgOptions"
          show-search
          :filter-option="(input: string, option: any) => option.label?.toLowerCase().includes(input.toLowerCase())"
        />
        <a-button @click="handleReset"><template #icon><ReloadOutlined /></template>重置</a-button>
        <a-divider type="vertical" style="margin: 0 4px" />
        <a-button v-if="has(CrmPermissions.CustomerCreate)" type="primary" @click="handleAdd">
          <template #icon><PlusOutlined /></template>新增客户
        </a-button>
      </template>
    </PageHeader>

    <!-- 状态快筛 Tab 兼 KPI 指标条 -->
    <div class="status-tab-bar">
      <div
        v-for="tab in statusTabs"
        :key="tab.key"
        class="status-tab-item"
        :class="{ active: activeStatusTab === tab.key }"
        @click="handleTabChange(tab.key)"
      >
        <span class="tab-dot" :style="{ background: tab.color }"></span>
        <span class="tab-label">{{ tab.label }}</span>
        <span class="tab-count" :style="{ color: activeStatusTab === tab.key ? tab.color : 'rgba(0,0,0,0.45)' }">
          {{ tab.count }}
        </span>
      </div>
    </div>

    <!-- 表格 -->
    <a-table
      :columns="tableColumns"
      :data-source="tableData"
      :loading="loading"
      :pagination="paginationConfig"
      row-key="id"
      :scroll="{ x: 1100 }"
      class="customer-table"
      :row-class-name="() => 'clickable-row'"
      @change="handleTableChange"
      @row-click="(record: any) => handleViewDetail(record)"
    >
      <template #bodyCell="{ column, record, index }">
        <!-- 序号 -->
        <template v-if="column.dataIndex === 'index'">
          {{ (pagination.pageIndex - 1) * pagination.pageSize + index + 1 }}
        </template>

        <!-- 客户名称（合并列） -->
        <template v-if="column.key === 'customerName'">
          <div class="customer-name-cell">
            <span class="name-primary">{{ record.shortName }}</span>
            <span v-if="record.fullName" class="name-secondary">{{ record.fullName }}</span>
          </div>
        </template>

        <!-- 报价数量 -->
        <template v-if="column.key === 'quotationCount'">
          <span
            class="quotation-count"
            :class="getQuotationCount(record.code || String(record.id)) > 0 ? 'count-positive' : 'count-zero'"
            @click.stop="getQuotationCount(record.code || String(record.id)) > 0 && handleQuotation(record)"
            :style="getQuotationCount(record.code || String(record.id)) > 0 ? 'cursor:pointer' : ''"
          >
            {{ quotationCountLoading ? '…' : getQuotationCount(record.code || String(record.id)) }}
          </span>
        </template>

        <!-- 联系信息（合并列） -->
        <template v-if="column.key === 'contactInfo'">
          <div class="contact-cell">
            <span v-if="record.contact" class="contact-name"><UserOutlined style="font-size:11px;margin-right:3px;opacity:.5" />{{ record.contact }}</span>
            <span v-if="record.phone" class="contact-phone"><PhoneOutlined style="font-size:11px;margin-right:3px;opacity:.5" />{{ record.phone }}</span>
            <span v-if="!record.contact && !record.phone" class="text-muted">-</span>
          </div>
        </template>

        <!-- 状态 -->
        <template v-if="column.dataIndex === 'status'">
          <span class="status-tag" :class="`status-${record.status}`">
            <span class="status-dot"></span>
            {{ statusTextMap[record.status] || '未知' }}
          </span>
        </template>

        <!-- BD负责人 -->
        <template v-if="column.key === 'bd'">
          <template v-if="record.bdEmployeeId">
            <div class="bd-cell">
              <span class="bd-avatar" :style="{ background: getBdColor(record.bdEmployeeId) }">
                {{ getEmployeeName(record.bdEmployeeId).charAt(0) }}
              </span>
              <span>{{ getEmployeeName(record.bdEmployeeId) }}</span>
            </div>
          </template>
          <span v-else class="text-muted">-</span>
        </template>

        <!-- 操作 -->
        <template v-if="column.key === 'action'">
          <div class="action-cell" @click.stop>
            <a-button type="link" size="small" @click="handleViewDetail(record as any)">
              <EyeOutlined />详情
            </a-button>
            <a-button v-if="has(CrmPermissions.CustomerEdit)" type="link" size="small" @click="handleEdit(record as any)">
              <EditOutlined />编辑
            </a-button>
            <a-dropdown trigger="click">
              <a-button type="link" size="small"><EllipsisOutlined /></a-button>
              <template #overlay>
                <a-menu>
                  <a-menu-item @click="handleQuotation(record)">
                    <DollarOutlined />报价
                  </a-menu-item>
                  <a-menu-item v-if="has(CrmPermissions.CustomerDelete)" danger @click="confirmDelete(record as any)">
                    <DeleteOutlined />删除
                  </a-menu-item>
                </a-menu>
              </template>
            </a-dropdown>
          </div>
        </template>
      </template>
      <template #emptyText>
        <EmptyState description="暂无客户数据" />
      </template>
    </a-table>

    <!-- 新增/编辑 Drawer -->
    <a-drawer
      v-model:open="dialogVisible"
      :title="dialogType === 'add' ? '新增客户' : '编辑客户'"
      width="640px"
      :destroy-on-close="true"
      placement="right"
    >
      <a-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        :label-col="{ style: { width: '100px' } }"
      >
        <!-- 基本信息 -->
        <div class="form-section-title">基本信息</div>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="客户简称" name="shortName">
              <a-input v-model:value="formData.shortName" placeholder="请输入" :maxlength="100" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="编号">
              <a-input v-model:value="formData.code" placeholder="可选" :maxlength="50" />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="16">
          <a-col :span="24">
            <a-form-item label="客户全称">
              <a-input v-model:value="formData.fullName" placeholder="可选" :maxlength="200" />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="行业">
              <a-input v-model:value="formData.industry" placeholder="请输入" :maxlength="50" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="规模">
              <a-input v-model:value="formData.scale" placeholder="请输入" :maxlength="50" />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="状态">
              <a-select v-model:value="formData.status" :options="statusOptions" placeholder="请选择" />
            </a-form-item>
          </a-col>
        </a-row>

        <!-- 联系信息 -->
        <div class="form-section-title">联系信息</div>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="联系人">
              <a-input v-model:value="formData.contact" placeholder="请输入" :maxlength="50" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="电话">
              <a-input v-model:value="formData.phone" placeholder="请输入" :maxlength="30" />
            </a-form-item>
          </a-col>
        </a-row>

        <!-- 组织与负责人 -->
        <div class="form-section-title">组织与负责人</div>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="所属组织">
              <a-select
                v-model:value="formData.orgId"
                :options="formOrgOptions"
                placeholder="请选择"
                allow-clear
                show-search
                :filter-option="(input: string, option: any) => option.label?.toLowerCase().includes(input.toLowerCase())"
                @change="(v: any) => handleOrgChange(v as number | undefined)"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="业务对象">
              <a-select
                v-model:value="formData.serviceObjectId"
                :options="serviceObjectOptions"
                :placeholder="formData.orgId ? '请选择' : '请先选择组织'"
                :disabled="!formData.orgId"
                allow-clear
                show-search
                :filter-option="filterEmployee"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="BD员工">
              <a-select
                v-model:value="formData.bdEmployeeId"
                :options="bdEmployeeOptions"
                :placeholder="formData.orgId ? '请选择' : '请先选择组织'"
                :disabled="!formData.orgId"
                allow-clear
                show-search
                :filter-option="filterEmployee"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="运维员工">
              <a-select
                v-model:value="formData.maintenanceEmployeeId"
                :options="maintenanceEmployeeOptions"
                :placeholder="formData.orgId ? '请选择' : '请先选择组织'"
                :disabled="!formData.orgId"
                allow-clear
                show-search
                :filter-option="filterEmployee"
              />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>

      <template #footer>
        <a-space>
          <a-button @click="dialogVisible = false">取消</a-button>
          <a-button type="primary" :loading="submitLoading" @click="handleSubmit">确定</a-button>
        </a-space>
      </template>
    </a-drawer>

    <QuotationDrawer
      v-model:open="quotationDrawerVisible"
      :client-id="currentQuotationClientId"
      client-type="KH"
      :client-name="currentQuotationClientName"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import type { FormInstance } from 'ant-design-vue'
import type { Rule } from 'ant-design-vue/es/form'
import { useRouter } from 'vue-router'
import {
  PlusOutlined, EditOutlined, DeleteOutlined, EyeOutlined,
  ReloadOutlined, UserOutlined, PhoneOutlined,
  EllipsisOutlined, DollarOutlined,
} from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import EmptyState from '@/components/EmptyState.vue'
import QuotationDrawer from '@/views/express/components/QuotationDrawer.vue'
import { usePermission, CrmPermissions } from '@/utils/permission'
import {
  getCustomerList,
  getCustomerById,
  createCustomer,
  updateCustomer,
  deleteCustomer,
  getBdList,
  getMaintenanceList,
  type CustomerListItemDto,
  type CreateCustomerRequest,
  type CrmRoleMappingListItemDto,
} from '@/api/crm'
import { getClientQuotationSummary } from '@/api/express'
import { getOrganizationTree, type OrgTreeNode } from '@/api/system'
import { getEmployeeList, type EmployeeDto } from '@/api/hr'

const router = useRouter()
const { has } = usePermission()

// ==================== 静态配置 ====================

const statusOptions = [
  { label: '潜在', value: 0 },
  { label: '活跃', value: 1 },
  { label: '流失', value: 2 },
]
const statusTextMap: Record<number, string> = { 0: '潜在', 1: '活跃', 2: '流失' }

const tableColumns = [
  { title: '序号', dataIndex: 'index', key: 'index', width: 56, align: 'center' as const },
  { title: '客户名称', key: 'customerName', width: 200, ellipsis: true },
  { title: '报价', key: 'quotationCount', width: 72, align: 'center' as const },
  { title: '联系信息', key: 'contactInfo', width: 160 },
  { title: '行业', dataIndex: 'industry', key: 'industry', width: 90, customRender: ({ text }: any) => text || '-' },
  { title: '状态', dataIndex: 'status', key: 'status', width: 88, align: 'center' as const },
  { title: 'BD负责人', key: 'bd', width: 130 },
  { title: '操作', key: 'action', width: 180, align: 'center' as const, fixed: 'right' as const },
]

// BD头像色板
const bdColors = ['#5B7290', '#6BA292', '#C99A6B', '#9B8AB8', '#C77B6B', '#8FB07E']
function getBdColor(id?: number): string {
  if (!id) return '#d9d9d9'
  return bdColors[id % bdColors.length]
}

// ==================== 状态Tab ====================

const activeStatusTab = ref<number | ''>('')

const stats = reactive({ total: 0, potential: 0, active: 0, lost: 0 })

const statusTabs = computed(() => [
  { key: '' as const, label: '全部', color: 'rgba(0,0,0,0.25)', count: stats.total },
  { key: 0 as const, label: '潜在', color: 'var(--color-info)', count: stats.potential },
  { key: 1 as const, label: '活跃', color: 'var(--color-success)', count: stats.active },
  { key: 2 as const, label: '流失', color: 'var(--color-danger)', count: stats.lost },
])

function handleTabChange(key: number | '') {
  activeStatusTab.value = key
  searchForm.status = key === '' ? undefined : key
  pagination.pageIndex = 1
  fetchList()
}

async function initStats() {
  try {
    const [allRes, potRes, actRes, lostRes] = await Promise.all([
      getCustomerList({ pageIndex: 1, pageSize: 1 }) as any,
      getCustomerList({ pageIndex: 1, pageSize: 1, status: 0 }) as any,
      getCustomerList({ pageIndex: 1, pageSize: 1, status: 1 }) as any,
      getCustomerList({ pageIndex: 1, pageSize: 1, status: 2 }) as any,
    ])
    stats.total = allRes?.total ?? 0
    stats.potential = potRes?.total ?? 0
    stats.active = actRes?.total ?? 0
    stats.lost = lostRes?.total ?? 0
  } catch { /* ignore */ }
}

// ==================== 报价数量 ====================

const quotationCountMap = ref<Record<string, number>>({})
const quotationCountLoading = ref(false)

function getQuotationCount(clientKey?: string): number {
  if (!clientKey) return 0
  return quotationCountMap.value[clientKey] ?? 0
}

async function fetchQuotationCounts() {
  quotationCountLoading.value = true
  try {
    const res = await getClientQuotationSummary({ pageSize: 9999, type: 'KH' }) as any
    const items = res?.items || res || []
    const map: Record<string, number> = {}
    for (const item of items) {
      map[String(item.id)] = item.quotationCount ?? 0
    }
    quotationCountMap.value = map
  } catch { /* ignore */ }
  finally { quotationCountLoading.value = false }
}

// ==================== 搜索与分页 ====================

const searchForm = reactive({
  keyword: '',
  orgId: undefined as number | undefined,
  status: undefined as number | undefined,
})

const loading = ref(false)
const tableData = ref<CustomerListItemDto[]>([])
const pagination = reactive({ pageIndex: 1, pageSize: 20, total: 0 })

const paginationConfig = computed(() => ({
  current: pagination.pageIndex,
  pageSize: pagination.pageSize,
  total: pagination.total,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50', '100'],
  showTotal: (t: number) => `共 ${t} 条`,
}))

function handleTableChange(pag: any) {
  pagination.pageIndex = pag.current
  pagination.pageSize = pag.pageSize
  fetchList()
}

function handleSearch() {
  pagination.pageIndex = 1
  fetchList()
}

function handleReset() {
  searchForm.keyword = ''
  searchForm.orgId = undefined
  searchForm.status = undefined
  activeStatusTab.value = ''
  pagination.pageIndex = 1
  fetchList()
}

async function fetchList() {
  loading.value = true
  try {
    const params: any = { pageIndex: pagination.pageIndex, pageSize: pagination.pageSize }
    if (searchForm.keyword) params.keyword = searchForm.keyword
    if (searchForm.status !== undefined) params.status = searchForm.status
    if (searchForm.orgId !== undefined) params.orgId = searchForm.orgId
    const res = await getCustomerList(params) as any
    if (res) {
      tableData.value = res?.items || res || []
      pagination.total = res?.total || res?.length || 0
    }
    fetchQuotationCounts()
  } finally {
    loading.value = false
  }
}

// ==================== 辅助数据 ====================

const orgOptions = ref<{ label: string; value: number }[]>([])
const formOrgOptions = ref<{ label: string; value: number }[]>([])
const bdEmployeeOptions = ref<{ label: string; value: number }[]>([])
const maintenanceEmployeeOptions = ref<{ label: string; value: number }[]>([])
const serviceObjectOptions = ref<{ label: string; value: number }[]>([])
const orgMap = ref<Record<number, string>>({})
const employeeMap = ref<Record<number, string>>({})

function getOrgName(id?: number) {
  return id ? (orgMap.value[id] || '-') : '-'
}

function getEmployeeName(id?: number) {
  return id ? (employeeMap.value[id] || '-') : '-'
}

function filterEmployee(input: string, option: any) {
  return option.label?.toLowerCase().includes(input.toLowerCase())
}

function flattenOrgs(nodes: OrgTreeNode[], result: { label: string; value: number }[] = [], filterTypeIds?: number[]) {
  for (const n of nodes) {
    if (!filterTypeIds || filterTypeIds.includes(n.typeId ?? 0)) {
      result.push({ label: n.name, value: n.id })
    }
    if (n.children?.length) flattenOrgs(n.children, result, filterTypeIds)
  }
  return result
}

async function fetchOrgs() {
  try {
    const res = await getOrganizationTree() as any
    const nodes: OrgTreeNode[] = res || []
    const flat = flattenOrgs(nodes)
    orgOptions.value = flat
    orgMap.value = Object.fromEntries(flat.map((o) => [o.value, o.label]))
    formOrgOptions.value = flattenOrgs(nodes, [], [1, 2])
  } catch { /* ignore */ }
}

async function fetchEmployees() {
  try {
    const res = await getEmployeeList({ pageSize: 9999 }) as any
    const items: EmployeeDto[] = res?.items || res || []
    employeeMap.value = Object.fromEntries(items.map((e) => [e.id, e.name]))
  } catch { /* ignore */ }
}

async function loadOrgDependentOptions(orgId: number) {
  try {
    const [bdRes, maintRes] = await Promise.all([
      getBdList(orgId) as any,
      getMaintenanceList(orgId) as any,
    ])
    const bdItems: CrmRoleMappingListItemDto[] = bdRes || []
    bdEmployeeOptions.value = bdItems.map((r) => ({
      label: employeeMap.value[r.employeeId] || `员工${r.employeeId}`,
      value: r.employeeId,
    }))
    const maintItems: CrmRoleMappingListItemDto[] = maintRes || []
    maintenanceEmployeeOptions.value = maintItems.map((r) => ({
      label: employeeMap.value[r.employeeId] || `员工${r.employeeId}`,
      value: r.employeeId,
    }))
    // 业务对象：活跃客户列表
    const custRes = await getCustomerList({ orgId, pageIndex: 1, pageSize: 500, status: 1 }) as any
    const custItems = custRes?.items || custRes || []
    serviceObjectOptions.value = (Array.isArray(custItems) ? custItems : []).map((item: any) => ({
      label: item.shortName,
      value: item.id,
    }))
  } catch { /* ignore */ }
}

function clearOrgDependentFields() {
  formData.bdEmployeeId = undefined
  formData.maintenanceEmployeeId = undefined
  formData.serviceObjectId = undefined
  bdEmployeeOptions.value = []
  maintenanceEmployeeOptions.value = []
  serviceObjectOptions.value = []
}

function handleOrgChange(orgId: number | undefined) {
  clearOrgDependentFields()
  if (orgId) loadOrgDependentOptions(orgId)
}

// ==================== 报价抽屉 ====================

const quotationDrawerVisible = ref(false)
const currentQuotationClientId = ref('')
const currentQuotationClientName = ref('')

function handleQuotation(record: any) {
  currentQuotationClientId.value = record.code || String(record.id)
  currentQuotationClientName.value = record.shortName || record.fullName || ''
  quotationDrawerVisible.value = true
}

// ==================== CRUD ====================

const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const formRef = ref<FormInstance>()
const submitLoading = ref(false)
const currentId = ref<number | null>(null)

const formData = reactive({
  code: '',
  shortName: '',
  fullName: '',
  contact: '',
  phone: '',
  industry: '',
  scale: '',
  serviceObjectId: undefined as number | undefined,
  status: 0,
  orgId: undefined as number | undefined,
  bdEmployeeId: undefined as number | undefined,
  maintenanceEmployeeId: undefined as number | undefined,
})

const formRules: Record<string, Rule[]> = {
  shortName: [{ required: true, message: '请输入客户简称', trigger: 'blur' }],
}

function resetForm() {
  formData.code = ''
  formData.shortName = ''
  formData.fullName = ''
  formData.contact = ''
  formData.phone = ''
  formData.industry = ''
  formData.scale = ''
  formData.serviceObjectId = undefined
  formData.status = 0
  formData.orgId = undefined
  formData.bdEmployeeId = undefined
  formData.maintenanceEmployeeId = undefined
  bdEmployeeOptions.value = []
  maintenanceEmployeeOptions.value = []
  serviceObjectOptions.value = []
}

function handleAdd() {
  dialogType.value = 'add'
  currentId.value = null
  resetForm()
  dialogVisible.value = true
}

async function handleEdit(row: CustomerListItemDto) {
  dialogType.value = 'edit'
  currentId.value = row.id
  resetForm()
  dialogVisible.value = true
  try {
    const detail = await getCustomerById(row.id) as any
    if (detail) {
      formData.code = detail.code || ''
      formData.shortName = detail.shortName || ''
      formData.fullName = detail.fullName || ''
      formData.contact = detail.contact || ''
      formData.phone = detail.phone || ''
      formData.industry = detail.industry || ''
      formData.scale = detail.scale || ''
      formData.status = detail.status ?? 0
      formData.orgId = detail.orgId
      if (detail.orgId) await loadOrgDependentOptions(detail.orgId)
      formData.bdEmployeeId = detail.bdEmployeeId
      formData.maintenanceEmployeeId = detail.maintenanceEmployeeId
      formData.serviceObjectId = detail.serviceObjectId
    }
  } catch (error) {
    console.error('获取客户详情失败:', error)
  }
}

function handleViewDetail(row: CustomerListItemDto) {
  router.push(`/crm/customers/${row.id}`)
}

async function handleSubmit() {
  if (!formRef.value) return
  try { await formRef.value.validate() } catch { return }

  submitLoading.value = true
  try {
    const data: CreateCustomerRequest = {
      code: formData.code || undefined,
      shortName: formData.shortName,
      fullName: formData.fullName || undefined,
      contact: formData.contact || undefined,
      phone: formData.phone || undefined,
      industry: formData.industry || undefined,
      scale: formData.scale || undefined,
      orgId: formData.orgId,
      bdEmployeeId: formData.bdEmployeeId,
      maintenanceEmployeeId: formData.maintenanceEmployeeId,
    }
    if (dialogType.value === 'add') {
      await createCustomer(data)
      message.success('新增成功')
    } else {
      await updateCustomer(currentId.value!, data)
      message.success('更新成功')
    }
    dialogVisible.value = false
    fetchList()
    initStats()
  } finally {
    submitLoading.value = false
  }
}

function confirmDelete(row: CustomerListItemDto) {
  Modal.confirm({
    title: '确定删除该客户吗？',
    okText: '确定',
    cancelText: '取消',
    okType: 'danger',
    onOk: async () => {
      try {
        await deleteCustomer(row.id)
        fetchList()
        initStats()
      } catch (error) {
        console.error('删除失败:', error)
      }
    },
  })
}

onMounted(() => {
  fetchOrgs()
  fetchEmployees()
  fetchList()
  initStats()
})
</script>

<style scoped lang="scss">
// ==================== 状态 Tab 行 ====================
.status-tab-bar {
  display: flex;
  align-items: stretch;
  background: #fff;
  border-bottom: 1px solid #f0f0f0;
  padding: 0 16px;
  flex-shrink: 0;
}

.status-tab-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 20px;
  height: 46px;
  cursor: pointer;
  position: relative;
  transition: color 0.2s;
  user-select: none;
  color: rgba(0, 0, 0, 0.65);

  &::after {
    content: '';
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 2px;
    background: transparent;
    border-radius: 1px;
    transition: background 0.2s;
  }

  &.active {
    color: rgba(0, 0, 0, 0.85);
    font-weight: 500;

    &::after {
      background: var(--color-primary);
    }
  }

  &:hover:not(.active) {
    color: rgba(0, 0, 0, 0.85);
    background: rgba(0, 0, 0, 0.02);
  }
}

.tab-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  display: inline-block;
  flex-shrink: 0;
}

.tab-label {
  font-size: 14px;
}

.tab-count {
  font-size: 13px;
  font-weight: 600;
  min-width: 20px;
  text-align: center;
  transition: color 0.2s;
}

// ==================== 表格 ====================
.customer-table {
  :deep(.ant-table) {
    border-radius: 0;
  }
  :deep(.ant-table-thead > tr > th) {
    background: #fafafa;
  }
  :deep(.clickable-row) {
    cursor: pointer;
    &:hover td {
      background: var(--color-primary-light) !important;
    }
  }
}

// 客户名称合并列
.customer-name-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;

  .name-primary {
    font-weight: 500;
    font-size: 14px;
    color: rgba(0, 0, 0, 0.85);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .name-secondary {
    font-size: 12px;
    color: rgba(0, 0, 0, 0.4);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

// 报价数量列
.quotation-count {
  font-weight: 700;
  font-size: 15px;

  &.count-positive {
    color: var(--color-success-text);
  }

  &.count-zero {
    color: var(--color-danger-text);
  }
}

// 联系信息合并列
.contact-cell {
  display: flex;
  flex-direction: column;
  gap: 3px;

  .contact-name {
    font-size: 13px;
    color: rgba(0, 0, 0, 0.75);
  }

  .contact-phone {
    font-size: 12px;
    color: rgba(0, 0, 0, 0.45);
  }
}

// 状态 Tag
.status-tag {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;

  .status-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  &.status-0 {
    background: var(--color-info-light);
    color: var(--color-info);
    .status-dot { background: var(--color-info); }
  }

  &.status-1 {
    background: var(--color-success-light);
    color: var(--color-success);
    .status-dot { background: var(--color-success); }
  }

  &.status-2 {
    background: var(--color-danger-light);
    color: var(--color-danger);
    .status-dot { background: var(--color-danger); }
  }
}

// BD负责人列
.bd-cell {
  display: flex;
  align-items: center;
  gap: 7px;
}

.bd-avatar {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  color: #fff;
  font-size: 12px;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

// 操作列
.action-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0;

  :deep(.ant-btn-link) {
    padding: 0 6px;
    font-size: 13px;
  }
}

// 通用灰色文本
.text-muted {
  color: rgba(0, 0, 0, 0.25);
}

// ==================== Drawer 表单分区 ====================
.form-section-title {
  font-size: 13px;
  font-weight: 600;
  color: rgba(0, 0, 0, 0.65);
  padding-left: 10px;
  border-left: 3px solid var(--color-info);
  margin: 20px 0 14px;

  &:first-child {
    margin-top: 4px;
  }
}
</style>
