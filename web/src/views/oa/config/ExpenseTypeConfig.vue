<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import type { TableColumnsType } from 'ant-design-vue'
import { PlusOutlined, EditOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import { getExpenseTypeList, createExpenseType, updateExpenseType, toggleExpenseType } from '@/api/oa'

const loading = ref(false)
const dataSource = ref<any[]>([])
const modalVisible = ref(false)
const isEdit = ref(false)

const formData = reactive({
  id: 0,
  code: '',
  name: '',
  scope: '通用',
  organizationId: undefined as number | undefined,
  sortOrder: 0,
  isEnabled: true,
})

const columns: TableColumnsType = [
  { title: '编码', dataIndex: 'code', key: 'code', width: 120 },
  { title: '名称', dataIndex: 'name', key: 'name', width: 180 },
  { title: '适用场景', dataIndex: 'scope', key: 'scope', width: 120 },
  { title: '组织', dataIndex: 'organizationName', key: 'organizationName', width: 120 },
  { title: '排序', dataIndex: 'sortOrder', key: 'sortOrder', width: 80, align: 'center' },
  { title: '状态', dataIndex: 'isEnabled', key: 'isEnabled', width: 100, align: 'center' },
  { title: '操作', key: 'action', width: 150, align: 'center' },
]

async function loadData() {
  loading.value = true
  try { dataSource.value = (await getExpenseTypeList() as any) || [] }
  catch {} finally { loading.value = false }
}

function handleAdd() {
  isEdit.value = false
  Object.assign(formData, { id: 0, code: '', name: '', scope: '通用', organizationId: undefined, sortOrder: 0, isEnabled: true })
  modalVisible.value = true
}

function handleEdit(record: any) {
  isEdit.value = true
  Object.assign(formData, record)
  modalVisible.value = true
}

async function handleOk() {
  if (!formData.code || !formData.name) { message.warning('请填写编码和名称'); return }
  try {
    if (isEdit.value) { await updateExpenseType(formData.id, formData); message.success('更新成功') }
    else { await createExpenseType(formData); message.success('创建成功') }
    modalVisible.value = false
    loadData()
  } catch { message.error('操作失败') }
}

async function handleToggle(record: any) {
  try { await toggleExpenseType(record.id); message.success(record.isEnabled ? '已禁用' : '已启用'); loadData() }
  catch { message.error('操作失败') }
}

onMounted(() => loadData())
</script>

<template>
  <div class="page-container">
    <PageHeader title="费用类型配置">
      <template #right>
        <a-space>
          <a-button type="primary" @click="handleAdd"><template #icon><PlusOutlined /></template>新增</a-button>
        </a-space>
      </template>
    </PageHeader>

    <a-table :columns="columns" :data-source="dataSource" :loading="loading" row-key="id" :pagination="{ pageSize: 20 }">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'isEnabled'">
          <a-tag :color="record.isEnabled ? 'green' : 'default'">{{ record.isEnabled ? '启用' : '禁用' }}</a-tag>
        </template>
        <template v-if="column.key === 'action'">
          <a-space>
            <a-button type="link" size="small" @click="handleEdit(record)"><template #icon><EditOutlined /></template>编辑</a-button>
            <a-button type="link" size="small" :danger="record.isEnabled" @click="handleToggle(record)">
              {{ record.isEnabled ? '禁用' : '启用' }}
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-modal v-model:open="modalVisible" :title="isEdit ? '编辑费用类型' : '新增费用类型'" @ok="handleOk" :width="480">
      <a-form layout="vertical" style="margin-top: 16px;">
        <a-form-item label="编码" required><a-input v-model:value="formData.code" placeholder="费用类型编码" /></a-form-item>
        <a-form-item label="名称" required><a-input v-model:value="formData.name" placeholder="费用类型名称" /></a-form-item>
        <a-form-item label="适用场景">
          <a-select v-model:value="formData.scope">
            <a-select-option value="通用">通用</a-select-option>
            <a-select-option value="费用报销">费用报销</a-select-option>
            <a-select-option value="对外付款">对外付款</a-select-option>
            <a-select-option value="备用金">备用金</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="排序"><a-input-number v-model:value="formData.sortOrder" :min="0" style="width: 100%" /></a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<style scoped lang="scss">
.page-container { padding: 16px; }
</style>
