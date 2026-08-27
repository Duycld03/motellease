<template>
  <div class="bg-white p-8 rounded-2xl shadow-xl shadow-slate-200/50 border border-slate-200">
    <div class="text-center mb-6">
      <h1 class="text-xl font-bold text-slate-900">
        {{ $t('auth.verifyOtpTitle') }}
      </h1>
      <p class="text-xs text-slate-500 mt-1">
        {{ $t('auth.verifyOtpSubtitle') }}
      </p>
      <p v-if="email" class="text-xs font-semibold text-primary-600 mt-1">
        {{ email }}
      </p>
    </div>

    <form @submit.prevent="handleSubmit" class="space-y-4">
      <BaseInput
        v-model="otpCode"
        type="text"
        label="Mã OTP (6 chữ số)"
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
const { post } = useApi()
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
    await post('/auth/register/verify-otp', {
      email: email.value,
      code: otpCode.value,
    })
    toast.success('Xác thực email thành công! Vui lòng đăng nhập.')
    navigateTo('/auth/login')
  } catch (err: any) {
    toast.error(err.message || 'Mã xác thực không hợp lệ.')
  } finally {
    isLoading.value = false
  }
}

const handleResend = async () => {
  if (!email.value) return
  isResending.value = true
  try {
    await post('/auth/register/send-otp', { email: email.value })
    toast.success(t('auth.otpSent'))
  } catch (err: any) {
    toast.error(err.message || 'Không thể gửi lại mã OTP.')
  } finally {
    isResending.value = false
  }
}
</script>
