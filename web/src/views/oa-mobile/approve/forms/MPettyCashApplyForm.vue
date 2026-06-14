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
  applyAmount: 0,
  applyReason: '',
  expectedReturnDate: '',
  paymentMethod: '',
  payeeName: '',
  payeeAccount: '',
  payeeBank: '',
  remark: '',
})

// Sync props.data -> formData
watch(() => props.data, (newData) => {
  if (newData) {
    formData.applyAmount = newData.applyAmount ?? newData.amount ?? 0
    formData.applyReason = newData.applyReason ?? newData.reason ?? ''
    formData.expectedReturnDate = newData.expectedReturnDate ?? newData.expectedUsagePeriod ?? ''
    formData.paymentMethod = newData.paymentMethod ?? ''
    formData.payeeName = newData.payeeName ?? ''
    formData.payeeAccount = newData.payeeAccount ?? ''
    formData.payeeBank = newData.payeeBank ?? newData.payeeBankName ?? ''
    formData.remark = newData.remark ?? newData.purpose ?? ''
  }
}, { immediate: true })

// Emit data changes to parent
watch(formData, () => {
  if (!props.readonly) {
    emit('update:data', {
      ...props.data,
      applyAmount: formData.applyAmount,
      applyReason: formData.applyReason,
      expectedReturnDate: formData.expectedReturnDate,
      paymentMethod: formData.paymentMethod,
      payeeName: formData.payeeName,
      payeeAccount: formData.payeeAccount,
      payeeBank: formData.payeeBank,
      remark: formData.remark,
    })
  }
}, { deep: true })

function formatAmount(val: any): string {
  const num = Number(val)
  if (isNaN(num)) return '-'
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(val: any): string {
  if (!val) return '-'
  const d = new Date(val)
  if (isNaN(d.getTime())) return String(val)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

function openUrl(url: string) { window.open(url, '_blank') }

defineExpose({
  getData: () => ({ ...formData }),
  validate: () => {
    if (!formData.applyAmount || formData.applyAmount <= 0) return '申请金额必须大于0'
    if (!formData.applyReason) return '请输入申请事由'
    return null
  }
})
</script>

<template>
  <VanCellGroup inset title="备用金申请">
    <!-- 只读模式 -->
    <template v-if="props.readonly">
      <VanCell title="申请事由" :value="formData.applyReason || '-'" />
      <VanCell title="申请金额" class="amount-highlight">
        <template #value>
          <span class="amount">¥{{ formatAmount(formData.applyAmount) }}</span>
        </template>
      </VanCell>
      <VanCell title="预计还款日期" :value="formatDate(formData.expectedReturnDate)" />
      <VanCell title="付款方式" :value="formData.paymentMethod || '-'" />
      <VanCell title="收款人" :value="formData.payeeName || '-'" />
      <VanCell title="收款账号" :value="formData.payeeAccount || '-'" />
      <VanCell title="开户行" :value="formData.payeeBank || '-'" />
      <VanCell title="备注" :value="formData.remark || '-'" />
    </template>

    <!-- 编辑模式 -->
    <template v-else>
      <VanField
        v-model="formData.applyReason"
        label="申请事由"
        placeholder="请输入申请事由"
        required
        :border="true"
      />
      <VanField
        v-model="formData.applyAmount"
        label="申请金额"
        type="digit"
        placeholder="请输入申请金额"
        required
        :border="true"
      />
      <VanField
        v-model="formData.expectedReturnDate"
        label="预计还款日期"
        placeholder="如：2025-12-31"
        :border="true"
      />
      <VanField
        v-model="formData.paymentMethod"
        label="付款方式"
        placeholder="请输入付款方式"
        required
        :border="true"
      />
      <VanField
        v-model="formData.payeeName"
        label="收款人名称"
        placeholder="请输入收款人名称"
        required
        :border="true"
      />
      <VanField
        v-model="formData.payeeAccount"
        label="收款账号"
        placeholder="请输入收款账号"
        :border="true"
      />
      <VanField
        v-model="formData.payeeBank"
        label="开户行"
        placeholder="请输入开户行"
        :border="true"
      />
      <VanField
        v-model="formData.remark"
        label="备注"
        type="textarea"
        placeholder="请输入备注"
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
  color: #ee0a24;
}
</style>
