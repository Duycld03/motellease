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
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('payments.succeeded') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('payments.succeededDesc') }}
        </p>
      </div>

      <!-- Transaction details box -->
      <div v-if="transaction" class="p-4 bg-slate-50 dark:bg-slate-800/60 rounded-2xl border border-slate-100 dark:border-slate-800 text-xs space-y-2.5 text-left">
        <div class="flex items-center justify-between">
          <span class="text-slate-500 dark:text-slate-400">{{ $t('payments.txnId') }}:</span>
          <span class="font-mono font-bold text-slate-800 dark:text-slate-200">{{ transaction.providerOrderId }}</span>
        </div>
        <div class="flex items-center justify-between">
          <span class="text-slate-500 dark:text-slate-400">{{ $t('payments.gateway') }}:</span>
          <span class="font-semibold text-slate-800 dark:text-slate-200">{{ $t(`enums.PaymentProvider.${transaction.provider}`) }}</span>
        </div>
        <div class="flex items-center justify-between">
          <span class="text-slate-500 dark:text-slate-400">{{ $t('payments.time') }}:</span>
          <span class="text-slate-800 dark:text-slate-200">{{ formatRelativeTime(transaction.completedAt || transaction.initiatedAt) }}</span>
        </div>
        <div class="flex items-center justify-between pt-2 border-t border-slate-200/60 dark:border-slate-700">
          <span class="text-slate-600 dark:text-slate-400 font-medium">{{ $t('payments.amount') }}:</span>
          <span class="text-base font-extrabold text-emerald-600 dark:text-emerald-400">{{ formatCurrency(transaction.amount) }}</span>
        </div>
      </div>

      <div class="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
        <BaseButton variant="primary" size="md" class="w-full sm:w-auto" @click="navigateTo(localePath('/tenant/deposits'))">
          {{ $t('payments.viewDeposits') }}
        </BaseButton>
        <BaseButton variant="outline" size="md" class="w-full sm:w-auto" @click="navigateTo(localePath('/'))">
          {{ $t('common.backToHome') }}
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
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('payments.pending') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('payments.pendingDesc') }}
        </p>
      </div>

      <div class="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
        <BaseButton variant="primary" size="md" :loading="isRechecking" @click="recheckTransaction">
          {{ $t('common.checkStatusAgain') }}
        </BaseButton>
        <BaseButton variant="outline" size="md" @click="navigateTo(localePath('/tenant/deposits'))">
          {{ $t('common.backToDepositsList') }}
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
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('payments.failed') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('payments.failedDesc') }}
        </p>
      </div>

      <div class="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
        <BaseButton variant="primary" size="md" @click="navigateTo(localePath('/tenant/deposits'))">
          {{ $t('payments.retry') }}
        </BaseButton>
        <BaseButton variant="outline" size="md" @click="navigateTo(localePath('/'))">
          {{ $t('common.backToHome') }}
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
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('common.noTxnFoundTitle') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400">
          {{ $t('common.noTxnFoundDesc') }}
        </p>
      </div>

      <div class="pt-2 flex items-center justify-center gap-3">
        <BaseButton variant="primary" size="md" @click="navigateTo(localePath('/tenant/deposits'))">
          {{ $t('common.backToDepositManagement') }}
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
const localePath = useLocalePath()

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
      navigateTo(localePath(`/payments/result?outcome=Succeeded&transactionId=${transactionId.value}`))
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
