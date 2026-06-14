<template>
  <div class="page-container">
    <PageHeader title="楼栋管理" description="管理宿舍楼栋信息">
      <template #right>
        <a-button type="primary" @click="handleAdd">
          <template #icon><PlusOutlined /></template>新增楼栋
        </a-button>
      </template>
      <template #toolbar>
        <div style="display:flex; align-items:center; justify-content:space-between; width:100%;">
          <div style="display:flex; align-items:center; gap:8px;">
            <a-form-item label="关键词" style="margin-bottom:0">
              <a-input
                v-model:value="searchForm.keyword"
                placeholder="请输入编码/名称"
                allow-clear
                style="width: 280px"
                @keyup.enter="handleSearch"
              />
            </a-form-item>
            <a-form-item label="状态" style="margin-bottom:0">
              <a-select
                v-model:value="searchForm.status"
                placeholder="全部"
                allow-clear
                style="width: 120px"
                :options="[
                  { label: '启用', value: 1 },
                  { label: '停用', value: 0 },
                ]"
              />
            </a-form-item>
          </div>
          <div style="display:flex; align-items:center; gap:8px;">
            <a-button type="primary" @click="handleSearch">
              <template #icon><SearchOutlined /></template>查询
            </a-button>
            <a-button @click="handleReset">
              <template #icon><ReloadOutlined /></template>重置
            </a-button>
          </div>
        </div>
      </template>
    </PageHeader>

    <a-card :bordered="false">
      <a-table
        :columns="tableColumns"
        :data-source="tableData"
        :loading="loading"
        :pagination="paginationConfig"
        row-key="id"
        bordered
        :scroll="{ x: 1100 }"
        @change="handleTableChange"
      >
        <template #bodyCell="{ column, record, index }">
          <template v-if="column.dataIndex === 'index'">
            {{ (pagination.pageIndex - 1) * pagination.pageSize + index + 1 }}
          </template>
          <template v-if="column.dataIndex === 'name'">
            <a-tooltip :title="record.name">{{ record.name }}</a-tooltip>
          </template>
          <template v-if="column.dataIndex === 'address'">
            <a-tooltip :title="record.address">{{ record.address }}</a-tooltip>
          </template>
          <template v-if="column.dataIndex === 'status'">
            <a-tag :color="record.status === 1 ? 'success' : 'error'">
              {{ record.status === 1 ? '启用' : '停用' }}
            </a-tag>
          </template>
          <template v-if="column.dataIndex === 'action'">
            <a-button type="link" size="small" @click="handleEdit(record)">
              <EditOutlined />编辑
            </a-button>
            <a-button type="link" size="small" @click="handleManageRooms(record)">
              <HomeOutlined />管理房间
            </a-button>
            <a-button
              type="link"
              size="small"
              :class="record.status === 1 ? 'btn-disable' : 'btn-enable'"
              @click="handleToggleStatus(record)"
            >
              {{ record.status === 1 ? '停用' : '启用' }}
            </a-button>
            <a-popconfirm
              title="确定删除该楼栋吗？"
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
          <EmptyState description="暂无楼栋数据" />
        </template>
      </a-table>
    </a-card>

    <!-- 新增/编辑弹窗 -->
    <a-modal
      v-model:open="dialogVisible"
      :title="dialogType === 'add' ? '新增楼栋' : '编辑楼栋'"
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
            <a-form-item label="楼栋编码" name="code">
              <a-input
                v-model:value="formData.code"
                placeholder="请输入楼栋编码"
                :maxlength="50"
                :disabled="dialogType === 'edit'"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="楼栋名称" name="name">
              <a-input
                v-model:value="formData.name"
                placeholder="请输入楼栋名称"
                :maxlength="100"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="20">
          <a-col :span="12">
            <a-form-item label="总楼层" name="floorCount">
              <a-input-number
                v-model:value="formData.floorCount"
                :min="1"
                :max="100"
                style="width: 100%"
                placeholder="请输入总楼层"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="宿舍类型" name="dormitoryType">
              <a-select
                v-model:value="formData.dormitoryType"
                placeholder="请选择宿舍类型"
                :options="[
                  { label: '男生宿舍', value: 'male' },
                  { label: '女生宿舍', value: 'female' },
                  { label: '混合宿舍', value: 'mixed' },
                ]"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="20">
          <a-col :span="12">
            <a-form-item label="管理员">
              <a-input
                v-model:value="formData.managerName"
                placeholder="请输入管理员姓名"
                :maxlength="50"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="联系电话">
              <a-input
                v-model:value="formData.managerPhone"
                placeholder="请输入联系电话"
                :maxlength="30"
              />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="20">
          <a-col :span="24">
            <a-form-item label="地址">
              <a-input
                v-model:value="formData.address"
                placeholder="请输入地址"
                :maxlength="300"
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
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import type { FormInstance } from 'ant-design-vue'
import type { Rule } from 'ant-design-vue/es/form'
import { PlusOutlined, EditOutlined, DeleteOutlined, HomeOutlined, SearchOutlined, ReloadOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import EmptyState from '@/components/EmptyState.vue'
import {
  getBuildingList,
  getBuildingDetail,
  createBuilding,
  updateBuilding,
  deleteBuilding,
  updateBuildingStatus,
  checkBuildingCode,
  type BuildingListItemDto,
} from '@/api/dormitory'

const router = useRouter()

// 表格列配置
const tableColumns = [
  { title: '序号', dataIndex: 'index', key: 'index', width: 60, align: 'center' as const },
  { title: '编码', dataIndex: 'code', key: 'code', width: 100 },
  { title: '名称', dataIndex: 'name', key: 'name', width: 150, ellipsis: true },
  { title: '地址', dataIndex: 'address', key: 'address', width: 200, ellipsis: true },
  { title: '总楼层', dataIndex: 'floorCount', key: 'floorCount', width: 80, align: 'center' as const },
  { title: '房间数', dataIndex: 'roomCount', key: 'roomCount', width: 80, align: 'center' as const },
  { title: '床位数', dataIndex: 'bedCount', key: 'bedCount', width: 80, align: 'center' as const },
  { title: '已入住', dataIndex: 'occupiedBeds', key: 'occupiedBeds', width: 80, align: 'center' as const },
  { title: '状态', dataIndex: 'status', key: 'status', width: 80, align: 'center' as const },
  { title: '操作', dataIndex: 'action', key: 'action', width: 280, align: 'center' as const, fixed: 'right' as const },
]

// 搜索表单
const searchForm = reactive({
  keyword: '',
  status: '' as number | string,
})

// 表格数据
const loading = ref(false)
const tableData = ref<BuildingListItemDto[]>([])
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

function handleTableChange(pag: any) {
  pagination.pageIndex = pag.current
  pagination.pageSize = pag.pageSize
  fetchBuildingList()
}

// 弹窗相关
const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const formRef = ref<FormInstance>()
const submitLoading = ref(false)
const currentBuildingId = ref<number | null>(null)

const formData = reactive({
  code: '',
  name: '',
  address: '',
  floorCount: 1,
  dormitoryType: 'male' as 'male' | 'female' | 'mixed',
  managerName: '',
  managerPhone: '',
  remark: '',
})

// 编码唯一性校验
const validateCode = async (_rule: any, value: string) => {
  if (!value) {
    return Promise.resolve()
  }
  if (dialogType.value === 'edit' && currentBuildingId.value) {
    const exists = await checkBuildingCode(value, currentBuildingId.value)
    if (exists) {
      return Promise.reject(new Error('楼栋编码已存在'))
    }
  } else {
    const exists = await checkBuildingCode(value)
    if (exists) {
      return Promise.reject(new Error('楼栋编码已存在'))
    }
  }
  return Promise.resolve()
}

const formRules: Record<string, Rule[]> = {
  code: [
    { required: true, message: '请输入楼栋编码', trigger: 'blur' },
    { validator: validateCode, trigger: 'blur' },
  ],
  name: [{ required: true, message: '请输入楼栋名称', trigger: 'blur' }],
  floorCount: [{ required: true, message: '请输入总楼层', trigger: 'blur' }],
  dormitoryType: [{ required: true, message: '请选择宿舍类型', trigger: 'change' }],
}

// 获取楼栋列表
async function fetchBuildingList() {
  loading.value = true
  try {
    const params: any = {
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize,
    }
    if (searchForm.keyword) params.keyword = searchForm.keyword
    if (searchForm.status !== '' && searchForm.status !== undefined) params.status = searchForm.status
    const res = await getBuildingList(params)
    if (res) {
      tableData.value = res.items || []
      pagination.total = res.totalCount || 0
    }
  } finally {
    loading.value = false
  }
}

// 搜索
function handleSearch() {
  pagination.pageIndex = 1
  fetchBuildingList()
}

// 重置搜索
function handleReset() {
  searchForm.keyword = ''
  searchForm.status = ''
  pagination.pageIndex = 1
  fetchBuildingList()
}

// 重置表单
function resetForm() {
  formData.code = ''
  formData.name = ''
  formData.address = ''
  formData.floorCount = 1
  formData.dormitoryType = 'male'
  formData.managerName = ''
  formData.managerPhone = ''
  formData.remark = ''
}

// 新增
function handleAdd() {
  dialogType.value = 'add'
  currentBuildingId.value = null
  resetForm()
  dialogVisible.value = true
}

// 编辑
async function handleEdit(row: any) {
  dialogType.value = 'edit'
  currentBuildingId.value = row.id
  resetForm()
  dialogVisible.value = true

  try {
    const detail = await getBuildingDetail(row.id)
    if (detail) {
      formData.code = detail.code
      formData.name = detail.name
      formData.address = detail.address || ''
      formData.floorCount = detail.floorCount
      formData.dormitoryType = 'male' // 后端暂未返回该字段，使用默认值
      formData.managerName = detail.managerName || ''
      formData.managerPhone = detail.managerPhone || ''
      formData.remark = detail.remark || ''
    }
  } catch (error) {
    console.error('获取楼栋详情失败:', error)
    message.error('获取楼栋详情失败')
  }
}

// 管理房间
function handleManageRooms(record: any) {
  router.push({ name: 'RoomManage', params: { buildingId: record.id.toString() } })
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
    const data = {
      code: formData.code,
      name: formData.name,
      address: formData.address || undefined,
      floorCount: formData.floorCount,
      managerName: formData.managerName || undefined,
      managerPhone: formData.managerPhone || undefined,
      remark: formData.remark || undefined,
    }

    if (dialogType.value === 'add') {
      await createBuilding(data)
      message.success('新增成功')
    } else {
      await updateBuilding(currentBuildingId.value!, data)
      message.success('更新成功')
    }
    dialogVisible.value = false
    fetchBuildingList()
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
    await deleteBuilding(row.id)
    message.success('删除成功')
    fetchBuildingList()
  } catch (error) {
    console.error('删除失败:', error)
    message.error('删除失败')
  }
}

// 启用/停用
async function handleToggleStatus(row: any) {
  const newStatus = row.status === 1 ? 0 : 1
  const actionText = newStatus === 1 ? '启用' : '停用'
  try {
    await updateBuildingStatus(row.id, newStatus)
    message.success(`${actionText}成功`)
    fetchBuildingList()
  } catch (error) {
    console.error(`${actionText}失败:`, error)
    message.error(`${actionText}失败`)
  }
}

onMounted(() => {
  fetchBuildingList()
})
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;
</style>
