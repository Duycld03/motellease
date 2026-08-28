<template>
  <div class="space-y-8">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">
          {{ $t('ownerDashboard.title') }}
        </h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('ownerDashboard.subtitle') }}
        </p>
      </div>

      <div class="flex items-center gap-3">
        <BaseButton variant="outline" size="sm" @click="fetchSummary">
          🔄 {{ $t('common.refresh') }}
        </BaseButton>
        <NuxtLinkLocale to="/owner/properties" class="btn-primary !text-xs !py-2 !px-4">
          + {{ $t('ownerDashboard.manageProperties') }}
        </NuxtLinkLocale>
      </div>
    </div>

    <!-- Quick Stats Cards (Live Data) -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 flex items-center justify-center font-bold text-xl">
          🏢
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('ownerDashboard.activeProperties') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ $t('ownerDashboard.propertiesAndRoomsCount', { houses: summary?.totalBoardingHouses ?? 0, rooms: summary?.totalRooms ?? 0 }) }}
          </span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-sky-100 dark:bg-sky-950/60 text-sky-700 dark:text-sky-300 flex items-center justify-center font-bold text-xl">
          📊
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('ownerDashboard.occupancyRate') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ Math.round((summary?.occupancyRate ?? 0) * 100) }}% ({{ $t('ownerDashboard.occupiedCount', { count: summary?.occupiedRooms ?? 0 }) }})
          </span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-amber-100 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 flex items-center justify-center font-bold text-xl">
          💰
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('ownerDashboard.availableBalance') }}</span>
          <span class="text-lg font-bold text-emerald-600 dark:text-emerald-400 mt-0.5 block">
            {{ formatCurrency(summary?.availableBalance ?? 0) }}
          </span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-purple-100 dark:bg-purple-950/60 text-purple-700 dark:text-purple-300 flex items-center justify-center font-bold text-xl">
          📄
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('ownerDashboard.activeLeases') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ $t('ownerDashboard.leasesCount', { count: summary?.activeLeases ?? 0 }) }}
          </span>
        </div>
      </BaseCard>
    </div>

    <!-- Financial & Actions Section -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <BaseCard :title="$t('ownerDashboard.monthlyFinanceTitle')" class="lg:col-span-2">
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-2">
          <div class="p-4 bg-emerald-50 dark:bg-emerald-950/30 rounded-2xl border border-emerald-100 dark:border-emerald-900/40">
            <span class="text-[10px] text-emerald-600 dark:text-emerald-400 font-semibold uppercase block">{{ $t('ownerDashboard.collectedRevenue') }}</span>
            <span class="text-xl font-extrabold text-emerald-700 dark:text-emerald-300 mt-1 block">
              {{ formatCurrency(summary?.revenueThisMonth ?? 0) }}
            </span>
          </div>

          <div class="p-4 bg-rose-50 dark:bg-rose-950/30 rounded-2xl border border-rose-100 dark:border-rose-900/40">
            <span class="text-[10px] text-rose-600 dark:text-rose-400 font-semibold uppercase block">{{ $t('ownerDashboard.operatingExpenses') }}</span>
            <span class="text-xl font-extrabold text-rose-700 dark:text-rose-300 mt-1 block">
              {{ formatCurrency(summary?.expensesThisMonth ?? 0) }}
            </span>
          </div>

          <div class="p-4 bg-primary-50 dark:bg-primary-950/30 rounded-2xl border border-primary-100 dark:border-primary-900/40">
            <span class="text-[10px] text-primary-600 dark:text-primary-400 font-semibold uppercase block">{{ $t('ownerDashboard.netProfit') }}</span>
            <span class="text-xl font-extrabold text-primary-700 dark:text-primary-300 mt-1 block">
              {{ formatCurrency(summary?.profitThisMonth ?? 0) }}
            </span>
          </div>
        </div>

        <div class="flex items-center justify-between pt-4 mt-4 border-t border-slate-100 dark:border-slate-800 text-xs">
          <span class="text-slate-500 dark:text-slate-400">
            {{ $t('ownerDashboard.unpaidBillsSummary', { count: summary?.unpaidBillsCount ?? 0, amount: formatCurrency(summary?.unpaidBillsAmount ?? 0) }) }}
          </span>
          <NuxtLinkLocale to="/owner/analytics" class="font-semibold text-primary-600 dark:text-primary-400 hover:underline">
            {{ $t('ownerDashboard.viewDetailReport') }}
          </NuxtLinkLocale>
        </div>
      </BaseCard>

      <BaseCard :title="$t('ownerDashboard.quickAccess')">
        <div class="space-y-2.5 pt-2">
          <NuxtLinkLocale
            to="/owner/bills"
            class="p-3 rounded-xl bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-100 dark:border-slate-800 flex items-center justify-between transition-colors"
          >
            <div class="flex items-center gap-2.5">
              <span>⚡</span>
              <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">{{ $t('ownerDashboard.generateMonthlyBills') }}</span>
            </div>
            <span class="text-slate-400 text-xs">→</span>
          </NuxtLinkLocale>

          <NuxtLinkLocale
            to="/owner/expenses"
            class="p-3 rounded-xl bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-100 dark:border-slate-800 flex items-center justify-between transition-colors"
          >
            <div class="flex items-center gap-2.5">
              <span>🛠️</span>
              <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">{{ $t('ownerDashboard.recordExpense') }}</span>
            </div>
            <span class="text-slate-400 text-xs">→</span>
          </NuxtLinkLocale>

          <NuxtLinkLocale
            to="/owner/withdraw"
            class="p-3 rounded-xl bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-100 dark:border-slate-800 flex items-center justify-between transition-colors"
          >
            <div class="flex items-center gap-2.5">
              <span>🏦</span>
              <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">{{ $t('ownerDashboard.withdrawMoney') }}</span>
            </div>
            <span class="text-slate-400 text-xs">→</span>
          </NuxtLinkLocale>

          <NuxtLinkLocale
            to="/owner/deposits"
            class="p-3 rounded-xl bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-100 dark:border-slate-800 flex items-center justify-between transition-colors"
          >
            <div class="flex items-center gap-2.5">
              <span>🔒</span>
              <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">{{ $t('ownerDashboard.holdingDepositRequests') }}</span>
            </div>
            <span class="text-slate-400 text-xs">→</span>
          </NuxtLinkLocale>
        </div>
      </BaseCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'
import type { DashboardSummaryResponse } from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get } = useApi()
const { formatCurrency } = useFormat()

const summary = ref<DashboardSummaryResponse | null>(null)

const fetchSummary = async () => {
  try {
    const data = await get<DashboardSummaryResponse>('/my/stats/summary')
    summary.value = data
  } catch {
    // Keep defaults
  }
}

onMounted(() => {
  fetchSummary()
})
</script>
