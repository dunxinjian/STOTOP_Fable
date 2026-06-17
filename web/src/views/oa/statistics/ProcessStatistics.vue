<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import {
  ClockCircleOutlined,
  CheckCircleOutlined,
  FieldTimeOutlined,
  ExclamationCircleOutlined,
  CrownOutlined,
  TrophyOutlined,
  RiseOutlined,
  FireOutlined,
} from '@ant-design/icons-vue'
import PageHeader from '@/components/PageHeader.vue'

const loading = ref(false)

// TODO: 替换为真实API调用 — GET /api/oa/statistics/overview
const stats = ref({
  pendingCount: 12,
  monthlyProcessed: 238,
  avgDuration: 4.2,
  rejectRate: 8.5,
})

// TODO: 替换为真实API调用 — GET /api/oa/statistics/by-type
const typeStats = ref([
  { type: '费用请款', code: 'expense_request', count: 56, passRate: 92, avgHours: 3.2 },
  { type: '费用报销', code: 'expense_reimburse', count: 48, passRate: 95, avgHours: 2.8 },
  { type: '对外付款', code: 'external_payment', count: 35, passRate: 88, avgHours: 5.1 },
  { type: '借款申请', code: 'loan_apply', count: 22, passRate: 85, avgHours: 6.3 },
  { type: '备用金申请', code: 'petty_cash_apply', count: 28, passRate: 90, avgHours: 3.5 },
  { type: '备用金报销', code: 'petty_cash_reimburse', count: 18, passRate: 94, avgHours: 2.1 },
  { type: '备用金还款', code: 'petty_cash_return', count: 15, passRate: 98, avgHours: 1.5 },
  { type: '预支工资', code: 'salary_advance', count: 16, passRate: 82, avgHours: 8.2 },
])

// TODO: 替换为真实API调用 — GET /api/oa/statistics/trend?days=30
const trendData = ref([
  { date: '04-05', label: '4月5日', count: 8 },
  { date: '04-06', label: '4月6日', count: 15 },
  { date: '04-07', label: '4月7日', count: 22 },
  { date: '04-08', label: '4月8日', count: 12 },
  { date: '04-09', label: '4月9日', count: 28 },
  { date: '04-10', label: '4月10日', count: 18 },
  { date: '04-11', label: '4月11日', count: 6 },
])

const trendMax = computed(() => Math.max(...trendData.value.map(d => d.count), 1))

// TODO: 替换为真实API调用 — GET /api/oa/statistics/approver-ranking
const approverRanking = ref([
  { rank: 1, name: '张三', count: 45, avgHours: 2.3, passRate: 94 },
  { rank: 2, name: '李四', count: 38, avgHours: 3.1, passRate: 91 },
  { rank: 3, name: '王五', count: 34, avgHours: 2.8, passRate: 96 },
  { rank: 4, name: '赵六', count: 29, avgHours: 4.0, passRate: 88 },
  { rank: 5, name: '孙七', count: 25, avgHours: 3.5, passRate: 93 },
  { rank: 6, name: '周八', count: 22, avgHours: 5.2, passRate: 85 },
  { rank: 7, name: '吴九', count: 18, avgHours: 2.9, passRate: 97 },
])

const typeColumns = [
  { title: '流程类型', dataIndex: 'type', key: 'type' },
  { title: '本月数量', dataIndex: 'count', key: 'count', sorter: (a: any, b: any) => a.count - b.count },
  { title: '通过率', dataIndex: 'passRate', key: 'passRate', sorter: (a: any, b: any) => a.passRate - b.passRate },
  { title: '平均时长', dataIndex: 'avgHours', key: 'avgHours', sorter: (a: any, b: any) => a.avgHours - b.avgHours },
]

const approverColumns = [
  { title: '排名', dataIndex: 'rank', key: 'rank', width: 70 },
  { title: '审批人', dataIndex: 'name', key: 'name' },
  { title: '处理数', dataIndex: 'count', key: 'count', sorter: (a: any, b: any) => a.count - b.count },
  { title: '平均时长', dataIndex: 'avgHours', key: 'avgHours', sorter: (a: any, b: any) => a.avgHours - b.avgHours },
  { title: '通过率', dataIndex: 'passRate', key: 'passRate', sorter: (a: any, b: any) => a.passRate - b.passRate },
]

async function loadData() {
  loading.value = true
  try {
    // TODO: 调用真实API加载数据
    // const [overview, types, trend, ranking] = await Promise.all([
    //   getProcessStatistics(),
    //   getTypeStatistics(),
    //   getTrendData(30),
    //   getApproverRanking(30),
    // ])
    // stats.value = overview
    // typeStats.value = types
    // trendData.value = trend
    // approverRanking.value = ranking
  } catch (e) {
    console.error('加载统计数据失败', e)
  } finally {
    loading.value = false
  }
}

function getRankIcon(rank: number) {
  if (rank === 1) return CrownOutlined
  if (rank === 2) return TrophyOutlined
  if (rank === 3) return FireOutlined
  return null
}

function getRankColor(rank: number) {
  if (rank === 1) return '#faad14'
  if (rank === 2) return '#8c8c8c'
  if (rank === 3) return '#d48806'
  return '#bfbfbf'
}

onMounted(() => loadData())
</script>

<template>
  <div class="page-container">
    <PageHeader title="流程统计" />

    <a-spin :spinning="loading">
      <!-- KPI 统计卡片 -->
      <a-row :gutter="16" class="kpi-row">
        <a-col :span="6">
          <div class="kpi-card kpi-pending">
            <a-statistic title="待审批总数" :value="stats.pendingCount" :value-style="{ color: 'var(--color-warning)', fontWeight: 700, fontSize: '28px' }">
              <template #prefix><ClockCircleOutlined /></template>
            </a-statistic>
            <div class="kpi-decoration">
              <ClockCircleOutlined class="kpi-bg-icon" />
            </div>
          </div>
        </a-col>
        <a-col :span="6">
          <div class="kpi-card kpi-processed">
            <a-statistic title="本月已处理" :value="stats.monthlyProcessed" :value-style="{ color: 'var(--color-success-text)', fontWeight: 700, fontSize: '28px' }">
              <template #prefix><CheckCircleOutlined /></template>
            </a-statistic>
            <div class="kpi-decoration">
              <CheckCircleOutlined class="kpi-bg-icon" />
            </div>
          </div>
        </a-col>
        <a-col :span="6">
          <div class="kpi-card kpi-duration">
            <a-statistic title="平均审批时长" :value="stats.avgDuration" suffix="小时" :value-style="{ color: '#1d39c4', fontWeight: 700, fontSize: '28px' }">
              <template #prefix><FieldTimeOutlined /></template>
            </a-statistic>
            <div class="kpi-decoration">
              <FieldTimeOutlined class="kpi-bg-icon" />
            </div>
          </div>
        </a-col>
        <a-col :span="6">
          <div class="kpi-card kpi-reject">
            <a-statistic
              title="本月驳回率"
              :value="stats.rejectRate"
              suffix="%"
              :value-style="{ color: stats.rejectRate > 20 ? 'var(--color-danger)' : 'var(--color-success-text)', fontWeight: 700, fontSize: '28px' }"
            >
              <template #prefix><ExclamationCircleOutlined /></template>
            </a-statistic>
            <div class="kpi-decoration">
              <ExclamationCircleOutlined class="kpi-bg-icon" />
            </div>
          </div>
        </a-col>
      </a-row>

      <!-- 流程类型分布 + 趋势 -->
      <a-row :gutter="16" style="margin-top: 16px;">
        <a-col :span="13">
          <a-card class="section-card">
            <template #title>
              <span class="section-title"><RiseOutlined style="margin-right: 6px;" />流程类型分布</span>
            </template>
            <a-table
              :columns="typeColumns"
              :data-source="typeStats"
              size="small"
              :pagination="false"
              row-key="code"
            >
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'type'">
                  <span class="type-name">{{ record.type }}</span>
                </template>
                <template v-else-if="column.key === 'count'">
                  <span class="count-value">{{ record.count }}</span>
                </template>
                <template v-else-if="column.key === 'passRate'">
                  <a-tag :color="record.passRate >= 90 ? 'green' : record.passRate >= 80 ? 'orange' : 'red'" size="small">
                    {{ record.passRate }}%
                  </a-tag>
                </template>
                <template v-else-if="column.key === 'avgHours'">
                  <span :style="{ color: record.avgHours > 5 ? 'var(--color-danger)' : '#333' }">{{ record.avgHours }}h</span>
                </template>
              </template>
            </a-table>
          </a-card>
        </a-col>
        <a-col :span="11">
          <a-card class="section-card">
            <template #title>
              <span class="section-title"><RiseOutlined style="margin-right: 6px;" />近7天处理趋势</span>
            </template>
            <div class="trend-chart">
              <div v-for="item in trendData" :key="item.date" class="trend-bar-row">
                <span class="trend-label">{{ item.date }}</span>
                <div class="trend-bar-wrapper">
                  <a-progress
                    :percent="Math.round((item.count / trendMax) * 100)"
                    :show-info="false"
                    :stroke-color="{ from: '#1d39c4', to: '#597ef7' }"
                    size="small"
                    :stroke-width="14"
                    style="flex: 1;"
                  />
                </div>
                <span class="trend-count">{{ item.count }}件</span>
              </div>
            </div>
          </a-card>
        </a-col>
      </a-row>

      <!-- 审批人排行 -->
      <a-card class="section-card" style="margin-top: 16px;">
        <template #title>
          <span class="section-title"><TrophyOutlined style="margin-right: 6px;" />审批人工作量排行（近30天）</span>
        </template>
        <a-table
          :columns="approverColumns"
          :data-source="approverRanking"
          size="small"
          :pagination="false"
          row-key="rank"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'rank'">
              <span class="rank-badge" :style="{ color: getRankColor(record.rank) }">
                <component v-if="getRankIcon(record.rank)" :is="getRankIcon(record.rank)" style="margin-right: 4px;" />
                {{ record.rank }}
              </span>
            </template>
            <template v-else-if="column.key === 'name'">
              <span class="approver-name">{{ record.name }}</span>
            </template>
            <template v-else-if="column.key === 'count'">
              <span class="count-value">{{ record.count }}</span>
            </template>
            <template v-else-if="column.key === 'avgHours'">
              <span :style="{ color: record.avgHours > 5 ? 'var(--color-danger)' : '#333' }">{{ record.avgHours }}h</span>
            </template>
            <template v-else-if="column.key === 'passRate'">
              <a-progress
                :percent="record.passRate"
                :stroke-color="record.passRate >= 90 ? 'var(--color-success)' : record.passRate >= 80 ? 'var(--color-warning)' : 'var(--color-danger)'"
                size="small"
                :stroke-width="8"
                style="width: 120px;"
              />
            </template>
          </template>
        </a-table>
      </a-card>
    </a-spin>
  </div>
</template>

<style scoped lang="scss">
.page-container {
  padding: 16px;
}

/* ======== KPI Cards ======== */
.kpi-row {
  margin-bottom: 0;
}
.kpi-card {
  position: relative;
  overflow: hidden;
  border-radius: 10px;
  padding: 20px 20px 16px;
  transition: transform 0.25s ease, box-shadow 0.25s ease;
  cursor: default;

  &:hover {
    transform: translateY(-3px);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
  }

  .kpi-decoration {
    position: absolute;
    right: -8px;
    bottom: -12px;
    opacity: 0.06;
    .kpi-bg-icon {
      font-size: 80px;
    }
  }
}
.kpi-pending {
  background: linear-gradient(135deg, var(--color-warning-light) 0%, #fff1b8 100%);
  border: 1px solid #ffe58f;
}
.kpi-processed {
  background: linear-gradient(135deg, var(--color-success-light) 0%, #d9f7be 100%);
  border: 1px solid #b7eb8f;
}
.kpi-duration {
  background: linear-gradient(135deg, #f0f5ff 0%, #d6e4ff 100%);
  border: 1px solid #adc6ff;
}
.kpi-reject {
  background: linear-gradient(135deg, var(--color-danger-light) 0%, #ffccc7 100%);
  border: 1px solid #ffa39e;
}

/* ======== Section Cards ======== */
.section-card {
  border-radius: 10px;

  .section-title {
    font-weight: 600;
    font-size: 15px;
    color: #1d1d1d;
  }
}

/* ======== Type Table ======== */
.type-name {
  font-weight: 500;
}
.count-value {
  font-weight: 600;
  font-size: 15px;
  color: #1d39c4;
}

/* ======== Trend Chart ======== */
.trend-chart {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.trend-bar-row {
  display: flex;
  align-items: center;
  gap: 10px;
}
.trend-label {
  width: 44px;
  font-size: 12px;
  color: #666;
  text-align: right;
  flex-shrink: 0;
}
.trend-bar-wrapper {
  flex: 1;
  display: flex;
  align-items: center;
}
.trend-count {
  width: 42px;
  font-size: 12px;
  font-weight: 600;
  color: #1d39c4;
  text-align: right;
  flex-shrink: 0;
}

/* ======== Ranking ======== */
.rank-badge {
  font-weight: 700;
  font-size: 15px;
  display: inline-flex;
  align-items: center;
}
.approver-name {
  font-weight: 500;
}
</style>
