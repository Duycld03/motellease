<template>
  <div class="space-y-8">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">
          {{ $t('nav.dashboard') }}
        </h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('admin.auditLogsSubtitle') }}
        </p>
      </div>

      <div class="flex items-center gap-3">
        <BaseButton variant="outline" size="sm" @click="fetchAdminStats">
          🔄 {{ $t('common.refresh') }}
        </BaseButton>
      </div>
    </div>

    <!-- Quick Stats Cards (Live Data) -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-primary-100 dark:bg-primary-950 text-primary-700 dark:text-primary-300 flex items-center justify-center font-bold text-xl">
          👥
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('admin.kpiTotalUsers') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ $t('common.accountsCount', { count: stats?.totalUsers ?? 0 }) }}
          </span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-sky-100 dark:bg-sky-950 text-sky-700 dark:text-sky-300 flex items-center justify-center font-bold text-xl">
          🏢
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('admin.kpiBoardingHouses') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ $t('common.housesAndRoomsCount', { houses: stats?.totalBoardingHouses ?? 0, rooms: stats?.totalRooms ?? 0 }) }}
          </span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-emerald-100 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-300 flex items-center justify-center font-bold text-xl">
          💳
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('admin.kpiTransactionVolume') }}</span>
          <span class="text-lg font-bold text-emerald-600 dark:text-emerald-400 mt-0.5 block">
            {{ formatCurrency(stats?.totalTransactionVolume ?? 0) }}
          </span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-300 flex items-center justify-center font-bold text-xl">
          📄
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('admin.kpiActiveLeases') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ $t('common.leasesCount', { count: stats?.activeLeases ?? 0 }) }}
          </span>
        </div>
      </BaseCard>
    </div>

    <!-- Attention Needed Banner -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <NuxtLinkLocale
        to="/admin/withdrawals"
        class="p-4 bg-amber-50 dark:bg-amber-950/40 rounded-2xl border border-amber-200 dark:border-amber-800 flex items-center justify-between hover:bg-amber-100/60 dark:hover:bg-amber-950/60 transition-colors"
      >
        <div class="flex items-center gap-3">
          <span class="text-2xl">💰</span>
          <div>
            <span class="text-xs font-bold text-amber-900 dark:text-amber-200 block">{{ $t('admin.pendingWithdrawalsAlert') }}</span>
            <span class="text-[11px] text-amber-700 dark:text-amber-400">{{ $t('admin.pendingWithdrawalsDesc') }}</span>
          </div>
        </div>
        <span class="px-2.5 py-1 rounded-full text-xs font-black bg-amber-200 dark:bg-amber-900 text-amber-900 dark:text-amber-100">
          {{ stats?.pendingWithdrawals ?? 0 }}
        </span>
      </NuxtLinkLocale>

      <NuxtLinkLocale
        to="/admin/reports"
        class="p-4 bg-red-50 dark:bg-red-950/40 rounded-2xl border border-red-200 dark:border-red-800 flex items-center justify-between hover:bg-red-100/60 dark:hover:bg-red-950/60 transition-colors"
      >
        <div class="flex items-center gap-3">
          <span class="text-2xl">⚠️</span>
          <div>
            <span class="text-xs font-bold text-red-900 dark:text-red-200 block">{{ $t('admin.pendingReportsAlert') }}</span>
            <span class="text-[11px] text-red-700 dark:text-red-400">{{ $t('admin.pendingReportsDesc') }}</span>
          </div>
        </div>
        <span class="px-2.5 py-1 rounded-full text-xs font-black bg-red-200 dark:bg-red-900 text-red-900 dark:text-red-100">
          {{ stats?.pendingReports ?? 0 }}
        </span>
      </NuxtLinkLocale>

      <NuxtLinkLocale
        to="/admin/moderation"
        class="p-4 bg-purple-50 dark:bg-purple-950/40 rounded-2xl border border-purple-200 dark:border-purple-800 flex items-center justify-between hover:bg-purple-100/60 dark:hover:bg-purple-950/60 transition-colors"
      >
        <div class="flex items-center gap-3">
          <span class="text-2xl">🔍</span>
          <div>
            <span class="text-xs font-bold text-purple-900 dark:text-purple-200 block">{{ $t('common.listingModerationCard') }}</span>
            <span class="text-[11px] text-purple-700 dark:text-purple-400">{{ $t('common.pendingListingModerationDesc') }}</span>
          </div>
        </div>
        <span class="px-2.5 py-1 rounded-full text-xs font-black bg-purple-200 dark:bg-purple-900 text-purple-900 dark:text-purple-100">
          {{ stats?.pendingListingReviews ?? 0 }}
        </span>
      </NuxtLinkLocale>
    </div>

    <!-- Quick Links Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <BaseCard :title="$t('common.systemManagementCard')">
        <div class="space-y-2.5 pt-2">
          <NuxtLinkLocale
            to="/admin/users"
            class="p-3.5 bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl border border-slate-100 dark:border-slate-800 flex items-center justify-between transition-colors"
          >
            <div class="flex items-center gap-2.5">
              <span>👥</span>
              <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">{{ $t('common.userAccountsCard') }}</span>
            </div>
            <span class="text-xs text-primary-600 font-semibold">{{ $t('common.viewDetail') }}</span>
          </NuxtLinkLocale>

          <NuxtLinkLocale
            to="/admin/facilities"
            class="p-3.5 bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl border border-slate-100 dark:border-slate-800 flex items-center justify-between transition-colors"
          >
            <div class="flex items-center gap-2.5">
              <span>✨</span>
              <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">{{ $t('common.standardFacilitiesCard') }}</span>
            </div>
            <span class="text-xs text-primary-600 font-semibold">{{ $t('common.viewDetail') }}</span>
          </NuxtLinkLocale>
        </div>
      </BaseCard>

      <BaseCard :title="$t('common.securityAndAuditCard')">
        <div class="space-y-2.5 pt-2">
          <NuxtLinkLocale
            to="/admin/reports"
            class="p-3.5 bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl border border-slate-100 dark:border-slate-800 flex items-center justify-between transition-colors"
          >
            <div class="flex items-center gap-2.5">
              <span>🚨</span>
              <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">{{ $t('common.reportsAndComplaintsCard') }}</span>
            </div>
            <span class="text-xs text-red-600 font-semibold">{{ $t('admin.viewAndHandle') }}</span>
          </NuxtLinkLocale>

          <NuxtLinkLocale
            to="/admin/audit-logs"
            class="p-3.5 bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl border border-slate-100 dark:border-slate-800 flex items-center justify-between transition-colors"
          >
            <div class="flex items-center gap-2.5">
              <span>📜</span>
              <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">{{ $t('common.auditLogsCard') }}</span>
            </div>
            <span class="text-xs text-slate-500 font-semibold">Xem logs →</span>
          </NuxtLinkLocale>
        </div>
      </BaseCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'
import type { AdminPlatformStatsResponse } from '~/types/api'

definePageMeta({
  layout: 'admin',
})

const { get } = useApi()
const { formatCurrency } = useFormat()

const stats = ref<AdminPlatformStatsResponse | null>(null)

const fetchAdminStats = async () => {
  try {
    const data = await get<AdminPlatformStatsResponse>('/admin/stats/summary')
    stats.value = data
  } catch {
    // Keep defaults
  }
}

onMounted(() => {
  fetchAdminStats()
})
</script>
