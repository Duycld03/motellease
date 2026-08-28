<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('withdrawals.adminTitle') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('common.adminWithdrawSubtitle') }}
        </p>
      </div>
      <BaseButton variant="outline" size="sm" @click="fetchWithdrawals">
        🔄 {{ $t('common.refresh') }}
      </BaseButton>
    </div>

    <!-- Filter Status Tabs -->
    <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
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
        {{ $t('common.allCount', { count: requests.length }) }}
      </button>
      <button
        v-for="st in ['Pending', 'Accepted', 'Rejected']"
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
        {{ $t(`enums.RequestStatus.${st}`) }}
      </button>
    </div>

    <!-- Requests Table -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredRequests.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('withdrawals.emptyAdminRequests') }}</p>
    </div>

    <div v-else class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm overflow-x-auto">
      <table class="w-full text-left text-xs">
        <thead>
          <tr class="border-b border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400">
            <th class="pb-3 font-semibold">{{ $t('roles.Owner') }}</th>
            <th class="pb-3 font-semibold">{{ $t('common.amount') }}</th>
            <th class="pb-3 font-semibold">{{ $t('withdrawals.bankNameLabel') }}</th>
            <th class="pb-3 font-semibold">{{ $t('common.status') }}</th>
            <th class="pb-3 font-semibold">{{ $t('admin.colTimestamp') }}</th>
            <th class="pb-3 font-semibold text-right">{{ $t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
          <tr
            v-for="r in filteredRequests"
            :key="r.id"
            class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors"
          >
            <td class="py-3 font-bold text-slate-900 dark:text-white">
              {{ r.ownerFullName }}
            </td>
            <td class="py-3 font-extrabold text-emerald-600 dark:text-emerald-400 text-sm">
              {{ formatCurrency(r.amount) }}
            </td>
            <td class="py-3 text-slate-700 dark:text-slate-300">
              <div class="font-bold">{{ r.bankName }}</div>
              <div class="text-[11px] text-slate-500 font-mono">STK: {{ r.bankAccountNumber }}</div>
              <div class="text-[11px] text-slate-500 uppercase font-semibold">{{ r.bankAccountHolder }}</div>
            </td>
            <td class="py-3">
              <StatusBadge type="RequestStatus" :status="r.status" />
              <div v-if="r.rejectReason" class="text-[10px] text-red-500 mt-1 max-w-xs truncate">
                {{ $t('common.rejectionReasonPrefix', { reason: r.rejectReason }) }}
              </div>
            </td>
            <td class="py-3 text-slate-500 dark:text-slate-400">
              {{ formatRelativeTime(r.createdAt) }}
            </td>
            <td class="py-3 text-right">
              <div v-if="r.status === 'Pending'" class="flex items-center justify-end gap-2">
                <BaseButton
                  variant="outline"
                  size="sm"
                  class="text-red-600 hover:text-red-700 !text-xs !py-1"
                  @click="openRejectModal(r)"
                >
                  ✕ {{ $t('withdrawals.rejectModalTitle') }}
                </BaseButton>
                <BaseButton
                  variant="primary"
                  size="sm"
                  class="!text-xs !py-1"
                  :loading="isApprovingId === r.id"
                  @click="handleApprove(r.id)"
                >
                  {{ $t('common.approveOrder') }}
                </BaseButton>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- MODAL: Reject Withdrawal -->
    <BaseModal
      v-model="isRejectModalOpen"
      :title="$t('withdrawals.rejectModalTitle')"
      max-width="md"
    >
      <form @submit.prevent="handleConfirmReject" class="space-y-4">
        <p class="text-xs text-slate-600 dark:text-slate-400">
          {{ $t('common.rejectWithdrawalPrompt', { amount: formatCurrency(selectedReq?.amount || 0), owner: selectedReq?.ownerFullName }) }}
        </p>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('common.rejectListingReasonOptional') }}
          </label>
          <textarea
            v-model="rejectReason"
            rows="3"
            class="input-field !text-xs !py-2"
            :placeholder="$t('withdrawals.rejectReasonRequired')"
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isRejectModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="danger" size="sm" type="submit" :loading="isRejecting">
            {{ $t('withdrawals.confirmRejectWithdraw') }}
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
import type { PagedResult, WithdrawRequestResponse } from '~/types/api'

definePageMeta({
  layout: 'admin',
})

const { get, put } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const { t } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const requests = ref<WithdrawRequestResponse[]>([])
const filterStatus = ref('')

const filteredRequests = computed(() => {
  if (!filterStatus.value) return requests.value
  return requests.value.filter((r) => r.status === filterStatus.value)
})

const fetchWithdrawals = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<WithdrawRequestResponse>>('/withdraw-requests', { pageSize: 100 })
    requests.value = data.items || []
  } catch {
    requests.value = []
  } finally {
    isLoading.value = false
  }
}

// Approve
const isApprovingId = ref<string | null>(null)
const handleApprove = async (id: string) => {
  if (!confirm(t('messages.confirmAction'))) return
  isApprovingId.value = id
  try {
    await put(`/withdraw-requests/${id}/approve`, {})
    toast.success(t('messages.approveWithdrawSuccess'))
    await fetchWithdrawals()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isApprovingId.value = null
  }
}

// Reject
const isRejectModalOpen = ref(false)
const selectedReq = ref<WithdrawRequestResponse | null>(null)
const rejectReason = ref('')
const isRejecting = ref(false)

const openRejectModal = (r: WithdrawRequestResponse) => {
  selectedReq.value = r
  rejectReason.value = ''
  isRejectModalOpen.value = true
}

const handleConfirmReject = async () => {
  if (!selectedReq.value) return
  isRejecting.value = true
  try {
    await put(`/withdraw-requests/${selectedReq.value.id}/reject`, {
      reason: rejectReason.value || undefined,
    })
    toast.success(t('messages.rejectWithdrawSuccess'))
    isRejectModalOpen.value = false
    await fetchWithdrawals()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isRejecting.value = false
  }
}

onMounted(() => {
  fetchWithdrawals()
})
</script>
