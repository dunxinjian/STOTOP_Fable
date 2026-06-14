<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import {
  CellGroup as VanCellGroup,
  Cell as VanCell,
  Icon as VanIcon,
  Button as VanButton,
  Field as VanField,
} from 'vant'
import 'vant/es/cell-group/style'
import 'vant/es/cell/style'
import 'vant/es/icon/style'
import 'vant/es/button/style'
import 'vant/es/field/style'

const props = defineProps<{
  data: any
  readonly?: boolean
  hideActions?: boolean
}>()

const emit = defineEmits<{
  'update:data': [value: any]
}>()

const formData = reactive({
  applicationRefId: null as number | null,
  originalAmount: 0,
  reimbursedTotal: 0,
  returnedTotal: 0,
  difference: 0,
  differenceDirection: '',
  remark: '',
})

// Sync props.data -> formData
watch(() => props.data, (newData) => {
  if (newData) {
    formData.applicationRefId = newData.applicationRefId || null
    formData.originalAmount = newData.originalAmount || 0
    formData.reimbursedTotal = newData.reimbursedTotal || 0
    formData.returnedTotal = newData.returnedTotal || 0
    formData.difference = newData.difference || newData.differenceAmount || 0
    formData.differenceDirection = newData.differenceDirection || ''
    formData.remark = newData.remark || ''
  }
}, { immediate: true })

function formatAmount(val: any): string {
  const num = Number(val)
  if (isNaN(num) || num === 0) return '0.00'
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatAmountInt(val: any): string {
  const num = Number(val)
  if (isNaN(num)) return '0'
  return Number.isInteger(num) ? num.toString() : num.toFixed(2)
}

const differenceDisplay = computed(() => {
  const dir = formData.differenceDirection
  if (dir) return dir
  const diff = Number(formData.difference)
  if (diff > 0) return '补付'
  if (diff < 0) return '退回'
  return '无差额'
})

// Emit data changes to parent
watch(formData, () => {
  if (!props.readonly) {
    emit('update:data', {
      ...props.data,
      applicationRefId: formData.applicationRefId,
      remark: formData.remark,
    })
  }
}, { deep: true })

defineExpose({
  getData: () => ({
    applicationRefId: formData.applicationRefId,
    originalAmount: formData.originalAmount,
    reimbursedTotal: formData.reimbursedTotal,
    returnedTotal: formData.returnedTotal,
    difference: formData.difference,
    differenceDirection: formData.differenceDirection,
    remark: formData.remark,
  }),
  validate: () => {
    if (!formData.applicationRefId) return '请关联备用金申请单'
    return null
  }
})
</script>

<template>
  <div class="petty-cash-writeoff-form">
    <!-- 关联备用金 -->
    <div class="section-card">
      <div class="link-row">
        <span class="link-label">关联备用金</span>
        <VanIcon name="add-o" size="22" color="#999" />
      </div>
      <div v-if="data?.pettyCashNo" class="link-detail">
        备用金编号：{{ data.pettyCashNo }}
      </div>
    </div>

    <!-- 金额概览卡片 -->
    <div class="section-card">
      <div class="total-amount-card">
        <div class="total-label">原始申请金额（元）</div>
        <div class="total-value">{{ formatAmountInt(formData.originalAmount) }}</div>
      </div>
    </div>

    <!-- 冲销明细 -->
    <div class="section-card">
      <div class="field-block">
        <div class="field-label">报销总额</div>
        <div class="field-value amount-value">¥{{ formatAmount(formData.reimbursedTotal) }}</div>
      </div>

      <div class="field-block">
        <div class="field-label">还款总额</div>
        <div class="field-value amount-value">¥{{ formatAmount(formData.returnedTotal) }}</div>
      </div>

      <div class="field-block">
        <div class="field-label">差额</div>
        <div class="field-value amount-value" :style="{ color: Number(formData.difference) >= 0 ? '#07c160' : '#ee0a24' }">
          ¥{{ formatAmount(formData.difference) }}
        </div>
      </div>

      <div class="field-block no-border">
        <div class="field-label">差额方向</div>
        <div class="field-value">{{ differenceDisplay }}</div>
      </div>
    </div>

    <!-- 备注（仅此字段可编辑） -->
    <div class="section-card">
      <div class="field-block no-border">
        <div class="field-label">备注</div>
        <template v-if="!props.readonly">
          <VanField
            v-model="formData.remark"
            type="textarea"
            placeholder="请输入"
            :autosize="{ minHeight: 40 }"
            maxlength="8000"
            show-word-limit
            class="inline-field"
            :border="false"
          />
        </template>
        <template v-else>
          <div class="field-value remark-value">{{ formData.remark || '' }}</div>
          <div class="char-count">{{ (formData.remark || '').length }}/8000</div>
        </template>
      </div>
    </div>

    <!-- 附件（只读时显示） -->
    <div class="section-card" v-if="data?.attachments?.length">
      <div class="field-block no-border">
        <div class="field-label">附件</div>
        <VanCell
          v-for="(file, idx) in data.attachments"
          :key="idx"
          :title="file.fileName"
          is-link
          @click="window.open(file.url, '_blank')"
          style="padding: 8px 0;"
        />
      </div>
    </div>

    <!-- 底部操作栏 -->
    <div v-if="!props.readonly && !props.hideActions" class="bottom-bar">
      <div class="draft-btn">
        <VanIcon name="notes-o" size="22" color="#666" />
        <span class="draft-text">存草稿</span>
      </div>
      <VanButton type="primary" round class="submit-btn">提交</VanButton>
    </div>

    <!-- 底部安全区占位 -->
    <div v-if="!props.readonly && !props.hideActions" class="bottom-placeholder"></div>
  </div>
</template>

<style scoped>
.petty-cash-writeoff-form {
  background: #f5f6f7;
  min-height: 100%;
}

/* 通用卡片区块 */
.section-card {
  background: #fff;
  border-radius: 12px;
  margin: 8px 12px;
  padding: 16px;
  overflow: hidden;
}

/* 关联行 */
.link-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.link-label {
  font-size: 16px;
  font-weight: 500;
  color: #333;
}
.link-detail {
  font-size: 13px;
  color: #666;
  margin-top: 8px;
}

/* 金额概览卡片 */
.total-amount-card {
  background: linear-gradient(135deg, #4d8cf7 0%, #6fa3fb 100%);
  border-radius: 10px;
  padding: 16px 20px;
}
.total-label {
  font-size: 13px;
  color: rgba(255, 255, 255, 0.85);
  margin-bottom: 4px;
}
.total-value {
  font-size: 36px;
  font-weight: 600;
  color: #fff;
  line-height: 1.2;
}

/* 字段区块 */
.field-block {
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
}
.field-block.no-border {
  border-bottom: none;
}
.field-label {
  font-size: 14px;
  color: #999;
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 6px;
}
.field-value {
  font-size: 16px;
  color: #333;
  line-height: 1.5;
}
.amount-value {
  font-size: 20px;
  font-weight: 500;
}
.char-count {
  text-align: right;
  font-size: 12px;
  color: #ccc;
  margin-top: 4px;
}
.remark-value {
  min-height: 40px;
  color: #999;
}
.remark-value:empty::before {
  content: '请输入';
  color: #ccc;
}

/* 内联输入字段 */
.inline-field {
  padding: 0 !important;
}
.inline-field :deep(.van-field__control) {
  font-size: 16px;
  color: #333;
}

/* 底部操作栏 */
.bottom-bar {
  position: sticky;
  bottom: 0;
  display: flex;
  align-items: center;
  padding: 8px 16px;
  padding-bottom: calc(8px + env(safe-area-inset-bottom));
  background: #fff;
  border-top: 1px solid #f0f0f0;
  z-index: 100;
}
.draft-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-right: 16px;
}
.draft-text {
  font-size: 10px;
  color: #666;
  margin-top: 2px;
}
.submit-btn {
  flex: 1;
  height: 44px;
  font-size: 16px;
  font-weight: 500;
}
.bottom-placeholder {
  height: 0px;
}
</style>
