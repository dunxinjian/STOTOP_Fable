<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import type { TableColumnsType } from 'ant-design-vue'
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import { getExpenseTypeList, getExpenseAccountMappingList, createExpenseAccountMapping, updateExpenseAccountMapping, deleteExpenseAccountMapping } from '@/api/oa'

const loading = ref(false)
const expenseTypes = ref<any[]>([])
const mappings = ref<any[]>([])
const selectedTypeId = ref<number | null>(null)
const modalVisible = ref(false)

const mappingForm = reactive({
  id: 0,
  expenseTypeId: 0,
  accountCode: '',
  accountName: '',
  isDefault: false,
})

const mappingColumns: TableColumnsType = [
  { title: '科目编码', dataIndex: 'accountCode', key: 'accountCode', width: 150 },
  { title: '科目名称', dataIndex: 'accountName', key: 'accountName' },
  { title: '默认', dataIndex: 'isDefault', key: 'isDefault', width: 80, align: 'center' },
  { title: '操作', key: 'action', width: 120, align: 'center' },
]

async function loadExpenseTypes() {
  try { expenseTypes.value = (await getExpenseTypeList() as any) || [] }
  catch {}
}

async function loadMappings() {
  if (!selectedTypeId.value) { mappings.value = []; return }
  loading.value = true
  try { mappings.value = (await getExpenseAccountMappingList({ expenseTypeId: selectedTypeId.value }) as any) || [] }
  catch {} finally { loading.value = false }
}

function handleSelectType(typeId: number) {
  selectedTypeId.value = typeId
  loadMappings()
}

function handleAdd() {
  if (!selectedTypeId.value) { message.warning('请先选择费用类型'); return }
  Object.assign(mappingForm, { id: 0, expenseTypeId: selectedTypeId.value, accountCode: '', accountName: '', isDefault: false })
  modalVisible.value = true
}

async function handleOk() {
  if (!mappingForm.accountCode || !mappingForm.accountName) { message.warning('请填写科目信息'); return }
  try {
    if (mappingForm.id) { await updateExpenseAccountMapping(mappingForm.id, mappingForm); message.success('更新成功') }
    else { await createExpenseAccountMapping(mappingForm); message.success('创建成功') }
    modalVisible.value = false
    loadMappings()
  } catch { message.error('操作失败') }
}

async function handleDelete(record: any) {
  Modal.confirm({
    title: '确认删除', content: '确定要删除此映射关系吗？',
    async onOk() {
      try { await deleteExpenseAccountMapping(record.id); message.success('已删除'); loadMappings() }
      catch { message.error('删除失败') }
    }
  })
}

async function handleSetDefault(record: any) {
  try {
    await updateExpenseAccountMapping(record.id, { ...record, isDefault: true })
    message.success('已设为默认')
    loadMappings()
  } catch { message.error('操作失败') }
}

onMounted(() => loadExpenseTypes())
</script>

<template>
  <div class="page-container">
    <PageHeader title="费用类型-科目映射">
      <template #right>
      </template>
    </PageHeader>

    <a-row :gutter="16">
      <!-- 左侧：费用类型列表 -->
      <a-col :span="8">
        <a-card title="费用类型" size="small">
          <a-list size="small" :data-source="expenseTypes" :loading="!expenseTypes.length">
            <template #renderItem="{ item }">
              <a-list-item
                :class="{ 'type-active': selectedTypeId === item.id }"
                style="cursor: pointer; padding: 8px 12px;"
                @click="handleSelectType(item.id)"
              >
                <span>{{ item.name }}</span>
                <template #extra><a-tag size="small">{{ item.code }}</a-tag></template>
              </a-list-item>
            </template>
          </a-list>
        </a-card>
      </a-col>

      <!-- 右侧：关联科目 -->
      <a-col :span="16">
        <a-card size="small">
          <template #title>
            <span>关联科目</span>
            <span v-if="selectedTypeId" style="color: #999; font-size: 13px; margin-left: 8px;">
              （{{ expenseTypes.find(t => t.id === selectedTypeId)?.name }}）
            </span>
          </template>
          <template #extra>
            <a-button type="primary" size="small" @click="handleAdd"><template #icon><PlusOutlined /></template>新增映射</a-button>
          </template>
          <a-table :columns="mappingColumns" :data-source="mappings" :loading="loading" :pagination="false" size="small" row-key="id">
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'isDefault'">
                <a-tag v-if="record.isDefault" color="green">默认</a-tag>
                <a-button v-else type="link" size="small" @click="handleSetDefault(record)">设为默认</a-button>
              </template>
              <template v-if="column.key === 'action'">
                <a-button type="link" danger size="small" @click="handleDelete(record)"><template #icon><DeleteOutlined /></template>删除</a-button>
              </template>
            </template>
          </a-table>
          <a-empty v-if="!selectedTypeId" description="请先选择左侧费用类型" style="padding: 40px 0;" />
        </a-card>
      </a-col>
    </a-row>

    <a-modal v-model:open="modalVisible" title="新增科目映射" @ok="handleOk" :width="440">
      <a-form layout="vertical" style="margin-top: 16px;">
        <a-form-item label="科目编码" required><a-input v-model:value="mappingForm.accountCode" placeholder="请输入科目编码" /></a-form-item>
        <a-form-item label="科目名称" required><a-input v-model:value="mappingForm.accountName" placeholder="请输入科目名称" /></a-form-item>
        <a-form-item><a-checkbox v-model:checked="mappingForm.isDefault">设为默认科目</a-checkbox></a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<style scoped lang="scss">
.page-container { padding: 16px; }
.type-active {
  background-color: var(--color-primary-light) !important;
  border-left: 3px solid var(--color-primary);
}
</style>
