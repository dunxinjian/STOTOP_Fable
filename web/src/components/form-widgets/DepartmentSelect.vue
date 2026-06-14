<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { getOrganizationTree } from '@/api/system'
import type { OrgTreeNode } from '@/api/system'

const props = withDefaults(defineProps<{
  value?: number | string
  disabled?: boolean
  placeholder?: string
  rootOrgId?: number | string
  orgType?: string
}>(), {
  value: undefined,
  disabled: false,
  placeholder: '请选择部门',
  rootOrgId: '',
  orgType: '',
})

const emit = defineEmits<{
  'update:value': [val: number | string | undefined]
}>()

interface TreeNode {
  value: number
  title: string
  children?: TreeNode[]
}

const treeData = ref<TreeNode[]>([])
const fullTree = ref<TreeNode[]>([])
const loading = ref(false)

function transformTree(nodes: OrgTreeNode[]): TreeNode[] {
  if (!nodes) return []
  return nodes.map(node => ({
    value: node.id,
    title: node.name,
    orgType: (node as any).orgType ?? (node as any).type ?? '',
    children: node.children ? transformTree(node.children) : undefined,
  }))
}

/** 在树中递归查找指定 id 的节点 */
function findNode(nodes: TreeNode[], id: number | string): TreeNode | undefined {
  for (const node of nodes) {
    if (String(node.value) === String(id)) return node
    if (node.children) {
      const found = findNode(node.children, id)
      if (found) return found
    }
  }
  return undefined
}

/** 按 orgType 递归过滤树，保留匹配节点及其含匹配后代的祖先 */
function filterByType(nodes: TreeNode[], type: string): TreeNode[] {
  const result: TreeNode[] = []
  for (const node of nodes) {
    const filteredChildren = node.children ? filterByType(node.children, type) : []
    if ((node as any).orgType === type || filteredChildren.length > 0) {
      result.push({
        ...node,
        children: filteredChildren.length > 0 ? filteredChildren : undefined,
      })
    }
  }
  return result
}

/** 根据 rootOrgId / orgType 过滤树 */
function applyFilters(nodes: TreeNode[]): TreeNode[] {
  let filtered = nodes
  if (props.rootOrgId) {
    const rootNode = findNode(filtered, props.rootOrgId)
    filtered = rootNode ? [{ ...rootNode }] : []
  }
  if (props.orgType) {
    filtered = filterByType(filtered, props.orgType)
  }
  return filtered
}

async function loadTree() {
  loading.value = true
  try {
    const tree = await getOrganizationTree() as any
    const raw = Array.isArray(tree) ? tree : (tree?.children ? [tree] : [tree])
    fullTree.value = transformTree(raw)
    treeData.value = applyFilters(fullTree.value)
  } catch {
    treeData.value = []
    fullTree.value = []
  } finally {
    loading.value = false
  }
}

function handleChange(val: number | string | undefined) {
  emit('update:value', val)
}

// 当范围配置变化时重新过滤
watch(
  () => [props.rootOrgId, props.orgType],
  () => {
    if (fullTree.value.length > 0) {
      treeData.value = applyFilters(fullTree.value)
    } else {
      loadTree()
    }
  },
)

onMounted(() => {
  loadTree()
})
</script>

<template>
  <a-tree-select
    :value="value"
    :disabled="disabled"
    :placeholder="placeholder"
    :tree-data="treeData"
    :loading="loading"
    show-search
    tree-node-filter-prop="title"
    allow-clear
    style="width: 100%"
    :dropdown-style="{ maxHeight: '300px', overflow: 'auto' }"
    @change="handleChange"
  />
</template>
