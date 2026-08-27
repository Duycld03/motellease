<template>
  <div class="space-y-8">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">
          Tổng quan Chủ trọ
        </h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Theo dõi doanh thu, tỷ lệ lấp đầy phòng, đặt cọc và hóa đơn cần xử lý
        </p>
      </div>

      <div class="flex items-center gap-3">
        <NuxtLink to="/owner/properties" class="btn-primary !text-xs !py-2 !px-4">
          + Quản lý khu trọ
        </NuxtLink>
      </div>
    </div>

    <!-- Quick Stats Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 flex items-center justify-center font-bold">
          🏢
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">Khu trọ đang hoạt động</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">2 khu</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-sky-100 dark:bg-sky-950/60 text-sky-700 dark:text-sky-300 flex items-center justify-center font-bold">
          📊
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">Tỷ lệ lấp đầy</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">85%</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-amber-100 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 flex items-center justify-center font-bold">
          💰
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">Cọc đang chờ duyệt</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">1 yêu cầu</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-purple-100 dark:bg-purple-950/60 text-purple-700 dark:text-purple-300 flex items-center justify-center font-bold">
          👥
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">Nhân viên phụ trách</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">2 nhân viên</span>
        </div>
      </BaseCard>
    </div>

    <!-- Quick Navigation Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <BaseCard title="Danh sách khu trọ">
        <div class="p-4 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <div>
            <h4 class="text-xs font-bold text-slate-900 dark:text-white">MotelLease An Phú</h4>
            <span class="text-[10px] text-slate-400">12 phòng · 10 đang thuê</span>
          </div>
          <NuxtLink to="/owner/properties" class="text-xs font-semibold text-primary-600 dark:text-primary-400">
            Xem chi tiết →
          </NuxtLink>
        </div>
      </BaseCard>

      <BaseCard title="Doanh thu tháng này">
        <div class="py-6 text-center">
          <span class="text-2xl font-black text-slate-900 dark:text-white">35.000.000 ₫</span>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">Đã thu từ 10 phòng đang thuê</p>
        </div>
      </BaseCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseCard from '~/components/common/BaseCard.vue'

definePageMeta({
  layout: 'owner',
})
</script>
