<template>
  <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl shadow-xl shadow-slate-200/50 dark:shadow-none border border-slate-200 dark:border-slate-800 transition-colors">
    <div class="text-center mb-6">
      <h1 class="text-xl font-bold text-slate-900 dark:text-white">
        {{ $t('auth.verifyOtpTitle') }}
      </h1>
      <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
        {{ $t('auth.verifyOtpSubtitle') }}
      </p>
      <p v-if="email" class="text-xs font-semibold text-primary-600 dark:text-primary-400 mt-1">
        {{ email }}
      </p>
    </div>

    <form @submit.prevent="handleSubmit" class="space-y-4">
      <BaseInput
        v-model="otpCode"
        type="text"
        :label="$t('auth.verifyOtpTitle')"
        placeholder="123456"
        required
      />

      <div class="pt-2 space-y-2">
        <BaseButton
          type="submit"
          variant="primary"
          size="md"
          full-width
          :loading="isLoading"
        >
          {{ $t('auth.verifyAndContinue') }}
        </BaseButton>

        <BaseButton
          type="button"
          variant="ghost"
          size="sm"
          full-width
          :loading="isResending"
          @click="handleResend"
        >
          {{ $t('auth.resendOtp') }}
        </BaseButton>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseInput from '~/components/common/BaseInput.vue'

definePageMeta({
  layout: 'auth',
})

const route = useRoute()
const { sendRegistrationOtp, verifyRegistrationOtp, register } = useAuth()
const toast = useToast()
const { t } = useI18n()

const email = computed(() => (route.query.email as string) || '')
const otpCode = ref('')
const isLoading = ref(false)
const isResending = ref(false)

const handleSubmit = async () => {
  if (!otpCode.value || !email.value) return
  isLoading.value = true
  try {
    await verifyRegistrationOtp(email.value, otpCode.value)

    let pendingData: any = null
    if (typeof window !== 'undefined' && window.sessionStorage) {
      const raw = window.sessionStorage.getItem('pending_registration')
      if (raw) {
        try {
          pendingData = JSON.parse(raw)
        } catch {
          // ignore
        }
      }
    }

    if (pendingData && pendingData.email === email.value) {
      if (typeof window !== 'undefined' && window.sessionStorage) {
        window.sessionStorage.removeItem('pending_registration')
      }
      await register(pendingData)
    } else {
      toast.success(t('auth.otpVerifySuccess'))
      navigateTo('/auth/login')
    }
  } catch (err: any) {
    toast.error(err.message || t('auth.otpInvalid'))
  } finally {
    isLoading.value = false
  }
}

const handleResend = async () => {
  if (!email.value) return
  isResending.value = true
  try {
    await sendRegistrationOtp(email.value)
    toast.success(t('auth.otpSent'))
  } catch (err: any) {
    toast.error(err.message || t('auth.otpInvalid'))
  } finally {
    isResending.value = false
  }
}
</script>
