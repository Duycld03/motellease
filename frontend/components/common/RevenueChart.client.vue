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
    labels: () => [
      'T1', 'T2', 'T3', 'T4', 'T5', 'T6',
      'T7', 'T8', 'T9', 'T10', 'T11', 'T12',
    ],
    revenueData: () => [32, 35, 34, 38, 42, 45, 43, 48, 50, 52, 55, 58],
    expenseData: () => [8, 9, 8.5, 10, 11, 12, 11.5, 13, 13.5, 14, 15, 15.5],
  }
)

const chartData = computed(() => ({
  labels: props.labels,
  datasets: [
    {
      label: 'Doanh thu (Triệu ₫)',
      backgroundColor: '#10b981',
      borderRadius: 6,
      data: props.revenueData,
    },
    {
      label: 'Chi phí (Triệu ₫)',
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
