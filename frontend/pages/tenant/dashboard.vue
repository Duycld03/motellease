<template>
  <div class="space-y-8">
    <!-- Welcome Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">
          Xin chào, {{ user?.fullName }}
        </h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Quản lý thông tin thuê phòng, hợp đồng và hóa đơn điện nước của bạn
        </p>
      </div>

      <NuxtLink to="/search" class="btn-primary !text-xs !py-2 !px-4">
        + Tìm kiếm phòng mới
      </NuxtLink>
    </div>

    <!-- Quick Stats Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-400 flex items-center justify-center font-bold">
          🏠
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">Hợp đồng đang thuê</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">1 phòng</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-sky-100 dark:bg-sky-950/60 text-sky-700 dark:text-sky-400 flex items-center justify-center font-bold">
          💳
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">Hóa đơn cần thanh toán</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">0</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-amber-100 dark:bg-amber-950/60 text-amber-700 dark:text-amber-400 flex items-center justify-center font-bold">
          📅
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">Lịch hẹn sắp tới</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">0</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-purple-100 dark:bg-purple-950/60 text-purple-700 dark:text-purple-400 flex items-center justify-center font-bold">
          🔧
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">Yêu cầu sửa chữa</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">0</span>
        </div>
      </BaseCard>
    </div>

    <!-- Active Lease Section -->
    <BaseCard title="Hợp đồng thuê hiện tại">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200/80 dark:border-slate-800">
        <div>
          <div class="flex items-center gap-2 mb-1">
            <StatusBadge type="LeaseStatus" status="Active" />
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200">Phòng 201 - Nhà trọ MotelLease An Phú</span>
          </div>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            Thời hạn hợp đồng: 01/01/2026 - 31/12/2026 · Tiền thuê: 3.500.000 ₫/tháng
          </p>
        </div>

        <div class="flex items-center gap-2">
          <NuxtLink to="/tenant/leases" class="btn-secondary !text-xs !py-1.5 !px-3">
            Xem hợp đồng
          </NuxtLink>
        </div>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import BaseCard from '~/components/common/BaseCard.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'

definePageMeta({
  layout: 'tenant',
})

const { user } = useAuth()
</script>
