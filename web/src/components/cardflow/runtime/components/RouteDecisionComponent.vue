<script setup lang="ts">
import type { CardComponentRuntime } from '@/types/cardflow'

defineProps<{
  component: CardComponentRuntime
}>()
</script>

<template>
  <section class="cf-explain-component">
    <div class="cf-explain-component__title">{{ component.title || '流转说明' }}</div>
    <div v-if="component.snapshots.length === 0" class="cf-explain-component__empty">
      暂无流转决策记录
    </div>
    <ol v-else class="cf-explain-component__list">
      <li v-for="snapshot in component.snapshots" :key="`${snapshot.snapshotType}-${snapshot.reason}`">
        <span>{{ snapshot.title || '流转决策' }}</span>
        <p>{{ snapshot.reason }}</p>
      </li>
    </ol>
  </section>
</template>

<style scoped lang="scss">
.cf-explain-component {
  border-left: 3px solid #3d7d5c;
  background: #f6faf7;
  padding: 10px 12px;
  border-radius: 4px;

  &__title {
    color: #22332b;
    font-size: 13px;
    font-weight: 600;
    margin-bottom: 6px;
  }

  &__empty {
    color: #7c8781;
    font-size: 12px;
  }

  &__list {
    margin: 0;
    padding-left: 18px;
    color: #31423a;

    li + li {
      margin-top: 8px;
    }

    span {
      font-size: 12px;
      font-weight: 600;
    }

    p {
      margin: 2px 0 0;
      color: #52615a;
      font-size: 12px;
      line-height: 18px;
    }
  }
}
</style>
