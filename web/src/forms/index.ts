import { defineAsyncComponent, type Component } from 'vue'

/**
 * 硬编码表单组件注册表
 * 新增硬编码表单时，在此处注册即可。
 */
const hardcodedForms: Record<string, Component> = {
  'ExpenseRequest': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MExpenseRequestForm.vue')),
  'ExpenseReimburse': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MExpenseReimburseForm.vue')),
  'ExternalPayment': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MExternalPaymentForm.vue')),
  'PettyCashApply': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MPettyCashApplyForm.vue')),
  'PettyCashReimburse': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MPettyCashReimburseForm.vue')),
  'PettyCashReturn': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MPettyCashReturnForm.vue')),
  'PettyCashWriteOff': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MPettyCashWriteOffForm.vue')),
  'SalaryAdvance': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MSalaryAdvanceForm.vue')),
  'LoanApply': defineAsyncComponent(() => import('@/views/oa-mobile/approve/forms/MLoanApplyForm.vue')),
}

/** 硬编码表单中文标签映射 */
const formLabels: Record<string, string> = {
  'ExpenseRequest': '费用请款',
  'ExpenseReimburse': '费用报销',
  'ExternalPayment': '对外付款',
  'PettyCashApply': '备用金申请',
  'PettyCashReimburse': '备用金报销',
  'PettyCashReturn': '备用金还款',
  'PettyCashWriteOff': '备用金冲销',
  'SalaryAdvance': '工资预支',
  'LoanApply': '借款申请',
}

/** 获取硬编码表单的中文标签 */
export function getFormLabel(name: string): string {
  return formLabels[name] || name
}

/** 根据名称获取已注册的硬编码表单组件 */
export function getHardcodedForm(name: string): Component | undefined {
  return hardcodedForms[name]
}

/** 获取所有已注册的硬编码表单名称列表 */
export function getRegisteredFormNames(): string[] {
  return Object.keys(hardcodedForms)
}

/** 动态注册硬编码表单组件 */
export function registerHardcodedForm(name: string, component: Component): void {
  hardcodedForms[name] = component
}

export default hardcodedForms
