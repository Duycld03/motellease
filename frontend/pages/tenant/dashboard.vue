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
      <NuxtLinkLocale to="/tenant/leases" class="block">
        <BaseCard no-padding custom-class="p-5 flex items-center gap-4 hover:border-slate-300 dark:hover:border-slate-700 transition-all cursor-pointer">
          <div class="w-12 h-12 rounded-xl bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-400 flex items-center justify-center font-bold">
            🏠
          </div>
          <div>
            <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('tenantDashboard.activeLeases') }}</span>
            <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
              {{ $t('tenantDashboard.oneRoom', { count: activeLeasesCount }) }}
            </span>
          </div>
        </BaseCard>
      </NuxtLinkLocale>

      <NuxtLinkLocale to="/tenant/bills" class="block">
        <BaseCard no-padding custom-class="p-5 flex items-center gap-4 hover:border-slate-300 dark:hover:border-slate-700 transition-all cursor-pointer">
          <div class="w-12 h-12 rounded-xl bg-sky-100 dark:bg-sky-950/60 text-sky-700 dark:text-sky-400 flex items-center justify-center font-bold">
            💳
          </div>
          <div>
            <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('tenantDashboard.billsToPay') }}</span>
            <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">{{ unpaidBillsCount }}</span>
          </div>
        </BaseCard>
      </NuxtLinkLocale>

      <NuxtLinkLocale to="/tenant/appointments" class="block">
        <BaseCard no-padding custom-class="p-5 flex items-center gap-4 hover:border-slate-300 dark:hover:border-slate-700 transition-all cursor-pointer">
          <div class="w-12 h-12 rounded-xl bg-amber-100 dark:bg-amber-950/60 text-amber-700 dark:text-amber-400 flex items-center justify-center font-bold">
            📅
          </div>
          <div>
            <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('tenantDashboard.upcomingAppointments') }}</span>
            <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">{{ upcomingAppointmentsCount }}</span>
          </div>
        </BaseCard>
      </NuxtLinkLocale>

      <NuxtLinkLocale to="/tenant/maintenance" class="block">
        <BaseCard no-padding custom-class="p-5 flex items-center gap-4 hover:border-slate-300 dark:hover:border-slate-700 transition-all cursor-pointer">
          <div class="w-12 h-12 rounded-xl bg-purple-100 dark:bg-purple-950/60 text-purple-700 dark:text-purple-400 flex items-center justify-center font-bold">
            🔧
          </div>
          <div>
            <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('tenantDashboard.maintenanceRequests') }}</span>
            <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">{{ maintenanceRequestsCount }}</span>
          </div>
        </BaseCard>
      </NuxtLinkLocale>
    </div>

    <!-- Active Lease Section -->
    <BaseCard :title="$t('tenantDashboard.currentLeaseTitle')">
      <div v-if="isLoading" class="py-8 text-center">
        <LoadingSpinner size="sm" />
      </div>

      <!-- Has Active Lease -->
      <div
        v-else-if="activeLease"
        class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200/80 dark:border-slate-800"
      >
        <div>
          <div class="flex items-center gap-2 mb-1">
            <StatusBadge type="LeaseStatus" :status="activeLease.status" />
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200">
              {{ $t('property.room') }} {{ activeLease.roomNumber }} - {{ activeLease.boardingHouseName }}
            </span>
          </div>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {{
              $t('tenantDashboard.leaseSampleDesc', {
                start: formatDate(activeLease.startDate),
                end: formatDate(activeLease.endDate),
                price: formatCurrency(activeLease.monthlyRent),
              })
            }}
          </p>
        </div>

        <div class="flex items-center gap-2">
          <NuxtLinkLocale to="/tenant/leases" class="btn-secondary !text-xs !py-1.5 !px-3">
            {{ $t('tenantDashboard.viewLease') }}
          </NuxtLinkLocale>
        </div>
      </div>

      <!-- No Active Lease (Empty State) -->
      <div
        v-else
        class="p-6 text-center rounded-xl bg-slate-50 dark:bg-slate-800/40 border border-dashed border-slate-200 dark:border-slate-800 space-y-3"
      >
        <p class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('tenantDashboard.noActiveLease') }}
        </p>
        <NuxtLinkLocale to="/search" class="btn-primary !text-xs !py-1.5 !px-3 inline-flex items-center gap-1.5">
          <span>+</span>
          <span>{{ $t('tenantDashboard.findRoomNow') }}</span>
        </NuxtLinkLocale>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import BaseCard from '~/components/common/BaseCard.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
import type { AppointmentResponse, BillResponse, LeaseResponse, MaintenanceRequestResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'tenant',
})

const { user } = useAuth()
const { get } = useApi()
const { formatCurrency, formatDate } = useFormat()

const isLoading = ref(true)
const activeLease = ref<LeaseResponse | null>(null)
const activeLeasesCount = ref(0)
const unpaidBillsCount = ref(0)
const upcomingAppointmentsCount = ref(0)
const maintenanceRequestsCount = ref(0)

const fetchDashboardData = async () => {
  isLoading.value = true
  try {
    const [leasesRes, billsRes, apptsRes, maintRes] = await Promise.allSettled([
      get<PagedResult<LeaseResponse>>('/leases', { page: 1, pageSize: 50 }),
      get<PagedResult<BillResponse>>('/bills', { page: 1, pageSize: 50 }),
      get<PagedResult<AppointmentResponse>>('/appointments', { page: 1, pageSize: 50 }),
      get<PagedResult<MaintenanceRequestResponse>>('/maintenance', { page: 1, pageSize: 50 }),
    ])

    if (leasesRes.status === 'fulfilled' && leasesRes.value?.items) {
      const items = leasesRes.value.items
      const active = items.filter((l) => l.status === 'Active' || l.status === 'Expiring')
      activeLeasesCount.value = active.length
      activeLease.value = active[0] || null
    }

    if (billsRes.status === 'fulfilled' && billsRes.value?.items) {
      const unpaid = billsRes.value.items.filter((b) => b.status === 'Issued' || b.status === 'Overdue')
      unpaidBillsCount.value = unpaid.length
    }

    if (apptsRes.status === 'fulfilled' && apptsRes.value?.items) {
      const upcoming = apptsRes.value.items.filter((a) => a.status === 'Pending' || a.status === 'Confirmed')
      upcomingAppointmentsCount.value = upcoming.length
    }

    if (maintRes.status === 'fulfilled' && maintRes.value?.items) {
      const ongoing = maintRes.value.items.filter((m) => m.status === 'Submitted' || m.status === 'InProgress')
      maintenanceRequestsCount.value = ongoing.length
    }
  } catch {
    // Non-critical dashboard statistics fetch error
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchDashboardData()
})
</script>
