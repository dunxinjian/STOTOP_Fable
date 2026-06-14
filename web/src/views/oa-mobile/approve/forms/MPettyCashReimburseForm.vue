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
import { genTempId } from '@/utils/tempId'

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
  totalAmount: 0,
  reason: '',
  remark: '',
  details: [] as Array<{
    expenseType: string
    summary: string
    amount: number
    occurDate: string
    expenseAccountCode: string
  }>
})

// Sync props.data -> formData
watch(() => props.data, (newData) => {
  if (newData) {
    formData.applicationRefId = newData.applicationRefId || null
    formData.reason = newData.reason || ''
    formData.remark = newData.remark || ''
    formData.details = (newData.details || []).map((d: any) => ({
      expenseType: d.expenseType || '',
      summary: d.summary || '',
      amount: d.amount || 0,
      occurDate: d.occurDate || '',
      expenseAccountCode: d.expenseAccountCode || d.accountCode || ''
    }))
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

function formatDate(val: any): string {
  if (!val) return '-'
  const d = new Date(val)
  if (isNaN(d.getTime())) return String(val)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

const totalAmount = computed(() => {
  return formData.details.reduce((sum, d) => sum + (Number(d.amount) || 0), 0)
})

// Emit data changes to parent
watch(formData, () => {
  if (!props.readonly) {
    emit('update:data', {
      ...props.data,
      applicationRefId: formData.applicationRefId,
      totalAmount: totalAmount.value,
      reason: formData.reason,
      remark: formData.remark,
      details: formData.details.map((d, i) => ({
        ...d,
        lineNo: i + 1
      }))
    })
  }
}, { deep: true })

function addDetail() {
  formData.details.push({
    _uid: genTempId(),
    expenseType: '',
    summary: '',
    amount: 0,
    occurDate: new Date().toISOString().split('T')[0],
    expenseAccountCode: ''
  })
}

function removeDetail(index: number) {
  formData.details.splice(index, 1)
}

defineExpose({
  getData: () => ({
    applicationRefId: formData.applicationRefId,
    totalAmount: totalAmount.value,
    reason: formData.reason,
    remark: formData.remark,
    details: formData.details.map((d, i) => ({
      ...d,
      lineNo: i + 1
    }))
  }),
  validate: () => {
    if (!formData.reason) return '请输入报销事由'
    if (formData.details.length === 0) return '请添加至少一条报销明细'
    for (const d of formData.details) {
      if (!d.amount || d.amount <= 0) return '报销金额必须大于0'
    }
    return null
  }
})
</script>

<template>
  <div class="petty-cash-reimburse-form">
    <!-- 关联备用金 -->
    <div class="section-card">
      <div class="link-row">
        <span class="link-label">关联备用金</span>
        <VanIcon name="add-o" size="22" color="#999" />
      </div>
      <div v-if="data?.pettyCashNo" class="link-detail">
        <span>备用金编号：{{ data.pettyCashNo }}</span>
        <span v-if="data?.originalAmount" style="margin-left: 12px;">
          原金额：¥{{ formatAmount(data.originalAmount) }}
        </span>
      </div>
    </div>

    <!-- 报销总额卡片 -->
    <div class="section-card">
      <div class="total-amount-card">
        <div class="total-label">报销总额（元）</div>
        <div class="total-value">{{ formatAmountInt(totalAmount) }}</div>
      </div>
    </div>

    <!-- 报销事由 -->
    <div class="section-card">
      <div class="field-block no-border">
        <div class="field-label">
          <span class="required-dot"></span>
          <span>报销事由</span>
        </div>
        <template v-if="!props.readonly">
          <VanField
            v-model="formData.reason"
            type="textarea"
            placeholder="请输入报销事由"
            :autosize="{ minHeight: 40 }"
            maxlength="5000"
            show-word-limit
            class="inline-field"
            :border="false"
          />
        </template>
        <div v-else class="field-value">{{ formData.reason || '-' }}</div>
      </div>
    </div>

    <!-- 报销明细 -->
    <div class="section-card" v-for="(item, index) in formData.details" :key="item._uid || index">
      <div class="detail-tab">
        <span class="detail-tab-text">报销明细 {{ index + 1 }}</span>
        <span v-if="!props.readonly" class="detail-remove-btn" @click="removeDetail(index)">
          <VanIcon name="cross" size="16" color="#f56c6c" />
        </span>
      </div>

      <!-- 费用类型 -->
      <div class="field-block">
        <div class="field-label">费用类型</div>
        <template v-if="!props.readonly">
          <VanField
            v-model="item.expenseType"
            placeholder="请输入费用类型"
            class="inline-field"
            :border="false"
          />
        </template>
        <div v-else class="field-value">{{ item.expenseType || '-' }}</div>
      </div>

      <!-- 摘要 -->
      <div class="field-block">
        <div class="field-label">摘要</div>
        <template v-if="!props.readonly">
          <VanField
            v-model="item.summary"
            type="textarea"
            placeholder="请输入摘要"
            :autosize="{ minHeight: 40 }"
            maxlength="5000"
            show-word-limit
            class="inline-field"
            :border="false"
          />
        </template>
        <div v-else class="field-value">{{ item.summary || '-' }}</div>
      </div>

      <!-- 报销金额 -->
      <div class="field-block">
        <div class="field-label">
          <span class="required-dot"></span>
          <span>报销金额（元）</span>
        </div>
        <template v-if="!props.readonly">
          <VanField
            v-model="item.amount"
            type="number"
            placeholder="请输入金额"
            class="inline-field amount-value"
            :border="false"
          />
        </template>
        <div v-else class="field-value amount-value">{{ formatAmountInt(item.amount) }}</div>
      </div>

      <!-- 发生日期 -->
      <div class="field-block">
        <div class="field-label">发生日期</div>
        <template v-if="!props.readonly">
          <input
            type="date"
            v-model="item.occurDate"
            class="date-input"
          />
        </template>
        <div v-else class="field-value">{{ formatDate(item.occurDate) }}</div>
      </div>

      <!-- 科目编码 -->
      <div class="field-block no-border">
        <div class="field-label">科目编码</div>
        <template v-if="!props.readonly">
          <VanField
            v-model="item.expenseAccountCode"
            placeholder="请输入科目编码"
            class="inline-field"
            :border="false"
          />
        </template>
        <div v-else class="field-value">{{ item.expenseAccountCode || '-' }}</div>
      </div>
    </div>

    <!-- 添加报销明细 -->
    <div class="section-card" v-if="!props.readonly">
      <div class="add-detail-btn" @click="addDetail">
        <VanIcon name="plus" size="14" color="#999" />
        <span>添加报销明细</span>
      </div>
    </div>

    <!-- 收款人 -->
    <div class="section-card" v-if="data?.payeeName">
      <div class="field-block no-border">
        <div class="field-label">收款人</div>
        <div class="field-value">{{ data.payeeName }}</div>
      </div>
    </div>

    <!-- 备注 -->
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
.petty-cash-reimburse-form {
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

/* 报销总额卡片 */
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

/* 明细标签 */
.detail-tab {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
.detail-tab-text {
  display: inline-block;
  font-size: 13px;
  color: #999;
  padding: 4px 0;
  border-bottom: 2px solid #4d8cf7;
}
.detail-remove-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  cursor: pointer;
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
.required-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #f56c6c;
  display: inline-block;
  flex-shrink: 0;
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
.inline-field.amount-value :deep(.van-field__control) {
  font-size: 20px;
  font-weight: 500;
}

/* 日期输入 */
.date-input {
  width: 100%;
  font-size: 16px;
  color: #333;
  border: none;
  outline: none;
  background: transparent;
  padding: 4px 0;
  line-height: 1.5;
}

/* 添加明细按钮 */
.add-detail-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 8px 0;
  color: #999;
  font-size: 14px;
  border: 1px dashed #ddd;
  border-radius: 8px;
  cursor: pointer;
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
