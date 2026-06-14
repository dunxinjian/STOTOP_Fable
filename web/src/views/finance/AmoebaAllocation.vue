<template>
  <div class="page-container">
    <PageHeader title="分摊配置">
      <template #right>
        <a-button type="primary" @click="handleAdd">
          <template #icon><PlusOutlined /></template>新增
        </a-button>
      </template>
    </PageHeader>

    <a-card :bordered="false">
      <a-table
        :columns="columns"
        :data-source="tableData"
        :loading="loading"
        :pagination="false"
        row-key="id"
        bordered
        size="small"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.dataIndex === 'unitName'">
            {{ record.unitName || record.fUnitName || '-' }}
          </template>
          <template v-if="column.dataIndex === 'allocationType'">
            <a-tag :color="record.allocationType === 'fixed' ? 'blue' : 'orange'">
              {{ record.allocationType === 'fixed' ? '固定比例' : '动态计算' }}
            </a-tag>
          </template>
          <template v-if="column.dataIndex === 'outboundRatio'">
            {{ record.allocationType === 'fixed' ? (record.outboundRatio ?? 0) + '%' : '-' }}
          </template>
          <template v-if="column.dataIndex === 'inboundRatio'">
            {{ record.allocationType === 'fixed' ? (record.inboundRatio ?? 0) + '%' : '-' }}
          </template>
          <template v-if="column.dataIndex === 'action'">
            <a-button type="link" size="small" @click="handleEdit(record)">编辑</a-button>
            <a-button type="link" size="small" danger @click="handleDelete(record)">删除</a-button>
          </template>
        </template>
        <template #emptyText>
          <a-empty description="暂无分摊配置" />
        </template>
      </a-table>
    </a-card>

    <!-- 新增/编辑弹窗 -->
    <a-modal
      v-model:open="dialogVisible"
      :title="dialogType === 'add' ? '新增分摊配置' : '编辑分摊配置'"
      width="520px"
      :destroy-on-close="true"
      @cancel="dialogVisible = false"
    >
      <a-form :model="formData" :label-col="{ style: { width: '90px' } }" style="padding: 10px 20px">
        <a-form-item label="经营单元">
          <a-tree-select
            v-model:value="formData.unitId"
            :tree-data="unitTreeOptions"
            placeholder="请选择经营单元"
            style="width: 100%"
            tree-default-expand-all
            allow-clear
          />
        </a-form-item>
        <a-form-item label="品牌编码">
          <a-input v-model:value="formData.brandCode" placeholder="请输入品牌编码" />
        </a-form-item>
        <a-form-item label="分摊方式">
          <a-select v-model:value="formData.allocationType">
            <a-select-option value="fixed">固定比例</a-select-option>
            <a-select-option value="dynamic">动态计算</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item v-if="formData.allocationType === 'fixed'" label="出港比例">
          <a-input-number v-model:value="formData.outboundRatio" :min="0" :max="100" :precision="1" addon-after="%" style="width: 150px" />
        </a-form-item>
        <a-form-item v-if="formData.allocationType === 'fixed'" label="进港比例">
          <a-input-number v-model:value="formData.inboundRatio" :min="0" :max="100" :precision="1" addon-after="%" style="width: 150px" />
        </a-form-item>
      </a-form>
      <template #footer>
        <a-button @click="dialogVisible = false">取消</a-button>
        <a-button type="primary" :loading="submitLoading" @click="handleSubmit">确定</a-button>
      </template>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { PlusOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import {
  getAmoebaAllocations,
  saveAmoebaAllocation,
  deleteAmoebaAllocation,
  getAmoebaTree,
} from '@/api/finance'

// ==================== 表格 ====================
const loading = ref(false)
const tableData = ref<any[]>([])

const columns = [
  { title: '经营单元', dataIndex: 'unitName', width: 160 },
  { title: '品牌编码', dataIndex: 'brandCode', width: 120 },
  { title: '分摊方式', dataIndex: 'allocationType', width: 120, align: 'center' as const },
  { title: '出港比例', dataIndex: 'outboundRatio', width: 100, align: 'center' as const },
  { title: '进港比例', dataIndex: 'inboundRatio', width: 100, align: 'center' as const },
  { title: '操作', dataIndex: 'action', width: 140, align: 'center' as const },
]

async function fetchData() {
  loading.value = true
  try {
    const res = await getAmoebaAllocations()
    tableData.value = Array.isArray(res) ? res : (res as any)?.items || []
  } catch {
    tableData.value = []
  } finally {
    loading.value = false
  }
}

// ==================== 经营单元树选项 ====================
const unitTreeOptions = ref<any[]>([])

function transformTreeSelect(nodes: any[]): any[] {
  return nodes.map(n => ({
    title: n.name || n.fName,
    value: n.id,
    key: n.id,
    children: n.children?.length ? transformTreeSelect(n.children) : undefined,
  }))
}

async function loadUnitTree() {
  try {
    const res = await getAmoebaTree()
    if (res && Array.isArray(res)) {
      unitTreeOptions.value = transformTreeSelect(res)
    }
  } catch {
    unitTreeOptions.value = []
  }
}

// ==================== 弹窗 ====================
const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const submitLoading = ref(false)
const currentId = ref<number | null>(null)

const formData = reactive({
  unitId: null as number | null,
  brandCode: '',
  allocationType: 'fixed' as string,
  outboundRatio: 50,
  inboundRatio: 50,
})

function resetForm() {
  formData.unitId = null
  formData.brandCode = ''
  formData.allocationType = 'fixed'
  formData.outboundRatio = 50
  formData.inboundRatio = 50
}

function handleAdd() {
  dialogType.value = 'add'
  currentId.value = null
  resetForm()
  dialogVisible.value = true
}

function handleEdit(row: any) {
  dialogType.value = 'edit'
  currentId.value = row.id
  formData.unitId = row.unitId
  formData.brandCode = row.brandCode || ''
  formData.allocationType = row.allocationType || 'fixed'
  formData.outboundRatio = row.outboundRatio ?? 50
  formData.inboundRatio = row.inboundRatio ?? 50
  dialogVisible.value = true
}

async function handleSubmit() {
  if (!formData.unitId) { message.warning('请选择经营单元'); return }
  if (!formData.brandCode) { message.warning('请输入品牌编码'); return }
  submitLoading.value = true
  try {
    const data: any = {
      ...formData,
      id: dialogType.value === 'edit' ? currentId.value : undefined,
    }
    await saveAmoebaAllocation(data)
    message.success(dialogType.value === 'add' ? '新增成功' : '更新成功')
    dialogVisible.value = false
    fetchData()
  } catch (e: any) {
    message.error(e?.message || '操作失败')
  } finally {
    submitLoading.value = false
  }
}

function handleDelete(row: any) {
  Modal.confirm({
    title: '确认删除',
    content: '确定要删除此分摊配置吗？',
    okType: 'danger',
    async onOk() {
      try {
        await deleteAmoebaAllocation(row.id)
        message.success('删除成功')
        fetchData()
      } catch (e: any) {
        message.error(e?.message || '删除失败')
      }
    },
  })
}

// ==================== 生命周期 ====================
onMounted(() => {
  fetchData()
  loadUnitTree()
})
</script>
