import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { setupErrorReport } from './utils/error-report'

// Vant 全局样式
import 'vant/lib/index.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

// 全局错误监控
setupErrorReport(router)

app.mount('#mobile-app')
