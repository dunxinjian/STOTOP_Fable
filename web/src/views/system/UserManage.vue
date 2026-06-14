<template>
  <div class="page-container">
    <PageHeader title="用户管理" description="管理系统用户账号">
      <template #actions>
        <a-input
          v-model:value="searchForm.keyword"
          placeholder="请输入姓名/账号/手机号"
          allowClear
          style="width: 240px"
          @pressEnter="handleSearch"
        />
        <a-button type="primary" @click="handleSearch">
          <template #icon><SearchOutlined /></template>查询
        </a-button>
        <a-button @click="handleReset">
          <template #icon><ReloadOutlined /></template>重置
        </a-button>
        <a-divider type="vertical" style="height: 24px; margin: 0 8px;" />
        <a-button type="primary" class="btn-primary-brand" @click="handleAdd">
          <template #icon><PlusOutlined /></template>新增用户
        </a-button>
      </template>
    </PageHeader>

    <div class="table-card">
      <a-table
        :columns="columns"
        :dataSource="tableData"
        :loading="loading"
        rowKey="id"
        :bordered="false"
        :scroll="{ y: 'calc(100vh - 260px)' }"
        :pagination="{
          current: pagination.pageIndex,
          pageSize: pagination.pageSize,
          total: pagination.total,
          showSizeChanger: true,
          showTotal: (t: number) => `共 ${t} 条`,
          pageSizeOptions: ['10', '20', '50', '100'],
          onChange: handlePageChange,
          onShowSizeChange: (_c: number, s: number) => handleSizeChange(s),
        }"
      >
        <template #bodyCell="{ column, record, index }">
          <template v-if="column.dataIndex === 'index'">{{ index + 1 }}</template>
          <template v-if="column.dataIndex === 'dingTalkBindStatus'">
            <span v-if="record.dingTalkBindStatus === 1" class="status-badge badge-active">
              <span class="badge-dot"></span>已绑
            </span>
            <span v-else class="status-badge badge-inactive">
              <span class="badge-dot"></span>未绑
            </span>
          </template>
          <template v-if="column.dataIndex === 'status'">
            <span class="status-badge" :class="record.status === 1 ? 'badge-active' : 'badge-inactive'">
              <span class="badge-dot"></span>
              {{ record.status === 1 ? '启用' : '停用' }}
            </span>
          </template>
          <template v-if="column.dataIndex === 'action'">
            <div class="action-links">
              <a-typography-link @click="handleEdit(record)">
                <EditOutlined />编辑
              </a-typography-link>
              <a-typography-link @click="handleResetPassword(record)" class="link-warning">
                <KeyOutlined />重置密码
              </a-typography-link>
              <a-popconfirm
                title="确定删除该用户吗？"
                okText="确定"
                cancelText="取消"
                @confirm="handleDelete(record)"
              >
                <a-typography-link class="link-danger">
                  <DeleteOutlined />删除
                </a-typography-link>
              </a-popconfirm>
            </div>
          </template>
        </template>
        <template #emptyText>
          <EmptyState description="暂无用户数据" />
        </template>
      </a-table>
    </div>

    <!-- 新增/编辑弹窗 -->
    <a-modal
      v-model:open="dialogVisible"
      :title="dialogType === 'add' ? '新增用户' : '编辑用户'"
      :width="800"
      :destroyOnClose="true"
    >
      <a-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        layout="vertical"
        class="modal-form"
      >
        <div class="form-grid">
          <a-form-item label="姓名" name="name">
            <a-input v-model:value="formData.name" placeholder="请输入姓名" :maxlength="50" class="form-input" />
          </a-form-item>
          <a-form-item label="账号" name="account">
            <a-input
              v-model:value="formData.account"
              placeholder="请输入账号"
              :maxlength="50"
              :disabled="dialogType === 'edit'"
              class="form-input"
            />
          </a-form-item>
        </div>
        <a-form-item
          v-if="dialogType === 'add'"
          label="密码"
          name="password"
          :rules="[{ required: dialogType === 'add', message: '请输入密码', trigger: 'blur' }]"
        >
          <a-input-password
            v-model:value="formData.password"
            placeholder="请输入密码"
            :maxlength="50"
            class="form-input"
          />
        </a-form-item>
        <div class="form-grid">
          <a-form-item label="手机号" name="phone">
            <a-input v-model:value="formData.phone" placeholder="请输入手机号" :maxlength="20" class="form-input" />
          </a-form-item>
          <a-form-item label="邮箱" name="email">
            <a-input v-model:value="formData.email" placeholder="请输入邮箱" :maxlength="100" class="form-input" />
          </a-form-item>
        </div>
        <a-form-item label="角色">
          <a-select
            v-model:value="formData.roleIds"
            mode="multiple"
            placeholder="请选择角色"
            :options="roleOptions.map(r => ({ label: r.name, value: r.id }))"
            allowClear
            style="width: 100%"
          />
        </a-form-item>
        <a-form-item label="状态" name="status">
          <a-radio-group v-model:value="formData.status" class="status-radio-group">
            <a-radio :value="1">
              <span class="radio-label radio-active">启用</span>
            </a-radio>
            <a-radio :value="0">
              <span class="radio-label radio-inactive">停用</span>
            </a-radio>
          </a-radio-group>
        </a-form-item>
      </a-form>

      <!-- 组织任职区域 -->
      <div v-if="dialogType === 'edit'" class="org-section">
        <div class="org-section-header">
          <span class="org-section-title">组织任职</span>
          <a-button type="primary" class="btn-primary-brand" size="small" @click="handleAddOrgAssign">
            <PlusOutlined />添加任职
          </a-button>
        </div>
        <a-table
          :columns="editOrgColumns"
          :dataSource="userOrgList"
          :loading="orgAssignLoading"
          size="small"
          :bordered="false"
          :pagination="false"
          rowKey="id"
          class="inner-table"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.dataIndex === 'position'">{{ record.position || '-' }}</template>
            <template v-if="column.dataIndex === 'jobNumber'">{{ record.jobNumber || '-' }}</template>
            <template v-if="column.dataIndex === 'isPrimaryOrg'">
              <a-tag v-if="record.isPrimaryOrg === 1" class="status-tag tag-primary">是</a-tag>
              <span v-else style="color: #8E8EA0;">-</span>
            </template>
            <template v-if="column.dataIndex === 'action'">
              <a-typography-link @click="handleEditOrgAssign(record)">编辑</a-typography-link>
              <a-divider type="vertical" />
              <a-popconfirm title="确定移除该任职吗？" @confirm="handleRemoveOrgAssign(record)">
                <a-typography-link class="link-danger">移除</a-typography-link>
              </a-popconfirm>
            </template>
          </template>
        </a-table>
      </div>
      <div v-else class="org-section-placeholder">
        <span style="color: #8E8EA0; font-size: 13px;">保存用户基本信息后，可在编辑模式下管理组织任职</span>
      </div>

      <template #footer>
        <div class="modal-footer">
          <a-button @click="dialogVisible = false">取消</a-button>
          <a-button type="primary" :loading="submitLoading" @click="handleSubmit">确定</a-button>
        </div>
      </template>
    </a-modal>

    <!-- 重置密码弹窗 -->
    <a-modal
      v-model:open="resetPwdVisible"
      title="重置密码"
      :width="400"
      :destroyOnClose="true"
    >
      <a-form
        ref="resetPwdFormRef"
        :model="resetPwdForm"
        :rules="resetPwdRules"
        layout="vertical"
        class="modal-form"
      >
        <a-form-item label="新密码" name="newPassword">
          <a-input-password
            v-model:value="resetPwdForm.newPassword"
            placeholder="请输入新密码"
            :maxlength="50"
            class="form-input"
          />
        </a-form-item>
      </a-form>
      <template #footer>
        <div class="modal-footer">
          <a-button @click="resetPwdVisible = false">取消</a-button>
          <a-button type="primary" :loading="resetPwdLoading" @click="handleResetPwdSubmit">确定</a-button>
        </div>
      </template>
    </a-modal>

    <!-- 添加/编辑组织任职弹窗 -->
    <a-modal
      v-model:open="orgAssignFormVisible"
      :title="orgAssignFormType === 'add' ? '添加组织任职' : '编辑组织任职'"
      :width="480"
      :destroyOnClose="true"
    >
      <a-form
        ref="orgAssignFormRef"
        :model="orgAssignForm"
        :rules="orgAssignFormRules"
        layout="vertical"
        class="modal-form"
      >
        <a-form-item v-if="orgAssignFormType === 'add'" label="所在部门" name="orgId">
          <a-tree-select
            v-model:value="orgAssignForm.orgId"
            :tree-data="deptTreeData"
            :fieldNames="{ label: 'name', value: 'id', children: 'children' }"
            placeholder="请选择所在部门"
            allowClear
            :treeDefaultExpandAll="false"
            style="width: 100%"
            class="form-input"
          />
        </a-form-item>
        <div class="form-grid">
          <a-form-item label="职位">
            <a-input v-model:value="orgAssignForm.position" placeholder="请输入职位" :maxlength="50" class="form-input" />
          </a-form-item>
          <a-form-item label="工号">
            <a-input v-model:value="orgAssignForm.jobNumber" placeholder="请输入工号" :maxlength="30" class="form-input" />
          </a-form-item>
          <a-form-item label="入职日期">
            <a-date-picker v-model:value="orgAssignForm.entryDate" placeholder="请选择入职日期" valueFormat="YYYY-MM-DD" style="width: 100%" class="form-input" />
          </a-form-item>
        </div>
        <a-form-item label="直接上级">
          <a-select
            v-model:value="orgAssignForm.directSuperiorId"
            placeholder="请选择直接上级"
            allowClear
            showSearch
            :filterOption="false"
            @search="searchSuperiors"
            :loading="superiorSearchLoading"
            :options="superiorOptions.map(u => ({ label: u.name, value: u.id }))"
            style="width: 100%"
            class="form-input"
          />
        </a-form-item>
        <a-form-item label="主组织">
          <a-switch v-model:checked="isPrimaryOrgBool" />
        </a-form-item>
      </a-form>
      <template #footer>
        <div class="modal-footer">
          <a-button @click="orgAssignFormVisible = false">取消</a-button>
          <a-button type="primary" :loading="orgAssignSubmitLoading" @click="handleOrgAssignSubmit">确定</a-button>
        </div>
      </template>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import type { FormInstance } from 'ant-design-vue'
import { PlusOutlined, EditOutlined, DeleteOutlined, KeyOutlined, SearchOutlined, ReloadOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import EmptyState from '@/components/EmptyState.vue'
import {
  getUserList,
  createUser,
  updateUser,
  deleteUser,
  resetPassword,
  getOrganizationTree,
  getUserOrganizations,
  addUserToOrganization,
  updateUserOrganization,
  removeUserFromOrganization,
  getRoleList,
  type UserItem,
} from '@/api/system'
import type { UserOrganizationDto } from '@/types/organization'

const columns = [
  { title: '序号', dataIndex: 'index', key: 'index', width: 60, align: 'center' as const },
  { title: '姓名', dataIndex: 'name', key: 'name', width: 100 },
  { title: '账号', dataIndex: 'account', key: 'account', width: 160 },
  { title: '手机号', dataIndex: 'phone', key: 'phone', width: 120 },
  
  { title: '组织', dataIndex: 'orgName', key: 'orgName', width: 120 },
  { title: '钉钉', dataIndex: 'dingTalkBindStatus', key: 'dingTalkBindStatus', width: 100, align: 'center' as const },
  { title: '状态', dataIndex: 'status', key: 'status', width: 90, align: 'center' as const },
  { title: '操作', dataIndex: 'action', key: 'action', width: 220, align: 'center' as const, fixed: 'right' as const },
]

// 编辑弹窗内嵌组织任职表格列（简化版）
const editOrgColumns = [
  { title: '组织名称', dataIndex: 'orgName', key: 'orgName' },
  { title: '职位', dataIndex: 'position', key: 'position', width: 100 },
  { title: '工号', dataIndex: 'jobNumber', key: 'jobNumber', width: 90 },
  { title: '主组织', dataIndex: 'isPrimaryOrg', key: 'isPrimaryOrg', width: 70, align: 'center' as const },
  { title: '操作', dataIndex: 'action', key: 'action', width: 120, align: 'center' as const },
]

// 搜索表单
const searchForm = reactive({ keyword: '' })

// 表格数据
const loading = ref(false)
const tableData = ref<UserItem[]>([])
const pagination = reactive({ pageIndex: 1, pageSize: 20, total: 0 })

// 部门树数据
const deptTreeData = ref<any[]>([])

// 弹窗相关
const dialogVisible = ref(false)
const dialogType = ref<'add' | 'edit'>('add')
const formRef = ref<FormInstance>()
const submitLoading = ref(false)
const currentUserId = ref<number | null>(null)
const currentUserName = ref('')

const formData = reactive({
  name: '', account: '', password: '', phone: '', email: '',
  status: 1,
  roleIds: [] as number[],
})

// 角色选项
const roleOptions = ref<{ id: number; name: string }[]>([])

const formRules: Record<string, any[]> = {
  name: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  account: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  status: [{ required: true, message: '请选择状态', trigger: 'change' }],
}

// 重置密码相关
const resetPwdVisible = ref(false)
const resetPwdFormRef = ref<FormInstance>()
const resetPwdLoading = ref(false)
const resetPwdForm = reactive({ newPassword: '' })
const resetPwdRules: Record<string, any[]> = {
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' },
  ],
}

// 组织任职相关
const orgAssignLoading = ref(false)
const userOrgList = ref<UserOrganizationDto[]>([])

// 添加/编辑组织任职
const orgAssignFormVisible = ref(false)
const orgAssignFormType = ref<'add' | 'edit'>('add')
const orgAssignFormRef = ref<FormInstance>()
void orgAssignFormRef
const orgAssignSubmitLoading = ref(false)
const currentOrgAssignId = ref<number | null>(null)
const superiorSearchLoading = ref(false)
const superiorOptions = ref<any[]>([])

const orgAssignForm = reactive({
  orgId: undefined as number | undefined,
  position: '',
  jobNumber: '',
  entryDate: '',
  directSuperiorId: undefined as number | undefined,
  isPrimaryOrg: 0,
})

const isPrimaryOrgBool = computed({
  get: () => orgAssignForm.isPrimaryOrg === 1,
  set: (v: boolean) => { orgAssignForm.isPrimaryOrg = v ? 1 : 0 },
})

const orgAssignFormRules: Record<string, any[]> = {
  orgId: [{ required: true, message: '请选择组织', trigger: 'change' }],
}

async function fetchUserList() {
  loading.value = true
  try {
    const res = await getUserList({
      pageIndex: pagination.pageIndex, pageSize: pagination.pageSize, keyword: searchForm.keyword,
    }) as any
    if (res) { tableData.value = res?.items || []; pagination.total = res?.total || 0 }
  } finally { loading.value = false }
}

async function fetchDeptTree() {
  try {
    const res = await getOrganizationTree() as any[]
    if (res) { deptTreeData.value = res || [] }
  } catch (error) { console.error('获取部门树失败:', error) }
}

function handleSearch() { pagination.pageIndex = 1; fetchUserList() }
function handleReset() { searchForm.keyword = ''; pagination.pageIndex = 1; fetchUserList() }
function handleSizeChange(val: number) { pagination.pageSize = val; fetchUserList() }
function handlePageChange(val: number) { pagination.pageIndex = val; fetchUserList() }

function handleAdd() {
  dialogType.value = 'add'; currentUserId.value = null; resetForm(); dialogVisible.value = true
}

async function handleEdit(row: any) {
  dialogType.value = 'edit'; currentUserId.value = row.id; currentUserName.value = row.name
  formData.name = row.name; formData.account = row.account
  formData.phone = row.phone || ''; formData.email = row.email || ''
  formData.status = row.status; formData.password = ''
  formData.roleIds = row.roles?.map((r: any) => r.id) || []

  // 加载用户组织任职列表
  orgAssignLoading.value = true
  userOrgList.value = []
  dialogVisible.value = true
  try {
    const res = await getUserOrganizations(row.id) as any
    userOrgList.value = res || []
  } catch { userOrgList.value = [] }
  finally { orgAssignLoading.value = false }
}

function resetForm() {
  formData.name = ''; formData.account = ''; formData.password = ''
  formData.phone = ''; formData.email = ''; formData.status = 1
  formData.roleIds = []
  userOrgList.value = []
}

async function handleSubmit() {
  if (!formRef.value) return
  try { await formRef.value.validate() } catch { return }

  submitLoading.value = true
  try {
    const data: any = {
      name: formData.name, account: formData.account, phone: formData.phone,
      email: formData.email,
      status: formData.status,
      roleIds: formData.roleIds,
      ...(dialogType.value === 'add' && { password: formData.password }),
    }
    if (dialogType.value === 'add') {
      await createUser(data); message.success('新增成功')
    } else {
      await updateUser(currentUserId.value!, data); message.success('更新成功')
    }
    dialogVisible.value = false; fetchUserList()
  } finally { submitLoading.value = false }
}

async function handleDelete(row: any) {
  try { await deleteUser(row.id); message.success('删除成功'); fetchUserList() }
  catch (error) { console.error('删除失败:', error) }
}

function handleResetPassword(row: any) {
  currentUserId.value = row.id; resetPwdForm.newPassword = ''; resetPwdVisible.value = true
}

async function handleResetPwdSubmit() {
  if (!resetPwdFormRef.value) return
  try { await resetPwdFormRef.value.validate() } catch { return }
  resetPwdLoading.value = true
  try {
    await resetPassword(currentUserId.value!, { newPassword: resetPwdForm.newPassword })
    message.success('密码重置成功'); resetPwdVisible.value = false
  } finally { resetPwdLoading.value = false }
}

// ==================== 组织任职管理 ====================

async function loadUserOrgList() {
  if (!currentUserId.value) return
  orgAssignLoading.value = true
  try {
    const res = await getUserOrganizations(currentUserId.value) as any
    userOrgList.value = res || []
  } catch { userOrgList.value = [] }
  finally { orgAssignLoading.value = false }
}

function handleAddOrgAssign() {
  orgAssignFormType.value = 'add'; currentOrgAssignId.value = null
  orgAssignForm.orgId = undefined; orgAssignForm.position = ''
  orgAssignForm.jobNumber = ''; orgAssignForm.entryDate = ''; orgAssignForm.directSuperiorId = undefined
  orgAssignForm.isPrimaryOrg = 0; superiorOptions.value = []
  orgAssignFormVisible.value = true
}

function handleEditOrgAssign(row: any) {
  orgAssignFormType.value = 'edit'; currentOrgAssignId.value = row.id
  orgAssignForm.orgId = row.orgId
  orgAssignForm.position = row.position || ''; orgAssignForm.jobNumber = row.jobNumber || ''
  orgAssignForm.entryDate = row.entryDate || ''; orgAssignForm.directSuperiorId = row.directSuperiorId
  orgAssignForm.isPrimaryOrg = row.isPrimaryOrg
  if (row.directSuperiorId && row.directSuperiorName) {
    superiorOptions.value = [{ id: row.directSuperiorId, name: row.directSuperiorName }]
  } else { superiorOptions.value = [] }
  orgAssignFormVisible.value = true
}

async function handleOrgAssignSubmit() {
  if (orgAssignFormType.value === 'add' && !orgAssignForm.orgId) {
    message.warning('请选择组织'); return
  }
  orgAssignSubmitLoading.value = true
  try {
    if (orgAssignFormType.value === 'add') {
      await addUserToOrganization({
        userId: currentUserId.value!, orgId: orgAssignForm.orgId!,
        directSuperiorId: orgAssignForm.directSuperiorId ?? undefined,
        isPrimaryOrg: orgAssignForm.isPrimaryOrg,
        position: orgAssignForm.position || undefined,
        jobNumber: orgAssignForm.jobNumber || undefined,
        entryDate: orgAssignForm.entryDate || undefined,
      })
      message.success('添加成功')
    } else {
      await updateUserOrganization(currentOrgAssignId.value!, {
        directSuperiorId: orgAssignForm.directSuperiorId ?? null,
        isPrimaryOrg: orgAssignForm.isPrimaryOrg,
        position: orgAssignForm.position || null,
        jobNumber: orgAssignForm.jobNumber || null,
        entryDate: orgAssignForm.entryDate || null,
      })
      message.success('更新成功')
    }
    orgAssignFormVisible.value = false
    await loadUserOrgList()
  } finally { orgAssignSubmitLoading.value = false }
}

async function handleRemoveOrgAssign(row: any) {
  try {
    await removeUserFromOrganization(row.id); message.success('移除成功')
    await loadUserOrgList()
  } catch (error) { console.error('移除失败:', error) }
}

async function searchSuperiors(query: string) {
  if (!query) { superiorOptions.value = []; return }
  superiorSearchLoading.value = true
  try {
    const res = await getUserList({ keyword: query, pageIndex: 1, pageSize: 20 }) as any
    superiorOptions.value = (res?.items || []).filter((u: any) => u.id !== currentUserId.value)
  } catch { superiorOptions.value = [] }
  finally { superiorSearchLoading.value = false }
}

async function fetchRoleOptions() {
  try {
    const res = await getRoleList() as any[]
    if (res) { roleOptions.value = (res || []).map((r: any) => ({ id: r.id, name: r.name })) }
  } catch (error) { console.error('获取角色列表失败:', error) }
}

onMounted(() => { fetchUserList(); fetchDeptTree(); fetchRoleOptions() })
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.page-container {
  display: flex;
  flex-direction: column;
  height: 100%;
}

// ===== 主操作按钮 — 品牌色 =====
.btn-primary-brand {
  background: $color-primary !important;
  border-color: $color-primary !important;
  border-radius: 8px !important;
  color: #fff !important;
  font-weight: 500;
  transition: $transition-smooth;

  &:hover {
    background: $color-primary-hover !important;
    border-color: $color-primary-hover !important;
    box-shadow: 0 4px 12px rgba(22, 119, 255, 0.3);
  }
}

// ===== 表格卡片容器 =====
.table-card {
  flex: 1;
  min-height: 0;
  background: $bg-card;
  border-radius: 0;
  box-shadow: $shadow-card;
  overflow: hidden;
  transition: $transition-smooth;

  &:hover {
    box-shadow: $shadow-card-hover;
  }

  // 表头背景
  :deep(.ant-table-thead > tr > th) {
    background: $bg-page !important;
    border-bottom: 1px solid $border-color !important;
    font-weight: 600;
    color: $text-primary;
    font-size: $font-size-sm;
    letter-spacing: 0.02em;
  }

  // 移除表格外边框
  :deep(.ant-table) {
    border: none !important;
  }

  :deep(.ant-table-container) {
    border: none !important;
    border-right: none !important;

    &::before, &::after {
      display: none;
    }
  }

  // 行 hover
  :deep(.ant-table-tbody > tr:hover > td) {
    background: rgba(22, 119, 255, 0.04) !important;
  }

  // 行分割线
  :deep(.ant-table-tbody > tr > td) {
    border-bottom: 1px solid $border-color-lighter !important;
  }

  // 分页区分割线
  :deep(.ant-pagination) {
    padding: 12px 16px;
    margin: 0 !important;
    border-top: 1px solid $border-color;
  }

  :deep(.ant-table-wrapper) {
    padding: 0;
  }
}

// ===== 操作链接 =====
.action-links {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;

  :deep(.ant-typography) {
    font-size: $font-size-sm;
    color: $color-primary;
    transition: $transition-fast;
    white-space: nowrap;

    &:hover {
      color: $color-accent;
    }
  }

  :deep(.link-success.ant-typography) {
    color: $color-success !important;
  }

  :deep(.link-warning.ant-typography) {
    color: $color-warning !important;
  }

  :deep(.link-danger.ant-typography) {
    color: $color-danger !important;
  }
}

// ===== 状态徽章 =====
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 3px 10px;
  border-radius: 20px;
  font-size: $font-size-sm;
  font-weight: 500;

  .badge-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  &.badge-active {
    background: rgba(82, 196, 26, 0.1);
    color: $color-success;

    .badge-dot {
      background: $color-success;
    }
  }

  &.badge-inactive {
    background: rgba(255, 77, 79, 0.08);
    color: $color-danger;

    .badge-dot {
      background: $color-danger;
    }
  }
}

// ===== 状态 Tag =====
.status-tag {
  border-radius: 20px;
  font-size: $font-size-sm;
  font-weight: 500;
  border: none;

  &.tag-success {
    background: rgba(82, 196, 26, 0.1);
    color: $color-success;
  }

  &.tag-primary {
    background: $color-primary-light;
    color: $color-primary;
  }

  &.tag-default {
    background: rgba(142, 142, 160, 0.1);
    color: $text-secondary;
  }
}

// ===== 弹窗表单 =====
.modal-form {
  padding: 8px 4px;

  // vertical layout 表单项间距
  :deep(.ant-form-item) {
    margin-bottom: 18px;
  }

  // 标签样式
  :deep(.ant-form-item-label > label) {
    font-weight: 500;
    color: $text-primary;
    font-size: $font-size-base;
  }
}

// 表单双列网格
.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0 16px;
}

// 输入框高度 40px
.form-input {
  :deep(.ant-input),
  :deep(.ant-input-affix-wrapper),
  :deep(.ant-select-selector),
  :deep(.ant-picker) {
    height: 40px !important;
    border-radius: 8px !important;
    font-size: $font-size-base;
  }

  :deep(.ant-input-password .ant-input) {
    height: auto !important;
  }
}

// 状态单选按钮组
.status-radio-group {
  :deep(.ant-radio-wrapper) {
    .radio-label {
      padding: 3px 10px;
      border-radius: 16px;
      font-size: $font-size-sm;
      font-weight: 500;

      &.radio-active {
        background: rgba(82, 196, 26, 0.1);
        color: $color-success;
      }

      &.radio-inactive {
        background: rgba(255, 77, 79, 0.08);
        color: $color-danger;
      }
    }
  }
}

// 弹窗底部按钮组
.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

// ===== 组织任职弹窗内部 =====
.org-section {
  margin-top: 4px;
  padding-top: 16px;
  border-top: 1px solid $border-color;
}

.org-section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.org-section-title {
  font-size: $font-size-base;
  font-weight: 600;
  color: $text-primary;
}

.org-section-placeholder {
  margin-top: 4px;
  padding: 16px 0 0;
  border-top: 1px solid $border-color;
  text-align: center;
}

.org-assign-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;

  .org-assign-user {
    font-size: $font-size-base;
    font-weight: 500;
    color: $text-primary;
  }
}

// 内嵌表格样式
.inner-table {
  :deep(.ant-table-thead > tr > th) {
    background: $bg-page !important;
    font-weight: 600;
    color: $text-primary;
    font-size: $font-size-sm;
    border-bottom: 1px solid $border-color !important;
  }

  :deep(.ant-table-tbody > tr > td) {
    border-bottom: 1px solid $border-color-lighter !important;
  }

  :deep(.ant-table-container) {
    border: none !important;
    &::before, &::after { display: none; }
  }
}

.section-title-sm {
  margin: 16px 0 8px;
  font-size: $font-size-base;
  font-weight: 500;
  color: $text-primary;
}

.position-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}
</style>
