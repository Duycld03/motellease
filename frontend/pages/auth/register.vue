<template>
  <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl shadow-xl shadow-slate-200/50 dark:shadow-none border border-slate-200 dark:border-slate-800 transition-colors">
    <div class="text-center mb-6">
      <h1 class="text-xl font-bold text-slate-900 dark:text-white">
        {{ $t('auth.registerTitle') }}
      </h1>
      <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
        {{ $t('auth.registerSubtitle') }}
      </p>
    </div>

    <form @submit.prevent="handleSubmit" class="space-y-4">
      <BaseInput
        v-model="form.fullName"
        type="text"
        :label="$t('auth.fullName')"
        :placeholder="$t('auth.fullNamePlaceholder')"
        required
      />

      <BaseInput
        v-model="form.email"
        type="email"
        :label="$t('auth.email')"
        :placeholder="$t('auth.emailPlaceholder')"
        required
      />

      <BaseInput
        v-model="form.phoneNumber"
        type="tel"
        :label="$t('auth.phoneNumber')"
        :placeholder="$t('auth.phoneNumberPlaceholder')"
      />

      <BaseSelect
        v-model="form.role"
        :label="$t('auth.role')"
        :options="roleOptions"
        required
      />

      <BaseInput
        v-model="form.password"
        type="password"
        :label="$t('auth.password')"
        :placeholder="$t('auth.passwordPlaceholder')"
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
          {{ $t('nav.register') }}
        </BaseButton>
      </div>
    </form>

    <div class="relative my-6">
      <div class="absolute inset-0 flex items-center">
        <div class="w-full border-t border-slate-200 dark:border-slate-800" />
      </div>
      <div class="relative flex justify-center text-xs uppercase">
        <span class="bg-white dark:bg-slate-900 px-3 text-slate-500 dark:text-slate-400 font-medium">
          {{ $t('auth.or') }}
        </span>
      </div>
    </div>

    <GoogleSignInButton :role="form.role" text="signup_with" />

    <div class="mt-6 pt-6 border-t border-slate-100 dark:border-slate-800 text-center">
      <p class="text-xs text-slate-500 dark:text-slate-400">
        {{ $t('auth.alreadyHaveAccount') }}
        <NuxtLinkLocale to="/auth/login" class="font-semibold text-primary-600 dark:text-primary-400 hover:text-primary-700 ml-1">
          {{ $t('nav.login') }}
        </NuxtLinkLocale>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import BaseSelect from '~/components/common/BaseSelect.vue'
import GoogleSignInButton from '~/components/common/GoogleSignInButton.vue'
import { UserRole } from '~/types/enums'

definePageMeta({
  layout: 'auth',
  guestOnly: true,
})

const { sendRegistrationOtp } = useAuth()
const toast = useToast()
const { t } = useI18n()
const localePath = useLocalePath()

const roleOptions = computed(() => [
  { label: `${t('roles.Tenant')} (Tenant)`, value: UserRole.Tenant },
  { label: `${t('roles.Owner')} (Owner)`, value: UserRole.Owner },
])

const form = reactive({
  fullName: '',
  email: '',
  phoneNumber: '',
  role: UserRole.Tenant,
  password: '',
})

const isLoading = ref(false)

const handleSubmit = async () => {
  if (!form.email || !form.password || !form.fullName) return
  isLoading.value = true
  try {
    await sendRegistrationOtp(form.email)
    if (typeof window !== 'undefined' && window.sessionStorage) {
      window.sessionStorage.setItem('pending_registration', JSON.stringify(form))
    }
    toast.success(t('auth.otpSent'))
    navigateTo(localePath({
      path: '/auth/verify-otp',
      query: { email: form.email },
    }))
  } catch (err: any) {
    toast.error(err.message || t('auth.registerFailed'))
  } finally {
    isLoading.value = false
  }
}
</script>
