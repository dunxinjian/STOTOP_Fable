<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { ArrowLeftOutlined, SaveOutlined, SendOutlined } from '@ant-design/icons-vue'
import { createExpenseRequest, updateExpenseRequest, getExpenseRequest, submitExpenseRequest } from '@/api/oa'
import { useOrgContextStore } from '@/stores/orgContext'
import { useUserStore } from '@/stores/user'
import ExpenseRequestForm from '@/views/oa-mobile/approve/forms/MExpenseRequestForm.vue'
import MobileFormContainer from '@/components/MobileFormContainer.vue'

const route = useRoute()
const router = useRouter()
const orgStore = useOrgContextStore()
const userStore = useUserStore()
const formRef = ref<InstanceType<typeof ExpenseRequestForm>>()
const saving = ref(false)
const submitting = ref(false)
const editId = ref<number | null>(route.query.id ? Number(route.query.id) : null)

const formData = ref<any>({
  docNumber: '系统自动生成',
  organizationName: orgStore.currentOrgName,
  applicantName: userStore.userInfo?.realName || '',
})

onMounted(async () => {
  if (editId.value) {
    try {
      const res = await getExpenseRequest(editId.value) as any
      formData.value = res
    } catch { message.error('加载失败') }
  }
})

async function handleSave() {
  const error = formRef.value?.validate()
  if (error) { message.error(error); return }
  saving.value = true
  try {
    const data = { ...formRef.value!.getData(), organizationId: orgStore.currentOrgId }
    if (editId.value) {
      await updateExpenseRequest(editId.value, data)
      message.success('保存成功')
    } else {
      const res = await createExpenseRequest(data) as any
      editId.value = res?.id || res
      message.success('创建成功')
    }
  } catch { message.error('保存失败') }
  finally { saving.value = false }
}

async function handleSubmit() {
  const submitError = formRef.value?.validate()
  if (submitError) { message.error(submitError); return }
  if (!editId.value) {
    saving.value = true
    try {
      const data = { ...formRef.value!.getData(), organizationId: orgStore.currentOrgId }
      const res = await createExpenseRequest(data) as any
      editId.value = res?.id || res
    } catch { message.error('保存失败'); saving.value = false; return }
    finally { saving.value = false }
  }
  submitting.value = true
  try {
    await submitExpenseRequest(editId.value!)
    message.success('提交成功')
    router.push('/oa/myprocess')
  } catch { message.error('提交失败') }
  finally { submitting.value = false }
}
</script>

<template>
  <div class="page-container">
    <div class="page-header">
      <a-space>
        <a-button @click="router.back()"><template #icon><ArrowLeftOutlined /></template>返回</a-button>
        <h3>费用请款</h3>
      </a-space>
      <a-space>
        <a-button :loading="saving" @click="handleSave"><template #icon><SaveOutlined /></template>保存</a-button>
        <a-button type="primary" :loading="submitting" @click="handleSubmit"><template #icon><SendOutlined /></template>提交审批</a-button>
      </a-space>
    </div>
    <a-card>
      <MobileFormContainer>
        <ExpenseRequestForm ref="formRef" :data="formData" :readonly="false" :hideActions="true" @update:data="(v: any) => formData = v" />
      </MobileFormContainer>
    </a-card>
  </div>
</template>

<style scoped lang="scss">
.page-container { padding: 16px; }
.page-header {
  display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;
  h3 { margin: 0; font-size: 18px; font-weight: 600; }
}
</style>
