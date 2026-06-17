<template>
  <div class="page-container">
    <PageHeader title="车辆台账" description="管理三轮车基础信息">
      <template #actions>
        <a-input-search v-model:value="searchForm.keyword" placeholder="编码/车牌号" style="width: 200px" @search="handleSearch" allowClear />
        <a-select v-model:value="searchForm.ownershipType" placeholder="权属类型" allow-clear style="width: 120px" :options="ownershipOptions" @change="handleSearch" />
        <a-select v-model:value="searchForm.vehicleStatus" placeholder="车辆状态" allow-clear style="width: 120px" :options="vehicleStatusOptions" @change="handleSearch" />
        <a-button @click="handleReset">重置</a-button>
        <a-divider type="vertical" style="height: 24px; margin: 0 8px;" />
        <a-button type="primary" @click="handleAdd">
          <template #icon><PlusOutlined /></template>新增车辆
        </a-button>
      </template>
    </PageHeader>

    <!-- 统计卡片 -->
    <a-row :gutter="16" style="margin-bottom: 16px">
      <a-col :span="6">
        <a-card :bordered="false" class="stat-card">
          <a-statistic title="总车辆数" :value="statistics.totalCount" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card :bordered="false" class="stat-card">
          <a-statistic title="闲置数" :value="getStatusCount(1)" :value-style="{ color: '#666' }" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card :bordered="false" class="stat-card">
          <a-statistic title="使用中" :value="getStatusCount(2)" :value-style="{ color: 'var(--color-success)' }" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card :bordered="false" class="stat-card">
          <a-statistic title="维修中" :value="getStatusCount(3)" :value-style="{ color: 'var(--color-warning)' }" />
        </a-card>
      </a-col>
    </a-row>

    <a-card :bordered="false">
      <a-table
        :columns="tableColumns"
        :data-source="tableData"
        :loading="loading"
        :pagination="paginationConfig"
        row-key="id"
        bordered
        :scroll="{ x: 1200 }"
        @change="handleTableChange"
      >
        <template #bodyCell="{ column, record, index }">
          <template v-if="column.dataIndex === 'index'">
            {{ (pagination.pageIndex - 1) * pagination.pageSize + index + 1 }}
          </template>
          <template v-if="column.dataIndex === 'code'">
            <a-tooltip :title="record.code">{{ record.code }}</a-tooltip>
          </template>
          <template v-if="column.dataIndex === 'ownershipType'">
            <a-tag :color="record.ownershipType === 1 ? 'blue' : 'green'">
              {{ record.ownershipType === 1 ? '公司' : '个人' }}
            </a-tag>
          </template>
          <template v-if="column.dataIndex === 'vehicleStatus'">
            <a-tag :color="getVehicleStatusColor(record.vehicleStatus)">
              {{ getVehicleStatusText(record.vehicleStatus) }}
            </a-tag>
          </template>
          <template v-if="column.dataIndex === 'createdTime'">
            {{ formatDate(record.createdTime) }}
          </template>
          <template v-if="column.dataIndex === 'action'">
            <a-button type="link" size="small" @click="handleEdit(record)">
              <EditOutlined />编辑
            </a-button>
            <a-popconfirm
              title="确定删除该车辆吗？"
              ok-text="确定"
              cancel-text="取消"
              @confirm="handleDelete(record)"
            >
              <a-button type="link" size="small" danger>
                <DeleteOutlined />删除
              </a-button>
            </a-popconfirm>
          </template>
        </template>
        <template #emptyText>
          <EmptyState description="暂无车辆数据" />
        </template>
      </a-table>
    </a-card>

    <!-- 新增/编辑弹窗 -->
    <a-modal
      v-model:open="dialogVisible"
      :title="dialogType === 'add' ? '新增车辆' : '编辑车辆'"
      width="700px"
      :destroy-on-close="true"
      @cancel="dialogVisible = false"
    >
      <a-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        :label-col="{ style: { width: '100px' } }"
        style="padding: 10px 20px"
      >
        <a-row :gutter="20">
          <a-col :span="12">
            <a-form-item label="编码" name="code">
              <a-input
                v-model:value="formData.code"
                placeholder="请输入车辆编码"
                :maxlength="50"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="车牌号">
              <a-input
                v-model:value="formData.plateNumber"
                placeholder="请输入车牌号"
                :maxlength="20"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="20">
          <a-col :span="12">
            <a-form-item label="品牌">
              <a-input
                v-model:value="formData.brand"
                placeholder="请输入品牌"
                :maxlength="50"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="车架号">
              <a-input
                v-model:value="formData.frameNumber"
                placeholder="请输入车架号"
                :maxlength="100"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="20">
          <a-col :span="12">
            <a-form-item label="权属类型" name="ownershipType">
              <a-select
                v-model:value="formData.ownershipType"
                placeholder="请选择权属类型"
                :options="ownershipOptions.slice(1)"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item v-if="formData.ownershipType === 2" label="所有人姓名">
              <a-input
                v-model:value="formData.ownerName"
                placeholder="请输入所有人姓名"
                :maxlength="50"
              />
            </a-form-item>
            <a-form-item v-else label="所有人ID">
              <a-input-number
                v-model:value="formData.ownerId"
                placeholder="请输入所有人ID"
                style="width: 100%"
                :min="1"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="20">
          <a-col :span="12">
            <a-form-item label="购入日期">
              <a-date-picker
                v-model:value="formData.purchaseDate"
                placeholder="请选择购入日期"
                style="width: 100%"
                value-format="YYYY-MM-DD"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="购入价格">
              <a-input-number
                v-model:value="formData.purchasePrice"
                placeholder="请输入购入价格"
                style="width: 100%"
                :min="0"
                :precision="2"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="20">
          <a-col :span="12">
            <a-form-item label="颜色">
              <a-input
                v-model:value="formData.color"
                placeholder="请输入颜色"
                :maxlength="20"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="GPS设备号">
              <a-input
                v-model:value="formData.gpsDeviceNo"
                placeholder="请输入GPS设备号"
                :maxlength="50"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="20">
          <a-col :span="24">
            <a-form-item label="备注">
              <a-textarea
                v-model:value="formData.remark"
                :rows="2"
                placeholder="请输入备注"
                :maxlength="500"
                show-count
              />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>
      <template #footer>
        <a-button @click="dialogVisible = false">取消</a-button>
        <a-button type="primary" :loading="submitLoading" @click="handleSubmit">确定</a-button>
      </template>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import type { FormInstance } from 'ant-design-vue'
import type { Rule } from 'ant-design-vue/es/form'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import EmptyState from '@/components/EmptyState.vue'
import {
  getVehicleList,
  getVehicleDetail,
  createVehicle,
  updateVehicle,
  deleteVehicle,
  getVehicleStatistics,
  type VehicleListItemDto,
  type VehicleStatisticsDto,
} from '@/api/vehicle'

// 选项配置
const ownershipOptions = [
  { label: '全部', value: '' },
  { label: '公司', value: 1 },
  { label: '员工个人', value: 2 },
]

const vehicleStatusOptions = [
  { label: '全部', value: '' },
  { label: '闲置', value: 1 },
  { label: '使用中', value: 2 },
  { label: '维修中', value: 3 },
  { label: '报废', value: 4 },
]

// 表格列配置
const tableColumns = [
  { title: '序号', dataIndex: 'index', key: 'index', width: 60, align: 'center' as const },
  { title: '编码', dataIndex: 'code', key: 'code', width: 100 },
  { title: '车牌号', dataIndex: 'plateNumber', key: 'plateNumber', width: 100 },
  { title: '品牌', dataIndex: 'brand', key: 'brand', width: 100 },
  { title: '权属类型', dataIndex: 'ownershipType', key: 'ownershipType', width: 100, align: 'center' as const },
  { title: '所有人', dataIndex: 'ownerName', key: 'ownerName', width: 100 },
  { title: '车辆状态', dataIndex: 'vehicleStatus', key: 'vehicleStatus', width: 100, align: 'center' as const },
  { title: '颜色', dataIndex: 'color', key: 'color', width: 80 },
  { title: '创建时间', dataIndex: 'createdTime', key: 'createdTime', width: 120 },
  { title: '操作', dataIndex: 'action', key: 'action', width: 150, align: 'center' as const, fixed: 'right' as const },
]

// 搜索表单
const searchForm = reactive({
  keyword: '',
  ownershipType: '' as number | string,
  vehicleStatus: '' as number | string,
})

// 表格数据
const loading = ref(false)
const tableData = ref<VehicleListItemDto[]>([])
const pagination = reactive({
  pageIndex: 1,
  pageSize: 20,
  total: 0,
})

const paginationConfig = computed(() => ({
  current: pagination.pageIndex,
  pageSize: pagination.pageSize,
  total: pagination.total,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50', '100'],
  showTotal: (t: number) => `共 ${t} 条`,
}))

// 统计数据
const statistics = ref<VehicleStatisticsDto>({
  totalCount: 0,
  statusGroups: [],
  ownershipGroups: [],
})

function getStatusCount(status: number): number {
  const group = statistics.value.statusGroups.find(g => g.status === status)
  return group?.count || 0
}

function getVehicleStatusColor(status: number): string {
  const colors: Record<number, string> = {
    1: 'default',
    2: 'success',
    3: 'warning',
    4: 'error',
  }
  return colors[status] || 'default'
}

function getVehicleStatusText(status: number): string {
  const texts: Record<number, string> = {
    1: '闲置',
    2: '使用中',
    3: '维修中',
    4: '报废',
  }
  return texts[status] || '未知'
}

function formatDate(dateStr: string): string {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN')
}

function handleTableChange(pag: any) {
  pagination.pageIndex = pag.current
  pagination.pageSize = pag.pageSize
  fetchVehicleList()
}

// 弹窗相关
const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const formRef = ref<FormInstance>()
const submitLoading = ref(false)
const currentVehicleId = ref<number | null>(null)

const formData = reactive({
  code: '',
  plateNumber: '',
  brand: '',
  frameNumber: '',
  ownershipType: 1,
  ownerId: undefined as number | undefined,
  ownerName: '',
  purchaseDate: '',
  purchasePrice: undefined as number | undefined,
  color: '',
  gpsDeviceNo: '',
  remark: '',
})

const formRules: Record<string, Rule[]> = {
  code: [{ required: true, message: '请输入车辆编码', trigger: 'blur' }],
  ownershipType: [{ required: true, message: '请选择权属类型', trigger: 'change' }],
}

// 获取车辆列表
async function fetchVehicleList() {
  loading.value = true
  try {
    const params: any = {
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize,
    }
    if (searchForm.keyword) params.keyword = searchForm.keyword
    if (searchForm.ownershipType !== '' && searchForm.ownershipType !== undefined) {
      params.ownershipType = searchForm.ownershipType
    }
    if (searchForm.vehicleStatus !== '' && searchForm.vehicleStatus !== undefined) {
      params.vehicleStatus = searchForm.vehicleStatus
    }
    const res = await getVehicleList(params)
    if (res) {
      tableData.value = res.items || []
      pagination.total = res.totalCount || 0
    }
  } finally {
    loading.value = false
  }
}

// 获取统计数据
async function fetchStatistics() {
  try {
    const res = await getVehicleStatistics()
    if (res) {
      statistics.value = res
    }
  } catch (error) {
    console.error('获取统计数据失败:', error)
  }
}

// 搜索
function handleSearch() {
  pagination.pageIndex = 1
  fetchVehicleList()
}

// 重置搜索
function handleReset() {
  searchForm.keyword = ''
  searchForm.ownershipType = ''
  searchForm.vehicleStatus = ''
  pagination.pageIndex = 1
  fetchVehicleList()
}

// 重置表单
function resetForm() {
  formData.code = ''
  formData.plateNumber = ''
  formData.brand = ''
  formData.frameNumber = ''
  formData.ownershipType = 1
  formData.ownerId = undefined
  formData.ownerName = ''
  formData.purchaseDate = ''
  formData.purchasePrice = undefined
  formData.color = ''
  formData.gpsDeviceNo = ''
  formData.remark = ''
}

// 新增
function handleAdd() {
  dialogType.value = 'add'
  currentVehicleId.value = null
  resetForm()
  dialogVisible.value = true
}

// 编辑
async function handleEdit(row: any) {
  dialogType.value = 'edit'
  currentVehicleId.value = row.id
  resetForm()
  dialogVisible.value = true

  try {
    const detail = await getVehicleDetail(row.id)
    if (detail) {
      formData.code = detail.code
      formData.plateNumber = detail.plateNumber || ''
      formData.brand = detail.brand || ''
      formData.frameNumber = detail.frameNumber || ''
      formData.ownershipType = detail.ownershipType
      formData.ownerId = detail.ownerId
      formData.ownerName = detail.ownerName || ''
      formData.purchaseDate = detail.purchaseDate || ''
      formData.purchasePrice = detail.purchasePrice
      formData.color = detail.color || ''
      formData.gpsDeviceNo = detail.gpsDeviceNo || ''
      formData.remark = detail.remark || ''
    }
  } catch (error) {
    console.error('获取车辆详情失败:', error)
    message.error('获取车辆详情失败')
  }
}

// 提交表单
async function handleSubmit() {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  submitLoading.value = true
  try {
    const data: any = {
      code: formData.code,
      plateNumber: formData.plateNumber || undefined,
      brand: formData.brand || undefined,
      frameNumber: formData.frameNumber || undefined,
      ownershipType: formData.ownershipType,
      purchaseDate: formData.purchaseDate || undefined,
      purchasePrice: formData.purchasePrice,
      color: formData.color || undefined,
      gpsDeviceNo: formData.gpsDeviceNo || undefined,
      remark: formData.remark || undefined,
    }
    
    if (formData.ownershipType === 2) {
      data.ownerName = formData.ownerName || undefined
    } else {
      data.ownerId = formData.ownerId
    }

    if (dialogType.value === 'add') {
      await createVehicle(data)
      message.success('新增成功')
    } else {
      await updateVehicle(currentVehicleId.value!, data)
      message.success('更新成功')
    }
    dialogVisible.value = false
    fetchVehicleList()
    fetchStatistics()
  } catch (error) {
    console.error('提交失败:', error)
    message.error('提交失败')
  } finally {
    submitLoading.value = false
  }
}

// 删除
async function handleDelete(row: any) {
  try {
    await deleteVehicle(row.id)
    message.success('删除成功')
    fetchVehicleList()
    fetchStatistics()
  } catch (error) {
    console.error('删除失败:', error)
    message.error('删除失败')
  }
}

onMounted(() => {
  fetchVehicleList()
  fetchStatistics()
})
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.stat-card {
  text-align: center;
}
</style>
