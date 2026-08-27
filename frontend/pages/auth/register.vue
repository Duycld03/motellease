<template>
  <div class="bg-white p-8 rounded-2xl shadow-xl shadow-slate-200/50 border border-slate-200">
    <div class="text-center mb-6">
      <h1 class="text-xl font-bold text-slate-900">
        {{ $t('auth.registerTitle') }}
      </h1>
      <p class="text-xs text-slate-500 mt-1">
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

    <div class="mt-6 pt-6 border-t border-slate-100 text-center">
      <p class="text-xs text-slate-500">
        {{ $t('auth.alreadyHaveAccount') }}
        <NuxtLink to="/auth/login" class="font-semibold text-primary-600 hover:text-primary-700 ml-1">
          {{ $t('nav.login') }}
        </NuxtLink>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import BaseSelect from '~/components/common/BaseSelect.vue'
import { UserRole } from '~/types/enums'

definePageMeta({
  layout: 'auth',
  guestOnly: true,
})

const { register } = useAuth()
const toast = useToast()

const roleOptions = [
  { label: 'Người thuê (Tenant)', value: UserRole.Tenant },
  { label: 'Chủ nhà trọ (Owner)', value: UserRole.Owner },
]

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
    await register(form)
    navigateTo({
      path: '/auth/verify-otp',
      query: { email: form.email },
    })
  } catch (err: any) {
    toast.error(err.message || 'Đăng ký không thành công.')
  } finally {
    isLoading.value = false
  }
}
</script>
