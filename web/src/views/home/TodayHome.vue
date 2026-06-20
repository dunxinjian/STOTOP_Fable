<template>
  <div class="today-home">
    <div class="today-home__head">
      <div class="today-home__greeting">
        {{ greeting }}，{{ userStore.userInfo?.realName || '你好' }}
        <span class="today-home__date">· {{ todayText }}</span>
      </div>
      <a-dropdown>
        <span class="today-home__perspective">
          <EyeOutlined /> 以 {{ ROLE_LABELS[role] }}
          <DownOutlined />
        </span>
        <template #overlay>
          <a-menu @click="({ key }) => setRole(key as TodayRole)">
            <a-menu-item v-for="(label, key) in ROLE_LABELS" :key="key">{{ label }}</a-menu-item>
          </a-menu>
        </template>
      </a-dropdown>
    </div>

    <DecisionLayer v-if="role === 'decision'" />
    <PowerLayer v-else-if="role === 'power'" />
    <LightLayer v-else />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { EyeOutlined, DownOutlined } from '@ant-design/icons-vue'
import dayjs from 'dayjs'
import { useUserStore } from '@/stores/user'
import { useTodayRole, ROLE_LABELS, type TodayRole } from './useTodayRole'
import DecisionLayer from './DecisionLayer.vue'
import PowerLayer from './PowerLayer.vue'
import LightLayer from './LightLayer.vue'

const userStore = useUserStore()
const { role, setRole } = useTodayRole()

const todayText = computed(() => dayjs().format('M月D日 ddd'))
const greeting = computed(() => {
  const h = dayjs().hour()
  if (h < 6) return '凌晨好'
  if (h < 12) return '早上好'
  if (h < 14) return '中午好'
  if (h < 18) return '下午好'
  return '晚上好'
})
</script>

<style scoped>
.today-home {
  padding: 16px 20px;
}

.today-home__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.today-home__greeting {
  font-size: 16px;
  font-weight: 500;
  color: var(--text-1);
}

.today-home__date {
  font-size: 13px;
  color: var(--text-3);
  font-weight: 400;
}

.today-home__perspective {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  font-size: 12px;
  color: var(--text-2);
  cursor: pointer;
}

.today-home__perspective:hover {
  border-color: var(--color-primary);
  color: var(--text-1);
}
</style>
