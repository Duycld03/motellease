<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900">{{ $t('nav.analytics') }}</h1>
        <p class="text-xs text-slate-500 mt-1">Báo cáo doanh thu 12 tháng, tỷ lệ lấp đầy và lợi nhuận ròng</p>
      </div>
    </div>

    <!-- Quick Stats Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <BaseCard no-padding custom-class="p-5">
        <span class="text-xs text-slate-500 block">Tổng doanh thu năm nay</span>
        <span class="text-xl font-black text-emerald-600 mt-1 block">542.000.000 ₫</span>
      </BaseCard>
      <BaseCard no-padding custom-class="p-5">
        <span class="text-xs text-slate-500 block">Tổng chi phí vận hành</span>
        <span class="text-xl font-black text-rose-500 mt-1 block">145.000.000 ₫</span>
      </BaseCard>
      <BaseCard no-padding custom-class="p-5">
        <span class="text-xs text-slate-500 block">Lợi nhuận ròng (Net Profit)</span>
        <span class="text-xl font-black text-primary-700 mt-1 block">397.000.000 ₫</span>
      </BaseCard>
    </div>

    <!-- Charts Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-2">
        <BaseCard title="Doanh thu & Chi phí 12 tháng gần nhất">
          <ClientOnly>
            <RevenueChart />
            <template #fallback>
              <div class="h-72 flex items-center justify-center text-xs text-slate-400">
                Đang tải biểu đồ...
              </div>
            </template>
          </ClientOnly>
        </BaseCard>
      </div>

      <div>
        <BaseCard title="Tỷ lệ lấp đầy phòng">
          <ClientOnly>
            <OccupancyChart :occupied="18" :reserved="2" :available="4" />
            <template #fallback>
              <div class="h-64 flex items-center justify-center text-xs text-slate-400">
                Đang tải biểu đồ...
              </div>
            </template>
          </ClientOnly>
        </BaseCard>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseCard from '~/components/common/BaseCard.vue'
import RevenueChart from '~/components/common/RevenueChart.client.vue'
import OccupancyChart from '~/components/common/OccupancyChart.client.vue'

definePageMeta({
  layout: 'owner',
})
</script>
