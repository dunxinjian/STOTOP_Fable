<template>
  <a-modal
    :open="visible"
    title="请选择组织"
    :width="480"
    :mask-closable="false"
    :keyboard="false"
    :closable="false"
    :destroy-on-close="true"
    :centered="true"
  >
    <!-- 无可用组织时的空状态提示 -->
    <template v-if="organizations.length === 0">
      <a-empty description="暂无可用组织，请联系管理员" />
    </template>

    <!-- 有可用组织时的选择列表 -->
    <template v-else>
      <div class="org-select-desc">
        <BankOutlined :style="{ fontSize: '20px', color: 'var(--text-2)' }" />
        <span>您有多个任职组织，请选择要进入的组织：</span>
      </div>

      <div class="org-select-list">
        <div
          v-for="org in organizations"
          :key="org.id"
          class="org-select-item"
          :class="{ selected: selectedOrgId === org.orgId }"
          @click="selectedOrgId = org.orgId"
        >
          <div class="org-select-item-info">
            <BankOutlined :style="{ fontSize: '18px' }" />
            <div class="org-select-item-text">
              <div class="org-select-item-name">{{ org.orgName }}</div>
              <div class="org-select-item-meta">
                <a-tag>{{ org.orgType }}</a-tag>
                <a-tag v-if="org.isPrimaryOrg === 1" color="blue">主组织</a-tag>
                <span v-if="org.position" class="org-position">{{ org.position }}</span>
              </div>
            </div>
          </div>
          <CheckCircleFilled v-if="selectedOrgId === org.orgId" class="selected-icon" :style="{ color: 'var(--color-primary)' }" />
        </div>
      </div>
    </template>

    <template #footer>
      <a-button v-if="organizations.length === 0" @click="emit('update:modelValue', false)">
        关闭
      </a-button>
      <a-button v-else type="primary" :loading="loading" :disabled="!selectedOrgId" @click="handleConfirm">
        确认进入
      </a-button>
    </template>
  </a-modal>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { BankOutlined, CheckCircleFilled } from '@ant-design/icons-vue'
import type { UserOrganizationDto } from '@/types/organization'

const props = defineProps<{
  modelValue: boolean
  organizations: UserOrganizationDto[]
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', val: boolean): void
  (e: 'select', orgId: number): void
}>()

const visible = ref(props.modelValue)
const selectedOrgId = ref<number | null>(null)
const loading = ref(false)

watch(() => props.modelValue, (val) => {
  visible.value = val
  if (val) {
    // 默认选中主组织
    const primary = props.organizations.find(o => o.isPrimaryOrg === 1)
    selectedOrgId.value = primary ? primary.orgId : (props.organizations[0]?.orgId || null)
  }
})

watch(visible, (val) => {
  if (!val) {
    loading.value = false
  }
  emit('update:modelValue', val)
})

async function handleConfirm() {
  if (!selectedOrgId.value) return
  loading.value = true
  emit('select', selectedOrgId.value)
}
</script>

<style scoped lang="scss">
@use '@/styles/variables.scss' as *;

.org-select-desc {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
  font-size: $font-size-base;
  color: $text-regular;
}

.org-select-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 400px;
  overflow-y: auto;
}

.org-select-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border: 1px solid $border-color-lighter;
  border-radius: $border-radius-md;
  cursor: pointer;
  transition: all $transition-normal;

  &:hover {
    border-color: $color-primary;
    background-color: rgba($color-primary, 0.02);
  }

  &.selected {
    border-color: $color-primary;
    background-color: rgba($color-primary, 0.06);
  }

  .org-select-item-info {
    display: flex;
    align-items: center;
    gap: 10px;
    flex: 1;
    min-width: 0;
  }

  .org-select-item-text {
    flex: 1;
    min-width: 0;
  }

  .org-select-item-name {
    font-size: $font-size-base;
    font-weight: 500;
    color: $text-primary;
    margin-bottom: 4px;
  }

  .org-select-item-meta {
    display: flex;
    align-items: center;
    gap: 6px;

    .org-position {
      font-size: $font-size-sm;
      color: $text-secondary;
    }
  }

  .selected-icon {
    flex-shrink: 0;
  }
}
</style>
