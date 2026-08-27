<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.withdrawals') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Quản lý số dư khả dụng và gửi yêu cầu rút tiền về tài khoản ngân hàng của bạn
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchData">
          🔄 Làm mới
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openWithdrawModal">
          + Tạo yêu cầu rút tiền
        </BaseButton>
      </div>
    </div>

    <!-- Available Balance Banner -->
    <div class="p-6 bg-gradient-to-br from-slate-900 to-slate-800 dark:from-slate-900 dark:to-slate-950 text-white rounded-3xl shadow-lg flex flex-col sm:flex-row sm:items-center justify-between gap-6">
      <div class="space-y-2">
        <span class="text-xs font-semibold text-slate-300 uppercase tracking-wide block">
          💰 Số dư khả dụng có thể rút
        </span>
        <div class="text-3xl font-black tracking-tight text-emerald-400">
          {{ formatCurrency(summaryStats?.availableBalance || 0) }}
        </div>
        <div class="flex flex-wrap items-center gap-4 text-xs text-slate-400 pt-1">
          <span>Doanh thu tháng này: <strong class="text-white">{{ formatCurrency(summaryStats?.revenueThisMonth || 0) }}</strong></span>
          <span>·</span>
          <span>Lợi nhuận tháng này: <strong class="text-emerald-400">{{ formatCurrency(summaryStats?.profitThisMonth || 0) }}</strong></span>
        </div>
      </div>

      <BaseButton
        variant="primary"
        size="md"
        class="shrink-0 !bg-emerald-500 hover:!bg-emerald-600 text-white font-bold shadow-md"
        @click="openWithdrawModal"
      >
        🏦 Rút tiền về Ngân hàng
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
        Tất cả ({{ requests.length }})
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

    <!-- Requests List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredRequests.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">Không có yêu cầu rút tiền nào.</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="r in filteredRequests"
        :key="r.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-extrabold text-emerald-600 dark:text-emerald-400">
                {{ formatCurrency(r.amount) }}
              </span>
              <span class="text-xs font-semibold text-slate-500 dark:text-slate-400">
                · {{ r.bankName }} (STK: {{ r.bankAccountNumber }})
              </span>
            </div>
            <p class="text-xs text-slate-600 dark:text-slate-400">
              Chủ tài khoản: <strong class="text-slate-900 dark:text-white uppercase">{{ r.bankAccountHolder }}</strong>
              · Tạo lúc: {{ formatRelativeTime(r.createdAt) }}
            </p>
          </div>

          <StatusBadge type="RequestStatus" :status="r.status" />
        </div>

        <div v-if="r.rejectReason" class="p-3 bg-red-50 dark:bg-red-950/30 rounded-xl text-xs text-red-700 dark:text-red-300">
          <strong>Lý do từ chối từ Admin:</strong> {{ r.rejectReason }}
        </div>

        <div v-if="r.processedAt" class="text-[11px] text-slate-500 dark:text-slate-400 pt-2 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <span>Xử lý bởi: {{ r.processedByFullName || 'Quản trị viên' }}</span>
          <span>Thời gian xử lý: {{ formatRelativeTime(r.processedAt) }}</span>
        </div>
      </div>
    </div>

    <!-- MODAL: Create Withdraw Request -->
    <BaseModal
      v-model="isWithdrawModalOpen"
      title="Tạo Yêu cầu Rút tiền về Ngân hàng"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitWithdraw" class="space-y-4">
        <!-- Balance notice -->
        <div class="p-3.5 bg-emerald-50 dark:bg-emerald-950/40 rounded-xl text-xs space-y-1">
          <div class="flex items-center justify-between">
            <span class="text-emerald-800 dark:text-emerald-300 font-medium">Số dư khả dụng:</span>
            <span class="font-extrabold text-emerald-700 dark:text-emerald-300 text-sm">
              {{ formatCurrency(summaryStats?.availableBalance || 0) }}
            </span>
          </div>
        </div>

        <!-- Amount -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Số tiền muốn rút (VNĐ) <span class="text-red-500">*</span>
          </label>
          <input
            v-model.number="withdrawForm.amount"
            type="number"
            min="50000"
            :max="summaryStats?.availableBalance || 0"
            step="10000"
            class="input-field !text-xs !py-2 font-bold"
            placeholder="Tối thiểu 50.000 đ"
            required
          />
        </div>

        <!-- Bank Name -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Ngân hàng thụ hưởng <span class="text-red-500">*</span>
          </label>
          <select v-model="withdrawForm.bankName" class="input-field !text-xs !py-2" required>
            <option value="">-- Chọn ngân hàng --</option>
            <option value="Vietcombank">Vietcombank - Ngân hàng Ngoại Thương</option>
            <option value="MBBank">MB Bank - Ngân hàng Quân Đội</option>
            <option value="Techcombank">Techcombank - Ngân hàng Kỹ Thương</option>
            <option value="VPBank">VPBank - Ngân hàng Việt Nam Thịnh Vượng</option>
            <option value="ACB">ACB - Ngân hàng Á Châu</option>
            <option value="BIDV">BIDV - Ngân hàng Đầu tư & Phát triển</option>
            <option value="VietinBank">VietinBank - Ngân hàng Công Thương</option>
            <option value="TPBank">TPBank - Ngân hàng Tiên Phong</option>
            <option value="Agribank">Agribank - Ngân hàng Nông nghiệp</option>
          </select>
        </div>

        <!-- Account Number -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Số tài khoản ngân hàng <span class="text-red-500">*</span>
          </label>
          <input
            v-model="withdrawForm.bankAccountNumber"
            type="text"
            class="input-field !text-xs !py-2"
            placeholder="VD: 0987654321"
            required
          />
        </div>

        <!-- Account Holder -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Tên chủ tài khoản (Viết hoa không dấu) <span class="text-red-500">*</span>
          </label>
          <input
            v-model="withdrawForm.bankAccountHolder"
            type="text"
            class="input-field !text-xs !py-2 uppercase"
            placeholder="VD: NGUYEN VAN A"
            required
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isWithdrawModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmittingWithdraw">
            Gửi yêu cầu rút tiền
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
import type { DashboardSummaryResponse, PagedResult, WithdrawRequestResponse } from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get, post } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const toast = useToast()

const isLoading = ref(true)
const summaryStats = ref<DashboardSummaryResponse | null>(null)
const requests = ref<WithdrawRequestResponse[]>([])
const filterStatus = ref('')

const filteredRequests = computed(() => {
  if (!filterStatus.value) return requests.value
  return requests.value.filter((r) => r.status === filterStatus.value)
})

const fetchData = async () => {
  isLoading.value = true
  try {
    const [statsData, reqsData] = await Promise.all([
      get<DashboardSummaryResponse>('/my/stats/summary'),
      get<PagedResult<WithdrawRequestResponse>>('/withdraw-requests', { pageSize: 50 }),
    ])
    summaryStats.value = statsData
    requests.value = reqsData.items || []
  } catch {
    // Keep defaults
  } finally {
    isLoading.value = false
  }
}

// Modal
const isWithdrawModalOpen = ref(false)
const isSubmittingWithdraw = ref(false)

const withdrawForm = reactive({
  amount: 500000,
  bankName: '',
  bankAccountNumber: '',
  bankAccountHolder: '',
})

const openWithdrawModal = () => {
  withdrawForm.amount = 500000
  withdrawForm.bankName = ''
  withdrawForm.bankAccountNumber = ''
  withdrawForm.bankAccountHolder = ''
  isWithdrawModalOpen.value = true
}

const handleSubmitWithdraw = async () => {
  if (withdrawForm.amount <= 0) {
    toast.error('Số tiền rút phải lớn hơn 0.')
    return
  }
  if (summaryStats.value && withdrawForm.amount > summaryStats.value.availableBalance) {
    toast.error('Số tiền yêu cầu rút vượt quá số dư khả dụng.')
    return
  }

  isSubmittingWithdraw.value = true
  try {
    await post('/withdraw-requests', {
      amount: withdrawForm.amount,
      bankName: withdrawForm.bankName,
      bankAccountNumber: withdrawForm.bankAccountNumber,
      bankAccountHolder: withdrawForm.bankAccountHolder.toUpperCase(),
    })
    toast.success('Gửi yêu cầu rút tiền thành công! Admin sẽ xử lý chuyển khoản trong 24h.')
    isWithdrawModalOpen.value = false
    await fetchData()
  } catch (err: any) {
    toast.error(err.message || 'Không thể tạo yêu cầu rút tiền.')
  } finally {
    isSubmittingWithdraw.value = false
  }
}

onMounted(() => {
  fetchData()
})
</script>
