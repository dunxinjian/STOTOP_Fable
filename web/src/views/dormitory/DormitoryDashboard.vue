<template>
  <div class="page-container">
    <PageHeader title="宿舍统计看板" description="宿舍管理数据概览" />

    <!-- 顶部统计卡片 -->
    <a-row :gutter="16" class="stat-cards">
      <a-col :xs="24" :sm="12" :lg="6">
        <a-card :bordered="false" :loading="loading">
          <a-statistic title="总楼栋数" :value="statistics.totalBuildings" suffix="栋">
            <template #prefix>
              <HomeOutlined style="color: #1890ff" />
            </template>
          </a-statistic>
        </a-card>
      </a-col>
      <a-col :xs="24" :sm="12" :lg="6">
        <a-card :bordered="false" :loading="loading">
          <a-statistic title="总房间数" :value="statistics.totalRooms" suffix="间">
            <template #prefix>
              <AppstoreOutlined style="color: #52c41a" />
            </template>
          </a-statistic>
        </a-card>
      </a-col>
      <a-col :xs="24" :sm="12" :lg="6">
        <a-card :bordered="false" :loading="loading">
          <a-statistic title="总床位数" :value="statistics.totalBeds" suffix="床">
            <template #prefix>
              <UserOutlined style="color: #722ed1" />
            </template>
          </a-statistic>
        </a-card>
      </a-col>
      <a-col :xs="24" :sm="12" :lg="6">
        <a-card :bordered="false" :loading="loading">
          <a-statistic title="总入住率" :value="occupancyRatePercent" suffix="%">
            <template #prefix>
              <PercentageOutlined :style="{ color: occupancyRateColor }" />
            </template>
          </a-statistic>
          <a-progress
            :percent="occupancyRatePercent"
            :stroke-color="occupancyRateColor"
            :show-info="false"
            size="small"
            style="margin-top: 8px"
          />
        </a-card>
      </a-col>
    </a-row>

    <!-- 其他统计卡片 -->
    <a-row :gutter="16" class="stat-cards" style="margin-top: 16px">
      <a-col :xs="24" :sm="12" :lg="8">
        <a-card :bordered="false" :loading="loading">
          <a-statistic title="待处理报修工单" :value="statistics.pendingRepairOrders" suffix="单">
            <template #prefix>
              <ToolOutlined :style="{ color: statistics.pendingRepairOrders > 0 ? '#faad14' : '#52c41a' }" />
            </template>
          </a-statistic>
        </a-card>
      </a-col>
      <a-col :xs="24" :sm="12" :lg="8">
        <a-card :bordered="false" :loading="loading">
          <a-statistic title="今日访客" :value="statistics.todayVisitors" suffix="人">
            <template #prefix>
              <TeamOutlined style="color: #13c2c2" />
            </template>
          </a-statistic>
        </a-card>
      </a-col>
      <a-col :xs="24" :sm="12" :lg="8">
        <a-card :bordered="false" :loading="loading">
          <a-statistic title="待缴费用" :value="statistics.pendingExpenses" suffix="元" :precision="2">
            <template #prefix>
              <AccountBookOutlined :style="{ color: statistics.pendingExpenses > 0 ? '#ff4d4f' : '#52c41a' }" />
            </template>
          </a-statistic>
        </a-card>
      </a-col>
    </a-row>

    <!-- 楼栋入住统计 -->
    <a-card title="各楼栋入住统计" :bordered="false" style="margin-top: 16px" :loading="buildingLoading">
      <a-table
        :columns="buildingColumns"
        :data-source="buildingData"
        :pagination="false"
        row-key="id"
        bordered
        size="middle"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.dataIndex === 'occupancyRate'">
            <div class="rate-cell">
              <a-progress
                :percent="record.occupancyRate"
                :stroke-color="getOccupancyColor(record.occupancyRate)"
                size="small"
                :show-info="false"
                style="width: 100px"
              />
              <span class="rate-text">{{ record.occupancyRate }}%</span>
            </div>
          </template>
          <template v-if="column.dataIndex === 'status'">
            <a-tag :color="record.status === 1 ? 'success' : 'error'">
              {{ record.status === 1 ? '正常' : '停用' }}
            </a-tag>
          </template>
        </template>
      </a-table>
    </a-card>

    <!-- 本月费用汇总 -->
    <a-card title="本月费用汇总" :bordered="false" style="margin-top: 16px" :loading="expenseLoading">
      <a-row :gutter="16">
        <a-col v-for="item in expenseSummary" :key="item.type" :xs="24" :sm="12" :lg="6">
          <div class="expense-card">
            <div class="expense-type">{{ item.type }}</div>
            <div class="expense-amount">
              <span class="currency">¥</span>
              <span class="amount">{{ item.amount.toFixed(2) }}</span>
            </div>
            <div class="expense-count">{{ item.count }} 笔</div>
          </div>
        </a-col>
      </a-row>
      <a-empty v-if="expenseSummary.length === 0 && !expenseLoading" description="暂无本月费用数据" />
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import {
  HomeOutlined,
  AppstoreOutlined,
  UserOutlined,
  PercentageOutlined,
  ToolOutlined,
  TeamOutlined,
  AccountBookOutlined,
} from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'
import {
  getStatistics,
  getAllBuildings,
  getExpenseList,
  type DormitoryStatisticsDto,
  type BuildingListItemDto,
  type ExpenseDto,
} from '@/api/dormitory'

// 统计数据
const loading = ref(false)
const statistics = reactive<DormitoryStatisticsDto>({
  totalBuildings: 0,
  totalRooms: 0,
  totalBeds: 0,
  occupiedBeds: 0,
  occupancyRate: 0,
  pendingRepairOrders: 0,
  todayVisitors: 0,
  pendingExpenses: 0,
})

// 计算入住率百分比
const occupancyRatePercent = computed(() => {
  return Math.round(statistics.occupancyRate * 100) / 100
})

// 根据入住率获取颜色
const occupancyRateColor = computed(() => {
  const rate = occupancyRatePercent.value
  if (rate >= 90) return '#52c41a'
  if (rate >= 70) return '#1890ff'
  if (rate >= 50) return '#faad14'
  return '#ff4d4f'
})

// 楼栋数据
const buildingLoading = ref(false)
const buildingData = ref<any[]>([])
const buildingColumns = [
  { title: '楼栋名称', dataIndex: 'name', key: 'name', width: 150 },
  { title: '地址', dataIndex: 'address', key: 'address', ellipsis: true },
  { title: '总床位', dataIndex: 'bedCount', key: 'bedCount', width: 100, align: 'center' as const },
  { title: '已入住', dataIndex: 'occupiedBeds', key: 'occupiedBeds', width: 100, align: 'center' as const },
  { title: '空闲', dataIndex: 'availableBeds', key: 'availableBeds', width: 80, align: 'center' as const },
  { title: '入住率', dataIndex: 'occupancyRate', key: 'occupancyRate', width: 180 },
  { title: '状态', dataIndex: 'status', key: 'status', width: 80, align: 'center' as const },
]

// 费用数据
const expenseLoading = ref(false)
const expenseSummary = ref<{ type: string; amount: number; count: number }[]>([])

// 费用类型中文映射
const expenseTypeMap: Record<string, string> = {
  Water: '水费',
  Electricity: '电费',
  Accommodation: '住宿费',
  Other: '其他',
}

function getExpenseTypeText(type: string): string {
  return expenseTypeMap[type] || type
}

// 获取入住率颜色
function getOccupancyColor(rate: number): string {
  if (rate >= 90) return '#52c41a'
  if (rate >= 70) return '#1890ff'
  if (rate >= 50) return '#faad14'
  return '#ff4d4f'
}

// 获取统计数据
async function fetchStatistics() {
  loading.value = true
  try {
    const res = await getStatistics()
    if (res) {
      statistics.totalBuildings = res.totalBuildings || 0
      statistics.totalRooms = res.totalRooms || 0
      statistics.totalBeds = res.totalBeds || 0
      statistics.occupiedBeds = res.occupiedBeds || 0
      statistics.occupancyRate = res.occupancyRate || 0
      statistics.pendingRepairOrders = res.pendingRepairOrders || 0
      statistics.todayVisitors = res.todayVisitors || 0
      statistics.pendingExpenses = res.pendingExpenses || 0
    }
  } finally {
    loading.value = false
  }
}

// 获取楼栋入住统计
async function fetchBuildingStats() {
  buildingLoading.value = true
  try {
    const buildings = await getAllBuildings()
    buildingData.value = buildings.map((b: BuildingListItemDto) => ({
      ...b,
      availableBeds: b.bedCount - b.occupiedBeds,
      occupancyRate: b.bedCount > 0 ? Math.round((b.occupiedBeds / b.bedCount) * 100) : 0,
    }))
  } finally {
    buildingLoading.value = false
  }
}

// 获取本月费用汇总
async function fetchExpenseSummary() {
  expenseLoading.value = true
  try {
    const currentMonth = new Date()
    const monthStr = `${currentMonth.getFullYear()}-${String(currentMonth.getMonth() + 1).padStart(2, '0')}`
    
    const res = await getExpenseList({
      pageIndex: 1,
      pageSize: 1000,
      month: monthStr,
    })
    
    if (res && res.items) {
      const summaryMap = new Map<string, { amount: number; count: number }>()
      
      res.items.forEach((expense: ExpenseDto) => {
        const type = getExpenseTypeText(expense.expenseType || 'Other')
        const current = summaryMap.get(type) || { amount: 0, count: 0 }
        current.amount += expense.amount || 0
        current.count += 1
        summaryMap.set(type, current)
      })
      
      expenseSummary.value = Array.from(summaryMap.entries()).map(([type, data]) => ({
        type,
        amount: data.amount,
        count: data.count,
      }))
    }
  } finally {
    expenseLoading.value = false
  }
}

onMounted(() => {
  fetchStatistics()
  fetchBuildingStats()
  fetchExpenseSummary()
})
</script>

<style scoped lang="scss">
.stat-cards {
  :deep(.ant-card) {
    .ant-statistic-title {
      font-size: 14px;
      color: #666;
    }
    .ant-statistic-content {
      font-size: 28px;
    }
  }
}

.rate-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.rate-text {
  font-weight: 500;
  min-width: 45px;
}

.expense-card {
  background: #fafafa;
  border-radius: 8px;
  padding: 16px;
  text-align: center;
  margin-bottom: 16px;
  
  .expense-type {
    font-size: 14px;
    color: #666;
    margin-bottom: 8px;
  }
  
  .expense-amount {
    font-size: 24px;
    font-weight: 600;
    color: #333;
    margin-bottom: 4px;
    
    .currency {
      font-size: 14px;
      margin-right: 2px;
    }
    
    .amount {
      font-size: 24px;
    }
  }
  
  .expense-count {
    font-size: 12px;
    color: #999;
  }
}
</style>
