<script setup lang="ts">
const props = withDefaults(defineProps<{
  value?: number
  disabled?: boolean
  placeholder?: string
}>(), {
  value: undefined,
  disabled: false,
  placeholder: '请输入金额',
})

const emit = defineEmits<{
  'update:value': [val: number | undefined]
}>()

function formatter(val: number | string | undefined): string {
  if (val === undefined || val === null || val === '') return ''
  return `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

function parser(val: string | undefined): string {
  if (!val) return ''
  return val.replace(/,/g, '')
}

function handleChange(val: number | undefined) {
  emit('update:value', val)
}
</script>

<template>
  <a-input-number
    :value="value"
    :disabled="disabled"
    :placeholder="placeholder"
    :precision="2"
    :min="0"
    :formatter="formatter"
    :parser="parser"
    style="width: 100%"
    @change="handleChange"
  />
</template>
