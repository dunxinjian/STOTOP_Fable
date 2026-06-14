<script setup lang="ts">
import type { CardComponentRuntime } from '@/types/cardflow'

defineProps<{ component: CardComponentRuntime }>()
</script>

<template>
  <section class="cf-status-component">
    <span>{{ component.title || '借款冲抵' }}</span>
    <strong>{{ component.value ?? component.props?.statusText ?? '未冲抵' }}</strong>
  </section>
</template>

<style scoped lang="scss">
.cf-status-component {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  border: 1px solid #e2e6e4;
  border-radius: 6px;
  padding: 10px 12px;
  background: #fff;
  color: #52615a;
  font-size: 13px;

  strong {
    color: #23322c;
    font-weight: 600;
  }
}
</style>
