<template>
  <div class="max-w-3xl mx-auto space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.profile') }}</h1>
      <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">Quản lý thông tin cá nhân, bảo mật mật khẩu và các phiên đăng nhập</p>
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
              v-if="user?.isEmailVerified"
              class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300"
            >
              ✓ Email đã xác minh
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
          Thông tin cá nhân
        </button>
        <button
          type="button"
          :class="[
            'py-3 text-xs font-semibold border-b-2 transition-colors -mb-px',
            activeTab === 'security' ? 'border-primary-600 text-primary-600 dark:text-primary-400' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300',
          ]"
          @click="activeTab = 'security'"
        >
          Đổi mật khẩu
        </button>
        <button
          type="button"
          :class="[
            'py-3 text-xs font-semibold border-b-2 transition-colors -mb-px',
            activeTab === 'sessions' ? 'border-primary-600 text-primary-600 dark:text-primary-400' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300',
          ]"
          @click="activeTab = 'sessions'"
        >
          Thiết bị đăng nhập ({{ sessions.length }})
        </button>
      </nav>
    </div>

    <!-- TAB 1: Personal Info -->
    <div v-if="activeTab === 'info'">
      <BaseCard title="Thông tin chi tiết">
        <form @submit.prevent="handleSaveProfile" class="space-y-4">
          <BaseInput
            v-model="profileForm.fullName"
            label="Họ và tên"
            required
          />

          <BaseInput
            v-model="profileForm.phoneNumber"
            label="Số điện thoại"
            placeholder="0912345678"
          />

          <BaseInput
            v-model="profileForm.idCardNumber"
            label="Số CCCD / CMND"
            placeholder="001203004567"
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
      <BaseCard title="Đổi mật khẩu tài khoản">
        <form @submit.prevent="handleChangePassword" class="space-y-4 max-w-md">
          <BaseInput
            v-model="passwordForm.oldPassword"
            type="password"
            label="Mật khẩu hiện tại"
            placeholder="••••••••"
            required
          />

          <BaseInput
            v-model="passwordForm.newPassword"
            type="password"
            label="Mật khẩu mới"
            placeholder="••••••••"
            required
          />

          <BaseInput
            v-model="passwordForm.confirmNewPassword"
            type="password"
            label="Xác nhận mật khẩu mới"
            placeholder="••••••••"
            required
          />

          <div class="pt-2">
            <BaseButton type="submit" variant="primary" size="md" :loading="isSavingPassword">
              Cập nhật mật khẩu
            </BaseButton>
          </div>
        </form>
      </BaseCard>
    </div>

    <!-- TAB 3: Active Sessions -->
    <div v-if="activeTab === 'sessions'">
      <BaseCard title="Danh sách các phiên đăng nhập">
        <p class="text-xs text-slate-500 mb-4">
          Mỗi thiết bị đăng nhập duy trì một phiên làm việc riêng biệt. Bạn có thể thu hồi phiên từ xa để đăng xuất khỏi thiết bị đó.
        </p>

        <div v-if="isLoadingSessions" class="py-8 text-center">
          <LoadingSpinner size="sm" />
        </div>

        <div v-else-if="sessions.length === 0" class="py-8 text-center text-slate-400 text-xs">
          Không có thông tin phiên làm việc.
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
                  <span class="text-xs font-bold text-slate-800 dark:text-slate-200">{{ s.deviceInfo || 'Thiết bị Web Browser' }}</span>
                  <span
                    v-if="s.isCurrent"
                    class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300"
                  >
                    Thiết bị này
                  </span>
                </div>
                <span class="text-[11px] text-slate-400 block mt-0.5">
                  IP: {{ s.ipAddress || '127.0.0.1' }} · Hoạt động: {{ formatRelativeTime(s.lastActiveAt) }}
                </span>
              </div>
            </div>

            <button
              v-if="!s.isCurrent"
              type="button"
              class="text-xs font-semibold text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 px-3 py-1.5 rounded-lg hover:bg-red-50 dark:hover:bg-red-950/40 transition-colors"
              @click="revokeSession(s.id)"
            >
              Thu hồi
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

const profileForm = reactive({
  fullName: user.value?.fullName || '',
  phoneNumber: user.value?.phoneNumber || '',
  idCardNumber: user.value?.idCardNumber || '',
})

watch(
  () => user.value,
  (u) => {
    if (u) {
      profileForm.fullName = u.fullName || ''
      profileForm.phoneNumber = u.phoneNumber || ''
      profileForm.idCardNumber = u.idCardNumber || ''
    }
  }
)

const isSavingProfile = ref(false)

const handleSaveProfile = async () => {
  isSavingProfile.value = true
  try {
    const updated = await put<User>('/me', profileForm)
    authStore.setUser(updated)
    toast.success('Cập nhật hồ sơ thành công!')
  } catch (err: any) {
    toast.error(err.message || 'Cập nhật thất bại.')
  } finally {
    isSavingProfile.value = false
  }
}

// Password change
const passwordForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})
const isSavingPassword = ref(false)

const handleChangePassword = async () => {
  if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
    toast.error('Mật khẩu xác nhận không khớp.')
    return
  }
  isSavingPassword.value = true
  try {
    await put('/auth/password', {
      oldPassword: passwordForm.oldPassword,
      newPassword: passwordForm.newPassword,
    })
    toast.success('Đổi mật khẩu thành công!')
    passwordForm.oldPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmNewPassword = ''
  } catch (err: any) {
    toast.error(err.message || 'Đổi mật khẩu thất bại.')
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
    toast.success('Đã thu hồi phiên đăng nhập!')
    await fetchSessions()
  } catch (err: any) {
    toast.error(err.message || 'Thu hồi phiên thất bại.')
  }
}

watch(activeTab, (tab) => {
  if (tab === 'sessions') {
    fetchSessions()
  }
})
</script>
