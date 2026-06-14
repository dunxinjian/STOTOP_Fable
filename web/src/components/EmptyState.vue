<template>
  <div class="empty-state">
    <!-- 自定义图标 -->
    <div v-if="icon" class="empty-icon" :style="{ fontSize: iconSize + 'px', color: iconColor }">
      <component :is="icon" />
    </div>
    <!-- 默认 a-empty 图片 -->
    <a-empty v-else :description="false" :image-style="{ height: imageSize + 'px' }" />

    <!-- 主标题 -->
    <div class="empty-title">{{ title }}</div>

    <!-- 描述文字 -->
    <div v-if="description" class="empty-description">{{ description }}</div>

    <!-- 操作按钮 -->
    <div v-if="showAction && actionText" class="empty-action">
      <a-button type="primary" @click="handleAction">{{ actionText }}</a-button>
    </div>

    <!-- 自定义内容插槽 -->
    <slot />
  </div>
</template>

<script setup lang="ts">
import type { Component } from 'vue'
import { useRouter } from 'vue-router'
import type { RouteLocationRaw } from 'vue-router'

const props = withDefaults(defineProps<{
  /** 主标题 */
  title?: string
  /** 描述文字 */
  description?: string
  /** 自定义图标组件 */
  icon?: Component
  /** 图标大小（px） */
  iconSize?: number
  /** 图标颜色 */
  iconColor?: string
  /** 默认图片大小（px） */
  imageSize?: number
  /** 操作按钮文字 */
  actionText?: string
  /** 操作跳转路由 */
  actionRoute?: string | RouteLocationRaw
  /** 是否显示操作区 */
  showAction?: boolean
}>(), {
  title: '暂无数据',
  description: undefined,
  icon: undefined,
  iconSize: 48,
  iconColor: '#d9d9d9',
  imageSize: 120,
  actionText: undefined,
  actionRoute: undefined,
  showAction: false,
})

const router = useRouter()

function handleAction() {
  if (props.actionRoute) {
    router.push(props.actionRoute)
  }
}
</script>

<style scoped lang="scss">
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 24px;
  min-height: 300px;
  background: transparent;
}

.empty-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 16px;
}

.empty-title {
  margin-top: 12px;
  font-size: 16px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.85);
}

.empty-description {
  margin-top: 8px;
  font-size: 14px;
  color: rgba(0, 0, 0, 0.45);
}

.empty-action {
  margin-top: 20px;
}
</style>
