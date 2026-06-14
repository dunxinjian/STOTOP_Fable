import type { App } from 'vue'
import formCreate from '@form-create/ant-design-vue'
import EmployeeSelect from './EmployeeSelect.vue'
import DepartmentSelect from './DepartmentSelect.vue'
import AmountInput from './AmountInput.vue'
import FileUpload from './FileUpload.vue'
import ApprovalComment from './ApprovalComment.vue'
import SignatureInput from './SignatureInput.vue'
import { getOrganizationTree } from '@/api/system'
import { getRefreshToken, getToken } from '@/utils/auth'

// 预加载的组织选项（供设计器属性面板使用）
let orgOptions: { label: string; value: number }[] = []

/** 预加载组织数据，在模块初始化后调用 */
export async function preloadOrgOptions() {
  if (!getToken() || !getRefreshToken()) {
    orgOptions = []
    return
  }

  try {
    const res = await getOrganizationTree()
    const data = (res as any)?.data || res || []
    orgOptions = []
    function flatten(nodes: any[]) {
      for (const node of nodes) {
        const name = node.name || node.fName || ''
        const id = node.id ?? node.fid
        if (name && id != null) {
          orgOptions.push({ label: `${name} (ID:${id})`, value: id })
        }
        if (node.children?.length) flatten(node.children)
      }
    }
    flatten(Array.isArray(data) ? data : [])
  } catch (e) {
    if (e instanceof Error && e.message === 'No refresh token') {
      orgOptions = []
      return
    }

    console.warn('[form-widgets] 预加载组织数据失败', e)
  }
}

/** 生成组织选择的 props rule */
function orgSelectRule(field: string, title: string, placeholder: string) {
  return {
    type: 'select', field, title,
    props: {
      showSearch: true, allowClear: true, placeholder,
      filterOption: (input: string, option: any) =>
        (option.label || '').toLowerCase().includes(input.toLowerCase()),
      options: orgOptions,
    },
  }
}

// 将自定义组件注册到 form-create
export function registerFormWidgets() {
  formCreate.component('EmployeeSelect', EmployeeSelect)
  formCreate.component('DepartmentSelect', DepartmentSelect)
  formCreate.component('AmountInput', AmountInput)
  formCreate.component('FileUpload', FileUpload)
  formCreate.component('ApprovalComment', ApprovalComment)
  formCreate.component('SignatureInput', SignatureInput)
}

// fc-designer 自定义组件菜单配置
export const customWidgetMenu = {
  title: '业务组件',
  name: 'custom-widgets',
  list: [
    {
      icon: 'icon-select',
      label: '员工选择',
      name: 'EmployeeSelect',
      rule() {
        return {
          type: 'EmployeeSelect',
          field: 'employeeId',
          title: '员工选择',
          value: '',
          props: {
            placeholder: '请选择员工',
          },
        }
      },
      props() {
        return [
          { type: 'switch', field: 'disabled', title: '禁用' },
          { type: 'input', field: 'placeholder', title: '占位提示' },
          orgSelectRule('orgId', '限定组织', '搜索并选择组织'),
          orgSelectRule('deptId', '限定部门', '搜索并选择部门'),
          { type: 'select', field: 'status', title: '员工状态', props: {
            allowClear: true, placeholder: '不限',
            options: [
              { label: '在职', value: 1 },
              { label: '离职', value: 0 },
            ]
          }},
        ]
      },
    },
    {
      icon: 'icon-tree',
      label: '部门选择',
      name: 'DepartmentSelect',
      rule() {
        return {
          type: 'DepartmentSelect',
          field: 'departmentId',
          title: '部门选择',
          value: '',
          props: {
            placeholder: '请选择部门',
          },
        }
      },
      props() {
        return [
          { type: 'switch', field: 'disabled', title: '禁用' },
          { type: 'input', field: 'placeholder', title: '占位提示' },
          orgSelectRule('rootOrgId', '根节点组织', '搜索并选择组织'),
          { type: 'select', field: 'orgType', title: '组织类型', props: {
            allowClear: true, placeholder: '不限',
            options: [
              { label: '集团', value: 'group' },
              { label: '公司', value: 'company' },
              { label: '部门', value: 'department' },
              { label: '团队', value: 'team' },
            ],
          }},
        ]
      },
    },
    {
      icon: 'icon-number',
      label: '金额输入',
      name: 'AmountInput',
      rule() {
        return {
          type: 'AmountInput',
          field: 'amount',
          title: '金额',
          value: 0,
          props: {
            placeholder: '请输入金额',
          },
        }
      },
      props() {
        return [
          { type: 'switch', field: 'disabled', title: '禁用' },
          { type: 'input', field: 'placeholder', title: '占位提示' },
        ]
      },
    },
    {
      icon: 'icon-upload',
      label: '附件上传',
      name: 'FileUpload',
      rule() {
        return {
          type: 'FileUpload',
          field: 'attachments',
          title: '附件',
          value: [],
          props: {
            maxCount: 5,
          },
        }
      },
      props() {
        return [
          { type: 'switch', field: 'disabled', title: '禁用' },
          { type: 'inputNumber', field: 'maxCount', title: '最大文件数' },
        ]
      },
    },
    {
      icon: 'icon-editor',
      label: '审批意见',
      name: 'ApprovalComment',
      rule() {
        return {
          type: 'ApprovalComment',
          field: 'approvalComment',
          title: '审批意见',
          value: '',
          props: {
            placeholder: '请输入审批意见',
            maxLength: 500,
            rows: 4,
          },
        }
      },
      props() {
        return [
          { type: 'switch', field: 'disabled', title: '禁用' },
          { type: 'input', field: 'placeholder', title: '占位提示' },
          { type: 'inputNumber', field: 'maxLength', title: '最大字数' },
          { type: 'inputNumber', field: 'rows', title: '行数' },
        ]
      },
    },
    {
      icon: 'icon-write',
      label: '手写签名',
      name: 'SignatureInput',
      rule() {
        return {
          type: 'SignatureInput',
          field: 'signature',
          title: '手写签名',
          value: '',
          props: {
            width: 400,
            height: 200,
          },
        }
      },
      props() {
        return [
          { type: 'switch', field: 'disabled', title: '禁用' },
          { type: 'inputNumber', field: 'width', title: '宽度(px)' },
          { type: 'inputNumber', field: 'height', title: '高度(px)' },
          { type: 'inputNumber', field: 'lineWidth', title: '线条粗细' },
          { type: 'input', field: 'lineColor', title: '线条颜色' },
        ]
      },
    },
  ],
}

// 业务组件拖拽规则（供 FcDesigner.addDragRule 使用）
export const customWidgetDragRules = [
  {
    menu: 'custom-widgets',
    icon: 'icon-select',
    label: '员工选择',
    name: 'EmployeeSelect',
    input: true,
    rule() {
      return {
        type: 'EmployeeSelect',
        field: 'employeeId',
        title: '员工选择',
        value: '',
        props: { placeholder: '请选择员工' },
      }
    },
    props() {
      return [
        { type: 'switch', field: 'disabled', title: '禁用' },
        { type: 'input', field: 'placeholder', title: '占位提示' },
        orgSelectRule('orgId', '限定组织', '搜索并选择组织'),
        orgSelectRule('deptId', '限定部门', '搜索并选择部门'),
        { type: 'select', field: 'status', title: '员工状态', props: {
          allowClear: true, placeholder: '不限',
          options: [
            { label: '在职', value: 1 },
            { label: '离职', value: 0 },
          ]
        }},
      ]
    },
  },
  {
    menu: 'custom-widgets',
    icon: 'icon-tree',
    label: '部门选择',
    name: 'DepartmentSelect',
    input: true,
    rule() {
      return {
        type: 'DepartmentSelect',
        field: 'departmentId',
        title: '部门选择',
        value: '',
        props: { placeholder: '请选择部门' },
      }
    },
    props() {
      return [
        { type: 'switch', field: 'disabled', title: '禁用' },
        { type: 'input', field: 'placeholder', title: '占位提示' },
        orgSelectRule('rootOrgId', '根节点组织', '搜索并选择组织'),
        { type: 'select', field: 'orgType', title: '组织类型', props: {
          allowClear: true, placeholder: '不限',
          options: [
            { label: '集团', value: 'group' },
            { label: '公司', value: 'company' },
            { label: '部门', value: 'department' },
            { label: '团队', value: 'team' },
          ],
        }},
      ]
    },
  },
  {
    menu: 'custom-widgets',
    icon: 'icon-number',
    label: '金额输入',
    name: 'AmountInput',
    input: true,
    rule() {
      return {
        type: 'AmountInput',
        field: 'amount',
        title: '金额',
        value: 0,
        props: { placeholder: '请输入金额' },
      }
    },
    props() {
      return [
        { type: 'switch', field: 'disabled', title: '禁用' },
        { type: 'input', field: 'placeholder', title: '占位提示' },
      ]
    },
  },
  {
    menu: 'custom-widgets',
    icon: 'icon-upload',
    label: '附件上传',
    name: 'FileUpload',
    input: true,
    rule() {
      return {
        type: 'FileUpload',
        field: 'attachments',
        title: '附件',
        value: [],
        props: { maxCount: 5 },
      }
    },
    props() {
      return [
        { type: 'switch', field: 'disabled', title: '禁用' },
        { type: 'inputNumber', field: 'maxCount', title: '最大文件数' },
      ]
    },
  },
  {
    menu: 'custom-widgets',
    icon: 'icon-editor',
    label: '审批意见',
    name: 'ApprovalComment',
    input: true,
    rule() {
      return {
        type: 'ApprovalComment',
        field: 'approvalComment',
        title: '审批意见',
        value: '',
        props: {
          placeholder: '请输入审批意见',
          maxLength: 500,
          rows: 4,
        },
      }
    },
    props() {
      return [
        { type: 'switch', field: 'disabled', title: '禁用' },
        { type: 'input', field: 'placeholder', title: '占位提示' },
        { type: 'inputNumber', field: 'maxLength', title: '最大字数' },
        { type: 'inputNumber', field: 'rows', title: '行数' },
      ]
    },
  },
  {
    menu: 'custom-widgets',
    icon: 'icon-write',
    label: '手写签名',
    name: 'SignatureInput',
    input: true,
    rule() {
      return {
        type: 'SignatureInput',
        field: 'signature',
        title: '手写签名',
        value: '',
        props: {
          width: 400,
          height: 200,
        },
      }
    },
    props() {
      return [
        { type: 'switch', field: 'disabled', title: '禁用' },
        { type: 'inputNumber', field: 'width', title: '宽度(px)' },
        { type: 'inputNumber', field: 'height', title: '高度(px)' },
        { type: 'inputNumber', field: 'lineWidth', title: '线条粗细' },
        { type: 'input', field: 'lineColor', title: '线条颜色' },
      ]
    },
  },
]

export { EmployeeSelect, DepartmentSelect, AmountInput, FileUpload, ApprovalComment, SignatureInput }
