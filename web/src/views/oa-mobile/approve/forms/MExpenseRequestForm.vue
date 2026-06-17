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

const props = defineProps<{
  data: any
  readonly?: boolean
  hideActions?: boolean
}>()

const emit = defineEmits<{
  'update:data': [value: any]
}>()

const formData = reactive({
  reason: '',              // 请款事由
  amount: 0,               // 请款金额
  expenseType: '',         // 费用类型
  expectedPayDate: '',     // 期望付款日期
  payeeName: '',           // 收款方名称
  payeeBank: '',           // 收款方开户行
  payeeAccount: '',        // 收款方账号
  remark: '',              // 备注
})

// Sync props.data -> formData
watch(() => props.data, (newData) => {
  if (newData) {
    formData.reason = newData.reason || ''
    formData.amount = newData.amount || 0
    formData.expenseType = newData.expenseType || ''
    formData.expectedPayDate = newData.expectedPayDate || ''
    formData.payeeName = newData.payeeName || ''
    formData.payeeBank = newData.payeeBank || newData.payeeBankName || ''
    formData.payeeAccount = newData.payeeAccount || ''
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

function formatDate(val: any): string {
  if (!val) return '-'
  const d = new Date(val)
  if (isNaN(d.getTime())) return String(val)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

const payeeDisplay = computed(() => {
  const parts = [formData.payeeName, formData.payeeBank, formData.payeeAccount].filter(Boolean)
  return parts.length > 0 ? parts.join(' ') : ''
})

// Emit data changes to parent
watch(formData, () => {
  if (!props.readonly) {
    emit('update:data', {
      ...props.data,
      reason: formData.reason,
      amount: formData.amount,
      expenseType: formData.expenseType,
      expectedPayDate: formData.expectedPayDate,
      payeeName: formData.payeeName,
      payeeBank: formData.payeeBank,
      payeeAccount: formData.payeeAccount,
      remark: formData.remark,
    })
  }
}, { deep: true })

defineExpose({
  getData: () => ({ ...formData }),
  validate: () => {
    if (!formData.amount || formData.amount <= 0) return '请款金额必须大于0'
    if (!formData.reason) return '请输入请款事由'
    return null
  }
})
</script>

<template>
  <div class="expense-request-form">
    <!-- 付款总额卡片 -->
    <div class="section-card">
      <div class="total-amount-card">
        <div class="total-label">付款总额（元）</div>
        <div class="total-value">{{ formatAmountInt(formData.amount) }}</div>
      </div>
    </div>

    <!-- 关联应付 -->
    <div class="section-card">
      <div class="link-row">
        <span class="link-label">关联应付</span>
        <VanIcon name="add-o" size="22" color="#999" />
      </div>
    </div>

    <!-- 关联收票 -->
    <div class="section-card">
      <div class="link-row">
        <span class="link-label">关联收票</span>
        <VanIcon name="add-o" size="22" color="#999" />
      </div>
    </div>

    <!-- 付款明细 -->
    <div class="section-card">
      <div class="detail-tab">
        <span class="detail-tab-text">付款明细 1</span>
      </div>

      <!-- 付款金额 -->
      <div class="field-block">
        <div class="field-label">
          <span class="required-dot"></span>
          <span>付款金额（元）</span>
        </div>
        <template v-if="!props.readonly">
          <VanField
            v-model="formData.amount"
            type="number"
            placeholder="请输入金额"
            class="inline-field amount-value"
            :border="false"
          />
        </template>
        <div v-else class="field-value amount-value">{{ formatAmountInt(formData.amount) }}</div>
      </div>

      <!-- 付款说明 -->
      <div class="field-block">
        <div class="field-label">付款说明</div>
        <template v-if="!props.readonly">
          <VanField
            v-model="formData.reason"
            type="textarea"
            placeholder="请输入付款说明"
            :autosize="{ minHeight: 40 }"
            maxlength="8000"
            show-word-limit
            class="inline-field"
            :border="false"
          />
        </template>
        <template v-else>
          <div class="field-value">{{ formData.reason || '-' }}</div>
          <div class="char-count" v-if="formData.reason">{{ formData.reason.length }}/8000</div>
        </template>
      </div>

      <!-- 期望付款日期 -->
      <div class="field-block">
        <div class="field-label">期望付款日期</div>
        <template v-if="!props.readonly">
          <input
            type="date"
            v-model="formData.expectedPayDate"
            class="date-input"
          />
        </template>
        <template v-else>
          <div class="field-value">{{ formatDate(formData.expectedPayDate) }}</div>
        </template>
      </div>
    </div>

    <!-- 添加付款明细 -->
    <div class="section-card" v-if="!props.readonly">
      <div class="add-detail-btn">
        <VanIcon name="plus" size="14" color="#999" />
        <span>添加付款明细</span>
      </div>
    </div>

    <!-- 收款账户 -->
    <div class="section-card">
      <div class="field-block no-border">
        <div class="field-label">收款账户</div>
        <template v-if="!props.readonly">
          <div class="field-block">
            <div class="field-label">收款方名称</div>
            <VanField
              v-model="formData.payeeName"
              placeholder="请输入收款方名称"
              class="inline-field"
              :border="false"
            />
          </div>
          <div class="field-block">
            <div class="field-label">开户行</div>
            <VanField
              v-model="formData.payeeBank"
              placeholder="请输入开户行"
              class="inline-field"
              :border="false"
            />
          </div>
          <div class="field-block no-border">
            <div class="field-label">收款账号</div>
            <VanField
              v-model="formData.payeeAccount"
              placeholder="请输入收款账号"
              class="inline-field"
              :border="false"
            />
          </div>
        </template>
        <template v-else>
          <div class="field-value-row">
            <span class="field-value" :class="{ placeholder: !payeeDisplay }">{{ payeeDisplay || '请选择' }}</span>
            <span class="field-actions">
              <VanIcon name="arrow" size="16" color="#ccc" />
            </span>
          </div>
          <div v-if="formData.payeeName" class="account-detail">
            <div class="account-line">{{ formData.payeeName }}</div>
            <div class="account-line" v-if="formData.payeeBank">{{ formData.payeeBank }}</div>
            <div class="account-line" v-if="formData.payeeAccount">{{ formData.payeeAccount }}</div>
          </div>
        </template>
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
          <span class="field-value">{{ data?.deptName || data?.orgName || '-' }}</span>
          <span class="field-actions">
            <VanIcon name="clear" size="16" color="#ccc" />
            <VanIcon name="arrow" size="16" color="#ccc" />
          </span>
        </div>
      </div>
    </div>

    <!-- 电子发票 -->
    <div class="section-card">
      <div class="link-row">
        <span class="link-label">电子发票</span>
        <VanIcon name="add-o" size="22" color="#999" />
      </div>
      <div class="invoice-hint">支持智能识别电子、纸质发票的金额等信息</div>
      <div class="invoice-tags">
        <VanTag plain type="default" class="invoice-tag">无发票</VanTag>
        <VanTag plain type="default" class="invoice-tag">待收发票</VanTag>
        <VanIcon name="question-o" size="16" color="#999" style="margin-left: 4px;" />
      </div>
    </div>

    <!-- 附件 -->
    <div class="section-card">
      <div class="link-row">
        <span class="link-label">附件</span>
        <VanIcon name="add-o" size="22" color="#999" />
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
          <div class="char-count">{{ formData.remark.length }}/8000</div>
        </template>
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
              <div class="node-label">审批人</div>
              <div class="node-sub">请选择审批人</div>
            </div>
            <div class="node-badge-area">
              <VanIcon name="plus" size="14" color="#999" class="node-plus" />
            </div>
          </div>
        </div>
      </div>
      <div class="process-node">
        <div class="node-dot"></div>
        <div class="node-line"></div>
        <div class="node-content">
          <div class="node-row">
            <div class="node-info">
              <div class="node-label">
                付款人
                <span class="required-star">*</span>
              </div>
              <div class="node-sub">请选择</div>
            </div>
            <div class="node-badge-area">
              <VanIcon name="plus" size="14" color="#999" class="node-plus" />
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
              <div class="node-sub">请选择抄送人</div>
            </div>
            <div class="node-badge-area">
              <VanIcon name="plus" size="14" color="#999" class="node-plus" />
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
.expense-request-form {
  background: #f5f6f7;
  min-height: 100%;
}

/* 通用卡片区块 */
.section-card {
  background: #fff;
  border-radius: var(--radius-lg);
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

/* 付款总额卡片 */
.total-amount-card {
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-hover) 100%);
  border-radius: var(--radius-lg);
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
  margin-bottom: 12px;
}
.detail-tab-text {
  display: inline-block;
  font-size: 13px;
  color: #999;
  padding: 4px 0;
  border-bottom: 2px solid var(--border);
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
  background: var(--color-danger);
  display: inline-block;
  flex-shrink: 0;
}
.field-value {
  font-size: 16px;
  color: #333;
  line-height: 1.5;
}
.field-value.placeholder {
  color: #999;
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

/* 收款账户详情 */
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
  color: var(--text-1);
}
.process-link:hover {
  color: var(--color-primary);
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
  background: var(--text-3);
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
.node-plus {
  width: 24px;
  height: 24px;
  border: 1px dashed #ccc;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}
.required-star {
  color: var(--color-danger);
  margin-left: 2px;
  font-weight: 600;
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
