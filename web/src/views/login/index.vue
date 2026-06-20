<template>
  <div class="login-page">
    <!-- 左右分栏容器 -->
    <div class="login-split">

      <!-- 左侧：品牌区 -->
      <div class="login-brand">
        <div class="brand-inner">
          <div class="brand-logo">
            <img
              v-if="enterpriseInfoStore.hasLogo"
              :src="enterpriseInfoStore.logoUrl"
              :alt="enterpriseInfoStore.displayName"
              class="brand-logo-img"
            />
            <div v-else class="brand-logo-text">{{ enterpriseInfoStore.displayName }}</div>
          </div>
          <p class="brand-tagline">把快递物流、仓储作业和经营数据放在同一个工作现场</p>
          <div class="brand-features">
            <div class="brand-feature-item">
              <span class="brand-feature-dot"></span>
              <span>揽收、报价、仓配、结算一体协同</span>
            </div>
            <div class="brand-feature-item">
              <span class="brand-feature-dot"></span>
              <span>多网点、多仓、多账套权限精细管控</span>
            </div>
            <div class="brand-feature-item">
              <span class="brand-feature-dot"></span>
              <span>运单质量、库存状态、经营分析实时联动</span>
            </div>
          </div>
        </div>
        <div class="brand-footer">{{ enterpriseInfoStore.name }} Enterprise System</div>
      </div>

      <!-- 右侧：表单区 -->
      <div class="login-form-panel">
        <div class="login-form-inner">
          <div class="login-system-status">
            <span>当前组织：MDSTO</span>
            <span class="system-status-badge">系统正常</span>
          </div>
          <div class="login-form-header">
            <h2 class="login-form-title">欢迎登录</h2>
            <p class="login-form-subtitle">请输入账号和密码进入企业办公系统</p>
          </div>

          <a-alert
            v-if="!dbConfigured"
            message="系统主数据库未配置，请联系管理员进行配置"
            type="warning"
            :closable="false"
            show-icon
            style="margin-bottom: 16px"
          />

          <a-form
            ref="formRef"
            :model="form"
            :rules="rules"
            class="login-form"
            size="large"
            @keyup.enter="handleLogin"
          >
            <a-form-item name="username">
              <a-input
                v-model:value="form.username"
                placeholder="请输入账号/手机号"
                autocomplete="username"
                aria-label="账号"
                allow-clear
                :disabled="loading"
              >
                <template #prefix><UserOutlined /></template>
              </a-input>
            </a-form-item>

            <a-form-item name="password">
              <a-input-password
                v-model:value="form.password"
                placeholder="请输入密码"
                autocomplete="current-password"
                aria-label="密码"
                :disabled="loading"
              >
                <template #prefix><LockOutlined /></template>
              </a-input-password>
            </a-form-item>

            <a-form-item>
              <div class="remember-row">
                <a-checkbox v-model:checked="rememberAccount" :disabled="loading">记住账号</a-checkbox>
                <a class="forgot-password" @click="handleForgotPassword">忘记密码？</a>
              </div>
            </a-form-item>

            <a-form-item>
              <a-button
                type="primary"
                class="login-btn"
                :loading="loading"
                :disabled="!dbConfigured || loading"
                html-type="button"
                @click="handleLogin"
              >
                {{ loading ? '登录中...' : '登录系统' }}
              </a-button>
            </a-form-item>
          </a-form>

          <div class="login-footer">
            <a-divider />
            <div class="other-login">
              <a-button
                type="link"
                :loading="dingtalkCallbackLoading"
                @click="handleDingtalkLogin"
              >
                <template v-if="!dingtalkCallbackLoading">
                  <svg viewBox="0 0 1024 1024" width="1em" height="1em" style="vertical-align: -0.125em; margin-right: 4px;"><path d="M512 64C264.6 64 64 264.6 64 512s200.6 448 448 448 448-200.6 448-448S759.4 64 512 64zm227 385.3c-1 4.2-3.5 10.4-7 17.8h.1l-.4.7c-20.3 43.1-73.1 127.7-73.1 127.7s-.1-.2-.3-.5l-15.5 26.8h74.5L575.1 810l32.3-128h-58.6l20.4-84.7c-16.5 3.9-35.9 9.4-59 16.8 0 0-31.2 18.2-89.9-35 0 0-39.6-34.7-16.6-43.4 9.8-3.7 47.4-8.4 77-12.3 40-5.4 64.6-8.2 64.6-8.2S422 517 392.7 512.5c-29.3-4.6-65.3-15.8-84-44.9 0 0-34.1-37.8 12.4-29.4 0 0 63.1 12.3 107.5 3.7 44.4-8.6 72.1-26.3 72.1-26.3s-45.7 2.5-92.9-8.6c-47.2-11.1-75.6-39.8-75.6-39.8s-35.7-31.3-14.2-29.4c21.5 1.9 63.1 16.8 100.2 18.4 37.1 1.5 79.6-7.6 79.6-7.6s-68.5-21.2-95.4-39.8c-26.8-18.6-48.6-49.4-48.6-49.4s-31.3-41.5 13-24.5c0 0 78.4 38 131.2 41.5 18.3 1.2 30.4-4.2 39.2-9.5z" fill="currentColor"/></svg>
                  钉钉扫码登录
                </template>
                <template v-else>
                  正在处理钉钉登录...
                </template>
              </a-button>
            </div>
          </div>

          <div class="login-industry-chips" aria-label="主营业务">
            <span>运单</span>
            <span>仓储</span>
            <span>结算</span>
          </div>

          <div v-if="loading" class="login-card-overlay">
            <a-spin size="large" />
            <p class="login-card-overlay-text">正在验证...</p>
          </div>
        </div>
      </div>
    </div>

    <OrgSelectModal
      v-model="orgSelectVisible"
      :organizations="orgList"
      @select="handleOrgSelected"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { message } from 'ant-design-vue'
import type { FormInstance } from 'ant-design-vue'
import type { Rule } from 'ant-design-vue/es/form'
import { UserOutlined, LockOutlined } from '@ant-design/icons-vue'
import { useUserStore } from '@/stores/user'
import { useOrgContextStore } from '@/stores/orgContext'
import { useEnterpriseInfoStore } from '@/stores/enterpriseInfo'
import { useSecurityStore } from '@/stores/security'
import { checkDbConnectionStatus } from '@/api/system'
import { getDingtalkConfig } from '@/api/auth'
import OrgSelectModal from '@/components/OrgSelectModal.vue'
import type { UserOrganizationDto } from '@/types/organization'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const orgContextStore = useOrgContextStore()
const enterpriseInfoStore = useEnterpriseInfoStore()
const securityStore = useSecurityStore()

type LoginNextAction =
  | { type: 'redirect'; redirect: string }
  | { type: 'org-selection' }

const formRef = ref<FormInstance>()
const loading = ref(false)
const rememberAccount = ref(false)
const dbConfigured = ref(true)
const orgSelectVisible = ref(false)
const orgList = ref<UserOrganizationDto[]>([])
const dingtalkEnabled = ref(false)
const dingtalkCallbackLoading = ref(false)
const dingtalkAppKey = ref('')
const dingtalkRedirectUri = ref('')

const form = reactive({
  username: 'admin',
  password: 'admin123',
})

const REMEMBER_ACCOUNT_KEY = 'stotop_remember_account'

onMounted(async () => {
  const savedAccount = localStorage.getItem(REMEMBER_ACCOUNT_KEY)
  if (savedAccount) {
    form.username = savedAccount
    rememberAccount.value = true
  }

  // 非阻塞加载企业信息，API返回后自动更新左栏显示
  enterpriseInfoStore.fetchEnterpriseInfo()

  const [, dingtalkConfig] = await Promise.allSettled([
    checkDbConnectionStatus().then((res: any) => {
      dbConfigured.value = res?.hasSystemConnection ?? true
    }).catch(() => {
      dbConfigured.value = true
    }),
    getDingtalkConfig().then(cfg => {
      dingtalkEnabled.value = cfg.enabled
      dingtalkAppKey.value = cfg.appKey || ''
      dingtalkRedirectUri.value = cfg.redirectUri || window.location.origin + '/login'
    }).catch(() => {
      dingtalkEnabled.value = false
    }),
  ])
  void dingtalkConfig

  const authCode = route.query.authCode as string | undefined
  if (authCode) {
    const savedState = sessionStorage.getItem('dingtalk_oauth_state')
    const returnedState = route.query.state as string | undefined
    if (savedState && returnedState && savedState !== returnedState) {
      message.error('钉钉登录失败：state 校验不通过，请重试')
      router.replace({ path: '/login' })
      return
    }
    sessionStorage.removeItem('dingtalk_oauth_state')

    dingtalkCallbackLoading.value = true
    try {
      await userStore.dingtalkLogin(authCode)
      await router.replace({ path: '/login' })
      await runPostLoginTransition()
    } catch {
      message.error('钉钉登录失败，请重试')
      router.replace({ path: '/login' })
    } finally {
      dingtalkCallbackLoading.value = false
    }
  }
})

const rules: Record<string, Rule[]> = {
  username: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码不少于6位', trigger: 'blur' },
  ],
}

function getRedirectTarget() {
  return (route.query.redirect as string) || '/'
}

async function loadOrgsAndRedirect(): Promise<LoginNextAction> {
  await orgContextStore.fetchOrganizations()
  const orgs = orgContextStore.organizations
  const redirect = getRedirectTarget()

  if (orgs.length === 0) {
    return { type: 'redirect', redirect }
  }

  if (orgs.length === 1) {
    await orgContextStore.doSwitchOrganization(orgs[0].orgId)
    return { type: 'redirect', redirect }
  }

  const primary = orgContextStore.primaryOrg
  if (primary) {
    await orgContextStore.doSwitchOrganization(primary.orgId)
    return { type: 'redirect', redirect }
  }

  orgList.value = orgs
  orgSelectVisible.value = true
  return { type: 'org-selection' }
}

async function runPostLoginTransition() {
  loading.value = true
  try {
    const nextAction = await loadOrgsAndRedirect()
    if (nextAction.type === 'redirect') {
      // 登录成功后启动空闲检测（进入主界面前初始化）
      await securityStore.fetchSecurityConfig()
      securityStore.initIdleDetection()
      await router.push(nextAction.redirect)
      return
    }
    // org-selection：内联组织选择面板由 orgSelectVisible 触发，留在本页
  } finally {
    loading.value = false
  }
}

async function handleLogin() {
  if (!formRef.value || loading.value) return
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  loading.value = true
  try {
    await userStore.login({ account: form.username, password: form.password })
    if (rememberAccount.value) {
      localStorage.setItem(REMEMBER_ACCOUNT_KEY, form.username)
    } else {
      localStorage.removeItem(REMEMBER_ACCOUNT_KEY)
    }

    await runPostLoginTransition()
  } catch {
    // 错误已在 request.ts 中处理
  } finally {
    loading.value = false
  }
}

async function handleOrgSelected(orgId: number) {
  try {
    const data = await orgContextStore.doSwitchOrganization(orgId)
    if (data) {
      await router.push(getRedirectTarget())
    } else {
      message.error('组织切换失败，请重试')
    }
  } catch {
    message.error('登录过程出错，请重试')
  } finally {
    orgSelectVisible.value = false
  }
}

function handleForgotPassword() {
  message.info('请联系系统管理员重置密码')
}

async function handleDingtalkLogin() {
  if (!dingtalkEnabled.value) {
    message.warning('钉钉登录尚未配置，请联系管理员')
    return
  }
  if (!dingtalkAppKey.value) {
    message.warning('钉钉登录配置未就绪，请稍后重试')
    return
  }
  const state = Math.random().toString(36).substring(2) + Date.now().toString(36)
  sessionStorage.setItem('dingtalk_oauth_state', state)

  const redirectUri = dingtalkRedirectUri.value || window.location.origin + '/login'
  const authUrl =
    `https://login.dingtalk.com/oauth2/auth` +
    `?client_id=${encodeURIComponent(dingtalkAppKey.value)}` +
    `&response_type=code` +
    `&scope=openid` +
    `&redirect_uri=${encodeURIComponent(redirectUri)}` +
    `&state=${encodeURIComponent(state)}` +
    `&prompt=consent`
  window.location.href = authUrl
}
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

// ======================================================
// 登录页外层
// ======================================================
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: stretch;
  background: #1A1B22;
  position: relative;
  overflow: hidden;
}

// ======================================================
// 左右分栏
// ======================================================
.login-split {
  display: flex;
  width: 100%;
  min-height: 100vh;
}

// ======================================================
// 左侧品牌区
// ======================================================
.login-brand {
  flex: 0 0 45%;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 56px 52px 40px;
  position: relative;
  overflow: hidden;
  // 静态精致深墨外壳，跟随权威外壳令牌
  background: var(--topbar-ink, #1F2430);
}

.brand-inner {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  gap: 32px;
}

.brand-logo {
  display: flex;
  align-items: center;
}

.brand-logo-img {
  max-height: 56px;
  max-width: 240px;
  object-fit: contain;
}

.brand-logo-text {
  font-size: 36px;
  font-weight: 800;
  color: #ffffff;
  letter-spacing: 5px;
}

.brand-tagline {
  margin: 0;
  font-size: 22px;
  font-weight: 700;
  line-height: 1.35;
  letter-spacing: -0.3px;
  color: rgba(255, 255, 255, 0.92);
}

.brand-features {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.brand-feature-item {
  background: rgba(255, 255, 255, 0.10);
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 8px;
  padding: 8px 12px;
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  font-size: $font-size-sm2;
  color: rgba(255, 255, 255, 0.78);
  letter-spacing: 0.5px;
}

.brand-feature-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--color-primary, #E85E00);
  flex: 0 0 auto;
}

.brand-footer {
  position: relative;
  z-index: 1;
  opacity: 0.5;
  font-size: 11px;
  color: rgba(255, 255, 255, 0.5);
  letter-spacing: 1.5px;
}

// ======================================================
// 右侧表单区
// ======================================================
.login-form-panel {
  flex: 1 1 auto;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #FAFAFA;
  position: relative;

  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 3px;
    background: var(--color-primary, #E85E00);
    z-index: 2;
  }
}

.login-form-inner {
  width: 400px;
  padding: 34px 40px 30px;
  border-radius: 12px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.06);
  border: 1px solid #F0F0F0;
  background: #FFFFFF;
  position: relative;
}

.login-system-status {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 22px;
  color: $text-secondary;
  font-size: 12px;
}

.system-status-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  height: 24px;
  padding: 0 10px;
  border-radius: 999px;
  color: #228a4a;
  background: #effaf3;
  border: 1px solid #d8f0df;

  &::before {
    content: '';
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #22a551;
  }
}

.login-form-header {
  margin-bottom: 28px;

  .login-form-title {
    font-size: 26px;
    font-weight: 700;
    color: rgba(0, 0, 0, 0.88);
    margin: 0 0 8px;
    letter-spacing: 0.5px;
  }

  .login-form-subtitle {
    font-size: $font-size-sm2;
    color: rgba(0, 0, 0, 0.55); // 从约0.45提高到0.55，确保WCAG AA 4.5:1
    margin: 0;
  }
}

// ======================================================
// 表单公共
// ======================================================
.login-form {
  .ant-form-item {
    margin-bottom: $spacing-md;
  }

  :deep(.ant-input-affix-wrapper) {
    transition: border-color 0.3s ease, box-shadow 0.3s ease;
    border: 1px solid var(--border, #E6E8EB);
    border-radius: var(--radius-md, 6px);
  }

  :deep(.ant-input-affix-wrapper:focus),
  :deep(.ant-input-affix-wrapper.ant-input-affix-wrapper-focused) {
    border-color: var(--color-primary, #E85E00);
    box-shadow: 0 0 0 2px var(--color-primary-border, rgba(232, 94, 0, 0.30));
  }

  .remember-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;

    :deep(.ant-checkbox) + span {
      font-size: $font-size-sm;
      color: $text-regular;
    }

    .forgot-password {
      font-size: $font-size-sm;
      color: rgba(0, 0, 0, 0.45);
      cursor: pointer;
      transition: color 0.2s ease;

      &:hover {
        color: var(--color-primary, #E85E00);
      }
    }
  }
}

// 焦点指示器优化
.login-form :deep(*:focus-visible) {
  outline: 2px solid var(--color-primary, #E85E00);
  outline-offset: 2px;
  border-radius: var(--radius-sm, 4px);
}

.login-btn {
  width: 100%;
  height: 40px;
  font-size: 15px;
  font-weight: 600;
  background: var(--color-primary, #E85E00);
  border: none;
  border-radius: var(--radius-lg, 8px);
  letter-spacing: 2px;
  transition: background 0.2s ease;

  &:hover {
    background: var(--color-primary-hover, #FF6700);
  }
}

.login-footer {
  margin-top: 8px;

  .other-login {
    display: flex;
    justify-content: center;
  }
}

.login-industry-chips {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  margin-top: 16px;

  span {
    height: 32px;
    border-radius: 8px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: $text-secondary;
    background: #f7f9fb;
    border: 1px solid #edf0f4;
    font-size: 12px;
  }
}

// 加载過罩层
.login-card-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 14px;
  background: rgba(255, 255, 255, 0.8);
  backdrop-filter: blur(2px);
  z-index: 3;

  :deep(.ant-spin-dot-item) {
    background-color: var(--color-primary, #E85E00);
  }
}

.login-card-overlay-text {
  margin: 0;
  color: $text-primary;
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 0.08em;
}

// 响应式：小屏幕隐藏品牌区
@media (max-width: 768px) {
  .login-brand {
    display: none;
  }

  .login-form-panel {
    background: #1E1F26;
  }

  .login-form-inner {
    padding: 48px 24px;
  }

  .login-form-header .login-form-title {
    color: rgba(255, 255, 255, 0.9);
  }

  .login-form-header .login-form-subtitle {
    color: rgba(255, 255, 255, 0.45);
  }
}
</style>


<style lang="scss">
// 消除登录输入框内部 input 边框与外层 wrapper 重复的问题
.login-form-panel .ant-input-affix-wrapper .ant-input,
.login-form-panel .ant-input-affix-wrapper .ant-input:focus {
  border: none !important;
  box-shadow: none !important;
  outline: none !important;
}

.login-form-panel .login-btn.ant-btn-primary {
  background: var(--color-primary, #E85E00) !important;
  border: none;

  &:hover,
  &:focus {
    background: var(--color-primary-hover, #FF6700) !important;
  }

  &:active {
    background: var(--color-primary-active, #C94E00) !important;
  }
}
</style>

