<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.myDeposits') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">{{ $t('deposits.subtitle') }}</p>
      </div>
      <BaseButton variant="outline" size="sm" @click="navigateTo(localePath('/search'))">
        {{ $t('saved.findNewRooms') }}
      </BaseButton>
    </div>

    <!-- Filter status tabs -->
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
        v-for="st in ['Pending', 'Accepted', 'Paid', 'Completed', 'Rejected', 'Cancelled']"
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

    <!-- Deposits List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredDeposits.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('deposits.emptyTenantDeposits') }}</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="d in filteredDeposits"
        :key="d.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <!-- Header Info -->
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-slate-900 dark:text-white">{{ d.boardingHouseName }}</span>
              <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                {{ $t('property.room') }} {{ d.roomNumber }}
              </span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
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

        <!-- Accepted Alert: Ready to Pay & Countdown Timer -->
        <div
          v-if="d.status === 'Accepted'"
          class="p-4 bg-emerald-50 dark:bg-emerald-950/40 rounded-xl border border-emerald-200 dark:border-emerald-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
        >
          <div class="flex items-start gap-2.5">
            <span class="text-emerald-600 dark:text-emerald-400 text-lg font-bold">🎉</span>
            <div>
              <h4 class="text-xs font-bold text-emerald-900 dark:text-emerald-200">{{ $t('common.approvedHoldNotice') }}</h4>
              <p class="text-xs text-emerald-700 dark:text-emerald-400 mt-0.5">
                {{ $t('common.reviewContractBeforePay') }}
                <strong v-if="d.expiresAt" class="text-red-600 dark:text-red-400 ml-1 font-bold">
                  {{ getTimeRemaining(d.expiresAt) }}
                </strong>
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2 shrink-0">
            <BaseButton variant="outline" size="sm" @click="openContractPreview(d)">
              {{ $t('deposits.viewDraftContract') }}
            </BaseButton>
            <BaseButton variant="primary" size="sm" @click="openCheckoutModal(d)">
              {{ $t('deposits.payDepositOnline') }}
            </BaseButton>
          </div>
        </div>

        <!-- Paid / Completed Info -->
        <div
          v-else-if="d.status === 'Paid'"
          class="p-3.5 bg-blue-50 dark:bg-blue-950/40 rounded-xl border border-blue-200 dark:border-blue-800 flex items-center justify-between gap-3 text-xs"
        >
          <div class="flex items-center gap-2 text-blue-800 dark:text-blue-200">
            <span>✓</span>
            <span>{{ $t('common.tenantDepositPaidNotice') }}</span>
          </div>
          <BaseButton variant="outline" size="sm" @click="openContractPreview(d)">
            {{ $t('common.viewDraftContractShort') }}
          </BaseButton>
        </div>

        <!-- Rejection / Cancellation Reason -->
        <div
          v-if="d.reasonForCancel"
          class="p-3 bg-red-50 dark:bg-red-950/30 rounded-xl border border-red-200 dark:border-red-800 text-xs text-red-700 dark:text-red-300"
        >
          <span class="font-bold">{{ $t('common.reasonPrefix', { reason: d.reasonForCancel }) }}</span>
        </div>

        <!-- Actions for Pending -->
        <div v-if="d.status === 'Pending'" class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800">
          <span class="text-xs text-slate-400 mr-auto">{{ $t('common.waitingOwnerApproval') }}</span>
          <BaseButton variant="ghost" size="sm" class="text-red-600 hover:text-red-700" @click="openCancelModal(d)">
            {{ $t('deposits.cancelDepositBtn') }}
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL 1: Contract Preview -->
    <BaseModal
      v-model="isContractModalOpen"
      :title="$t('deposits.contractDraftTitle')"
      max-width="2xl"
    >
      <div v-if="isLoadingContract" class="py-12 text-center">
        <LoadingSpinner size="md" />
      </div>

      <div v-else-if="contractPreview" class="space-y-6 text-xs text-slate-700 dark:text-slate-300">
        <!-- Contract Header -->
        <div class="text-center pb-4 border-b border-slate-200 dark:border-slate-800 space-y-1">
          <h3 class="text-sm font-bold uppercase tracking-wider text-slate-900 dark:text-white">
            {{ $t('common.draftContractHeaderCountry') }}
          </h3>
          <h4 class="text-base font-bold text-primary-700 dark:text-primary-400 pt-3">
            {{ $t('common.draftContractHeaderTitle') }}
          </h4>
        </div>

        <!-- Party Info -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700">
          <div>
            <h5 class="font-bold text-slate-900 dark:text-white mb-1">{{ $t('property.landlord') }}:</h5>
            <p><strong>{{ $t('common.branchLabel', { name: contractPreview.boardingHouseName }) }}</strong></p>
            <p><strong>{{ $t('common.addressLabel', { address: `${contractPreview.addressLine}, ${contractPreview.ward}, ${contractPreview.district}, ${contractPreview.province}` }) }}</strong></p>
          </div>
          <div>
            <h5 class="font-bold text-slate-900 dark:text-white mb-1">{{ $t('roles.Tenant') }}:</h5>
            <p><strong>{{ $t('common.tenantFullNameLabel', { name: contractPreview.tenantFullName }) }}</strong></p>
            <p><strong>{{ $t('common.tenantPhoneLabel', { phone: contractPreview.tenantPhoneNumber || $t('common.defaultProfilePhone') }) }}</strong></p>
          </div>
        </div>

        <!-- Lease Terms -->
        <div class="space-y-3">
          <h5 class="font-bold text-slate-900 dark:text-white text-xs uppercase tracking-wide">
            {{ $t('common.agreedTermsTitle') }}
          </h5>
          <ul class="space-y-2 list-disc list-inside">
            <li>
              <strong>{{ $t('common.leasedRoomTerm', { room: contractPreview.roomNumber }) }}</strong>
            </li>
            <li>
              <strong>{{ $t('common.leaseDurationTerm', { months: contractPreview.termMonths, start: contractPreview.startDate, end: contractPreview.endDate }) }}</strong>
            </li>
            <li>
              <strong>{{ $t('common.monthlyRentTerm', { amount: formatCurrency(contractPreview.monthlyRent) }) }}</strong>
            </li>
            <li>
              <strong>{{ $t('common.depositSecurityTerm', { amount: formatCurrency(contractPreview.depositHeld) }) }}</strong>
            </li>
          </ul>
        </div>

        <div class="p-3 bg-blue-50 dark:bg-blue-950/40 rounded-xl text-[11px] text-blue-800 dark:text-blue-300">
          {{ $t('common.draftContractDisclaimer') }}
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" @click="isContractModalOpen = false">
            {{ $t('common.close') }}
          </BaseButton>
          <BaseButton
            v-if="selectedDeposit?.status === 'Accepted'"
            variant="primary"
            size="sm"
            @click="isContractModalOpen = false; openCheckoutModal(selectedDeposit!)"
          >
            {{ $t('common.proceedDepositPaymentBtn') }}
          </BaseButton>
        </div>
      </div>
    </BaseModal>

    <!-- MODAL 2: Checkout (MoMo / VNPay) -->
    <BaseModal
      v-model="isCheckoutModalOpen"
      :title="$t('deposits.selectGatewayModalTitle')"
      max-width="md"
    >
      <div v-if="selectedDeposit" class="space-y-5">
        <!-- Summary Box -->
        <div class="p-4 bg-primary-50 dark:bg-primary-950/40 rounded-xl border border-primary-200 dark:border-primary-800 space-y-2 text-xs">
          <div class="flex items-center justify-between">
            <span class="text-slate-600 dark:text-slate-400">{{ $t('property.room') }}:</span>
            <span class="font-bold text-slate-900 dark:text-white">{{ selectedDeposit.boardingHouseName }} - P.{{ selectedDeposit.roomNumber }}</span>
          </div>
          <div class="flex items-center justify-between pt-2 border-t border-primary-200/60 dark:border-primary-800/60">
            <span class="text-slate-600 dark:text-slate-400 font-medium">{{ $t('common.totalToPay') }}</span>
            <span class="text-base font-extrabold text-primary-600 dark:text-primary-400">{{ formatCurrency(selectedDeposit.amount) }}</span>
          </div>
        </div>

        <!-- Payment Gateway Selection -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-2">{{ $t('deposits.selectGatewayModalTitle') }}:</label>
          <div class="grid grid-cols-2 gap-3">
            <!-- MoMo -->
            <label
              :class="[
                'p-3.5 rounded-xl border-2 cursor-pointer flex flex-col items-center justify-center text-center transition-all select-none',
                selectedGateway === 'MoMo'
                  ? 'border-pink-500 bg-pink-50/50 dark:bg-pink-950/30 text-pink-700 dark:text-pink-300 font-bold'
                  : 'border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700 text-slate-600 dark:text-slate-400',
              ]"
            >
              <input type="radio" value="MoMo" v-model="selectedGateway" class="sr-only" />
              <span class="text-2xl mb-1">🌸</span>
              <span class="text-xs">{{ $t('common.momoWallet') }}</span>
              <span class="text-[10px] text-slate-400 font-normal mt-0.5">{{ $t('common.scanQrMomo') }}</span>
            </label>

            <!-- VNPay -->
            <label
              :class="[
                'p-3.5 rounded-xl border-2 cursor-pointer flex flex-col items-center justify-center text-center transition-all select-none',
                selectedGateway === 'VNPay'
                  ? 'border-blue-500 bg-blue-50/50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-300 font-bold'
                  : 'border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700 text-slate-600 dark:text-slate-400',
              ]"
            >
              <input type="radio" value="VNPay" v-model="selectedGateway" class="sr-only" />
              <span class="text-2xl mb-1">🏦</span>
              <span class="text-xs">{{ $t('common.vnpayGateway') }}</span>
              <span class="text-[10px] text-slate-400 font-normal mt-0.5">ATM / Visa / QR VNPay</span>
            </label>
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCheckoutModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" :loading="isProcessingCheckout" @click="handleStartCheckout">
            {{ $t('common.payAmountBtn', { amount: formatCurrency(selectedDeposit.amount) }) }}
          </BaseButton>
        </div>
      </div>
    </BaseModal>

    <!-- MODAL 3: Cancel Request -->
    <BaseModal
      v-model="isCancelModalOpen"
      :title="$t('deposits.cancelModalTitle')"
      max-width="md"
    >
      <form @submit.prevent="handleConfirmCancel" class="space-y-4">
        <p class="text-xs text-slate-600 dark:text-slate-400">
          {{ $t('common.confirmCancelDepositPrompt', { house: selectedDeposit?.boardingHouseName, room: selectedDeposit?.roomNumber }) }}
        </p>

        <div>
          <label class="block text-xs font-medium text-slate-700 dark:text-slate-300 mb-1">{{ $t('deposits.cancelReasonOptional') }}</label>
          <textarea
            v-model="cancelReason"
            rows="3"
            class="input-field !text-xs !py-2"
            :placeholder="$t('common.cancelDepositTenantPlaceholder')"
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCancelModalOpen = false">
            {{ $t('common.backBtn') }}
          </BaseButton>
          <BaseButton variant="danger" size="sm" type="submit" :loading="isCancelling">
            {{ $t('deposits.confirmCancelDeposit') }}
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
import type {
  DepositResponse,
  DepositContractPreviewResponse,
  PaymentCheckoutResponse,
  PagedResult,
} from '~/types/api'

definePageMeta({
  layout: 'tenant',
})

const { get, post, put } = useApi()
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

const isExpiringSoon = (d: DepositResponse) => {
  if (d.status !== 'Approved' || !d.expiresAt) return false
  const diff = new Date(d.expiresAt).getTime() - Date.now()
  return diff > 0 && diff < 6 * 60 * 60 * 1000 // < 6 hours
}

const getTimeRemaining = (expiresAt: string) => {
  const diff = new Date(expiresAt).getTime() - Date.now()
  if (diff <= 0) return '0h 0m'
  const hours = Math.floor(diff / (1000 * 60 * 60))
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
  return `${hours}h ${minutes}m`
}

// Contract Draft Modal
const isContractModalOpen = ref(false)
const selectedDeposit = ref<DepositResponse | null>(null)
const contractPreview = ref<DepositContractPreviewResponse | null>(null)
const isLoadingContract = ref(false)

const openContractPreview = async (d: DepositResponse) => {
  selectedDeposit.value = d
  isContractModalOpen.value = true
  isLoadingContract.value = true
  try {
    contractPreview.value = await get<DepositContractPreviewResponse>(`/deposits/${d.id}/contract-preview`)
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
    isContractModalOpen.value = false
  } finally {
    isLoadingContract.value = false
  }
}
const openContractModal = openContractPreview

// Checkout Modal
const isCheckoutModalOpen = ref(false)
const selectedGateway = ref<'MoMo' | 'VNPay'>('MoMo')
const isProcessingCheckout = ref(false)

const openCheckoutModal = (d: DepositResponse) => {
  selectedDeposit.value = d
  selectedGateway.value = 'MoMo'
  isCheckoutModalOpen.value = true
}

const handleStartCheckout = async () => {
  if (!selectedDeposit.value) return
  isProcessingCheckout.value = true
  try {
    const res = await post<PaymentCheckoutResponse>(`/deposits/${selectedDeposit.value.id}/checkout`, {
      provider: selectedGateway.value,
    })
    if (res.paymentUrl) {
      toast.success(t('messages.redirectingToPayment'))
      window.location.href = res.paymentUrl
    }
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isProcessingCheckout.value = false
  }
}

// Cancel Request
const isCancelModalOpen = ref(false)
const cancelReason = ref('')
const isCancelling = ref(false)

const openCancelModal = (d: DepositResponse) => {
  selectedDeposit.value = d
  cancelReason.value = ''
  isCancelModalOpen.value = true
}

const handleConfirmCancel = async () => {
  if (!selectedDeposit.value) return
  isCancelling.value = true
  try {
    await put(`/deposits/${selectedDeposit.value.id}/cancel`, {
      reason: cancelReason.value || undefined,
    })
    toast.success(t('messages.cancelDepositSuccess'))
    isCancelModalOpen.value = false
    await fetchDeposits()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isCancelling.value = false
  }
}

onMounted(() => {
  fetchDeposits()
})
</script>
