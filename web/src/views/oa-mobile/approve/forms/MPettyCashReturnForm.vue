<script setup lang="ts">
import { reactive, watch } from 'vue'
import {
  CellGroup as VanCellGroup,
  Cell as VanCell,
  Field as VanField,
} from 'vant'
import 'vant/es/cell-group/style'
import 'vant/es/cell/style'
import 'vant/es/field/style'

const props = defineProps<{
  data: any
  readonly?: boolean
}>()

const emit = defineEmits<{
  'update:data': [value: any]
}>()

const formData = reactive({
  applicationRefId: 0,
  returnAmount: 0,
  returnMethod: '',
  returnNote: '',
})

// Sync props.data -> formData
watch(() => props.data, (newData) => {
  if (newData) {
    formData.applicationRefId = newData.applicationRefId ?? 0
    formData.returnAmount = newData.returnAmount ?? newData.amount ?? 0
    formData.returnMethod = newData.returnMethod ?? ''
    formData.returnNote = newData.returnNote ?? newData.remark ?? newData.reason ?? ''
  }
}, { immediate: true })

// Emit data changes to parent
watch(formData, () => {
  if (!props.readonly) {
    emit('update:data', {
      ...props.data,
      applicationRefId: formData.applicationRefId,
      returnAmount: formData.returnAmount,
      returnMethod: formData.returnMethod,
      returnNote: formData.returnNote,
    })
  }
}, { deep: true })

function formatAmount(val: any): string {
  const num = Number(val)
  if (isNaN(num)) return '-'
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function openUrl(url: string) { window.open(url, '_blank') }

defineExpose({
  getData: () => ({ ...formData }),
  validate: () => {
    if (!formData.returnAmount || formData.returnAmount <= 0) return '还款金额必须大于0'
    if (!formData.returnMethod) return '请输入还款方式'
    return null
  }
})
</script>

<template>
  <VanCellGroup inset title="备用金还款">
    <!-- 只读模式 -->
    <template v-if="props.readonly">
      <VanCell title="关联申请单ID" :value="formData.applicationRefId || '-'" />
      <VanCell title="还款金额" class="amount-highlight">
        <template #value>
          <span class="amount">¥{{ formatAmount(formData.returnAmount) }}</span>
        </template>
      </VanCell>
      <VanCell title="还款方式" :value="formData.returnMethod || '-'" />
      <VanCell title="还款备注" :value="formData.returnNote || '-'" />
    </template>

    <!-- 编辑模式 -->
    <template v-else>
      <VanField
        v-model="formData.applicationRefId"
        label="关联申请单ID"
        type="digit"
        placeholder="请输入关联申请单ID"
        required
        :border="true"
      />
      <VanField
        v-model="formData.returnAmount"
        label="还款金额"
        type="digit"
        placeholder="请输入还款金额"
        required
        :border="true"
      />
      <VanField
        v-model="formData.returnMethod"
        label="还款方式"
        placeholder="请输入还款方式"
        required
        :border="true"
      />
      <VanField
        v-model="formData.returnNote"
        label="还款备注"
        type="textarea"
        placeholder="请输入还款备注"
        :autosize="{ minHeight: 40 }"
        :border="true"
      />
    </template>
  </VanCellGroup>

  <!-- 附件 -->
  <VanCellGroup inset title="附件" v-if="data?.attachments?.length">
    <VanCell
      v-for="(file, idx) in data.attachments"
      :key="idx"
      :title="file.fileName"
      is-link
      @click="openUrl(file.url)"
    />
  </VanCellGroup>
</template>

<style scoped>
.amount-highlight .amount {
  font-size: 20px;
  font-weight: bold;
  color: var(--color-danger);
}
</style>
