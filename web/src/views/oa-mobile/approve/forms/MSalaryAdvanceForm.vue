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
  advanceAmount: 0,
  advanceMonth: '',
  applyReason: '',
  paymentMethod: '',
  payeeName: '',
  payeeAccount: '',
  payeeBank: '',
  remark: '',
})

// Sync props.data -> formData
watch(() => props.data, (newData) => {
  if (newData) {
    formData.advanceAmount = newData.advanceAmount ?? newData.amount ?? 0
    formData.advanceMonth = newData.advanceMonth ?? newData.salaryMonth ?? ''
    formData.applyReason = newData.applyReason ?? newData.reason ?? ''
    formData.paymentMethod = newData.paymentMethod ?? newData.repaymentMethod ?? ''
    formData.payeeName = newData.payeeName ?? ''
    formData.payeeAccount = newData.payeeAccount ?? ''
    formData.payeeBank = newData.payeeBank ?? newData.payeeBankName ?? ''
    formData.remark = newData.remark ?? ''
  }
}, { immediate: true })

// Emit data changes to parent
watch(formData, () => {
  if (!props.readonly) {
    emit('update:data', {
      ...props.data,
      advanceAmount: formData.advanceAmount,
      advanceMonth: formData.advanceMonth,
      applyReason: formData.applyReason,
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

function openUrl(url: string) { window.open(url, '_blank') }

defineExpose({
  getData: () => ({ ...formData }),
  validate: () => {
    if (!formData.advanceAmount || formData.advanceAmount <= 0) return '预支金额必须大于0'
    if (!formData.advanceMonth) return '请输入预支月份'
    if (!formData.applyReason) return '请输入申请事由'
    return null
  }
})
</script>

<template>
  <VanCellGroup inset title="预支工资">
    <!-- 只读模式 -->
    <template v-if="props.readonly">
      <VanCell title="申请事由" :value="formData.applyReason || '-'" />
      <VanCell title="预支金额" class="amount-highlight">
        <template #value>
          <span class="amount">¥{{ formatAmount(formData.advanceAmount) }}</span>
        </template>
      </VanCell>
      <VanCell title="预支月份" :value="formData.advanceMonth || '-'" />
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
        v-model="formData.advanceAmount"
        label="预支金额"
        type="digit"
        placeholder="请输入预支金额"
        required
        :border="true"
      />
      <VanField
        v-model="formData.advanceMonth"
        label="预支月份"
        placeholder="如：2025-06"
        required
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
