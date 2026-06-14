<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { Input, Tree, Tag, List, Avatar, Empty } from 'ant-design-vue'
import { SearchOutlined, UserOutlined, CloseOutlined } from '@ant-design/icons-vue'
import { getOrganizationTree, getUserList } from '@/api/system'

interface UserItem {
  id: number
  name: string
  account?: string
  departmentId?: number
  departmentName?: string
  status?: number
}

interface OrgTreeNode {
  id: number
  name: string
  children?: OrgTreeNode[]
}

const props = defineProps<{
  modelValue: number[]
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: number[]): void
}>()

// 组织树数据
const orgTreeData = ref<any[]>([])
const selectedOrgKeys = ref<number[]>([])
const orgUsers = ref<UserItem[]>([])
const loading = ref(false)

// 搜索
const searchKeyword = ref('')
const searchResults = ref<UserItem[]>([])
const isSearching = ref(false)

// 已选用户
const selectedUsers = ref<UserItem[]>([])

// 当前显示的用户列表
const displayUsers = computed(() => {
  if (isSearching.value && searchKeyword.value) {
    return searchResults.value
  }
  return orgUsers.value
})

// 获取组织树
async function fetchOrgTree() {
  try {
    const res = await getOrganizationTree() as any[]
    orgTreeData.value = res || []
  } catch (error) {
    console.error('获取组织树失败:', error)
  }
}

// 获取部门用户
async function fetchOrgUsers(orgId: number) {
  loading.value = true
  try {
    const res = await getUserList({
      departmentId: orgId,
      pageIndex: 1,
      pageSize: 100,
    }) as any
    orgUsers.value = res?.items || []
  } catch {
    orgUsers.value = []
  } finally {
    loading.value = false
  }
}

// 搜索用户
async function handleSearch() {
  if (!searchKeyword.value.trim()) {
    isSearching.value = false
    searchResults.value = []
    return
  }
  
  isSearching.value = true
  loading.value = true
  try {
    const res = await getUserList({
      keyword: searchKeyword.value.trim(),
      pageIndex: 1,
      pageSize: 50,
    }) as any
    searchResults.value = res?.items || []
  } catch {
    searchResults.value = []
  } finally {
    loading.value = false
  }
}

// 组织节点选择
function handleOrgSelect(keys: any, info: any) {
  if (info.selected && info.node) {
    const data = info.node.dataRef || info.node
    fetchOrgUsers(data.id)
  }
}

// 检查用户是否已选
function isSelected(userId: number): boolean {
  return selectedUsers.value.some(u => u.id === userId)
}

// 添加用户
function addUser(user: UserItem) {
  if (!isSelected(user.id)) {
    selectedUsers.value.push(user)
    emitUpdate()
  }
}

// 移除用户
function removeUser(userId: number) {
  selectedUsers.value = selectedUsers.value.filter(u => u.id !== userId)
  emitUpdate()
}

// 触发更新
function emitUpdate() {
  emit('update:modelValue', selectedUsers.value.map(u => u.id))
}

// 监听外部值变化，加载用户信息
watch(() => props.modelValue, async (newVal) => {
  if (newVal && newVal.length > 0) {
    // 如果已选用户列表为空或ID不匹配，需要加载用户信息
    const currentIds = selectedUsers.value.map(u => u.id)
    const hasChanges = newVal.length !== currentIds.length || 
      newVal.some(id => !currentIds.includes(id))
    
    if (hasChanges) {
      // 加载用户信息
      loading.value = true
      try {
        const res = await getUserList({
          ids: newVal,
          pageIndex: 1,
          pageSize: 100,
        }) as any
        selectedUsers.value = res?.items || []
      } catch {
        // 如果批量查询失败，保持现有数据
      } finally {
        loading.value = false
      }
    }
  } else {
    selectedUsers.value = []
  }
}, { immediate: true })

onMounted(() => {
  fetchOrgTree()
  // 默认选中第一个组织并加载用户
  if (orgTreeData.value.length > 0) {
    const firstOrg = orgTreeData.value[0]
    selectedOrgKeys.value = [firstOrg.id]
    fetchOrgUsers(firstOrg.id)
  }
})

// 监听组织树加载完成后默认选中
watch(orgTreeData, (newVal) => {
  if (newVal.length > 0 && selectedOrgKeys.value.length === 0) {
    const firstOrg = newVal[0]
    selectedOrgKeys.value = [firstOrg.id]
    fetchOrgUsers(firstOrg.id)
  }
})
</script>

<template>
  <div class="attendee-selector">
    <a-row :gutter="16">
      <!-- 左侧组织树 -->
      <a-col :span="10">
        <div class="panel">
          <div class="panel-title">组织架构</div>
          <a-tree
            :tree-data="orgTreeData"
            :field-names="{ title: 'name', key: 'id', children: 'children' }"
            v-model:selected-keys="selectedOrgKeys"
            :default-expand-all="true"
            @select="handleOrgSelect"
          />
        </div>
      </a-col>
      
      <!-- 中间用户列表 -->
      <a-col :span="14">
        <div class="panel">
          <div class="panel-title">人员列表</div>
          <a-input-search
            v-model:value="searchKeyword"
            placeholder="搜索人员"
            allow-clear
            @search="handleSearch"
            @change="(e: any) => { if (!e.target.value) isSearching = false }"
            style="margin-bottom: 12px"
          />
          
          <a-list
            :data-source="displayUsers"
            :loading="loading"
            size="small"
            class="user-list"
          >
            <template #renderItem="{ item }">
              <a-list-item
                :class="{ 'is-selected': isSelected(item.id) }"
                @click="addUser(item)"
              >
                <a-list-item-meta>
                  <template #avatar>
                    <a-avatar :size="32">
                      <template #icon><UserOutlined /></template>
                    </a-avatar>
                  </template>
                  <template #title>{{ item.name }}</template>
                  <template #description>{{ item.departmentName || '-' }}</template>
                </a-list-item-meta>
                <template #actions>
                  <a-button
                    v-if="isSelected(item.id)"
                    type="link"
                    size="small"
                    @click.stop="removeUser(item.id)"
                  >
                    已添加
                  </a-button>
                  <a-button
                    v-else
                    type="primary"
                    size="small"
                    ghost
                    @click.stop="addUser(item)"
                  >
                    添加
                  </a-button>
                </template>
              </a-list-item>
            </template>
          </a-list>
        </div>
      </a-col>
    </a-row>
    
    <!-- 已选人员 -->
    <div class="selected-panel">
      <div class="selected-title">
        已选人员 ({{ selectedUsers.length }})
      </div>
      <div class="selected-tags">
        <a-tag
          v-for="user in selectedUsers"
          :key="user.id"
          closable
          color="blue"
          @close="removeUser(user.id)"
        >
          {{ user.name }}
        </a-tag>
        <span v-if="selectedUsers.length === 0" class="empty-text">请选择人员</span>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.attendee-selector {
  .panel {
    border: 1px solid #e8e8e8;
    border-radius: 4px;
    padding: 12px;
    height: 320px;
    overflow: hidden;
    display: flex;
    flex-direction: column;

    .panel-title {
      font-weight: 500;
      margin-bottom: 12px;
      padding-bottom: 8px;
      border-bottom: 1px solid #f0f0f0;
    }
  }

  :deep(.ant-tree) {
    overflow-y: auto;
    flex: 1;
  }

  .user-list {
    flex: 1;
    overflow-y: auto;

    :deep(.ant-list-item) {
      cursor: pointer;
      padding: 8px 12px;
      border-radius: 4px;
      transition: background-color 0.2s;

      &:hover {
        background-color: #f5f5f5;
      }

      &.is-selected {
        background-color: #e6f7ff;
      }
    }

    :deep(.ant-list-item-meta) {
      align-items: center;
    }

    :deep(.ant-list-item-meta-title) {
      margin-bottom: 0;
      font-size: 14px;
    }

    :deep(.ant-list-item-meta-description) {
      font-size: 12px;
    }
  }

  .selected-panel {
    margin-top: 16px;
    padding-top: 16px;
    border-top: 1px solid #e8e8e8;

    .selected-title {
      font-weight: 500;
      margin-bottom: 12px;
    }

    .selected-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      min-height: 32px;

      .empty-text {
        color: #bfbfbf;
        font-size: 14px;
      }
    }
  }
}
</style>
