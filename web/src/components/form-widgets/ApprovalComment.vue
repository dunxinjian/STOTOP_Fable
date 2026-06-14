<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  value?: string
  disabled?: boolean
  placeholder?: string
  maxLength?: number
  rows?: number
}>(), {
  value: '',
  disabled: false,
  placeholder: '请输入审批意见',
  maxLength: 500,
  rows: 4,
})

const emit = defineEmits<{
  'update:value': [val: string]
}>()

const innerValue = computed({
  get: () => props.value ?? '',
  set: (val) => emit('update:value', val),
})
</script>

<template>
  <a-textarea
    v-model:value="innerValue"
    :disabled="disabled"
    :placeholder="placeholder"
    :maxlength="maxLength"
    :rows="rows"
    show-count
    allow-clear
  />
</template>
