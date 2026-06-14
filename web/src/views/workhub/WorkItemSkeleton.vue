<template>
  <div class="work-item-skeleton">
    <div v-for="i in count" :key="i" class="skeleton-card">
      <div class="skeleton-header">
        <a-skeleton-button :active="true" size="small" shape="round" style="width: 64px; height: 22px;" />
        <a-skeleton-input :active="true" size="small" style="width: 200px; height: 18px;" />
        <div style="flex: 1" />
        <a-skeleton-input :active="true" size="small" style="width: 80px; height: 14px;" />
      </div>
      <div class="skeleton-body">
        <a-skeleton-input :active="true" size="small" style="width: 100%; height: 14px;" />
        <a-skeleton-input :active="true" size="small" style="width: 75%; height: 14px;" />
      </div>
      <div class="skeleton-footer">
        <a-skeleton-button :active="true" size="small" shape="round" style="width: 56px; height: 20px;" />
        <a-skeleton-input :active="true" size="small" style="width: 100px; height: 14px;" />
        <div style="flex: 1" />
        <a-skeleton-button :active="true" size="small" style="width: 72px; height: 26px;" />
        <a-skeleton-button :active="true" size="small" style="width: 72px; height: 26px;" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  count?: number
}>(), {
  count: 4,
})
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.work-item-skeleton {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.skeleton-card {
  background: $bg-card;
  border-radius: 8px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.06);
}

.skeleton-header {
  display: flex;
  align-items: center;
  gap: 10px;
}

.skeleton-body {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.skeleton-footer {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
