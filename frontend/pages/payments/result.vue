<template>
  <div class="max-w-xl mx-auto px-4 py-12 space-y-6">
    <!-- Success State -->
    <div
      v-if="outcome === 'Succeeded'"
      class="bg-white dark:bg-slate-900 rounded-3xl p-6 sm:p-8 border border-emerald-200 dark:border-emerald-900/60 shadow-lg text-center space-y-6"
    >
      <div class="w-16 h-16 bg-emerald-100 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 rounded-2xl flex items-center justify-center text-3xl mx-auto shadow-sm">
        ✓
      </div>

      <div class="space-y-1">
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">Thanh toán Thành công!</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          Giao dịch thanh toán của bạn đã được xác nhận và ghi nhận an toàn vào hệ thống.
        </p>
      </div>

      <!-- Transaction details box -->
      <div v-if="transaction" class="p-4 bg-slate-50 dark:bg-slate-800/60 rounded-2xl border border-slate-100 dark:border-slate-800 text-xs space-y-2.5 text-left">
        <div class="flex items-center justify-between">
          <span class="text-slate-500 dark:text-slate-400">Mã giao dịch:</span>
          <span class="font-mono font-bold text-slate-800 dark:text-slate-200">{{ transaction.providerOrderId }}</span>
        </div>
        <div class="flex items-center justify-between">
          <span class="text-slate-500 dark:text-slate-400">Cổng thanh toán:</span>
          <span class="font-semibold text-slate-800 dark:text-slate-200">{{ transaction.provider }}</span>
        </div>
        <div class="flex items-center justify-between">
          <span class="text-slate-500 dark:text-slate-400">Thời gian thực hiện:</span>
          <span class="text-slate-800 dark:text-slate-200">{{ formatRelativeTime(transaction.completedAt || transaction.initiatedAt) }}</span>
        </div>
        <div class="flex items-center justify-between pt-2 border-t border-slate-200/60 dark:border-slate-700">
          <span class="text-slate-600 dark:text-slate-400 font-medium">Số tiền thanh toán:</span>
          <span class="text-base font-extrabold text-emerald-600 dark:text-emerald-400">{{ formatCurrency(transaction.amount) }}</span>
        </div>
      </div>

      <div class="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
        <BaseButton variant="primary" size="md" class="w-full sm:w-auto" @click="navigateTo('/tenant/deposits')">
          Xem yêu cầu đặt cọc của tôi
        </BaseButton>
        <BaseButton variant="outline" size="md" class="w-full sm:w-auto" @click="navigateTo('/')">
          Về trang chủ
        </BaseButton>
      </div>
    </div>

    <!-- Pending Verification State -->
    <div
      v-else-if="outcome === 'Pending'"
      class="bg-white dark:bg-slate-900 rounded-3xl p-6 sm:p-8 border border-amber-200 dark:border-amber-900/60 shadow-lg text-center space-y-6"
    >
      <div class="w-16 h-16 bg-amber-100 dark:bg-amber-950/60 text-amber-600 dark:text-amber-400 rounded-2xl flex items-center justify-center text-3xl mx-auto shadow-sm">
        ⏳
      </div>

      <div class="space-y-1">
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">Đang xác minh giao dịch...</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          Cổng thanh toán đang xử lý giao dịch của bạn. Hệ thống sẽ tự động cập nhật ngay khi nhận được tín hiệu xác nhận (IPN).
        </p>
      </div>

      <div class="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
        <BaseButton variant="primary" size="md" :loading="isRechecking" @click="recheckTransaction">
          Kiểm tra lại trạng thái
        </BaseButton>
        <BaseButton variant="outline" size="md" @click="navigateTo('/tenant/deposits')">
          Về danh sách đặt cọc
        </BaseButton>
      </div>
    </div>

    <!-- Failed State -->
    <div
      v-else-if="outcome === 'Failed'"
      class="bg-white dark:bg-slate-900 rounded-3xl p-6 sm:p-8 border border-red-200 dark:border-red-900/60 shadow-lg text-center space-y-6"
    >
      <div class="w-16 h-16 bg-red-100 dark:bg-red-950/60 text-red-600 dark:text-red-400 rounded-2xl flex items-center justify-center text-3xl mx-auto shadow-sm">
        ✕
      </div>

      <div class="space-y-1">
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">Thanh toán Không thành công</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          Giao dịch đã bị hủy hoặc gặp lỗi trong quá trình thanh toán tại cổng MoMo/VNPay. Bạn có thể thực hiện lại.
        </p>
      </div>

      <div class="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
        <BaseButton variant="primary" size="md" @click="navigateTo('/tenant/deposits')">
          Thử thanh toán lại
        </BaseButton>
        <BaseButton variant="outline" size="md" @click="navigateTo('/')">
          Về trang chủ
        </BaseButton>
      </div>
    </div>

    <!-- Invalid State -->
    <div
      v-else
      class="bg-white dark:bg-slate-900 rounded-3xl p-6 sm:p-8 border border-slate-200 dark:border-slate-800 shadow-lg text-center space-y-6"
    >
      <div class="w-16 h-16 bg-slate-100 dark:bg-slate-800 text-slate-400 rounded-2xl flex items-center justify-center text-3xl mx-auto shadow-sm">
        ⚠️
      </div>

      <div class="space-y-1">
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">Không tìm thấy thông tin giao dịch</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          Yêu cầu thanh toán không hợp lệ hoặc liên kết đã hết hạn.
        </p>
      </div>

      <div class="pt-2 flex items-center justify-center gap-3">
        <BaseButton variant="primary" size="md" @click="navigateTo('/tenant/deposits')">
          Về trang quản lý đặt cọc
        </BaseButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import type { PaymentTransactionResponse } from '~/types/api'

const route = useRoute()
const { get } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()

const outcome = computed(() => (route.query.outcome as string) || 'Invalid')
const transactionId = computed(() => route.query.transactionId as string)

const transaction = ref<PaymentTransactionResponse | null>(null)
const isRechecking = ref(false)

const fetchTransaction = async () => {
  if (!transactionId.value) return
  try {
    transaction.value = await get<PaymentTransactionResponse>(`/payments/${transactionId.value}`)
  } catch {
    // Ignore if not logged in or transaction not found
  }
}

const recheckTransaction = async () => {
  if (!transactionId.value) return
  isRechecking.value = true
  try {
    const data = await get<PaymentTransactionResponse>(`/payments/${transactionId.value}`)
    transaction.value = data
    if (data.status === 'Succeeded') {
      navigateTo(`/payments/result?outcome=Succeeded&transactionId=${transactionId.value}`)
    }
  } catch {
    // Ignore
  } finally {
    isRechecking.value = false
  }
}

onMounted(() => {
  fetchTransaction()
})
</script>
