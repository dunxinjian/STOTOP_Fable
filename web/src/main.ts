import { createApp } from 'vue'
import { createPinia } from 'pinia'
import Antd from 'ant-design-vue'
import 'ant-design-vue/dist/reset.css'
import FcDesigner from '@form-create/antd-designer'
import router from './router'
import App from './App.vue'
import '@/styles/index.scss'
import { registerFormWidgets, customWidgetMenu, customWidgetDragRules, preloadOrgOptions } from '@/components/form-widgets'
import dayjs from 'dayjs'
import 'dayjs/locale/zh-cn'
dayjs.locale('zh-cn')

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(Antd)
app.use(FcDesigner)
app.use(FcDesigner.formCreate)

// 注册自定义表单组件到 form-create
registerFormWidgets()

// 添加业务组件菜单分类（左侧面板分组标题）
FcDesigner.addMenu({
  title: '业务组件',
  name: 'custom-widgets',
})

// 注册业务组件的拖拽规则（关键！使组件可拖拽）
FcDesigner.addDragRule(customWidgetDragRules)

// 预加载组织数据供设计器属性面板下拉框使用
preloadOrgOptions()

// 全局未捕获异常处理
app.config.errorHandler = (err, instance, info) => {
  console.error('Uncaught error:', err, info)
}

app.mount('#app')
