<template>
  <div class="w-full h-72">
    <Bar :data="chartData" :options="chartOptions" />
  </div>
</template>

<script setup lang="ts">
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  BarElement,
  CategoryScale,
  LinearScale,
} from 'chart.js'
import { Bar } from 'vue-chartjs'

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

const props = withDefaults(
  defineProps<{
    labels?: string[]
    revenueData?: number[]
    expenseData?: number[]
  }>(),
  {
    labels: undefined,
    revenueData: () => [32, 35, 34, 38, 42, 45, 43, 48, 50, 52, 55, 58],
    expenseData: () => [8, 9, 8.5, 10, 11, 12, 11.5, 13, 13.5, 14, 15, 15.5],
  }
)

const { t } = useI18n()

const defaultLabels = computed(() => [
  t('months.m1'), t('months.m2'), t('months.m3'), t('months.m4'),
  t('months.m5'), t('months.m6'), t('months.m7'), t('months.m8'),
  t('months.m9'), t('months.m10'), t('months.m11'), t('months.m12'),
])

const chartData = computed(() => ({
  labels: props.labels || defaultLabels.value,
  datasets: [
    {
      label: t('ownerAnalytics.revenueChartDataset'),
      backgroundColor: '#10b981',
      borderRadius: 6,
      data: props.revenueData,
    },
    {
      label: t('ownerAnalytics.expenseChartDataset'),
      backgroundColor: '#f87171',
      borderRadius: 6,
      data: props.expenseData,
    },
  ],
}))

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'top' as const,
      labels: {
        font: {
          family: 'system-ui, sans-serif',
          size: 12,
        },
      },
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      grid: {
        color: '#f1f5f9',
      },
      ticks: {
        font: {
          family: 'system-ui, sans-serif',
          size: 11,
        },
      },
    },
    x: {
      grid: {
        display: false,
      },
      ticks: {
        font: {
          family: 'system-ui, sans-serif',
          size: 11,
        },
      },
    },
  },
}
</script>
