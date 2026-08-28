<template>
  <div class="max-w-3xl mx-auto space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.profile') }}</h1>
      <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">{{ $t('profile.subtitle') }}</p>
    </div>

    <!-- Profile Header Card -->
    <BaseCard>
      <div class="flex items-center gap-4">
        <div class="w-16 h-16 rounded-2xl bg-primary-100 dark:bg-primary-950 text-primary-700 dark:text-primary-300 flex items-center justify-center font-bold text-2xl shadow-sm">
          {{ user?.fullName ? user.fullName.charAt(0).toUpperCase() : 'U' }}
        </div>
        <div>
          <h3 class="text-base font-bold text-slate-900 dark:text-white">{{ user?.fullName }}</h3>
          <span class="text-xs text-slate-500 dark:text-slate-400 block mt-0.5">{{ user?.email }}</span>
          <div class="mt-2 flex items-center gap-2">
            <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-primary-50 dark:bg-primary-950/50 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-800">
              {{ roleLabel }}
            </span>
            <span
              v-if="user?.emailConfirmed"
              class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300"
            >
              ✓ {{ $t('profile.emailVerified') }}
            </span>
          </div>
        </div>
      </div>
    </BaseCard>

    <!-- Sub-tabs -->
    <div class="border-b border-slate-200 dark:border-slate-800">
      <nav class="flex space-x-6">
        <button
          type="button"
          :class="[
            'py-3 text-xs font-semibold border-b-2 transition-colors -mb-px',
            activeTab === 'info' ? 'border-primary-600 text-primary-600 dark:text-primary-400' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300',
          ]"
          @click="activeTab = 'info'"
        >
          {{ $t('profile.personalInfo') }}
        </button>
        <button
          type="button"
          :class="[
            'py-3 text-xs font-semibold border-b-2 transition-colors -mb-px',
            activeTab === 'security' ? 'border-primary-600 text-primary-600 dark:text-primary-400' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300',
          ]"
          @click="activeTab = 'security'"
        >
          {{ $t('profile.security') }}
        </button>
        <button
          type="button"
          :class="[
            'py-3 text-xs font-semibold border-b-2 transition-colors -mb-px',
            activeTab === 'sessions' ? 'border-primary-600 text-primary-600 dark:text-primary-400' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300',
          ]"
          @click="activeTab = 'sessions'"
        >
          {{ $t('profile.sessions') }} ({{ sessions.length }})
        </button>
      </nav>
    </div>

    <!-- TAB 1: Personal Info -->
    <div v-if="activeTab === 'info'">
      <BaseCard :title="$t('profile.personalInfo')">
        <form @submit.prevent="handleSaveProfile" class="space-y-4 max-w-lg">
          <BaseInput
            v-model="profileForm.fullName"
            :label="$t('auth.fullName')"
            required
          />

          <BaseInput
            v-model="profileForm.phoneNumber"
            :label="$t('auth.phoneNumber')"
            placeholder="0912345678"
          />

          <BaseSelect
            v-model="profileForm.gender"
            :label="$t('auth.gender')"
            :options="genderOptions"
            required
          />

          <div class="pt-2">
            <BaseButton type="submit" variant="primary" size="md" :loading="isSavingProfile">
              {{ $t('common.save') }}
            </BaseButton>
          </div>
        </form>
      </BaseCard>
    </div>

    <!-- TAB 2: Change Password -->
    <div v-if="activeTab === 'security'">
      <BaseCard :title="$t('profile.security')">
        <form @submit.prevent="handleChangePassword" class="space-y-4 max-w-md">
          <BaseInput
            v-model="passwordForm.currentPassword"
            type="password"
            :label="$t('auth.currentPassword')"
            :placeholder="$t('auth.passwordPlaceholder')"
            required
          />

          <BaseInput
            v-model="passwordForm.newPassword"
            type="password"
            :label="$t('auth.newPassword')"
            :placeholder="$t('auth.passwordPlaceholder')"
            required
          />

          <BaseInput
            v-model="passwordForm.confirmNewPassword"
            type="password"
            :label="$t('auth.confirmNewPassword')"
            :placeholder="$t('auth.passwordPlaceholder')"
            required
          />

          <div class="pt-2">
            <BaseButton type="submit" variant="primary" size="md" :loading="isSavingPassword">
              {{ $t('profile.security') }}
            </BaseButton>
          </div>
        </form>
      </BaseCard>
    </div>

    <!-- TAB 3: Active Sessions -->
    <div v-if="activeTab === 'sessions'">
      <BaseCard :title="$t('profile.sessions')">
        <p class="text-xs text-slate-500 dark:text-slate-400 mb-4">
          {{ $t('profile.sessionsDescription') }}
        </p>

        <div v-if="isLoadingSessions" class="py-8 text-center">
          <LoadingSpinner size="sm" />
        </div>

        <div v-else-if="sessions.length === 0" class="py-8 text-center text-slate-400 text-xs">
          {{ $t('profile.noSessions') }}
        </div>

        <div v-else class="divide-y divide-slate-100 dark:divide-slate-800">
          <div
            v-for="s in sessions"
            :key="s.id"
            class="py-3.5 flex items-center justify-between gap-4"
          >
            <div class="flex items-center gap-3">
              <div class="w-9 h-9 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 flex items-center justify-center font-bold text-sm">
                💻
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <span class="text-xs font-bold text-slate-800 dark:text-slate-200">{{ s.deviceInfo || 'Web Browser' }}</span>
                  <span
                    v-if="s.isCurrent"
                    class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300"
                  >
                    {{ $t('profile.currentDevice') }}
                  </span>
                </div>
                <span class="text-[11px] text-slate-400 block mt-0.5">
                  IP: {{ s.ipAddress || '127.0.0.1' }} · {{ formatRelativeTime(s.lastActiveAt) }}
                </span>
              </div>
            </div>

            <button
              v-if="!s.isCurrent"
              type="button"
              class="text-xs font-semibold text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 px-3 py-1.5 rounded-lg hover:bg-red-50 dark:hover:bg-red-950/40 transition-colors"
              @click="revokeSession(s.id)"
            >
              {{ $t('profile.revokeSession') }}
            </button>
          </div>
        </div>
      </BaseCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import BaseSelect from '~/components/common/BaseSelect.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import type { User, SessionInfo } from '~/types/api'

definePageMeta({
  layout: 'tenant',
})

const { user, role } = useAuth()
const { put, get, delete: deleteApi } = useApi()
const { formatRelativeTime } = useFormat()
const authStore = useAuthStore()
const toast = useToast()
const { t } = useI18n()

const activeTab = ref('info')

const roleLabel = computed(() => (role.value ? t(`roles.${role.value}`) : ''))

const genderOptions = computed(() => [
  { label: t('auth.genderMale'), value: 'Male' },
  { label: t('auth.genderFemale'), value: 'Female' },
  { label: t('auth.genderOther'), value: 'Other' },
])

const profileForm = reactive({
  fullName: user.value?.fullName || '',
  phoneNumber: user.value?.phoneNumber || '',
  gender: 'Other',
})

watch(
  () => user.value,
  (u) => {
    if (u) {
      profileForm.fullName = u.fullName || ''
      profileForm.phoneNumber = u.phoneNumber || ''
    }
  },
  { immediate: true }
)

const isSavingProfile = ref(false)

const handleSaveProfile = async () => {
  if (!profileForm.fullName) return
  isSavingProfile.value = true
  try {
    const updated = await put<User>('/me', {
      fullName: profileForm.fullName,
      phoneNumber: profileForm.phoneNumber || null,
      gender: profileForm.gender,
    })
    authStore.setUser(updated)
    toast.success(t('profile.updateSuccess'))
  } catch (err: any) {
    toast.error(err.message || t('profile.updateFailed'))
  } finally {
    isSavingProfile.value = false
  }
}

// Password change
const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})
const isSavingPassword = ref(false)

const handleChangePassword = async () => {
  if (!passwordForm.currentPassword || !passwordForm.newPassword) return
  if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
    toast.error(t('auth.passwordMismatch'))
    return
  }
  isSavingPassword.value = true
  try {
    await put('/auth/password', {
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
    })
    toast.success(t('auth.passwordChangeSuccess'))
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmNewPassword = ''
  } catch (err: any) {
    toast.error(err.message || t('auth.passwordChangeFailed'))
  } finally {
    isSavingPassword.value = false
  }
}

// Sessions management
const sessions = ref<SessionInfo[]>([])
const isLoadingSessions = ref(false)

const fetchSessions = async () => {
  isLoadingSessions.value = true
  try {
    sessions.value = await get<SessionInfo[]>('/me/sessions')
  } catch {
    sessions.value = []
  } finally {
    isLoadingSessions.value = false
  }
}

const revokeSession = async (sessionId: string) => {
  try {
    await deleteApi(`/me/sessions/${sessionId}`)
    toast.success(t('profile.revokeSessionSuccess'))
    await fetchSessions()
  } catch (err: any) {
    toast.error(err.message || t('profile.updateFailed'))
  }
}

watch(activeTab, (tab) => {
  if (tab === 'sessions') {
    fetchSessions()
  }
})
</script>
