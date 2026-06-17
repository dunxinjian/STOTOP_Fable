<template>
  <div class="page-container">
    <PageHeader title="个人中心" description="管理个人资料和密码" />

    <a-row :gutter="24">
      <!-- 左侧：个人信息卡片 -->
      <a-col :xs="24" :lg="8">
        <a-card class="profile-card">
          <div class="avatar-section">
            <a-avatar :size="100" :src="userStore.userInfo?.avatar">
              {{ userStore.userInfo?.realName?.charAt(0) || 'U' }}
            </a-avatar>
            <h3 class="user-name">{{ userStore.userInfo?.realName || userStore.userInfo?.username }}</h3>
            <p class="user-role">{{ userStore.userInfo?.roleName || '普通用户' }}</p>
          </div>
          <a-divider />
          <div class="info-list">
            <div class="info-item">
              <span class="info-label">账号</span>
              <span class="info-value">{{ userStore.userInfo?.username }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">手机号</span>
              <span class="info-value">{{ userStore.userInfo?.phone || '-' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">邮箱</span>
              <span class="info-value">{{ userStore.userInfo?.email || '-' }}</span>
            </div>
          </div>
        </a-card>
      </a-col>

      <!-- 右侧：编辑表单 -->
      <a-col :xs="24" :lg="16">
        <!-- 基本信息编辑 -->
        <a-card title="基本信息" class="form-card">
          <a-form
            :model="profileForm"
            :rules="profileRules"
            ref="profileFormRef"
            layout="vertical"
          >
            <a-row :gutter="16">
              <a-col :span="12">
                <a-form-item label="真实姓名" name="realName">
                  <a-input
                    v-model:value="profileForm.realName"
                    placeholder="请输入真实姓名"
                    :maxLength="50"
                  />
                </a-form-item>
              </a-col>
              <a-col :span="12">
                <a-form-item label="邮箱" name="email">
                  <a-input
                    v-model:value="profileForm.email"
                    placeholder="请输入邮箱地址"
                    :maxLength="100"
                  />
                </a-form-item>
              </a-col>
            </a-row>
            <a-row :gutter="16">
              <a-col :span="12">
                <a-form-item label="手机号" name="phone">
                  <a-input
                    v-model:value="profileForm.phone"
                    placeholder="请输入手机号"
                    :maxLength="20"
                  />
                </a-form-item>
              </a-col>
              <a-col :span="12">
                <a-form-item label="头像链接" name="avatar">
                  <a-input
                    v-model:value="profileForm.avatar"
                    placeholder="请输入头像图片链接"
                    :maxLength="500"
                  />
                </a-form-item>
              </a-col>
            </a-row>
            <a-form-item>
              <a-button type="primary" @click="handleSaveProfile" :loading="savingProfile">
                <SaveOutlined />
                保存基本信息
              </a-button>
            </a-form-item>
          </a-form>
        </a-card>

        <!-- 密码修改 -->
        <a-card title="修改密码" class="form-card password-card">
          <a-form
            :model="passwordForm"
            :rules="passwordRules"
            ref="passwordFormRef"
            layout="vertical"
          >
            <a-form-item label="旧密码" name="oldPassword">
              <a-input-password
                v-model:value="passwordForm.oldPassword"
                placeholder="请输入旧密码"
              />
            </a-form-item>
            <a-form-item label="新密码" name="newPassword">
              <a-input-password
                v-model:value="passwordForm.newPassword"
                placeholder="请输入新密码"
              />
            </a-form-item>
            <a-form-item label="确认密码" name="confirmPassword">
              <a-input-password
                v-model:value="passwordForm.confirmPassword"
                placeholder="请再次输入新密码"
              />
            </a-form-item>
            <a-form-item>
              <a-button type="primary" danger @click="handleChangePassword" :loading="changingPassword">
                <LockOutlined />
                修改密码
              </a-button>
            </a-form-item>
          </a-form>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { SaveOutlined, LockOutlined } from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import { useUserStore } from '@/stores/user'
import type { Rule } from 'ant-design-vue/es/form'

const userStore = useUserStore()

// 表单引用
const profileFormRef = ref()
const passwordFormRef = ref()

// 加载状态
const savingProfile = ref(false)
const changingPassword = ref(false)

// 基本信息表单
const profileForm = reactive({
  realName: '',
  email: '',
  phone: '',
  avatar: '',
})

// 密码表单
const passwordForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
})

// 基本信息表单验证规则
const profileRules: Record<string, Rule[]> = {
  realName: [
    { required: true, message: '请输入真实姓名', trigger: 'blur' },
    { max: 50, message: '姓名最多50个字符', trigger: 'blur' },
  ],
  email: [
    { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' },
  ],
  phone: [
    { pattern: /^1[3-9]\d{9}$/, message: '请输入有效的手机号', trigger: 'blur' },
  ],
}

// 密码表单验证规则
const passwordRules: Record<string, Rule[]> = {
  oldPassword: [
    { required: true, message: '请输入旧密码', trigger: 'blur' },
  ],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    {
      validator: (_rule: any, value: string) => {
        if (value && value !== passwordForm.newPassword) {
          return Promise.reject('两次输入的密码不一致')
        }
        return Promise.resolve()
      },
      trigger: 'blur',
    },
  ],
}

// 初始化表单数据
onMounted(() => {
  const userInfo = userStore.userInfo
  if (userInfo) {
    profileForm.realName = userInfo.realName || ''
    profileForm.email = userInfo.email || ''
    profileForm.phone = userInfo.phone || ''
    profileForm.avatar = userInfo.avatar || ''
  }
})

// 保存基本信息
async function handleSaveProfile() {
  try {
    await profileFormRef.value.validate()
    savingProfile.value = true

    // TODO: 调用API保存个人信息
    // await updateProfile(profileForm)

    // 更新本地存储
    userStore.updateUserInfo({
      realName: profileForm.realName,
      email: profileForm.email,
      phone: profileForm.phone,
      avatar: profileForm.avatar,
    })

    message.success('个人信息保存成功')
  } catch (error) {
    console.error('保存个人信息失败:', error)
  } finally {
    savingProfile.value = false
  }
}

// 修改密码
async function handleChangePassword() {
  try {
    await passwordFormRef.value.validate()
    changingPassword.value = true

    // TODO: 调用API修改密码
    // await changePassword({
    //   oldPassword: passwordForm.oldPassword,
    //   newPassword: passwordForm.newPassword,
    // })

    message.success('密码修改成功，请重新登录')

    // 清空密码表单
    passwordForm.oldPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmPassword = ''

    // 可选：修改密码后退出登录
    // setTimeout(() => {
    //   userStore.logout()
    //   router.push('/login')
    // }, 1500)
  } catch (error) {
    console.error('修改密码失败:', error)
  } finally {
    changingPassword.value = false
  }
}
</script>

<style scoped lang="scss">
.profile-card {
  .avatar-section {
    text-align: center;
    padding: 24px 0;

    .user-name {
      margin-top: 16px;
      margin-bottom: 4px;
      font-size: 20px;
      font-weight: 600;
      color: rgba(0, 0, 0, 0.85);
    }

    .user-role {
      margin: 0;
      color: rgba(0, 0, 0, 0.45);
      font-size: 14px;
    }
  }

  .info-list {
    padding: 8px 0;

    .info-item {
      display: flex;
      justify-content: space-between;
      padding: 12px 0;
      border-bottom: 1px solid #f0f0f0;

      &:last-child {
        border-bottom: none;
      }

      .info-label {
        color: rgba(0, 0, 0, 0.45);
      }

      .info-value {
        color: rgba(0, 0, 0, 0.85);
        font-weight: 500;
      }
    }
  }
}

.form-card {
  margin-bottom: 24px;

  &:last-child {
    margin-bottom: 0;
  }
}

.password-card {
  :deep(.ant-card-head) {
    border-bottom-color: var(--color-danger);
  }

  :deep(.ant-card-head-title) {
    color: var(--color-danger);
  }
}
</style>
