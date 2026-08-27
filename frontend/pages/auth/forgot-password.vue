<template>
  <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl shadow-xl shadow-slate-200/50 dark:shadow-none border border-slate-200 dark:border-slate-800 transition-colors">
    <div class="text-center mb-6">
      <h1 class="text-xl font-bold text-slate-900 dark:text-white">
        {{ $t('auth.forgotPasswordTitle') }}
      </h1>
      <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
        {{ $t('auth.forgotPasswordSubtitle') }}
      </p>
    </div>

    <!-- Step 1: Send OTP -->
    <form v-if="step === 1" @submit.prevent="handleSendOtp" class="space-y-4">
      <BaseInput
        v-model="email"
        type="email"
        :label="$t('auth.email')"
        :placeholder="$t('auth.emailPlaceholder')"
        required
      />

      <div class="pt-2">
        <BaseButton
          type="submit"
          variant="primary"
          size="md"
          full-width
          :loading="isLoading"
        >
          {{ $t('auth.sendOtp') }}
        </BaseButton>
      </div>
    </form>

    <!-- Step 2: Reset with OTP and new password -->
    <form v-else @submit.prevent="handleResetPassword" class="space-y-4">
      <BaseInput
        v-model="otpCode"
        type="text"
        label="Mã OTP (6 chữ số)"
        placeholder="123456"
        required
      />

      <BaseInput
        v-model="newPassword"
        type="password"
        label="Mật khẩu mới"
        placeholder="••••••••"
        required
      />

      <div class="pt-2">
        <BaseButton
          type="submit"
          variant="primary"
          size="md"
          full-width
          :loading="isLoading"
        >
          {{ $t('auth.resetPassword') }}
        </BaseButton>
      </div>
    </form>

    <div class="mt-6 pt-6 border-t border-slate-100 text-center">
      <NuxtLink to="/auth/login" class="text-xs font-semibold text-slate-600 hover:text-slate-900">
        ← {{ $t('common.back') }} {{ $t('nav.login') }}
      </NuxtLink>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseInput from '~/components/common/BaseInput.vue'

definePageMeta({
  layout: 'auth',
  guestOnly: true,
})

const { post } = useApi()
const toast = useToast()
const { t } = useI18n()

const step = ref(1)
const email = ref('')
const otpCode = ref('')
const newPassword = ref('')
const isLoading = ref(false)

const handleSendOtp = async () => {
  if (!email.value) return
  isLoading.value = true
  try {
    await post('/auth/password/forgot', { email: email.value })
    toast.success(t('auth.otpSent'))
    step.value = 2
  } catch (err: any) {
    toast.error(err.message || 'Không thể gửi yêu cầu.')
  } finally {
    isLoading.value = false
  }
}

const handleResetPassword = async () => {
  if (!otpCode.value || !newPassword.value) return
  isLoading.value = true
  try {
    await post('/auth/password/reset', {
      email: email.value,
      token: otpCode.value,
      newPassword: newPassword.value,
    })
    toast.success(t('auth.passwordResetSuccess'))
    navigateTo('/auth/login')
  } catch (err: any) {
    toast.error(err.message || 'Đặt lại mật khẩu thất bại.')
  } finally {
    isLoading.value = false
  }
}
</script>
