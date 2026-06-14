// OA 模块 API
import { get, post, put, del } from './request'

// ===== 费用请款 =====
export function getExpenseRequestList(params: any) {
  return get('/oa/expense-request/list', params)
}
export function getExpenseRequest(id: number) {
  return get(`/oa/expense-request/${id}`)
}
export function createExpenseRequest(data: any) {
  return post('/oa/expense-request', data)
}
export function updateExpenseRequest(id: number, data: any) {
  return put(`/oa/expense-request/${id}`, data)
}
export function deleteExpenseRequest(id: number) {
  return del(`/oa/expense-request/${id}`)
}
export function submitExpenseRequest(id: number) {
  return post(`/oa/expense-request/${id}/submit`)
}

// ===== 费用报销 =====
export function getExpenseReimburseList(params: any) {
  return get('/oa/expense-reimburse/list', params)
}
export function getExpenseReimburse(id: number) {
  return get(`/oa/expense-reimburse/${id}`)
}
export function createExpenseReimburse(data: any) {
  return post('/oa/expense-reimburse', data)
}
export function updateExpenseReimburse(id: number, data: any) {
  return put(`/oa/expense-reimburse/${id}`, data)
}
export function submitExpenseReimburse(id: number) {
  return post(`/oa/expense-reimburse/${id}/submit`)
}
export function getAvailableRequests(orgId?: number) {
  return get('/oa/expense-reimburse/available-requests', orgId ? { orgId } : undefined)
}
export function getAvailableLoans(orgId?: number) {
  return get('/oa/expense-reimburse/available-loans', orgId ? { orgId } : undefined)
}

// ===== 对外付款 =====
export function getExternalPaymentList(params: any) { return get('/oa/external-payment/list', params) }
export function getExternalPayment(id: number) { return get(`/oa/external-payment/${id}`) }
export function createExternalPayment(data: any) { return post('/oa/external-payment', data) }
export function updateExternalPayment(id: number, data: any) { return put(`/oa/external-payment/${id}`, data) }
export function submitExternalPayment(id: number) { return post(`/oa/external-payment/${id}/submit`) }

// ===== 备用金 =====
export function getPettyCashApplyList(params: any) { return get('/oa/petty-cash/apply/list', params) }
export function getPettyCashApply(id: number) { return get(`/oa/petty-cash/apply/${id}`) }
export function createPettyCashApply(data: any) { return post('/oa/petty-cash/apply', data) }
export function updatePettyCashApply(id: number, data: any) { return put(`/oa/petty-cash/apply/${id}`, data) }
export function submitPettyCashApply(id: number) { return post(`/oa/petty-cash/apply/${id}/submit`) }
export function getPettyCashLedger(params: any) { return get('/oa/petty-cash/ledger', params) }

// ===== 备用金报销 =====
export function getPettyCashReimburseList(params: any) { return get('/oa/petty-cash/reimburse/list', params) }
export function getPettyCashReimburse(id: number) { return get(`/oa/petty-cash/reimburse/${id}`) }
export function createPettyCashReimburse(data: any) { return post('/oa/petty-cash/reimburse', data) }
export function updatePettyCashReimburse(id: number, data: any) { return put(`/oa/petty-cash/reimburse/${id}`, data) }
export function submitPettyCashReimburse(id: number) { return post(`/oa/petty-cash/reimburse/${id}/submit`) }

// ===== 备用金还款 =====
export function getPettyCashReturnList(params: any) { return get('/oa/petty-cash/return/list', params) }
export function getPettyCashReturn(id: number) { return get(`/oa/petty-cash/return/${id}`) }
export function createPettyCashReturn(data: any) { return post('/oa/petty-cash/return', data) }
export function updatePettyCashReturn(id: number, data: any) { return put(`/oa/petty-cash/return/${id}`, data) }
export function submitPettyCashReturn(id: number) { return post(`/oa/petty-cash/return/${id}/submit`) }

// ===== 备用金冲销 =====
export function getPettyCashWriteOffList(params: any) { return get('/oa/petty-cash/writeoff/list', params) }
export function getPettyCashWriteOff(id: number) { return get(`/oa/petty-cash/writeoff/${id}`) }
export function createPettyCashWriteOff(data: any) { return post('/oa/petty-cash/writeoff', data) }
export function updatePettyCashWriteOff(id: number, data: any) { return put(`/oa/petty-cash/writeoff/${id}`, data) }
export function submitPettyCashWriteOff(id: number) { return post(`/oa/petty-cash/writeoff/${id}/submit`) }

// ===== 预支工资 =====
export function getSalaryAdvanceList(params: any) { return get('/oa/salary-advance/list', params) }
export function getSalaryAdvance(id: number) { return get(`/oa/salary-advance/${id}`) }
export function createSalaryAdvance(data: any) { return post('/oa/salary-advance', data) }
export function updateSalaryAdvance(id: number, data: any) { return put(`/oa/salary-advance/${id}`, data) }
export function submitSalaryAdvance(id: number) { return post(`/oa/salary-advance/${id}/submit`) }

// ===== 借款申请 =====
export function getLoanApplyList(params: any) { return get('/oa/loan-apply/list', params) }
export function getLoanApply(id: number) { return get(`/oa/loan-apply/${id}`) }
export function createLoanApply(data: any) { return post('/oa/loan-apply', data) }
export function updateLoanApply(id: number, data: any) { return put(`/oa/loan-apply/${id}`, data) }
export function submitLoanApply(id: number) { return post(`/oa/loan-apply/${id}/submit`) }
export function getLoanLedger(params: any) { return get('/oa/loan-apply/ledger', params) }

// ===== 附件 =====
export function uploadAttachment(docType: string, docId: number, file: FormData) {
  return post(`/oa/attachment/upload?docType=${docType}&docId=${docId}`, file)
}
export function getAttachmentList(docType: string, docId: number) {
  return get('/oa/attachment/list', { docType, docId })
}

// ===== 委托 =====
export function getDelegationList() { return get('/oa/delegation/list') }
export function createDelegation(data: any) { return post('/oa/delegation', data) }
export function revokeDelegation(id: number) { return post(`/oa/delegation/${id}/revoke`) }

// ===== 费用类型 =====
export function getExpenseTypeList(params?: any) { return get('/oa/expense-type/list', params) }
export function createExpenseType(data: any) { return post('/oa/expense-type', data) }
export function updateExpenseType(id: number, data: any) { return put(`/oa/expense-type/${id}`, data) }
export function toggleExpenseType(id: number) { return post(`/oa/expense-type/${id}/toggle`) }

// ===== 费用科目映射 =====
export function getExpenseAccountMappingList(params: any) { return get('/oa/expense-account-mapping/list', params) }
export function createExpenseAccountMapping(data: any) { return post('/oa/expense-account-mapping', data) }
export function updateExpenseAccountMapping(id: number, data: any) { return put(`/oa/expense-account-mapping/${id}`, data) }
export function deleteExpenseAccountMapping(id: number) { return del(`/oa/expense-account-mapping/${id}`) }

// ====== 日历事件 ======

/** 获取日历事件列表（日历视图） */
export function getCalendarEvents(params: { startDate: string; endDate: string; orgId?: number; organizerId?: number; status?: number }) {
  return get('/oa/calendar-event/list', params)
}

/** 获取看板视图数据（按状态分组） */
export function getCalendarBoard(params: { startDate: string; endDate: string; orgId?: number }) {
  return get('/oa/calendar-event/board', params)
}

/** 获取日历事件详情 */
export function getCalendarEventDetail(id: number) {
  return get(`/oa/calendar-event/${id}`)
}

/** 创建日历事件 */
export function createCalendarEvent(data: any) {
  return post('/oa/calendar-event', data)
}

/** 更新日历事件 */
export function updateCalendarEvent(id: number, data: any) {
  return put(`/oa/calendar-event/${id}`, data)
}

/** 删除日历事件 */
export function deleteCalendarEvent(id: number) {
  return del(`/oa/calendar-event/${id}`)
}

/** 开始会议 */
export function startCalendarEvent(id: number) {
  return post(`/oa/calendar-event/${id}/start`)
}

/** 结束会议 */
export function endCalendarEvent(id: number) {
  return post(`/oa/calendar-event/${id}/end`)
}

/** 取消会议 */
export function cancelCalendarEvent(id: number) {
  return post(`/oa/calendar-event/${id}/cancel`)
}

/** 添加参与者 */
export function addEventAttendees(id: number, data: { userIds: number[]; isRequired?: boolean }) {
  return post(`/oa/calendar-event/${id}/attendees`, data)
}

/** 移除参与者 */
export function removeEventAttendee(id: number, userId: number) {
  return del(`/oa/calendar-event/${id}/attendees/${userId}`)
}

/** 参与者回复 */
export function respondToEvent(id: number, userId: number, data: { responseStatus: number }) {
  return put(`/oa/calendar-event/${id}/attendees/${userId}/response`, data)
}

/** 推送到钉钉 */
export function syncCalendarPush() {
  return post('/oa/calendar-event/sync/push')
}

/** 从钉钉拉取 */
export function syncCalendarPull() {
  return post('/oa/calendar-event/sync/pull')
}

