<template>
  <div class="w-full h-64 flex items-center justify-center">
    <Doughnut :data="chartData" :options="chartOptions" />
  </div>
</template>

<script setup lang="ts">
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  ArcElement,
} from 'chart.js'
import { Doughnut } from 'vue-chartjs'

ChartJS.register(ArcElement, Title, Tooltip, Legend)

const props = withDefaults(
  defineProps<{
    occupied?: number
    reserved?: number
    available?: number
  }>(),
  {
    occupied: 18,
    reserved: 2,
    available: 4,
  }
)

const chartData = computed(() => ({
  labels: ['Đang thuê (Occupied)', 'Đã cọc (Reserved)', 'Phòng trống (Available)'],
  datasets: [
    {
      backgroundColor: ['#3b82f6', '#f59e0b', '#10b981'],
      borderWidth: 2,
      borderColor: '#ffffff',
      data: [props.occupied, props.reserved, props.available],
    },
  ],
}))

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: {
        font: {
          family: 'system-ui, sans-serif',
          size: 11,
        },
        padding: 16,
      },
    },
  },
  cutout: '70%',
}
</script>
