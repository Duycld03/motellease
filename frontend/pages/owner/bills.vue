<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('common.ownerBillsTitle') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('bills.ownerSubtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchBills">
          🔄 {{ $t('common.refresh') }}
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openCreateBillModal">
          {{ $t('bills.createBill') }}
        </BaseButton>
      </div>
    </div>

    <!-- Month / Year & Status Filters -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <!-- Status Pills -->
      <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            filterStatus === ''
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700',
          ]"
          @click="filterStatus = ''"
        >
          {{ $t('common.allCount', { count: bills.length }) }}
        </button>
        <button
          v-for="st in ['Draft', 'Issued', 'Paid', 'Overdue', 'Cancelled']"
          :key="st"
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            filterStatus === st
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700',
          ]"
          @click="filterStatus = st"
        >
          {{ $t(`enums.BillStatus.${st}`) }}
        </button>
      </div>

      <!-- Month & Year Selector -->
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
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('bills.emptyOwnerBills') }}</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="b in filteredBills"
        :key="b.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <!-- Card Header -->
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-slate-900 dark:text-white">{{ b.boardingHouseName }}</span>
              <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                {{ $t('property.room') }} {{ b.roomNumber }}
              </span>
              <span class="text-xs font-semibold text-primary-600 dark:text-primary-400">
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
              <span class="text-xs text-slate-400 block">{{ $t('bills.totalAmount') }}</span>
              <span class="text-base font-extrabold text-primary-600 dark:text-primary-400">{{ formatCurrency(b.totalAmount) }}</span>
            </div>
            <StatusBadge type="BillStatus" :status="b.status" />
          </div>
        </div>

        <!-- Breakdown Grid -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
          <!-- Rent -->
          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">{{ $t('bills.roomRateAmount') }}</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(b.rentAmount) }}</span>
          </div>

          <!-- Electricity -->
          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">⚡ {{ $t('bills.elecAmountTotal', { usage: b.electricityQty }) }}</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(b.electricityAmount) }}</span>
            <span class="text-[10px] text-slate-400 block mt-0.5">{{ b.electricityOld }} ➔ {{ b.electricityNew }}</span>
          </div>

          <!-- Water -->
          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">💧 {{ $t('bills.waterAmountTotal', { usage: b.waterQty }) }}</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(b.waterAmount) }}</span>
            <span class="text-[10px] text-slate-400 block mt-0.5">{{ b.waterOld }} ➔ {{ b.waterNew }}</span>
          </div>

          <!-- Additional fees -->
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

        <!-- Actions -->
        <div class="flex flex-wrap items-center justify-end gap-2 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton
            variant="outline"
            size="sm"
            :loading="isDownloadingPdfId === b.id"
            @click="handleDownloadPdf(b.id, b.roomNumber, b.month, b.year)"
          >
            📥 {{ $t('bills.downloadInvoicePdf') }}
          </BaseButton>

          <BaseButton
            v-if="b.status === 'Draft'"
            variant="primary"
            size="sm"
            :loading="isIssuingId === b.id"
            @click="handleIssueDraft(b)"
          >
            ⚡ {{ $t('bills.issueBillNowBtn') }}
          </BaseButton>

          <BaseButton
            v-if="b.status === 'Draft' || b.status === 'Issued'"
            variant="ghost"
            size="sm"
            class="text-red-600 hover:text-red-700"
            @click="handleCancelBill(b.id)"
          >
            {{ $t('bills.cancelBillBtn') }}
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Create New Bill -->
    <BaseModal
      v-model="isCreateModalOpen"
      :title="$t('bills.createNewBillModalTitle')"
      max-width="lg"
    >
      <form @submit.prevent="handleSubmitCreateBill" class="space-y-4">
        <!-- Room Selector -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('bills.selectRoom') }} <span class="text-red-500">*</span>
          </label>
          <select v-model="billForm.roomId" class="input-field !text-xs !py-2" @change="onRoomChanged" required>
            <option value="">-- {{ $t('common.select') }} --</option>
            <option v-for="rm in activeLeaseRooms" :key="rm.roomId" :value="rm.roomId">
              {{ rm.boardingHouseName }} - {{ $t('property.room') }} {{ rm.roomNumber }} ({{ rm.primaryTenantFullName }})
            </option>
          </select>
        </div>

        <!-- Month / Year -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              {{ $t('common.month') }} <span class="text-red-500">*</span>
            </label>
            <select v-model.number="billForm.month" class="input-field !text-xs !py-2" @change="fetchBillPreview" required>
              <option v-for="m in 12" :key="m" :value="m">{{ $t('common.month') }} {{ m }}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              {{ $t('common.year') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model.number="billForm.year"
              type="number"
              class="input-field !text-xs !py-2"
              @input="fetchBillPreview"
              required
            />
          </div>
        </div>

        <!-- Meter Readings -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              {{ $t('bills.elecMeterEnd') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model.number="billForm.electricityNew"
              type="number"
              step="0.1"
              min="0"
              class="input-field !text-xs !py-2"
              @input="fetchBillPreview"
              required
            />
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              {{ $t('bills.waterMeterEnd') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model.number="billForm.waterNew"
              type="number"
              step="0.1"
              min="0"
              class="input-field !text-xs !py-2"
              @input="fetchBillPreview"
              required
            />
          </div>
        </div>

        <!-- Due Date -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('deposits.depositDateLabel') }}
          </label>
          <input
            v-model="billForm.dueDate"
            type="date"
            class="input-field !text-xs !py-2"
          />
        </div>

        <!-- Real-time Preview Calculation Box -->
        <div v-if="billPreview" class="p-4 bg-emerald-50 dark:bg-emerald-950/30 rounded-2xl border border-emerald-200 dark:border-emerald-800 text-xs space-y-2">
          <h5 class="font-bold text-emerald-900 dark:text-emerald-200 uppercase text-[11px] tracking-wide">
            {{ $t('bills.calculationPreviewTitle') }}:
          </h5>
          <div class="space-y-1 text-slate-700 dark:text-slate-300 text-[11px]">
            <div class="flex justify-between">
              <span>🏠 {{ $t('bills.roomRateAmount') }}</span>
              <span class="font-bold">{{ formatCurrency(billPreview.rentAmount) }}</span>
            </div>
            <div class="flex justify-between">
              <span>⚡ {{ $t('bills.elecAmountTotal', { usage: billPreview.electricityQty }) }}:</span>
              <span>{{ formatCurrency(billPreview.electricityAmount) }}</span>
            </div>
            <div class="flex justify-between">
              <span>💧 {{ $t('bills.waterAmountTotal', { usage: billPreview.waterQty }) }}:</span>
              <span>{{ formatCurrency(billPreview.waterAmount) }}</span>
            </div>
            <div v-if="billPreview.additionalFeeTotal > 0" class="flex justify-between">
              <span>📋 {{ $t('bills.otherServices') }}:</span>
              <span>{{ formatCurrency(billPreview.additionalFeeTotal) }}</span>
            </div>
          </div>
          <div class="flex justify-between items-center pt-2 border-t border-emerald-200 dark:border-emerald-800">
            <span class="font-bold text-slate-900 dark:text-white">{{ $t('bills.grandTotal') }}</span>
            <span class="text-base font-extrabold text-emerald-700 dark:text-emerald-300">{{ formatCurrency(billPreview.totalAmount) }}</span>
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCreateModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton
            variant="secondary"
            size="sm"
            type="button"
            :loading="isSubmitting"
            @click="submitBillWithStatus('Draft')"
          >
            {{ $t('bills.saveAsDraftBtn') }}
          </BaseButton>
          <BaseButton
            variant="primary"
            size="sm"
            type="button"
            :loading="isSubmitting"
            @click="submitBillWithStatus('Issued')"
          >
            ⚡ {{ $t('bills.issueBillNowBtn') }}
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
import type { BillResponse, LeaseResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get, post, put } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const { t, locale } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const bills = ref<BillResponse[]>([])
const filterStatus = ref('')
const filterMonth = ref<number | null>(new Date().getMonth() + 1)
const filterYear = ref(new Date().getFullYear())

const isOverdue = (b: BillResponse) => {
  if (b.status !== 'Issued' || !b.dueDate) return false
  return new Date(b.dueDate).getTime() < Date.now()
}

const filteredBills = computed(() => {
  let list = bills.value
  if (filterStatus.value) {
    list = list.filter((b) => b.status === filterStatus.value)
  }
  return list
})

const fetchBills = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<BillResponse>>('/bills', {
      status: filterStatus.value || undefined,
      month: filterMonth.value || undefined,
      year: filterYear.value || undefined,
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
const isDownloadingPdfId = ref<string | null>(null)
const handleDownloadPdf = async (billId: string, roomNumber: string, month: number, year: number) => {
  isDownloadingPdfId.value = billId
  try {
    const config = useRuntimeConfig()
    const authStore = useAuthStore()
    const token = authStore.accessToken || useCookie('ml_access_token').value
    const response = await fetch(`${config.public.apiBase}/bills/${billId}/pdf`, {
      headers: {
        Authorization: token ? `Bearer ${token}` : '',
        'Accept-Language': locale.value || 'vi',
      },
    })
    if (!response.ok) {
      let errorMsg = t('messages.actionFailed')
      try {
        const problem = await response.json()
        if (problem.detail) errorMsg = problem.detail
        else if (problem.title) errorMsg = problem.title
      } catch {}
      throw new Error(errorMsg)
    }
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
  } finally {
    isDownloadingPdfId.value = null
  }
}

// Issue Draft
const isIssuingId = ref<string | null>(null)
const handleIssueDraft = async (b: BillResponse) => {
  isIssuingId.value = b.id
  try {
    const nextWeek = new Date()
    nextWeek.setDate(nextWeek.getDate() + 7)
    await put(`/bills/${b.id}/issue`, {
      dueDate: b.dueDate || nextWeek.toISOString().slice(0, 10),
    })
    toast.success(t('messages.issueBillSuccess'))
    await fetchBills()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isIssuingId.value = null
  }
}

// Cancel Bill
const handleCancelBill = async (billId: string) => {
  if (!confirm(t('messages.confirmAction'))) return
  try {
    await put(`/bills/${billId}/cancel`, {})
    toast.success(t('messages.cancelBillSuccess'))
    await fetchBills()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

// Create Bill Modal State
const isCreateModalOpen = ref(false)
const isSubmitting = ref(false)
const activeLeaseRooms = ref<LeaseResponse[]>([])
const billPreview = ref<BillResponse | null>(null)

const billForm = reactive({
  roomId: '',
  month: new Date().getMonth() + 1,
  year: new Date().getFullYear(),
  electricityNew: 0,
  waterNew: 0,
  dueDate: '',
})

const openCreateBillModal = () => {
  const nextWeek = new Date()
  nextWeek.setDate(nextWeek.getDate() + 7)
  billForm.dueDate = nextWeek.toISOString().slice(0, 10)
  billForm.roomId = ''
  billForm.electricityNew = 0
  billForm.waterNew = 0
  billPreview.value = null
  isCreateModalOpen.value = true

  // Load active leases to get occupied rooms
  get<PagedResult<LeaseResponse>>('/leases', { status: 'Active', pageSize: 100 })
    .then((data) => {
      activeLeaseRooms.value = data.items || []
    })
    .catch(() => {
      activeLeaseRooms.value = []
    })
}

const onRoomChanged = () => {
  fetchBillPreview()
}

const fetchBillPreview = async () => {
  if (!billForm.roomId || !billForm.month || !billForm.year) return
  try {
    const res = await post<BillResponse>('/bills/preview', {
      roomId: billForm.roomId,
      month: billForm.month,
      year: billForm.year,
      electricityNew: billForm.electricityNew || 0,
      waterNew: billForm.waterNew || 0,
    })
    billPreview.value = res
  } catch {
    // Ignore preview errors during typing
  }
}

const submitBillWithStatus = async (status: 'Draft' | 'Issued') => {
  if (!billForm.roomId) {
    toast.error(t('messages.actionFailed'))
    return
  }
  isSubmitting.value = true
  try {
    await post('/bills', {
      roomId: billForm.roomId,
      month: billForm.month,
      year: billForm.year,
      electricityNew: billForm.electricityNew || 0,
      waterNew: billForm.waterNew || 0,
      dueDate: billForm.dueDate || undefined,
      status: status,
    })
    toast.success(status === 'Issued' ? t('messages.issueBillSuccess') : t('messages.saveDraftBillSuccess'))
    isCreateModalOpen.value = false
    await fetchBills()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  fetchBills()
})
</script>
