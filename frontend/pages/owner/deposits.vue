<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('common.ownerDepositsTitle') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('common.ownerDepositsSubtitleFull') }}
        </p>
      </div>
    </div>

    <!-- Filter status pills -->
    <div class="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none">
      <button
        type="button"
        :class="[
          'px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
          filterStatus === ''
            ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
            : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
        ]"
        @click="filterStatus = ''"
      >
        {{ $t('common.allCount', { count: deposits.length }) }}
      </button>
      <button
        v-for="st in ['Pending', 'Accepted', 'Paid', 'Completed', 'Rejected', 'Expired']"
        :key="st"
        type="button"
        :class="[
          'px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
          filterStatus === st
            ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
            : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
        ]"
        @click="filterStatus = st"
      >
        {{ $t(`enums.DepositStatus.${st}`) }}
      </button>
    </div>

    <!-- Deposits Table / Cards -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredDeposits.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('common.noData') }}</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="d in filteredDeposits"
        :key="d.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-slate-900 dark:text-white">{{ d.boardingHouseName }}</span>
              <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                {{ $t('property.room') }} {{ d.roomNumber }}
              </span>
            </div>

            <!-- Tenant Contact -->
            <div class="flex flex-wrap items-center gap-2 text-xs text-slate-600 dark:text-slate-400 pt-0.5">
              <span>👤 {{ $t('common.prospectiveTenant', { name: d.tenantFullName }) }}</span>
              <span v-if="d.tenantPhoneNumber" class="text-primary-600 dark:text-primary-400 font-semibold">
                · 📞 <a :href="`tel:${d.tenantPhoneNumber}`" class="hover:underline">{{ d.tenantPhoneNumber }}</a>
              </span>
            </div>

            <p class="text-[11px] text-slate-400">
              {{ $t('common.depositRequestMeta', { time: formatRelativeTime(d.createdAt), date: d.requestedStartDate, months: d.requestedTermMonths }) }}
            </p>
          </div>

          <div class="flex items-center gap-3">
            <div class="text-right">
              <span class="text-xs text-slate-400 block">{{ $t('deposits.depositAmountLabel') }}</span>
              <span class="text-base font-extrabold text-primary-600 dark:text-primary-400">{{ formatCurrency(d.amount) }}</span>
            </div>
            <StatusBadge type="DepositStatus" :status="d.status" />
          </div>
        </div>

        <!-- Notification Banner depending on state -->
        <div
          v-if="d.status === 'Accepted'"
          class="p-3 bg-amber-50 dark:bg-amber-950/30 rounded-xl border border-amber-200 dark:border-amber-800 text-xs text-amber-800 dark:text-amber-300 flex items-center justify-between gap-3"
        >
          <div class="flex items-center gap-2">
            <span>⏳</span>
            <span>{{ $t('common.approvedHoldNotice') }}</span>
          </div>
          <span v-if="d.expiresAt" class="font-bold text-red-600 dark:text-red-400 shrink-0">
            {{ getTimeRemaining(d.expiresAt) }}
          </span>
        </div>

        <div
          v-else-if="d.status === 'Paid'"
          class="p-4 bg-emerald-50 dark:bg-emerald-950/40 rounded-xl border border-emerald-200 dark:border-emerald-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
        >
          <div class="flex items-start gap-2.5">
            <span class="text-emerald-600 dark:text-emerald-400 text-lg font-bold">💰</span>
            <div>
              <h4 class="text-xs font-bold text-emerald-900 dark:text-emerald-200">{{ $t('common.depositSecuredAlert') }}</h4>
              <p class="text-xs text-emerald-700 dark:text-emerald-400 mt-0.5">
                {{ $t('common.handoverReadyNotice') }}
              </p>
            </div>
          </div>

          <BaseButton
            variant="primary"
            size="sm"
            class="shrink-0 font-bold"
            :loading="isConfirmingLeaseId === d.id"
            @click="handleConfirmLease(d)"
          >
            {{ $t('deposits.confirmLease') }}
          </BaseButton>
        </div>

        <div
          v-else-if="d.status === 'Completed'"
          class="p-3 bg-blue-50 dark:bg-blue-950/30 rounded-xl border border-blue-200 dark:border-blue-800 text-xs text-blue-800 dark:text-blue-300 flex items-center justify-between gap-2"
        >
          <span>{{ $t('common.depositConvertedNotice') }}</span>
          <BaseButton variant="outline" size="sm" @click="navigateTo(localePath('/owner/leases'))">
            {{ $t('common.viewLeasesListBtn') }}
          </BaseButton>
        </div>

        <div
          v-if="d.reasonForCancel"
          class="p-3 bg-red-50 dark:bg-red-950/30 rounded-xl border border-red-200 dark:border-red-800 text-xs text-red-700 dark:text-red-300"
        >
          <span class="font-bold">{{ $t('common.rejectOrCancelReasonLabel', { reason: d.reasonForCancel }) }}</span>
        </div>

        <!-- Actions for Pending -->
        <div v-if="d.status === 'Pending'" class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800">
          <BaseButton
            variant="outline"
            size="sm"
            class="text-red-600 dark:text-red-400 border-red-200 dark:border-red-800 hover:bg-red-50 dark:hover:bg-red-950/40"
            @click="openRejectModal(d)"
          >
            {{ $t('common.rejectListingAction') }}
          </BaseButton>
          <BaseButton
            variant="primary"
            size="sm"
            :loading="isApprovingId === d.id"
            @click="handleApprove(d)"
          >
            {{ $t('deposits.approveDeposit24h') }}
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Reject Deposit Request -->
    <BaseModal
      v-model="isRejectModalOpen"
      :title="$t('deposits.rejectModalTitle')"
      max-width="md"
    >
      <form @submit.prevent="handleConfirmReject" class="space-y-4">
        <p class="text-xs text-slate-600 dark:text-slate-400">
          {{ $t('deposits.rejectPrompt', { tenant: selectedDeposit?.tenantFullName }) }}
        </p>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('deposits.rejectReasonRequired') }} <span class="text-red-500">*</span>
          </label>
          <textarea
            v-model="rejectReason"
            rows="3"
            class="input-field !text-xs !py-2"
            :placeholder="$t('common.rejectDepositOwnerPlaceholder')"
            required
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isRejectModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="danger" size="sm" type="submit" :loading="isRejecting">
            {{ $t('deposits.confirmRejectDeposit') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
import type { DepositResponse, PagedResult, LeaseResponse } from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get, put, post } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const { t } = useI18n()
const localePath = useLocalePath()
const toast = useToast()

const isLoading = ref(true)
const deposits = ref<DepositResponse[]>([])
const filterStatus = ref('')

const filteredDeposits = computed(() => {
  if (!filterStatus.value) return deposits.value
  return deposits.value.filter((d) => d.status === filterStatus.value)
})

const fetchDeposits = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<DepositResponse>>('/deposits', { page: 1, pageSize: 50 })
    deposits.value = data.items || []
  } catch {
    deposits.value = []
  } finally {
    isLoading.value = false
  }
}

const getTimeRemaining = (expiresAt: string) => {
  const diff = new Date(expiresAt).getTime() - Date.now()
  if (diff <= 0) return '0h 0m'
  const hours = Math.floor(diff / (1000 * 60 * 60))
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
  return `${hours}h ${minutes}m`
}

// Approve
const isApprovingId = ref<string | null>(null)
const handleApprove = async (d: DepositResponse) => {
  isApprovingId.value = d.id
  try {
    await put(`/deposits/${d.id}/approve`, {})
    toast.success(t('messages.approveDepositSuccess'))
    await fetchDeposits()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isApprovingId.value = null
  }
}

// Reject
const isRejectModalOpen = ref(false)
const selectedDeposit = ref<DepositResponse | null>(null)
const rejectReason = ref('')
const isRejecting = ref(false)

const openRejectModal = (d: DepositResponse) => {
  selectedDeposit.value = d
  rejectReason.value = ''
  isRejectModalOpen.value = true
}

const handleConfirmReject = async () => {
  if (!selectedDeposit.value || !rejectReason.value) return
  isRejecting.value = true
  try {
    await put(`/deposits/${selectedDeposit.value.id}/reject`, {
      reason: rejectReason.value,
    })
    toast.success(t('messages.rejectDepositSuccess'))
    isRejectModalOpen.value = false
    await fetchDeposits()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isRejecting.value = false
  }
}

// Confirm Lease
const isConfirmingLeaseId = ref<string | null>(null)
const handleConfirmLease = async (d: DepositResponse) => {
  if (!confirm(t('messages.confirmAction'))) return
  isConfirmingLeaseId.value = d.id
  try {
    await post<LeaseResponse>(`/deposits/${d.id}/confirm-lease`, {})
    toast.success(t('messages.confirmLeaseSuccess'))
    await fetchDeposits()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isConfirmingLeaseId.value = null
  }
}

onMounted(() => {
  fetchDeposits()
})
</script>
