<template>
  <div class="login-page">
    <!-- 左右分栏容器 -->
    <div class="login-split" :class="{ 'is-transitioning': transitioning }">

      <!-- 左侧：品牌区 -->
      <div class="login-brand" :class="`brand-${seasonTheme.season}`" :style="brandStyle">
        <!-- 季节粒子动画层 -->
        <div class="season-particles" :class="seasonTheme.particleClass" aria-hidden="true">
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
          <span class="particle"></span>
        </div>
        <div class="logistics-route-map" aria-hidden="true">
          <svg viewBox="0 0 720 220" focusable="false">
            <path class="route-path" d="M18 166 C118 84, 202 196, 312 108 S494 60, 682 126" />
            <path class="route-path route-path--secondary" d="M92 42 C192 82, 254 28, 348 82 S508 174, 648 58" />
            <line class="warehouse-line" x1="36" y1="198" x2="210" y2="198" />
            <line class="warehouse-line" x1="36" y1="178" x2="210" y2="178" />
            <line class="warehouse-line" x1="52" y1="160" x2="52" y2="210" />
            <line class="warehouse-line" x1="96" y1="160" x2="96" y2="210" />
            <line class="warehouse-line" x1="140" y1="160" x2="140" y2="210" />
            <circle class="hub-node" cx="18" cy="166" r="5" />
            <circle class="hub-node" cx="312" cy="108" r="5" />
            <circle class="hub-node" cx="682" cy="126" r="5" />
            <circle class="hub-node" cx="648" cy="58" r="5" />
          </svg>
        </div>
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
        <!-- 抽象几何装饰图形（左栏中下部） -->
        <div class="brand-decoration" aria-hidden="true">
          <div class="deco-ring deco-ring-1"></div>
          <div class="deco-ring deco-ring-2"></div>
          <div class="deco-ring deco-ring-3"></div>
          <div class="deco-line deco-line-1"></div>
          <div class="deco-line deco-line-2"></div>
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
                :disabled="loading || transitioning"
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
                :disabled="loading || transitioning"
              >
                <template #prefix><LockOutlined /></template>
              </a-input-password>
            </a-form-item>

            <a-form-item>
              <div class="remember-row">
                <a-checkbox v-model:checked="rememberAccount" :disabled="loading || transitioning">记住账号</a-checkbox>
                <a class="forgot-password" @click="handleForgotPassword">忘记密码？</a>
              </div>
            </a-form-item>

            <a-form-item>
              <a-button
                type="primary"
                class="login-btn"
                :loading="loading"
                :disabled="!dbConfigured || loading || transitioning"
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
                :disabled="transitioning"
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

    <Transition name="screen-fade">
      <div v-if="transitioning" class="login-transition-screen">
        <div class="login-transition-content">
          <img
            v-if="enterpriseInfoStore.hasLogo"
            :src="enterpriseInfoStore.logoUrl"
            :alt="enterpriseInfoStore.displayName"
            class="transition-logo-img"
          />
          <div v-else class="transition-logo-text">{{ enterpriseInfoStore.displayName }}</div>

          <div class="transition-ring" />

          <Transition name="step-fade" mode="out-in">
            <p :key="currentTransitionStep" class="transition-step-text">{{ currentTransitionStep }}</p>
          </Transition>

          <div class="transition-stage-list">
            <div
              v-for="(text, i) in TRANSITION_STEP_TEXTS"
              :key="text"
              class="transition-stage-item"
              :class="{
                active: i === transitionStepIndex,
                done: i < transitionStepIndex
              }"
            >
              <span class="transition-stage-dot"></span>
              <span>{{ text }}</span>
            </div>
          </div>

          <div class="transition-dots" aria-hidden="true">
            <div
              v-for="(_, i) in TRANSITION_STEP_TEXTS"
              :key="i"
              class="dot"
              :class="{
                active: i === transitionStepIndex,
                done: i < transitionStepIndex
              }"
            />
          </div>
        </div>
      </div>
    </Transition>

    <OrgSelectModal
      v-model="orgSelectVisible"
      :organizations="orgList"
      @select="handleOrgSelected"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, onBeforeUnmount } from 'vue'
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
import { getCurrentSeasonTheme } from '@/utils/seasonTheme'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const orgContextStore = useOrgContextStore()
const enterpriseInfoStore = useEnterpriseInfoStore()
const securityStore = useSecurityStore()

// 季节主题（按当前月份自动切换）
const seasonTheme = getCurrentSeasonTheme()
const brandStyle = {
  '--season-bg-1': seasonTheme.bgGradient[0],
  '--season-bg-2': seasonTheme.bgGradient[1],
  '--season-bg-3': seasonTheme.bgGradient[2],
  '--season-glow-primary': seasonTheme.glowPrimary,
  '--season-glow-secondary': seasonTheme.glowSecondary,
  '--season-dot-1': seasonTheme.dotGradient[0],
  '--season-dot-2': seasonTheme.dotGradient[1],
} as Record<string, string>

const TRANSITION_STEP_TEXTS = [
  '已加载组织与权限',
  '已恢复网点、仓库与快捷入口',
  '正在同步今日运单与待办',
  '即将进入工作台',
] as const
const TRANSITION_STEP_DELAYS = [0, 450, 900, 1350] as const
const MIN_TRANSITION_DURATION = 1500

type LoginNextAction =
  | { type: 'redirect'; redirect: string }
  | { type: 'org-selection' }

const formRef = ref<FormInstance>()
const loading = ref(false)
const transitioning = ref(false)
const rememberAccount = ref(false)
const dbConfigured = ref(true)
const orgSelectVisible = ref(false)
const orgList = ref<UserOrganizationDto[]>([])
const dingtalkEnabled = ref(false)
const dingtalkCallbackLoading = ref(false)
const dingtalkAppKey = ref('')
const dingtalkRedirectUri = ref('')
const transitionStepIndex = ref(0)
let transitionStepTimers: Array<ReturnType<typeof window.setTimeout>> = []

const currentTransitionStep = computed(
  () => TRANSITION_STEP_TEXTS[transitionStepIndex.value] ?? TRANSITION_STEP_TEXTS[0],
)

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

onBeforeUnmount(() => {
  clearTransitionStepPlayback()
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

function wait(ms: number) {
  return new Promise<void>(resolve => {
    window.setTimeout(resolve, ms)
  })
}

function clearTransitionStepPlayback() {
  transitionStepTimers.forEach(timer => window.clearTimeout(timer))
  transitionStepTimers = []
}

function startTransitionStepPlayback() {
  clearTransitionStepPlayback()
  transitionStepIndex.value = 0
  transitionStepTimers = TRANSITION_STEP_DELAYS.slice(1).map((delay, index) => {
    return window.setTimeout(() => {
      transitionStepIndex.value = index + 1
    }, delay)
  })
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
  loading.value = false
  transitioning.value = true
  startTransitionStepPlayback()

  try {
    const [nextAction] = await Promise.all([
      loadOrgsAndRedirect(),
      wait(MIN_TRANSITION_DURATION),
    ])

    if (nextAction.type === 'redirect') {
      // 登录成功后启动空闲检测（进入主界面前初始化）
      await securityStore.fetchSecurityConfig()
      securityStore.initIdleDetection()
      await router.push(nextAction.redirect)
      return
    }

    transitioning.value = false
    clearTransitionStepPlayback()
  } catch (error) {
    transitioning.value = false
    clearTransitionStepPlayback()
    throw error
  }
}

async function handleLogin() {
  if (!formRef.value || loading.value || transitioning.value) return
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
  transition: opacity 0.4s ease, transform 0.4s ease;

  &.is-transitioning {
    opacity: 0;
    transform: scale(0.98);
    pointer-events: none;
  }
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
  // 季节主题渐变背景（变量由脚本注入）
  background: linear-gradient(
    145deg,
    var(--season-bg-1, #1E1F26) 0%,
    var(--season-bg-2, #23242D) 60%,
    var(--season-bg-3, #1A1B22) 100%
  );
  transition: background 0.6s ease;

  // 右上装饰光晓
  &::before {
    content: '';
    position: absolute;
    width: 500px;
    height: 500px;
    background: radial-gradient(ellipse at 90% 10%, var(--season-glow-primary, rgba(255, 103, 0, 0.18)), transparent 60%);
    top: -120px;
    right: -80px;
    pointer-events: none;
  }

  &::after {
    content: '';
    position: absolute;
    width: 350px;
    height: 350px;
    background: radial-gradient(ellipse at 10% 90%, var(--season-glow-secondary, rgba(99, 102, 241, 0.12)), transparent 55%);
    bottom: -80px;
    left: -60px;
    pointer-events: none;
  }
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
  filter: drop-shadow(0 4px 16px rgba(255, 103, 0, 0.25));
}

.brand-logo-text {
  font-size: 36px;
  font-weight: 800;
  color: #ffffff;
  letter-spacing: 5px;
  background: linear-gradient(135deg, #FF6700 0%, #FFAA44 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  text-shadow: none;
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
  background: linear-gradient(
    135deg,
    var(--season-dot-1, #FF6700),
    var(--season-dot-2, #FFAA44)
  );
  flex: 0 0 auto;
  box-shadow: 0 0 6px var(--season-dot-1, rgba(255, 103, 0, 0.5));
}

// ======================================================
// 季节粒子动画层
// ======================================================
.season-particles {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
  z-index: 0;

  .particle {
    position: absolute;
    display: block;
    will-change: transform, opacity;
  }
}

.logistics-route-map {
  position: absolute;
  left: 52px;
  right: 72px;
  bottom: 92px;
  height: 210px;
  opacity: 0.34;
  z-index: 0;
  pointer-events: none;

  svg {
    width: 100%;
    height: 100%;
    overflow: visible;
  }

  .route-path {
    fill: none;
    stroke: var(--season-glow-primary, rgba(255, 138, 52, 0.5));
    stroke-width: 1.5;
    stroke-dasharray: 7 9;
  }

  .route-path--secondary {
    opacity: 0.65;
  }

  .hub-node {
    fill: var(--season-dot-1, #FF6700);
    filter: drop-shadow(0 0 8px rgba(255, 103, 0, 0.55));
  }

  .warehouse-line {
    stroke: rgba(255, 255, 255, 0.16);
    stroke-width: 1;
  }
}

// --- 春：嵫绿气泡缓缓上浮 ---
.particles-spring {
  .particle {
    border-radius: 50%;
    background: radial-gradient(circle at 30% 30%, rgba(210, 250, 200, 0.95), rgba(140, 212, 138, 0.2) 70%);
    box-shadow: 0 0 10px rgba(140, 212, 138, 0.45);
    bottom: -24px;
    opacity: 0;
    animation: spring-rise 8s linear infinite;
  }
  .particle:nth-child(1) { left: 6%;  width: 7px;  height: 7px;  animation-duration: 8s;   animation-delay: 0s; }
  .particle:nth-child(2) { left: 18%; width: 11px; height: 11px; animation-duration: 7s;   animation-delay: 2s; }
  .particle:nth-child(3) { left: 30%; width: 5px;  height: 5px;  animation-duration: 9s;   animation-delay: 4s; }
  .particle:nth-child(4) { left: 44%; width: 9px;  height: 9px;  animation-duration: 7.5s; animation-delay: 1.2s; }
  .particle:nth-child(5) { left: 58%; width: 6px;  height: 6px;  animation-duration: 8.5s; animation-delay: 3.4s; }
  .particle:nth-child(6) { left: 72%; width: 10px; height: 10px; animation-duration: 7.2s; animation-delay: 5.8s; }
  .particle:nth-child(7) { left: 84%; width: 5px;  height: 5px;  animation-duration: 8.8s; animation-delay: 0.6s; }
  .particle:nth-child(8) { left: 92%; width: 8px;  height: 8px;  animation-duration: 7.8s; animation-delay: 2.6s; }
  // 极小点缀粒子（9-12）
  .particle:nth-child(9)  { left: 12%; width: 3px; height: 3px; animation-duration: 8.2s; animation-delay: 1.8s; opacity: 0; }
  .particle:nth-child(10) { left: 38%; width: 4px; height: 4px; animation-duration: 7.6s; animation-delay: 4.6s; opacity: 0; }
  .particle:nth-child(11) { left: 64%; width: 3px; height: 3px; animation-duration: 8.6s; animation-delay: 2.2s; opacity: 0; }
  .particle:nth-child(12) { left: 88%; width: 4px; height: 4px; animation-duration: 7.4s; animation-delay: 5.2s; opacity: 0; }
  .particle:nth-child(9),
  .particle:nth-child(10),
  .particle:nth-child(11),
  .particle:nth-child(12) {
    box-shadow: 0 0 4px rgba(140, 212, 138, 0.25);
    animation-name: spring-rise-faint;
  }
}

@keyframes spring-rise-faint {
  0%   { transform: translate3d(0, 0, 0);            opacity: 0; }
  15%  { opacity: 0.28; }
  50%  { transform: translate3d(12px, -45vh, 0);     opacity: 0.22; }
  85%  { opacity: 0.2; }
  100% { transform: translate3d(-8px, -100vh, 0);    opacity: 0; }
}

@keyframes spring-rise {
  0%   { transform: translate3d(0, 0, 0);            opacity: 0; }
  12%  { opacity: 0.7; }
  50%  { transform: translate3d(18px, -45vh, 0);     opacity: 0.55; }
  88%  { opacity: 0.4; }
  100% { transform: translate3d(-12px, -100vh, 0);   opacity: 0; }
}

// --- 夏：金色光斑闪烁 ---
.particles-summer {
  .particle {
    border-radius: 50%;
    background: radial-gradient(circle, rgba(255, 235, 150, 1), rgba(255, 196, 76, 0) 70%);
    box-shadow: 0 0 14px rgba(255, 210, 110, 0.55);
    opacity: 0;
    animation: summer-twinkle 7s ease-in-out infinite;
  }
  .particle:nth-child(1) { top: 14%; left: 12%; width: 6px;  height: 6px;  animation-delay: 0s;   animation-duration: 6.2s; }
  .particle:nth-child(2) { top: 28%; left: 78%; width: 10px; height: 10px; animation-delay: 0.8s; animation-duration: 7.4s; }
  .particle:nth-child(3) { top: 46%; left: 22%; width: 5px;  height: 5px;  animation-delay: 1.6s; animation-duration: 6.6s; }
  .particle:nth-child(4) { top: 36%; left: 60%; width: 8px;  height: 8px;  animation-delay: 2.2s; animation-duration: 7.8s; }
  .particle:nth-child(5) { top: 62%; left: 14%; width: 7px;  height: 7px;  animation-delay: 0.4s; animation-duration: 7.2s; }
  .particle:nth-child(6) { top: 70%; left: 70%; width: 5px;  height: 5px;  animation-delay: 1.2s; animation-duration: 6.4s; }
  .particle:nth-child(7) { top: 82%; left: 40%; width: 9px;  height: 9px;  animation-delay: 2.8s; animation-duration: 7.6s; }
  .particle:nth-child(8) { top: 18%; left: 50%; width: 6px;  height: 6px;  animation-delay: 3.4s; animation-duration: 6.8s; }
  // 极小点缀粒子（9-12）
  .particle:nth-child(9)  { top: 22%; left: 32%; width: 3px; height: 3px; animation-delay: 0.6s; animation-duration: 6.5s; }
  .particle:nth-child(10) { top: 54%; left: 86%; width: 4px; height: 4px; animation-delay: 2.0s; animation-duration: 7.5s; }
  .particle:nth-child(11) { top: 76%; left: 56%; width: 3px; height: 3px; animation-delay: 1.4s; animation-duration: 6.2s; }
  .particle:nth-child(12) { top: 40%; left: 8%;  width: 4px; height: 4px; animation-delay: 3.0s; animation-duration: 7.8s; }
  .particle:nth-child(9),
  .particle:nth-child(10),
  .particle:nth-child(11),
  .particle:nth-child(12) {
    box-shadow: 0 0 6px rgba(255, 210, 110, 0.3);
    animation-name: summer-twinkle-faint;
  }
}

@keyframes summer-twinkle-faint {
  0%, 100% { opacity: 0;    transform: scale(0.6); }
  50%      { opacity: 0.28; transform: scale(1.05); }
}

@keyframes summer-twinkle {
  0%, 100% { opacity: 0;    transform: scale(0.7); }
  50%      { opacity: 0.85; transform: scale(1.25); }
}

// --- 秋：叶片飘落 ---
.particles-autumn {
  .particle {
    width: 11px;
    height: 14px;
    border-radius: 0 100% 0 100%;
    background: linear-gradient(135deg, #FFB266 0%, #FF7A1F 60%, #B84A0F 100%);
    box-shadow: 0 0 6px rgba(255, 138, 40, 0.4);
    top: -32px;
    opacity: 0;
    transform-origin: 50% 50%;
    animation: autumn-fall 9s linear infinite;
  }
  .particle:nth-child(1) { left: 8%;  width: 11px; height: 14px; animation-duration: 9s;   animation-delay: 0s; }
  .particle:nth-child(2) { left: 20%; width: 9px;  height: 12px; animation-duration: 10s;  animation-delay: 2.4s; }
  .particle:nth-child(3) { left: 33%; width: 13px; height: 16px; animation-duration: 8s;   animation-delay: 4.8s; }
  .particle:nth-child(4) { left: 46%; width: 10px; height: 13px; animation-duration: 9.5s; animation-delay: 1.6s; }
  .particle:nth-child(5) { left: 58%; width: 8px;  height: 11px; animation-duration: 8.4s; animation-delay: 3.2s; }
  .particle:nth-child(6) { left: 70%; width: 12px; height: 15px; animation-duration: 9.8s; animation-delay: 5.6s; }
  .particle:nth-child(7) { left: 82%; width: 11px; height: 14px; animation-duration: 8.6s; animation-delay: 0.8s; }
  .particle:nth-child(8) { left: 92%; width: 9px;  height: 12px; animation-duration: 9.2s; animation-delay: 2.0s; }
  // 极小点缀粒子（9-12）
  .particle:nth-child(9)  { left: 14%; width: 3px; height: 4px; animation-duration: 9.4s; animation-delay: 1.0s; }
  .particle:nth-child(10) { left: 40%; width: 4px; height: 4px; animation-duration: 8.2s; animation-delay: 3.6s; }
  .particle:nth-child(11) { left: 65%; width: 3px; height: 4px; animation-duration: 9.6s; animation-delay: 2.2s; }
  .particle:nth-child(12) { left: 88%; width: 4px; height: 4px; animation-duration: 8.8s; animation-delay: 5.0s; }
  .particle:nth-child(9),
  .particle:nth-child(10),
  .particle:nth-child(11),
  .particle:nth-child(12) {
    background: linear-gradient(135deg, #FFB266 0%, #FF7A1F 100%);
    box-shadow: 0 0 3px rgba(255, 138, 40, 0.25);
    animation-name: autumn-fall-faint;
  }
}

@keyframes autumn-fall-faint {
  0%   { transform: translate3d(0, 0, 0)         rotate(0deg);    opacity: 0; }
  12%  { opacity: 0.28; }
  50%  { transform: translate3d(10px, 50vh, 0)   rotate(220deg);  opacity: 0.22; }
  90%  { opacity: 0.2; }
  100% { transform: translate3d(-12px, 110vh, 0) rotate(440deg);  opacity: 0; }
}

@keyframes autumn-fall {
  0%   { transform: translate3d(0, 0, 0)         rotate(0deg);    opacity: 0; }
  10%  { opacity: 0.85; }
  25%  { transform: translate3d(-22px, 25vh, 0)  rotate(120deg); }
  50%  { transform: translate3d(18px, 50vh, 0)   rotate(240deg); }
  75%  { transform: translate3d(-14px, 75vh, 0)  rotate(360deg); }
  92%  { opacity: 0.6; }
  100% { transform: translate3d(22px, 110vh, 0)  rotate(540deg);  opacity: 0; }
}

// --- 冬：雪点飘落 ---
.particles-winter {
  .particle {
    border-radius: 50%;
    background: radial-gradient(circle, #ffffff 0%, rgba(210, 230, 255, 0.4) 60%, rgba(160, 200, 240, 0) 100%);
    box-shadow: 0 0 8px rgba(220, 235, 255, 0.55);
    top: -24px;
    opacity: 0;
    animation: winter-snow 8s linear infinite;
  }
  .particle:nth-child(1) { left: 7%;  width: 5px; height: 5px; animation-duration: 8s;   animation-delay: 0s; }
  .particle:nth-child(2) { left: 19%; width: 8px; height: 8px; animation-duration: 7s;   animation-delay: 2.6s; }
  .particle:nth-child(3) { left: 31%; width: 4px; height: 4px; animation-duration: 9s;   animation-delay: 4.2s; }
  .particle:nth-child(4) { left: 44%; width: 7px; height: 7px; animation-duration: 7.5s; animation-delay: 1.4s; }
  .particle:nth-child(5) { left: 57%; width: 5px; height: 5px; animation-duration: 8.6s; animation-delay: 3.6s; }
  .particle:nth-child(6) { left: 71%; width: 9px; height: 9px; animation-duration: 7.2s; animation-delay: 5.4s; }
  .particle:nth-child(7) { left: 84%; width: 4px; height: 4px; animation-duration: 8.8s; animation-delay: 0.7s; }
  .particle:nth-child(8) { left: 93%; width: 6px; height: 6px; animation-duration: 7.8s; animation-delay: 2.2s; }
  // 极小点缀粒子（9-12）
  .particle:nth-child(9)  { left: 13%; width: 3px; height: 3px; animation-duration: 8.4s; animation-delay: 1.8s; }
  .particle:nth-child(10) { left: 38%; width: 4px; height: 4px; animation-duration: 7.6s; animation-delay: 4.6s; }
  .particle:nth-child(11) { left: 64%; width: 3px; height: 3px; animation-duration: 8.2s; animation-delay: 2.2s; }
  .particle:nth-child(12) { left: 88%; width: 4px; height: 4px; animation-duration: 7.4s; animation-delay: 5.0s; }
  .particle:nth-child(9),
  .particle:nth-child(10),
  .particle:nth-child(11),
  .particle:nth-child(12) {
    box-shadow: 0 0 4px rgba(220, 235, 255, 0.3);
    animation-name: winter-snow-faint;
  }
}

@keyframes winter-snow-faint {
  0%   { transform: translate3d(0, 0, 0);          opacity: 0; }
  15%  { opacity: 0.28; }
  50%  { transform: translate3d(12px, 50vh, 0);    opacity: 0.22; }
  100% { transform: translate3d(-10px, 110vh, 0);  opacity: 0; }
}

@keyframes winter-snow {
  0%   { transform: translate3d(0, 0, 0);          opacity: 0; }
  10%  { opacity: 0.85; }
  50%  { transform: translate3d(20px, 50vh, 0);    opacity: 0.7; }
  100% { transform: translate3d(-18px, 110vh, 0);  opacity: 0; }
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
// 抽象几何装饰图形（左栏中下部，增加层次与科技感）
// ======================================================
.brand-decoration {
  position: absolute;
  left: 0;
  right: 0;
  top: 60%;
  bottom: 15%;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;

  .deco-ring {
    position: absolute;
    border-radius: 50%;
    border-style: solid;
    background: transparent;
  }

  .deco-ring-1 {
    width: 120px;
    height: 120px;
    border-width: 2px;
    border-color: var(--season-glow-primary, rgba(255, 103, 0, 0.18));
    opacity: 0.18;
    left: 8%;
    top: 18%;
  }

  .deco-ring-2 {
    width: 80px;
    height: 80px;
    border-width: 2px;
    border-color: var(--season-glow-primary, rgba(255, 103, 0, 0.18));
    opacity: 0.14;
    left: 22%;
    top: 50%;
  }

  .deco-ring-3 {
    width: 56px;
    height: 56px;
    border-width: 1px;
    border-color: var(--season-glow-primary, rgba(255, 103, 0, 0.18));
    opacity: 0.22;
    left: 14%;
    top: 8%;
  }

  .deco-line {
    position: absolute;
    width: 1px;
    background: var(--season-glow-primary, rgba(255, 103, 0, 0.18));
  }

  .deco-line-1 {
    height: 80px;
    left: 38%;
    top: 20%;
    transform: rotate(35deg);
    opacity: 0.16;
  }

  .deco-line-2 {
    height: 64px;
    left: 18%;
    top: 70%;
    transform: rotate(-42deg);
    opacity: 0.14;
  }
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
    background: linear-gradient(90deg, #FF6700, #FF9A44);
    border-radius: 0;
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
    border: 1px solid #d9d9d9;
    border-radius: 6px;
  }

  :deep(.ant-input-affix-wrapper:focus),
  :deep(.ant-input-affix-wrapper.ant-input-affix-wrapper-focused) {
    border-color: #FF6700;
    box-shadow: 0 0 0 2px rgba(255, 103, 0, 0.1);
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
        color: #FF6700;
      }
    }
  }
}

// 焦点指示器优化
.login-form :deep(*:focus-visible) {
  outline: 2px solid rgba(255, 103, 0, 0.5);
  outline-offset: 2px;
  border-radius: 4px;
}

.login-btn {
  width: 100%;
  height: 40px;
  font-size: 15px;
  font-weight: 600;
  background: linear-gradient(135deg, #FF6700, #FF8533);
  border: none;
  border-radius: 8px;
  letter-spacing: 2px;
  transition: all 0.2s ease;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(255, 103, 0, 0.3);
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
    background-color: $color-primary;
  }
}

.login-card-overlay-text {
  margin: 0;
  color: $text-primary;
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 0.08em;
}

// ======================================================
// 登录过渡层
// ======================================================
.login-transition-screen {
  position: fixed;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #1A1B23;
  z-index: 9999;
  overflow: hidden;

  &::before,
  &::after {
    content: '';
    position: absolute;
    border-radius: 50%;
    background: rgba(255, 103, 0, 0.05);
  }

  &::before {
    width: 600px;
    height: 600px;
    top: -200px;
    right: -100px;
  }

  &::after {
    width: 400px;
    height: 400px;
    bottom: -150px;
    left: -100px;
  }
}

.login-transition-content {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 24px;
  padding: 48px 32px;
  text-align: center;
}

.transition-logo-img {
  max-width: 240px;
  max-height: 64px;
  object-fit: contain;
  filter: drop-shadow(0 10px 24px rgba(0, 0, 0, 0.18));
}

.transition-logo-text {
  font-size: 18px;
  color: rgba(255, 255, 255, 0.9);
  font-weight: 300;
  letter-spacing: 4px;
  margin-bottom: 32px;
  animation: transition-fade-in 0.4s ease both;
}

@keyframes transition-fade-in {
  from { opacity: 0; transform: translateY(6px); }
  to   { opacity: 1; transform: translateY(0); }
}

.transition-ring {
  width: 54px;
  height: 54px;
  border-radius: 50%;
  position: relative;
  background: conic-gradient(
    from 0deg,
    rgba(255, 103, 0, 0.04) 0deg,
    rgba(255, 103, 0, 0.3) 150deg,
    rgba(255, 154, 68, 0.9) 315deg,
    rgba(255, 103, 0, 0.04) 360deg
  );
  animation: ring-spin 1s linear infinite;
  box-shadow: 0 0 24px rgba(255, 103, 0, 0.18);
  -webkit-mask: radial-gradient(farthest-side, transparent calc(100% - 4px), #000 calc(100% - 3px));
  mask: radial-gradient(farthest-side, transparent calc(100% - 4px), #000 calc(100% - 3px));

  &::after {
    content: '';
    position: absolute;
    inset: 12px;
    border-radius: 50%;
    background: rgba(255, 103, 0, 0.08);
    filter: blur(8px);
  }
}

.transition-step-text {
  margin: 0;
  font-size: 13px;
  line-height: 1.4;
  color: rgba(255, 255, 255, 0.6);
  letter-spacing: 0.08em;
  margin-bottom: 20px;
  height: 20px;
}

.transition-stage-list {
  width: min(420px, calc(100vw - 48px));
  padding: 16px 18px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.065);
  border: 1px solid rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(8px);
}

.transition-stage-item {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 30px;
  color: rgba(255, 255, 255, 0.52);
  font-size: 13px;
  text-align: left;
  transition: color 0.25s ease;

  &.done {
    color: rgba(255, 255, 255, 0.62);
  }

  &.active {
    color: rgba(255, 255, 255, 0.92);
    font-weight: 600;
  }
}

.transition-stage-dot {
  width: 16px;
  height: 16px;
  border-radius: 50%;
  border: 1px solid rgba(255, 255, 255, 0.22);
  flex: 0 0 auto;
  position: relative;
}

.transition-stage-item.done .transition-stage-dot {
  border-color: rgba(53, 182, 90, 0.7);
  background: rgba(53, 182, 90, 0.16);

  &::after {
    content: '';
    position: absolute;
    inset: 4px;
    border-radius: 50%;
    background: #35b65a;
  }
}

.transition-stage-item.active .transition-stage-dot {
  border-color: #FF8533;
  background: rgba(255, 103, 0, 0.16);
  box-shadow: 0 0 16px rgba(255, 103, 0, 0.35);

  &::after {
    content: '';
    position: absolute;
    inset: 4px;
    border-radius: 50%;
    background: #FF8533;
  }
}

.transition-dots {
  display: flex;
  gap: 6px;
  justify-content: center;
  margin-top: 8px;

  .dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.2);
    transition: all 0.3s ease;

    &.active {
      background: #FF6700;
      transform: scale(1.3);
    }

    &.done {
      background: #FF6700;
      opacity: 0.5;
    }
  }
}

// ======================================================
// 过渡动画
// ======================================================
.screen-fade-enter-active,
.screen-fade-leave-active {
  transition: opacity 0.35s ease;
}

.screen-fade-enter-from,
.screen-fade-leave-to {
  opacity: 0;
}

.step-fade-enter-active,
.step-fade-leave-active {
  transition: opacity 0.28s ease, transform 0.28s ease;
}

.step-fade-enter-from,
.step-fade-leave-to {
  opacity: 0;
  transform: translateY(6px);
}

@keyframes ring-spin {
  to {
    transform: rotate(360deg);
  }
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
  background: linear-gradient(135deg, #FF6700, #FF8533) !important;
  border: none;
  box-shadow: 0 4px 12px rgba(255, 103, 0, 0.3);

  &:hover,
  &:focus {
    background: linear-gradient(135deg, #FF8533, #FF9A44) !important;
    box-shadow: 0 6px 16px rgba(255, 103, 0, 0.4);
    transform: translateY(-1px);
  }
}
</style>

