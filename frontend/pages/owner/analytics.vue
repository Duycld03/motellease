<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.analytics') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Báo cáo doanh thu 12 tháng, chi phí vận hành, lợi nhuận ròng và tỷ lệ lấp đầy phòng
        </p>
      </div>

      <!-- Year and House Filters -->
      <div class="flex items-center gap-2">
        <select v-model="selectedHouseId" class="input-field !text-xs !py-1.5 w-44" @change="fetchAllStats">
          <option value="">Tất cả khu trọ</option>
          <option v-for="h in boardingHouses" :key="h.id" :value="h.id">
            {{ h.name }}
          </option>
        </select>
        <select v-model.number="selectedYear" class="input-field !text-xs !py-1.5 w-28" @change="fetchAllStats">
          <option v-for="y in availableYears" :key="y" :value="y">Năm {{ y }}</option>
        </select>
        <BaseButton variant="outline" size="sm" @click="fetchAllStats">
          🔄
        </BaseButton>
      </div>
    </div>

    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else class="space-y-6">
      <!-- KPI Summary Cards -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
          <span class="text-[10px] text-slate-400 font-semibold uppercase block">Tổng doanh thu năm {{ selectedYear }}</span>
          <span class="text-lg font-black text-emerald-600 dark:text-emerald-400 mt-1 block">
            {{ formatCurrency(profitStats?.totalRevenue || 0) }}
          </span>
          <span class="text-[10px] text-slate-500 dark:text-slate-400 block mt-0.5">
            {{ revenueStats?.totalPaidBills || 0 }} hóa đơn đã thanh toán
          </span>
        </div>

        <div class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
          <span class="text-[10px] text-slate-400 font-semibold uppercase block">Tổng chi phí vận hành</span>
          <span class="text-lg font-black text-rose-500 dark:text-rose-400 mt-1 block">
            {{ formatCurrency(profitStats?.totalExpense || 0) }}
          </span>
          <span class="text-[10px] text-slate-500 dark:text-slate-400 block mt-0.5">
            Điện nước tổng & bảo trì
          </span>
        </div>

        <div class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
          <span class="text-[10px] text-slate-400 font-semibold uppercase block">Lợi nhuận ròng (Net Profit)</span>
          <span class="text-lg font-black text-primary-600 dark:text-primary-400 mt-1 block">
            {{ formatCurrency(profitStats?.totalNetProfit || 0) }}
          </span>
          <span class="text-[10px] text-emerald-600 dark:text-emerald-400 font-semibold block mt-0.5">
            Doanh thu - Chi phí
          </span>
        </div>

        <div class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
          <span class="text-[10px] text-slate-400 font-semibold uppercase block">Tỷ lệ lấp đầy bình quân</span>
          <span class="text-lg font-black text-blue-600 dark:text-blue-400 mt-1 block">
            {{ Math.round((occupancyStats?.overallOccupancyRate || 0) * 100) }}%
          </span>
          <span class="text-[10px] text-slate-500 dark:text-slate-400 block mt-0.5">
            {{ occupancyStats?.rentedRooms || 0 }} / {{ occupancyStats?.totalRooms || 0 }} phòng đang thuê
          </span>
        </div>
      </div>

      <!-- Charts Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- 12-Month Revenue & Expense Bar Chart -->
        <div class="lg:col-span-2 p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
          <div class="flex items-center justify-between">
            <h3 class="text-sm font-bold text-slate-900 dark:text-white">
              Doanh thu & Chi phí 12 tháng năm {{ selectedYear }}
            </h3>
            <span class="text-xs text-slate-500">Đơn vị: Triệu VNĐ</span>
          </div>
          <ClientOnly>
            <RevenueChart
              :revenue-data="monthlyRevenueArray"
              :expense-data="monthlyExpenseArray"
            />
            <template #fallback>
              <div class="h-72 flex items-center justify-center text-xs text-slate-400">
                Đang tải biểu đồ...
              </div>
            </template>
          </ClientOnly>
        </div>

        <!-- Occupancy Doughnut Chart -->
        <div class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
          <h3 class="text-sm font-bold text-slate-900 dark:text-white">
            Tỷ lệ lấp đầy phòng trọ
          </h3>
          <ClientOnly>
            <OccupancyChart
              :occupied="occupancyStats?.rentedRooms || 0"
              :reserved="occupancyStats?.reservedRooms || 0"
              :available="occupancyStats?.vacantRooms || 0"
            />
            <template #fallback>
              <div class="h-64 flex items-center justify-center text-xs text-slate-400">
                Đang tải biểu đồ...
              </div>
            </template>
          </ClientOnly>
        </div>
      </div>

      <!-- Table: Occupancy Breakdown by Property -->
      <div class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-3">
        <h3 class="text-sm font-bold text-slate-900 dark:text-white">
          Thống kê phòng theo từng Khu trọ
        </h3>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs">
            <thead>
              <tr class="border-b border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400">
                <th class="pb-2 font-semibold">Tên khu trọ</th>
                <th class="pb-2 font-semibold text-center">Tổng số phòng</th>
                <th class="pb-2 font-semibold text-center">Đang thuê</th>
                <th class="pb-2 font-semibold text-center">Đã cọc</th>
                <th class="pb-2 font-semibold text-center">Phòng trống</th>
                <th class="pb-2 font-semibold text-right">Tỷ lệ lấp đầy</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              <tr
                v-for="house in occupancyStats?.houses || []"
                :key="house.boardingHouseId"
                class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors"
              >
                <td class="py-3 font-bold text-slate-900 dark:text-white">
                  {{ house.boardingHouseName }}
                </td>
                <td class="py-3 text-center font-medium text-slate-700 dark:text-slate-300">
                  {{ house.totalRooms }}
                </td>
                <td class="py-3 text-center text-blue-600 dark:text-blue-400 font-semibold">
                  {{ house.rentedRooms }}
                </td>
                <td class="py-3 text-center text-amber-600 dark:text-amber-400 font-semibold">
                  {{ house.reservedRooms }}
                </td>
                <td class="py-3 text-center text-emerald-600 dark:text-emerald-400 font-semibold">
                  {{ house.vacantRooms }}
                </td>
                <td class="py-3 text-right font-bold text-slate-900 dark:text-white">
                  {{ Math.round(house.occupancyRate * 100) }}%
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import RevenueChart from '~/components/common/RevenueChart.client.vue'
import OccupancyChart from '~/components/common/OccupancyChart.client.vue'
import type {
  BoardingHouse,
  OccupancyStatsResponse,
  PagedResult,
  ProfitStatsResponse,
  RevenueStatsResponse,
  RevenueYearsResponse,
} from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get } = useApi()
const { formatCurrency } = useFormat()

const isLoading = ref(true)
const selectedYear = ref(new Date().getFullYear())
const availableYears = ref<number[]>([2025, 2026, 2027])
const selectedHouseId = ref('')
const boardingHouses = ref<BoardingHouse[]>([])

const revenueStats = ref<RevenueStatsResponse | null>(null)
const profitStats = ref<ProfitStatsResponse | null>(null)
const occupancyStats = ref<OccupancyStatsResponse | null>(null)

// 12-month series in millions VNĐ for chart
const monthlyRevenueArray = computed(() => {
  const arr = new Array(12).fill(0)
  if (profitStats.value?.monthlyBreakdown) {
    for (const item of profitStats.value.monthlyBreakdown) {
      if (item.month >= 1 && item.month <= 12) {
        arr[item.month - 1] = Math.round((Number(item.revenue) / 1000000) * 10) / 10
      }
    }
  }
  return arr
})

const monthlyExpenseArray = computed(() => {
  const arr = new Array(12).fill(0)
  if (profitStats.value?.monthlyBreakdown) {
    for (const item of profitStats.value.monthlyBreakdown) {
      if (item.month >= 1 && item.month <= 12) {
        arr[item.month - 1] = Math.round((Number(item.expense) / 1000000) * 10) / 10
      }
    }
  }
  return arr
})

const fetchYearsAndHouses = async () => {
  try {
    const [yearsData, housesData] = await Promise.all([
      get<RevenueYearsResponse>('/my/stats/revenue/years'),
      get<PagedResult<BoardingHouse>>('/my/boarding-houses', { pageSize: 50 }),
    ])
    if (yearsData.years && yearsData.years.length > 0) {
      availableYears.value = yearsData.years
      if (!availableYears.value.includes(selectedYear.value)) {
        selectedYear.value = availableYears.value[0]
      }
    }
    boardingHouses.value = housesData.items || []
  } catch {
    // Keep defaults
  }
}

const fetchAllStats = async () => {
  isLoading.value = true
  try {
    const params = {
      year: selectedYear.value,
      boardingHouseId: selectedHouseId.value || undefined,
    }
    const [revData, profitData, occData] = await Promise.all([
      get<RevenueStatsResponse>('/my/stats/revenue', params),
      get<ProfitStatsResponse>('/my/stats/profit', params),
      get<OccupancyStatsResponse>('/my/stats/occupancy'),
    ])
    revenueStats.value = revData
    profitStats.value = profitData
    occupancyStats.value = occData
  } catch {
    // Keep defaults
  } finally {
    isLoading.value = false
  }
}

onMounted(async () => {
  await fetchYearsAndHouses()
  await fetchAllStats()
})
</script>
