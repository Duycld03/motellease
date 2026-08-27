<template>
  <div class="space-y-8">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-slate-900">
          Khu vực Nhân viên
        </h1>
        <p class="text-xs text-slate-500 mt-1">
          Quản lý công việc được giao, xem phòng và ghi chỉ số điện nước
        </p>
      </div>
    </div>

    <!-- Quick Stats -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-sky-100 text-sky-700 flex items-center justify-center font-bold">
          🏢
        </div>
        <div>
          <span class="text-xs text-slate-500 block">Khu trọ được phân công</span>
          <span class="text-lg font-bold text-slate-900 mt-0.5 block">1 khu</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-amber-100 text-amber-700 flex items-center justify-center font-bold">
          ⚡
        </div>
        <div>
          <span class="text-xs text-slate-500 block">Công việc đang xử lý</span>
          <span class="text-lg font-bold text-slate-900 mt-0.5 block">0 việc</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-emerald-100 text-emerald-700 flex items-center justify-center font-bold">
          📅
        </div>
        <div>
          <span class="text-xs text-slate-500 block">Lịch hẹn xem phòng</span>
          <span class="text-lg font-bold text-slate-900 mt-0.5 block">0 hẹn</span>
        </div>
      </BaseCard>
    </div>

    <BaseCard title="Công việc cần làm hôm nay">
      <div class="py-12 text-center text-slate-400 text-xs">
        <p>{{ $t('common.noData') }}</p>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import BaseCard from '~/components/common/BaseCard.vue'

definePageMeta({
  layout: 'staff',
})
</script>
