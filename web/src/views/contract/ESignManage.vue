<template>
  <div class="page-container">
    <PageHeader title="电子签管理" description="管理合同电子签署记录">
      <template #toolbar>
        <div style="display: flex; align-items: center; justify-content: flex-end; width: 100%; gap: 8px;">
          <a-input v-model:value="searchForm.keyword" size="small" placeholder="合同号" style="width: 200px" allowClear @keyup.enter="handleSearch" />
          <a-select v-model:value="searchForm.signStatus" size="small" placeholder="签署状态" style="width: 140px" allowClear :options="signStatusOptions" />
          <a-button type="primary" size="small" @click="handleSearch">查询</a-button>
          <a-button size="small" @click="handleReset">重置</a-button>
        </div>
      </template>
    </PageHeader>

    <a-alert
      type="info"
      message="电子签章功能即将上线，当前使用线下签署模式。签署完成后请手动上传签署文件。"
      show-icon
      style="margin-bottom: 16px"
    />

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
          <template v-if="column.dataIndex === 'signStatus'">
            <a-tag :color="signStatusColor(record.signStatus)">
              {{ signStatusText(record.signStatus) }}
            </a-tag>
          </template>
          <template v-if="column.dataIndex === 'signedTime'">
            {{ record.signedTime || '-' }}
          </template>
          <template v-if="column.dataIndex === 'action'">
            <a-button
              v-if="has(ContractPermissions.ESignManage) && record.signStatus === 0"
              type="link"
              size="small"
              @click="handleMarkSigned(record)"
            >
              标记已签
            </a-button>
            <a-button
              v-if="has(ContractPermissions.ESignManage) && record.signStatus === 0"
              type="link"
              size="small"
              danger
              @click="handleReject(record)"
            >
              拒签
            </a-button>
          </template>
        </template>
        <template #emptyText>
          <EmptyState description="暂无电子签记录" />
        </template>
      </a-table>
    </a-card>

    <!-- 标记已签弹窗 -->
    <a-modal
      v-model:open="signDialogVisible"
      title="标记已签"
      width="500px"
      :destroy-on-close="true"
      @cancel="signDialogVisible = false"
    >
      <a-form :label-col="{ style: { width: '100px' } }" style="padding: 10px 20px">
        <a-form-item label="签署时间">
          <a-date-picker
            v-model:value="signForm.signedTime"
            show-time
            format="YYYY-MM-DD HH:mm:ss"
            placeholder="请选择签署时间"
            style="width: 100%"
          />
        </a-form-item>
        <a-form-item label="签署文件">
          <a-upload
            :file-list="signForm.fileList"
            :before-upload="beforeUpload"
            @remove="handleRemoveFile"
          >
            <a-button>
              <UploadOutlined />选择文件
            </a-button>
          </a-upload>
        </a-form-item>
      </a-form>
      <template #footer>
        <a-button @click="signDialogVisible = false">取消</a-button>
        <a-button type="primary" :loading="submitLoading" @click="handleSubmitSign">确定</a-button>
      </template>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import type { UploadFile } from 'ant-design-vue'
import { UploadOutlined } from '@ant-design/icons-vue'
import dayjs, { type Dayjs } from 'dayjs'
import PageHeader from '@/components/PageHeader.vue'
import EmptyState from '@/components/EmptyState.vue'
import { usePermission, ContractPermissions } from '@/utils/permission'
import {
  getESignRecordList,
  completeESign,
  rejectESign,
  type ESignRecordDto,
} from '@/api/contract'

const { has } = usePermission()

const signStatusOptions = [
  { label: '待签', value: 0 },
  { label: '已签', value: 1 },
  { label: '已拒签', value: 2 },
]

function signStatusText(status: number) {
  return ['待签', '已签', '已拒签'][status] || '未知'
}

function signStatusColor(status: number) {
  return ['processing', 'success', 'error'][status] || 'default'
}

const tableColumns = [
  { title: '序号', dataIndex: 'index', key: 'index', width: 60, align: 'center' as const },
  { title: '合同号', dataIndex: 'contractNo', key: 'contractNo', width: 150 },
  { title: '签署人', dataIndex: 'signer', key: 'signer', width: 120 },
  { title: '签署角色', dataIndex: 'signerRole', key: 'signerRole', width: 100 },
  { title: '签署状态', dataIndex: 'signStatus', key: 'signStatus', width: 100, align: 'center' as const },
  { title: '签署时间', dataIndex: 'signedTime', key: 'signedTime', width: 180 },
  { title: '创建时间', dataIndex: 'createdTime', key: 'createdTime', width: 180 },
  { title: '操作', dataIndex: 'action', key: 'action', width: 160, align: 'center' as const, fixed: 'right' as const },
]

const searchForm = reactive({
  keyword: '',
  signStatus: undefined as number | undefined,
})

const loading = ref(false)
const tableData = ref<ESignRecordDto[]>([])
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

const signDialogVisible = ref(false)
const submitLoading = ref(false)
const currentRecord = ref<ESignRecordDto | null>(null)

const signForm = reactive({
  signedTime: null as Dayjs | null,
  fileList: [] as UploadFile[],
})

async function fetchList() {
  loading.value = true
  try {
    const params: any = {
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize,
    }
    if (searchForm.signStatus !== undefined) params.signStatus = searchForm.signStatus
    const res = await getESignRecordList(params) as any
    if (res) {
      tableData.value = res?.items || res || []
      pagination.total = res?.total || res?.length || 0
    }
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  pagination.pageIndex = 1
  fetchList()
}

function handleReset() {
  searchForm.keyword = ''
  searchForm.signStatus = undefined
  pagination.pageIndex = 1
  fetchList()
}

function handleMarkSigned(record: ESignRecordDto) {
  currentRecord.value = record
  signForm.signedTime = dayjs()
  signForm.fileList = []
  signDialogVisible.value = true
}

function beforeUpload(file: UploadFile) {
  signForm.fileList = [file]
  return false
}

function handleRemoveFile() {
  signForm.fileList = []
}

async function handleSubmitSign() {
  if (!currentRecord.value) return
  submitLoading.value = true
  try {
    await completeESign(currentRecord.value.id, {
      signedFilePath: signForm.fileList.length > 0 ? (signForm.fileList[0] as any).name : undefined,
    })
    message.success('标记签署成功')
    signDialogVisible.value = false
    fetchList()
  } finally {
    submitLoading.value = false
  }
}

async function handleReject(record: ESignRecordDto) {
  try {
    await rejectESign(record.id)
    message.success('已标记拒签')
    fetchList()
  } catch (error) {
    console.error('操作失败:', error)
  }
}

onMounted(() => {
  fetchList()
})
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;
</style>
