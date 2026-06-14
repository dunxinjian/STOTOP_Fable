<script setup lang="ts">
import { ref } from 'vue'
import { CellGroup as VanCellGroup, Cell as VanCell, Collapse as VanCollapse, CollapseItem as VanCollapseItem } from 'vant'
import 'vant/es/cell-group/style'
import 'vant/es/cell/style'
import 'vant/es/collapse/style'
import 'vant/es/collapse-item/style'

defineProps<{
  title: string
  details: Array<Record<string, any>>
  labelField?: string
  amountField?: string
  fields: Array<{ key: string; label: string }>
}>()

const activeNames = ref<number[]>([0])

function formatAmount(val: any): string {
  const num = Number(val)
  if (isNaN(num)) return val?.toString() || '-'
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
</script>

<template>
  <VanCellGroup inset :title="title">
    <VanCollapse v-model="activeNames">
      <VanCollapseItem
        v-for="(item, index) in details"
        :key="item.FID || item.id || `detail-${index}`"
        :name="index"
        :title="`${item[labelField || 'summary'] || `明细 ${index + 1}`} ¥${formatAmount(item[amountField || 'amount'])}`"
      >
        <VanCell
          v-for="field in fields"
          :key="field.key"
          :title="field.label"
          :value="item[field.key]?.toString() || '-'"
        />
      </VanCollapseItem>
    </VanCollapse>
  </VanCellGroup>
</template>

<style scoped>
:deep(.van-collapse-item__title) {
  font-weight: 500;
}
</style>
