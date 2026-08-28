<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.myBills') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('bills.subtitle') }}
        </p>
      </div>
      <BaseButton variant="outline" size="sm" @click="fetchBills">
        🔄 {{ $t('common.refresh') }}
      </BaseButton>
    </div>

    <!-- Status Filters & Month Filter -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
        <button
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            filterStatus === ''
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700',
          ]"
          @click="filterStatus = ''"
        >
          {{ $t('common.allCount', { count: bills.length }) }}
        </button>
        <button
          v-for="st in ['Issued', 'Overdue', 'Paid', 'Cancelled']"
          :key="st"
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            filterStatus === st
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700',
          ]"
          @click="filterStatus = st"
        >
          {{ $t(`enums.BillStatus.${st}`) }}
        </button>
      </div>

      <div class="flex items-center gap-2 shrink-0">
        <select v-model="filterMonth" class="input-field !text-xs !py-1.5 w-28" @change="fetchBills">
          <option :value="null">{{ $t('common.all') }}</option>
          <option v-for="m in 12" :key="m" :value="m">{{ $t('common.month') }} {{ m }}</option>
        </select>
        <select v-model="filterYear" class="input-field !text-xs !py-1.5 w-24" @change="fetchBills">
          <option :value="2025">2025</option>
          <option :value="2026">2026</option>
          <option :value="2027">2027</option>
        </select>
      </div>
    </div>

    <!-- Bills List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredBills.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('bills.emptyTenantBills') }}</p>
    </div>

    <div v-else class="space-y-6">
      <div
        v-for="b in filteredBills"
        :key="b.id"
        class="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-5 transition-all"
      >
        <!-- Header -->
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-slate-900 dark:text-white">{{ b.boardingHouseName }}</span>
              <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-primary-50 dark:bg-primary-950/50 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-800">
                {{ $t('property.room') }} {{ b.roomNumber }}
              </span>
              <span class="text-xs font-bold text-primary-600 dark:text-primary-400">
                · {{ $t('bills.billMonth', { month: b.month, year: b.year }) }}
              </span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              <span v-if="b.issuedAt">{{ $t('common.createdAt', { time: formatRelativeTime(b.issuedAt) }) }}</span>
              <span v-if="b.dueDate"> · {{ $t('deposits.depositDateLabel') }}: <strong :class="isOverdue(b) ? 'text-red-600 dark:text-red-400 font-bold' : 'text-slate-700 dark:text-slate-300'">{{ b.dueDate }}</strong></span>
              <span v-if="b.paidAt"> · {{ $t('enums.BillStatus.Paid') }}: <strong class="text-emerald-600 dark:text-emerald-400">{{ formatRelativeTime(b.paidAt) }}</strong></span>
            </p>
          </div>

          <div class="flex items-center gap-3">
            <div class="text-right">
              <span class="text-xs text-slate-400 block">{{ $t('common.totalAmountDue') }}</span>
              <span class="text-base font-extrabold text-primary-600 dark:text-primary-400">{{ formatCurrency(b.totalAmount) }}</span>
            </div>
            <StatusBadge type="BillStatus" :status="b.status" />
          </div>
        </div>

        <!-- Overdue warning -->
        <div
          v-if="isOverdue(b)"
          class="p-3 bg-red-50 dark:bg-red-950/30 rounded-xl border border-red-200 dark:border-red-800 text-xs text-red-800 dark:text-red-300 flex items-center justify-between gap-2"
        >
          <div class="flex items-center gap-2">
            <span>⚠️</span>
            <span>{{ $t('common.overdueWarning', { date: b.dueDate }) }}</span>
          </div>
        </div>

        <!-- Breakdown Grid -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">{{ $t('bills.roomRateAmount') }}</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(b.rentAmount) }}</span>
          </div>

          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">⚡ {{ $t('bills.elecAmountTotal', { usage: b.electricityQty }) }}</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(b.electricityAmount) }}</span>
            <span class="text-[10px] text-slate-400 block mt-0.5">{{ b.electricityOld }} ➔ {{ b.electricityNew }}</span>
          </div>

          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">💧 {{ $t('bills.waterAmountTotal', { usage: b.waterQty }) }}</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(b.waterAmount) }}</span>
            <span class="text-[10px] text-slate-400 block mt-0.5">{{ b.waterOld }} ➔ {{ b.waterNew }}</span>
          </div>

          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">{{ $t('bills.otherServices') }}</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(b.additionalFeeTotal) }}</span>
            <span class="text-[10px] text-slate-400 block mt-0.5">{{ b.additionalFees?.length || 0 }}</span>
          </div>
        </div>

        <!-- Additional Fees List if any -->
        <div v-if="b.additionalFees && b.additionalFees.length > 0" class="p-3 bg-slate-50/50 dark:bg-slate-800/30 rounded-xl text-xs space-y-1">
          <div class="font-semibold text-slate-700 dark:text-slate-300 text-[11px] mb-1">{{ $t('bills.otherServices') }}:</div>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-2 text-[11px] text-slate-600 dark:text-slate-400">
            <div v-for="fee in b.additionalFees" :key="fee.id" class="flex justify-between">
              <span>• {{ fee.feeName }}:</span>
              <span class="font-medium text-slate-900 dark:text-white">{{ formatCurrency(fee.feeAmount) }}</span>
            </div>
          </div>
        </div>

        <!-- Per-Tenant Split Calculation -->
        <div v-if="b.tenantSplits && b.tenantSplits.length > 1" class="p-3.5 bg-primary-50/40 dark:bg-primary-950/20 rounded-xl border border-primary-100 dark:border-primary-900/40 space-y-2 text-xs">
          <div class="flex items-center justify-between">
            <span class="font-bold text-primary-900 dark:text-primary-200 text-[11px] uppercase tracking-wide">
              👥 {{ $t('common.splitCostPerHead', { count: b.tenantSplits.length }) }}
            </span>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
            <div
              v-for="ts in b.tenantSplits"
              :key="ts.tenantId"
              class="p-2.5 bg-white dark:bg-slate-900 rounded-lg border border-slate-200 dark:border-slate-800 flex items-center justify-between text-[11px]"
            >
              <div class="flex items-center gap-1.5 truncate">
                <span class="font-semibold text-slate-800 dark:text-slate-200 truncate">{{ ts.fullName }}</span>
                <span v-if="ts.isPrimary" class="text-[9px] font-bold text-primary-600 dark:text-primary-400">{{ $t('common.primaryTenantBadge') }}</span>
              </div>
              <span class="font-bold text-primary-600 dark:text-primary-400 ml-2">{{ formatCurrency(ts.amount) }}</span>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex flex-wrap items-center justify-end gap-2 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton
            variant="outline"
            size="sm"
            @click="handleDownloadPdf(b.id, b.roomNumber, b.month, b.year)"
          >
            📥 {{ $t('bills.downloadInvoicePdf') }}
          </BaseButton>

          <BaseButton
            v-if="b.status === 'Issued' || b.status === 'Overdue'"
            variant="primary"
            size="sm"
            @click="openCheckoutModal(b)"
          >
            💳 {{ $t('common.payAmountBtn', { amount: formatCurrency(b.totalAmount) }) }}
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Pay Rent Online (MoMo / VNPay) -->
    <BaseModal
      v-model="isCheckoutModalOpen"
      :title="$t('common.payOnlineModalTitle')"
      max-width="md"
    >
      <div v-if="selectedBill" class="space-y-5">
        <!-- Summary Box -->
        <div class="p-4 bg-primary-50 dark:bg-primary-950/40 rounded-xl border border-primary-200 dark:border-primary-800 space-y-2 text-xs">
          <div class="flex items-center justify-between">
            <span class="text-slate-600 dark:text-slate-400">{{ $t('common.paymentPeriod') }}</span>
            <span class="font-bold text-slate-900 dark:text-white">{{ $t('bills.billMonth', { month: selectedBill.month, year: selectedBill.year }) }}</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-slate-600 dark:text-slate-400">{{ $t('property.room') }}:</span>
            <span class="font-bold text-slate-900 dark:text-white">{{ selectedBill.boardingHouseName }} - P.{{ selectedBill.roomNumber }}</span>
          </div>
          <div class="flex items-center justify-between pt-2 border-t border-primary-200/60 dark:border-primary-800/60">
            <span class="text-slate-600 dark:text-slate-400 font-medium">{{ $t('common.totalToPay') }}</span>
            <span class="text-base font-extrabold text-primary-600 dark:text-primary-400">{{ formatCurrency(selectedBill.totalAmount) }}</span>
          </div>
        </div>

        <!-- Gateway Selector -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-2">{{ $t('deposits.selectGatewayModalTitle') }}:</label>
          <div class="grid grid-cols-2 gap-3">
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
            {{ $t('common.payAmountBtn', { amount: formatCurrency(selectedBill.totalAmount) }) }}
          </BaseButton>
        </div>
      </div>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
import type { BillResponse, PaymentCheckoutResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'tenant',
})

const { get, post } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const { t } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const bills = ref<BillResponse[]>([])
const filterStatus = ref('')
const filterMonth = ref<number | null>(null)
const filterYear = ref(new Date().getFullYear())

const isOverdue = (b: BillResponse) => {
  if (b.status !== 'Issued' || !b.dueDate) return false
  return new Date(b.dueDate).getTime() < Date.now()
}

const filteredBills = computed(() => {
  let list = bills.value
  if (filterStatus.value) {
    if (filterStatus.value === 'Overdue') {
      list = list.filter((b) => isOverdue(b))
    } else {
      list = list.filter((b) => b.status === filterStatus.value)
    }
  }
  return list
})

const fetchBills = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<BillResponse>>('/bills', {
      status: filterStatus.value && filterStatus.value !== 'Overdue' ? filterStatus.value : undefined,
      month: filterMonth.value || undefined,
      year: filterYear.value || undefined,
      page: 1,
      pageSize: 50,
    })
    bills.value = data.items || []
  } catch {
    bills.value = []
  } finally {
    isLoading.value = false
  }
}

// PDF Download
const handleDownloadPdf = async (billId: string, roomNumber: string, month: number, year: number) => {
  try {
    const config = useRuntimeConfig()
    const token = useCookie('auth_token').value
    const response = await fetch(`${config.public.apiBase}/bills/${billId}/pdf`, {
      headers: {
        Authorization: token ? `Bearer ${token}` : '',
        'Accept-Language': locale.value || 'vi',
      },
    })
    if (!response.ok) throw new Error('Cannot download PDF')
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = locale.value === 'en'
      ? `Bill_Room_${roomNumber}_M${month}_${year}.pdf`
      : `Hoa_don_Phong_${roomNumber}_T${month}_${year}.pdf`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    window.URL.revokeObjectURL(url)
    toast.success(t('messages.downloadBillPdfSuccess'))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

// Checkout Modal
const isCheckoutModalOpen = ref(false)
const selectedBill = ref<BillResponse | null>(null)
const selectedGateway = ref<'MoMo' | 'VNPay'>('MoMo')
const isProcessingCheckout = ref(false)

const openCheckoutModal = (b: BillResponse) => {
  selectedBill.value = b
  selectedGateway.value = 'MoMo'
  isCheckoutModalOpen.value = true
}

const handleStartCheckout = async () => {
  if (!selectedBill.value) return
  isProcessingCheckout.value = true
  try {
    const res = await post<PaymentCheckoutResponse>(`/payments/bills/${selectedBill.value.id}/checkout`, {
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

onMounted(() => {
  fetchBills()
})
</script>
