<template>
  <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl shadow-xl shadow-slate-200/50 dark:shadow-none border border-slate-200 dark:border-slate-800 transition-colors">
    <div class="text-center mb-6">
      <h1 class="text-xl font-bold text-slate-900 dark:text-white">
        {{ $t('auth.loginTitle') }}
      </h1>
      <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
        {{ $t('auth.loginSubtitle') }}
      </p>
    </div>

    <form @submit.prevent="handleSubmit" class="space-y-4">
      <BaseInput
        v-model="form.email"
        type="email"
        :label="$t('auth.email')"
        :placeholder="$t('auth.emailPlaceholder')"
        required
      />

      <div>
        <div class="flex items-center justify-between mb-1">
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">
            {{ $t('auth.password') }} <span class="text-red-500">*</span>
          </label>
          <NuxtLinkLocale to="/auth/forgot-password" class="text-xs text-primary-600 dark:text-primary-400 hover:text-primary-700 font-medium">
            {{ $t('auth.forgotPasswordQuestion') }}
          </NuxtLinkLocale>
        </div>
        <BaseInput
          v-model="form.password"
          type="password"
          :placeholder="$t('auth.passwordPlaceholder')"
          required
        />
      </div>

      <div class="pt-2">
        <BaseButton
          type="submit"
          variant="primary"
          size="md"
          full-width
          :loading="isLoading"
        >
          {{ $t('nav.login') }}
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

    <GoogleSignInButton text="signin_with" />

    <div class="mt-6 pt-6 border-t border-slate-100 dark:border-slate-800 text-center">
      <p class="text-xs text-slate-500 dark:text-slate-400">
        {{ $t('auth.dontHaveAccount') }}
        <NuxtLinkLocale to="/auth/register" class="font-semibold text-primary-600 dark:text-primary-400 hover:text-primary-700 ml-1">
          {{ $t('nav.register') }}
        </NuxtLinkLocale>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import GoogleSignInButton from '~/components/common/GoogleSignInButton.vue'

definePageMeta({
  layout: 'auth',
  guestOnly: true,
})

const { login } = useAuth()
const toast = useToast()
const { t } = useI18n()

const form = reactive({
  email: '',
  password: '',
})

const isLoading = ref(false)

const handleSubmit = async () => {
  if (!form.email || !form.password) return
  isLoading.value = true
  try {
    await login(form.email, form.password)
  } catch (err: any) {
    toast.error(err.message || t('auth.loginFailed'))
  } finally {
    isLoading.value = false
  }
}
</script>
