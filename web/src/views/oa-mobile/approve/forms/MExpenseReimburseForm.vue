<script setup lang="ts">
import { computed, reactive, watch } from 'vue'
import {
  CellGroup as VanCellGroup,
  Cell as VanCell,
  Tag as VanTag,
  Icon as VanIcon,
  Switch as VanSwitch,
  Button as VanButton,
  Field as VanField,
} from 'vant'
import 'vant/es/cell-group/style'
import 'vant/es/cell/style'
import 'vant/es/tag/style'
import 'vant/es/icon/style'
import 'vant/es/switch/style'
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
  reason: '',
  totalAmount: 0,
  payeeName: '',
  payeeBank: '',
  payeeAccount: '',
  remark: '',
  details: [] as Array<{
    expenseType: string
    summary: string
    amount: number
    occurDate: string
    remark: string
  }>
})

// Sync props.data -> formData
watch(() => props.data, (newData) => {
  if (newData) {
    formData.reason = newData.reason || ''
    formData.payeeName = newData.payeeName || ''
    formData.payeeBank = newData.payeeBank || newData.payeeBankName || ''
    formData.payeeAccount = newData.payeeAccount || ''
    formData.remark = newData.remark || ''
    formData.details = (newData.details || []).map((d: any) => ({
      expenseType: d.expenseType || d.expenseTypeName || '',
      summary: d.summary || '',
      amount: d.amount || 0,
      occurDate: d.occurDate || '',
      remark: d.remark || ''
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
      reason: formData.reason,
      totalAmount: totalAmount.value,
      payeeName: formData.payeeName,
      payeeBank: formData.payeeBank,
      payeeAccount: formData.payeeAccount,
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
    remark: ''
  })
}

function removeDetail(index: number) {
  formData.details.splice(index, 1)
}

defineExpose({
  getData: () => ({
    reason: formData.reason,
    totalAmount: totalAmount.value,
    payeeName: formData.payeeName,
    payeeBank: formData.payeeBank,
    payeeAccount: formData.payeeAccount,
    remark: formData.remark,
    details: formData.details.map((d, i) => ({
      ...d,
      lineNo: i + 1
    }))
  }),
  validate: () => {
    if (formData.details.length === 0) return '请添加至少一条报销明细'
    for (const d of formData.details) {
      if (!d.amount || d.amount <= 0) return '报销金额必须大于0'
      if (!d.occurDate) return '请选择发生日期'
    }
    return null
  }
})
</script>

<template>
  <div class="expense-reimburse-form">
    <!-- 关联备用金 -->
    <div class="section-card">
      <div class="link-row">
        <span class="link-label">关联备用金</span>
        <VanIcon name="add-o" size="22" color="#999" />
      </div>
    </div>

    <!-- 报销总额卡片 -->
    <div class="section-card">
      <div class="total-amount-card">
        <div class="total-label">报销总额（元）</div>
        <div class="total-value">{{ formatAmountInt(totalAmount) }}</div>
      </div>

      <!-- 操作按钮行 -->
      <div class="action-buttons" v-if="!props.readonly">
        <div class="action-btn">
          <VanIcon name="edit" size="16" color="#333" />
          <span>导入随手记</span>
        </div>
        <div class="action-btn">
          <VanIcon name="scan" size="16" color="#333" />
          <span>发票识别</span>
        </div>
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

      <!-- 费用说明 -->
      <div class="field-block">
        <div class="field-label">费用说明</div>
        <template v-if="!props.readonly">
          <VanField
            v-model="item.summary"
            type="textarea"
            placeholder="请输入费用说明"
            :autosize="{ minHeight: 40 }"
            maxlength="5000"
            show-word-limit
            class="inline-field"
            :border="false"
          />
        </template>
        <template v-else>
          <div class="field-value">{{ item.summary || item.expenseType || '-' }}</div>
          <div class="char-count" v-if="item.summary">{{ (item.summary || '').length }}/5000</div>
        </template>
      </div>

      <!-- 发生日期 -->
      <div class="field-block">
        <div class="field-label">
          <span class="required-dot"></span>
          <span>发生日期</span>
        </div>
        <template v-if="!props.readonly">
          <input
            type="date"
            v-model="item.occurDate"
            class="date-input"
          />
        </template>
        <template v-else>
          <div class="field-value-row">
            <span class="field-value">{{ formatDate(item.occurDate) }}</span>
            <span class="field-actions">
              <VanIcon name="clear" size="16" color="#ccc" />
              <VanIcon name="arrow" size="16" color="#ccc" />
            </span>
          </div>
        </template>
      </div>
    </div>

    <!-- 添加报销明细 -->
    <div class="section-card" v-if="!props.readonly">
      <div class="add-detail-btn" @click="addDetail">
        <VanIcon name="plus" size="14" color="#999" />
        <span>添加报销明细</span>
      </div>
    </div>

    <!-- 发票 -->
    <div class="section-card">
      <div class="link-row">
        <span class="link-label">发票</span>
        <VanIcon name="add-o" size="22" color="#999" />
      </div>
      <div class="invoice-hint">支持智能识别电子、纸质发票的金额等信息</div>
      <div class="invoice-tags">
        <VanTag plain type="default" class="invoice-tag">无发票</VanTag>
        <VanTag plain type="default" class="invoice-tag">待收发票</VanTag>
        <VanIcon name="question-o" size="16" color="#999" style="margin-left: 4px;" />
      </div>
    </div>

    <!-- 归属人 -->
    <div class="section-card">
      <div class="field-block no-border">
        <div class="field-label">归属人</div>
        <div class="field-value-row">
          <span class="field-value">{{ data?.applicantName || '-' }}</span>
          <span class="field-actions">
            <VanIcon name="clear" size="16" color="#ccc" />
            <VanIcon name="arrow" size="16" color="#ccc" />
          </span>
        </div>
      </div>
    </div>

    <!-- 归属部门 -->
    <div class="section-card">
      <div class="field-block no-border">
        <div class="field-label">归属部门</div>
        <div class="field-value-row">
          <span class="field-value">{{ data?.deptName || data?.organizationName || '-' }}</span>
          <span class="field-actions">
            <VanIcon name="clear" size="16" color="#ccc" />
            <VanIcon name="arrow" size="16" color="#ccc" />
          </span>
        </div>
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

    <!-- 收款账户 -->
    <div class="section-card">
      <VanCell title="收款账户" is-link :value="formData.payeeName || '请选择'" class="account-cell" />
      <div v-if="formData.payeeName" class="account-detail">
        <div class="account-line">{{ formData.payeeName }}</div>
        <div class="account-line" v-if="formData.payeeBank">{{ formData.payeeBank }}</div>
        <div class="account-line" v-if="formData.payeeAccount">{{ formData.payeeAccount }}</div>
      </div>
    </div>

    <!-- 流程 -->
    <div class="section-card process-card">
      <div class="process-header">
        <span class="process-title">流程</span>
        <span class="process-link">流程设置 &gt;</span>
      </div>
      <div class="process-node">
        <div class="node-dot"></div>
        <div class="node-line"></div>
        <div class="node-content">
          <div class="node-row">
            <div class="node-info">
              <div class="node-label">付款人</div>
              <div class="node-sub">1人付款</div>
            </div>
            <div class="node-badge-area">
              <VanTag type="primary" round size="medium" class="role-tag">职</VanTag>
              <span class="node-person">{{ data?.applicantName || '-' }}</span>
            </div>
          </div>
        </div>
      </div>
      <div class="process-node">
        <div class="node-dot"></div>
        <div class="node-content">
          <div class="node-row">
            <div class="node-info">
              <div class="node-label">抄送人</div>
              <div class="node-sub">抄送1人</div>
            </div>
            <div class="node-badge-area">
              <VanTag color="#07c160" round size="medium" class="cc-tag">{{ (data?.applicantName || '-').charAt(0) }}</VanTag>
              <VanIcon name="plus" size="14" color="#999" class="cc-plus" />
              <VanIcon name="plus" size="14" color="#999" class="cc-plus" />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 发送到聊天 -->
    <div class="section-card">
      <div class="chat-row">
        <div class="chat-left">
          <span>发送到聊天</span>
          <VanIcon name="question-o" size="16" color="#999" style="margin-left: 4px;" />
        </div>
        <VanIcon name="add-o" size="22" color="#999" />
      </div>
      <div class="chat-hint">通过聊天发送给审批人</div>
      <div class="chat-switch-row">
        <VanSwitch :model-value="false" size="20" disabled />
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
.expense-reimburse-form {
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

/* 关联备用金行 */
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

/* 报销总额卡片 */
.total-amount-card {
  background: linear-gradient(135deg, #4d8cf7 0%, #6fa3fb 100%);
  border-radius: 10px;
  padding: 16px 20px;
  margin-bottom: 16px;
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

/* 操作按钮行 */
.action-buttons {
  display: flex;
  gap: 12px;
}
.action-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  height: 40px;
  border: 1px solid #e8e8e8;
  border-radius: 8px;
  font-size: 14px;
  color: #333;
  background: #fff;
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
.field-value-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.field-actions {
  display: flex;
  align-items: center;
  gap: 8px;
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

/* 发票区 */
.invoice-hint {
  font-size: 13px;
  color: #999;
  margin-top: 8px;
}
.invoice-tags {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 10px;
}
.invoice-tag {
  border-radius: 4px !important;
  color: #666 !important;
  border-color: #ddd !important;
  background: #fff !important;
}

/* 收款账户 */
.account-cell {
  padding: 0 !important;
}
.account-cell :deep(.van-cell__title) {
  font-size: 16px;
  font-weight: 500;
  color: #333;
}
.account-cell :deep(.van-cell__value) {
  color: #999;
}
.account-detail {
  padding-top: 8px;
  border-top: 1px solid #f0f0f0;
  margin-top: 8px;
}
.account-line {
  font-size: 14px;
  color: #666;
  line-height: 1.8;
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

/* 流程卡片 */
.process-card {
  padding-bottom: 12px;
}
.process-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.process-title {
  font-size: 16px;
  font-weight: 600;
  color: #333;
}
.process-link {
  font-size: 13px;
  color: #4d8cf7;
}
.process-node {
  position: relative;
  padding-left: 20px;
  padding-bottom: 16px;
}
.node-dot {
  position: absolute;
  left: 0;
  top: 6px;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #4d8cf7;
}
.node-line {
  position: absolute;
  left: 4px;
  top: 18px;
  bottom: 0;
  width: 2px;
  background: #e8e8e8;
}
.node-content {
  padding-left: 8px;
}
.node-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.node-info {
  flex: 1;
}
.node-label {
  font-size: 15px;
  color: #333;
  font-weight: 500;
}
.node-sub {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}
.node-badge-area {
  display: flex;
  align-items: center;
  gap: 6px;
}
.role-tag {
  font-size: 12px !important;
}
.cc-tag {
  font-size: 12px !important;
}
.cc-plus {
  width: 24px;
  height: 24px;
  border: 1px dashed #ccc;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}
.node-person {
  font-size: 12px;
  color: #666;
}

/* 发送到聊天 */
.chat-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.chat-left {
  display: flex;
  align-items: center;
  font-size: 16px;
  font-weight: 500;
  color: #333;
}
.chat-hint {
  font-size: 13px;
  color: #999;
  margin-top: 8px;
}
.chat-switch-row {
  display: flex;
  justify-content: flex-end;
  margin-top: 8px;
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
