<script setup lang="ts">
import { ref, computed } from 'vue'
import { message } from 'ant-design-vue'
import { UploadOutlined } from '@ant-design/icons-vue'
import { uploadAttachment } from '@/api/oa'
import type { UploadFile, UploadChangeParam } from 'ant-design-vue'

const props = withDefaults(defineProps<{
  value?: UploadFile[]
  disabled?: boolean
  maxCount?: number
}>(), {
  value: () => [],
  disabled: false,
  maxCount: 5,
})

const emit = defineEmits<{
  'update:value': [val: UploadFile[]]
}>()

const fileList = computed({
  get: () => props.value || [],
  set: (val) => emit('update:value', val),
})

function handleChange(info: UploadChangeParam) {
  const list = [...info.fileList]
  // 更新状态
  emit('update:value', list)

  if (info.file.status === 'done') {
    message.success(`${info.file.name} 上传成功`)
  } else if (info.file.status === 'error') {
    message.error(`${info.file.name} 上传失败`)
  }
}

function handleRemove(file: UploadFile) {
  const newList = (props.value || []).filter(f => f.uid !== file.uid)
  emit('update:value', newList)
  return true
}

// 使用 ant-design-vue 原生 action 上传方式
// 如果后端需要特定参数，可通过 customRequest 实现
const uploadAction = '/api/oa/attachment/upload'
</script>

<template>
  <a-upload
    :file-list="fileList"
    :disabled="disabled"
    :max-count="maxCount"
    :action="uploadAction"
    :multiple="true"
    @change="handleChange"
    @remove="handleRemove"
  >
    <a-button :disabled="disabled || (fileList?.length ?? 0) >= maxCount">
      <upload-outlined />
      点击上传
    </a-button>
    <template #itemRender="{ file, actions }">
      <div class="upload-item">
        <span :class="['file-name', { 'file-error': file.status === 'error' }]">
          {{ file.name }}
        </span>
        <a-button
          v-if="!disabled"
          type="link"
          size="small"
          danger
          @click="actions.remove"
        >
          删除
        </a-button>
      </div>
    </template>
  </a-upload>
</template>

<style scoped>
.upload-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 0;
}

.file-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.file-error {
  color: #ff4d4f;
}
</style>
