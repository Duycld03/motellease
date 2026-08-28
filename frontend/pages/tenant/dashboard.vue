<template>
  <div class="space-y-8">
    <!-- Welcome Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">
          {{ $t('tenantDashboard.welcome', { name: user?.fullName || '' }) }}
        </h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('tenantDashboard.subtitle') }}
        </p>
      </div>

      <NuxtLinkLocale to="/search" class="btn-primary !text-xs !py-2 !px-4">
        + {{ $t('tenantDashboard.findNewRooms') }}
      </NuxtLinkLocale>
    </div>

    <!-- Quick Stats Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-400 flex items-center justify-center font-bold">
          🏠
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('tenantDashboard.activeLeases') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">{{ $t('tenantDashboard.oneRoom', { count: 1 }) }}</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-sky-100 dark:bg-sky-950/60 text-sky-700 dark:text-sky-400 flex items-center justify-center font-bold">
          💳
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('tenantDashboard.billsToPay') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">0</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-amber-100 dark:bg-amber-950/60 text-amber-700 dark:text-amber-400 flex items-center justify-center font-bold">
          📅
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('tenantDashboard.upcomingAppointments') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">0</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-purple-100 dark:bg-purple-950/60 text-purple-700 dark:text-purple-400 flex items-center justify-center font-bold">
          🔧
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('tenantDashboard.maintenanceRequests') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">0</span>
        </div>
      </BaseCard>
    </div>

    <!-- Active Lease Section -->
    <BaseCard :title="$t('tenantDashboard.currentLeaseTitle')">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200/80 dark:border-slate-800">
        <div>
          <div class="flex items-center gap-2 mb-1">
            <StatusBadge type="LeaseStatus" status="Active" />
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200">{{ $t('property.room') }} 201 - Nhà trọ MotelLease An Phú</span>
          </div>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {{ $t('tenantDashboard.leaseSampleDesc', { start: '01/01/2026', end: '31/12/2026', price: '3.500.000 ₫' }) }}
          </p>
        </div>

        <div class="flex items-center gap-2">
          <NuxtLinkLocale to="/tenant/leases" class="btn-secondary !text-xs !py-1.5 !px-3">
            {{ $t('tenantDashboard.viewLease') }}
          </NuxtLinkLocale>
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
