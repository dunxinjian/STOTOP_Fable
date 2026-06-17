<script setup lang="ts">
import { ref } from 'vue'
import { ActionBar as VanActionBar, ActionBarButton as VanActionBarButton, Popup as VanPopup, Field as VanField, Button as VanButton } from 'vant'
import 'vant/es/action-bar/style'
import 'vant/es/action-bar-button/style'
import 'vant/es/popup/style'
import 'vant/es/field/style'
import 'vant/es/button/style'

const props = defineProps<{
  loading?: boolean
}>()

const emit = defineEmits<{
  (e: 'approve', comment: string): void
  (e: 'reject', comment: string): void
}>()

const showPopup = ref(false)
const isApprove = ref(true)
const comment = ref('')
const submitting = ref(false)

function showApprovePopup() {
  isApprove.value = true
  comment.value = ''
  showPopup.value = true
}

function showRejectPopup() {
  isApprove.value = false
  comment.value = ''
  showPopup.value = true
}

async function handleSubmit() {
  submitting.value = true
  try {
    if (isApprove.value) {
      emit('approve', comment.value)
    } else {
      emit('reject', comment.value)
    }
  } finally {
    submitting.value = false
    showPopup.value = false
  }
}
</script>

<template>
  <VanActionBar>
    <VanActionBarButton type="danger" text="拒绝" :loading="loading" @click="showRejectPopup" />
    <VanActionBarButton type="success" text="同意" :loading="loading" @click="showApprovePopup" />
  </VanActionBar>

  <VanPopup v-model:show="showPopup" position="bottom" round :style="{ minHeight: '30vh' }">
    <div class="comment-popup">
      <h3 class="popup-title">{{ isApprove ? '同意审批' : '拒绝审批' }}</h3>
      <VanField
        v-model="comment"
        type="textarea"
        :placeholder="isApprove ? '请输入审批意见（可选）' : '请输入拒绝原因'"
        rows="3"
        autosize
        show-word-limit
        maxlength="500"
      />
      <div class="popup-actions">
        <VanButton block type="default" @click="showPopup = false">取消</VanButton>
        <VanButton
          block
          :type="isApprove ? 'success' : 'danger'"
          :loading="submitting"
          @click="handleSubmit"
        >
          确认提交
        </VanButton>
      </div>
    </div>
  </VanPopup>
</template>

<style scoped>
.comment-popup {
  padding: 16px;
}
.popup-title {
  font-size: var(--font-lg);
  font-weight: 600;
  text-align: center;
  margin-bottom: var(--space-lg16);
  color: var(--text-1);
}
.popup-actions {
  display: flex;
  gap: 12px;
  margin-top: 16px;
}
</style>
