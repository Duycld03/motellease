<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.myDeposits') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">Quản lý các yêu cầu đặt cọc giữ phòng, xem hợp đồng dự thảo và thanh toán cọc online</p>
      </div>
      <BaseButton variant="outline" size="sm" @click="navigateTo('/search')">
        🔍 Tìm thêm phòng trọ
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
        Tất cả ({{ deposits.length }})
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
      <p class="font-medium text-slate-500 dark:text-slate-400">Không tìm thấy yêu cầu đặt cọc nào.</p>
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
                Phòng {{ d.roomNumber }}
              </span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              Ngày gửi: {{ formatRelativeTime(d.createdAt) }} · Dự kiến chuyển vào: <strong class="text-slate-700 dark:text-slate-300">{{ d.requestedStartDate }}</strong> · Thời hạn: <strong class="text-slate-700 dark:text-slate-300">{{ d.requestedTermMonths }} tháng</strong>
            </p>
          </div>

          <div class="flex items-center gap-3">
            <div class="text-right">
              <span class="text-xs text-slate-400 block">Tiền cọc giữ chỗ</span>
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
              <h4 class="text-xs font-bold text-emerald-900 dark:text-emerald-200">Chủ nhà đã chấp thuận yêu cầu giữ phòng của bạn!</h4>
              <p class="text-xs text-emerald-700 dark:text-emerald-400 mt-0.5">
                Vui lòng xem trước hợp đồng và hoàn tất thanh toán cọc trước khi hết hạn giữ chỗ:
                <strong v-if="d.expiresAt" class="text-red-600 dark:text-red-400 ml-1 font-bold">
                  {{ getTimeRemaining(d.expiresAt) }}
                </strong>
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2 shrink-0">
            <BaseButton variant="outline" size="sm" @click="openContractPreview(d)">
              📄 Xem dự thảo hợp đồng
            </BaseButton>
            <BaseButton variant="primary" size="sm" @click="openCheckoutModal(d)">
              💳 Thanh toán cọc ngay
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
            <span>Đã thanh toán tiền cọc thành công. Chờ chủ nhà chuẩn bị phòng và hoàn tất thủ tục bàn giao.</span>
          </div>
          <BaseButton variant="outline" size="sm" @click="openContractPreview(d)">
            📄 Xem dự thảo HĐ
          </BaseButton>
        </div>

        <!-- Rejection / Cancellation Reason -->
        <div
          v-if="d.reasonForCancel"
          class="p-3 bg-red-50 dark:bg-red-950/30 rounded-xl border border-red-200 dark:border-red-800 text-xs text-red-700 dark:text-red-300"
        >
          <span class="font-bold">Lý do:</span> {{ d.reasonForCancel }}
        </div>

        <!-- Actions for Pending -->
        <div v-if="d.status === 'Pending'" class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800">
          <span class="text-xs text-slate-400 mr-auto">⏳ Đang chờ chủ nhà phê duyệt giữ phòng...</span>
          <BaseButton variant="ghost" size="sm" class="text-red-600 hover:text-red-700" @click="openCancelModal(d)">
            Hủy yêu cầu
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL 1: Contract Preview -->
    <BaseModal
      v-model="isContractModalOpen"
      title="Dự thảo Hợp đồng Thuê phòng"
      max-width="2xl"
    >
      <div v-if="isLoadingContract" class="py-12 text-center">
        <LoadingSpinner size="md" />
      </div>

      <div v-else-if="contractPreview" class="space-y-6 text-xs text-slate-700 dark:text-slate-300">
        <!-- Contract Header -->
        <div class="text-center pb-4 border-b border-slate-200 dark:border-slate-800 space-y-1">
          <h3 class="text-sm font-bold uppercase tracking-wider text-slate-900 dark:text-white">
            CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM
          </h3>
          <p class="text-[11px] font-semibold">Độc lập - Tự do - Hạnh phúc</p>
          <h4 class="text-base font-bold text-primary-700 dark:text-primary-400 pt-3">
            HỢP ĐỒNG THUÊ PHÒNG TRỌ (DỰ THẢO)
          </h4>
        </div>

        <!-- Party Info -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700">
          <div>
            <h5 class="font-bold text-slate-900 dark:text-white mb-1">BÊN CHO THUÊ (BÊN A):</h5>
            <p><strong>Cơ sở:</strong> {{ contractPreview.boardingHouseName }}</p>
            <p><strong>Địa chỉ:</strong> {{ contractPreview.addressLine }}, {{ contractPreview.ward }}, {{ contractPreview.district }}, {{ contractPreview.province }}</p>
          </div>
          <div>
            <h5 class="font-bold text-slate-900 dark:text-white mb-1">BÊN THUÊ (BÊN B):</h5>
            <p><strong>Họ tên:</strong> {{ contractPreview.tenantFullName }}</p>
            <p><strong>Điện thoại:</strong> {{ contractPreview.tenantPhoneNumber || 'Theo hồ sơ tài khoản' }}</p>
          </div>
        </div>

        <!-- Lease Terms -->
        <div class="space-y-3">
          <h5 class="font-bold text-slate-900 dark:text-white text-xs uppercase tracking-wide">
            Điều khoản thỏa thuận:
          </h5>
          <ul class="space-y-2 list-disc list-inside">
            <li>
              <strong>Phòng thuê:</strong> Phòng số <strong>{{ contractPreview.roomNumber }}</strong>.
            </li>
            <li>
              <strong>Thời hạn thuê:</strong> {{ contractPreview.termMonths }} tháng (Từ ngày <strong>{{ contractPreview.startDate }}</strong> đến ngày <strong>{{ contractPreview.endDate }}</strong>).
            </li>
            <li>
              <strong>Giá thuê hàng tháng:</strong> <span class="font-bold text-primary-600 dark:text-primary-400">{{ formatCurrency(contractPreview.monthlyRent) }} / tháng</span> (Cố định trong suốt thời hạn hợp đồng).
            </li>
            <li>
              <strong>Tiền đặt cọc bảo đảm:</strong> <span class="font-bold text-emerald-600 dark:text-emerald-400">{{ formatCurrency(contractPreview.depositHeld) }}</span> (Được bảo toàn và hoàn trả khi kết thúc hợp đồng theo quy định).
            </li>
          </ul>
        </div>

        <div class="p-3 bg-blue-50 dark:bg-blue-950/40 rounded-xl text-[11px] text-blue-800 dark:text-blue-300">
          💡 Đây là bản dự thảo hợp đồng được tạo tự động dựa trên số tiền và thông tin đã chốt. Hợp đồng chính thức sẽ được kích hoạt khi hoàn tất thanh toán cọc và nhận phòng.
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
            Tiến hành thanh toán cọc →
          </BaseButton>
        </div>
      </div>
    </BaseModal>

    <!-- MODAL 2: Checkout (MoMo / VNPay) -->
    <BaseModal
      v-model="isCheckoutModalOpen"
      title="Thanh toán Đặt cọc giữ phòng"
      max-width="md"
    >
      <div v-if="selectedDeposit" class="space-y-5">
        <!-- Summary Box -->
        <div class="p-4 bg-primary-50 dark:bg-primary-950/40 rounded-xl border border-primary-200 dark:border-primary-800 space-y-2 text-xs">
          <div class="flex items-center justify-between">
            <span class="text-slate-600 dark:text-slate-400">Khu trọ / Phòng:</span>
            <span class="font-bold text-slate-900 dark:text-white">{{ selectedDeposit.boardingHouseName }} - P.{{ selectedDeposit.roomNumber }}</span>
          </div>
          <div class="flex items-center justify-between pt-2 border-t border-primary-200/60 dark:border-primary-800/60">
            <span class="text-slate-600 dark:text-slate-400 font-medium">Số tiền cần thanh toán:</span>
            <span class="text-base font-extrabold text-primary-600 dark:text-primary-400">{{ formatCurrency(selectedDeposit.amount) }}</span>
          </div>
        </div>

        <!-- Payment Gateway Selection -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-2">Chọn cổng thanh toán:</label>
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
              <span class="text-xs">Ví MoMo</span>
              <span class="text-[10px] text-slate-400 font-normal mt-0.5">Quét mã QR MoMo</span>
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
              <span class="text-xs">Cổng VNPay</span>
              <span class="text-[10px] text-slate-400 font-normal mt-0.5">ATM / Visa / QR VNPay</span>
            </label>
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCheckoutModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" :loading="isProcessingCheckout" @click="handleStartCheckout">
            Thanh toán {{ formatCurrency(selectedDeposit.amount) }}
          </BaseButton>
        </div>
      </div>
    </BaseModal>

    <!-- MODAL 3: Cancel Request -->
    <BaseModal
      v-model="isCancelModalOpen"
      title="Hủy yêu cầu đặt cọc"
      max-width="md"
    >
      <form @submit.prevent="handleConfirmCancel" class="space-y-4">
        <p class="text-xs text-slate-600 dark:text-slate-400">
          Bạn có chắc chắn muốn hủy yêu cầu đặt cọc giữ phòng <strong>{{ selectedDeposit?.boardingHouseName }} - Phòng {{ selectedDeposit?.roomNumber }}</strong>?
        </p>

        <div>
          <label class="block text-xs font-medium text-slate-700 dark:text-slate-300 mb-1">Lý do hủy (Tùy chọn)</label>
          <textarea
            v-model="cancelReason"
            rows="3"
            class="input-field !text-xs !py-2"
            placeholder="VD: Em đã tìm được phòng khác gần trường hơn..."
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCancelModalOpen = false">
            Quay lại
          </BaseButton>
          <BaseButton variant="danger" size="sm" type="submit" :loading="isCancelling">
            Xác nhận hủy
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

// Countdown timer helper for Accepted deposits
const getTimeRemaining = (expiresAt: string) => {
  const diff = new Date(expiresAt).getTime() - Date.now()
  if (diff <= 0) return 'Đã hết hạn'
  const hours = Math.floor(diff / (1000 * 60 * 60))
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
  return `Còn ${hours} giờ ${minutes} phút`
}

// Contract Preview
const isContractModalOpen = ref(false)
const isLoadingContract = ref(false)
const contractPreview = ref<DepositContractPreviewResponse | null>(null)
const selectedDeposit = ref<DepositResponse | null>(null)

const openContractPreview = async (d: DepositResponse) => {
  selectedDeposit.value = d
  isContractModalOpen.value = true
  isLoadingContract.value = true
  try {
    contractPreview.value = await get<DepositContractPreviewResponse>(`/deposits/${d.id}/contract-preview`)
  } catch (err: any) {
    toast.error(err.message || 'Không thể tải dự thảo hợp đồng.')
    isContractModalOpen.value = false
  } finally {
    isLoadingContract.value = false
  }
}

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
      toast.success('Đang chuyển hướng đến cổng thanh toán...')
      window.location.href = res.paymentUrl
    }
  } catch (err: any) {
    toast.error(err.message || 'Không thể khởi tạo thanh toán. Vui lòng thử lại.')
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
    toast.success('Đã hủy yêu cầu đặt cọc.')
    isCancelModalOpen.value = false
    await fetchDeposits()
  } catch (err: any) {
    toast.error(err.message || 'Không thể hủy yêu cầu.')
  } finally {
    isCancelling.value = false
  }
}

onMounted(() => {
  fetchDeposits()
})
</script>
