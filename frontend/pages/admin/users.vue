<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('admin.usersTitle') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('admin.usersSubtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchAccounts">
          🔄 {{ $t('common.refresh') }}
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openCreateModal">
          {{ $t('common.createAdminAccountBtn') }}
        </BaseButton>
      </div>
    </div>

    <!-- Filters & Search Bar -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <!-- Search Input -->
      <div class="w-full sm:w-72">
        <input
          v-model="searchQuery"
          type="text"
          class="input-field !text-xs !py-1.5"
          :placeholder="$t('admin.searchUsersPlaceholder')"
          @input="debounceFetch"
        />
      </div>

      <!-- Filters -->
      <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
        <select v-model="filterRole" class="input-field !text-xs !py-1.5 w-36" @change="fetchAccounts">
          <option value="">{{ $t('common.all') }}</option>
          <option value="Tenant">{{ $t('enums.UserRole.Tenant') }}</option>
          <option value="Owner">{{ $t('enums.UserRole.Owner') }}</option>
          <option value="Staff">{{ $t('enums.UserRole.Staff') }}</option>
          <option value="Admin">{{ $t('enums.UserRole.Admin') }}</option>
        </select>

        <select v-model="filterLock" class="input-field !text-xs !py-1.5 w-36" @change="fetchAccounts">
          <option :value="null">{{ $t('common.all') }}</option>
          <option :value="false">{{ $t('common.statusActive') }}</option>
          <option :value="true">{{ $t('common.statusLocked') }}</option>
        </select>
      </div>
    </div>

    <!-- Accounts Table -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="accounts.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('common.noData') }}</p>
    </div>

    <div v-else class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm overflow-x-auto">
      <table class="w-full text-left text-xs">
        <thead>
          <tr class="border-b border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400">
            <th class="pb-3 font-semibold">{{ $t('admin.colUser') }}</th>
            <th class="pb-3 font-semibold">{{ $t('auth.phoneNumber') }}</th>
            <th class="pb-3 font-semibold">{{ $t('admin.colRole') }}</th>
            <th class="pb-3 font-semibold">{{ $t('admin.colStatus') }}</th>
            <th class="pb-3 font-semibold">{{ $t('admin.colJoinedDate') }}</th>
            <th class="pb-3 font-semibold text-right">{{ $t('admin.colActions') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
          <tr
            v-for="acc in accounts"
            :key="acc.id"
            class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors"
          >
            <td class="py-3">
              <div class="flex items-center gap-2.5">
                <div class="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-950 text-primary-700 dark:text-primary-300 font-bold flex items-center justify-center text-xs shrink-0">
                  {{ acc.fullName?.charAt(0) || 'U' }}
                </div>
                <div>
                  <span class="font-bold text-slate-900 dark:text-white block">{{ acc.fullName }}</span>
                  <span class="text-[11px] text-slate-400">{{ acc.email }}</span>
                </div>
              </div>
            </td>
            <td class="py-3 text-slate-600 dark:text-slate-400">
              <div>@{{ acc.username }}</div>
              <div class="text-[11px] text-slate-400">{{ acc.phoneNumber || $t('common.noPhoneProvided') }}</div>
            </td>
            <td class="py-3">
              <span
                :class="[
                  'px-2 py-0.5 rounded-md text-[10px] font-bold uppercase tracking-wider',
                  getRoleBadgeClass(acc.role),
                ]"
              >
                {{ $t(`enums.UserRole.${acc.role}`) }}
              </span>
            </td>
            <td class="py-3">
              <span
                :class="[
                  'px-2 py-0.5 rounded-md text-[10px] font-bold',
                  acc.isLocked
                    ? 'bg-red-100 dark:bg-red-950 text-red-700 dark:text-red-300'
                    : 'bg-emerald-100 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-300',
                ]"
              >
                {{ acc.isLocked ? $t('common.statusLocked') : $t('common.statusActive') }}
              </span>
            </td>
            <td class="py-3 text-slate-500 dark:text-slate-400">
              {{ formatRelativeTime(acc.createdAt) }}
            </td>
            <td class="py-3 text-right">
              <BaseButton
                v-if="acc.role !== 'Admin'"
                variant="ghost"
                size="sm"
                :class="acc.isLocked ? 'text-emerald-600' : 'text-red-600 hover:text-red-700'"
                @click="handleToggleLock(acc)"
              >
                {{ acc.isLocked ? $t('common.unlockAction') : $t('common.lockAction') }}
              </BaseButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- MODAL: Create Admin Account -->
    <BaseModal
      v-model="isCreateModalOpen"
      :title="$t('common.createAdminAccountModalTitle')"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitCreateAdmin" class="space-y-4">
        <BaseInput
          v-model="createForm.fullName"
          :label="$t('auth.fullName')"
          :placeholder="$t('auth.fullNamePlaceholder')"
          required
        />
        <div class="grid grid-cols-2 gap-3">
          <BaseInput
            v-model="createForm.username"
            label="Username"
            placeholder="admin_02"
            required
          />
          <BaseInput
            v-model="createForm.phoneNumber"
            :label="$t('auth.phoneNumber')"
            placeholder="0912345678"
          />
        </div>
        <BaseInput
          v-model="createForm.email"
          label="Email"
          type="email"
          placeholder="admin@motellease.vn"
          required
        />
        <BaseInput
          v-model="createForm.password"
          :label="$t('auth.password')"
          type="password"
          :placeholder="$t('common.adminMinPasswordLength')"
          required
        />

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCreateModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmittingCreate">
            {{ $t('common.createAdminSubmit') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import type {
  AdminAccountSummaryResponse,
  Gender,
  PagedResult,
  UserRole,
} from '~/types/api'

definePageMeta({
  layout: 'admin',
})

const { get, post } = useApi()
const { formatRelativeTime } = useFormat()
const { t } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const accounts = ref<AdminAccountSummaryResponse[]>([])
const filterRole = ref('')
const filterLock = ref<boolean | null>(null)
const searchQuery = ref('')

const getRoleBadgeClass = (role: UserRole) => {
  switch (role) {
    case 'Admin':
      return 'bg-purple-100 dark:bg-purple-950 text-purple-700 dark:text-purple-300'
    case 'Owner':
      return 'bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-300'
    case 'Staff':
      return 'bg-blue-100 dark:bg-blue-950 text-blue-700 dark:text-blue-300'
    default:
      return 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400'
  }
}

let debounceTimer: any = null
const debounceFetch = () => {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    fetchAccounts()
  }, 300)
}

const fetchAccounts = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<AdminAccountSummaryResponse>>('/admin/accounts', {
      role: filterRole.value || undefined,
      isLocked: filterLock.value ?? undefined,
      search: searchQuery.value || undefined,
      pageSize: 50,
    })
    accounts.value = data.items || []
  } catch {
    accounts.value = []
  } finally {
    isLoading.value = false
  }
}

// Lock / Unlock
const handleToggleLock = async (acc: AdminAccountSummaryResponse) => {
  const action = acc.isLocked ? 'unlock' : 'lock'
  if (!confirm(t('messages.confirmAction'))) return
  try {
    await post(`/admin/accounts/${acc.id}/${action}`, {
      reason: acc.isLocked ? undefined : t('admin.lockedByAdminReason'),
    })
    toast.success(t('messages.toggleUserLockSuccess'))
    await fetchAccounts()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

// Create Admin
const isCreateModalOpen = ref(false)
const isSubmittingCreate = ref(false)
const createForm = reactive({
  fullName: '',
  username: '',
  email: '',
  password: '',
  phoneNumber: '',
  gender: 'Male' as Gender,
})

const openCreateModal = () => {
  createForm.fullName = ''
  createForm.username = ''
  createForm.email = ''
  createForm.password = ''
  createForm.phoneNumber = ''
  createForm.gender = 'Male'
  isCreateModalOpen.value = true
}

const handleSubmitCreateAdmin = async () => {
  isSubmittingCreate.value = true
  try {
    await post('/admin/accounts', {
      fullName: createForm.fullName,
      username: createForm.username,
      email: createForm.email,
      password: createForm.password,
      phoneNumber: createForm.phoneNumber || undefined,
      gender: createForm.gender,
      role: 'Admin',
    })
    toast.success(t('messages.createAdminSuccess'))
    isCreateModalOpen.value = false
    await fetchAccounts()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSubmittingCreate.value = false
  }
}

onMounted(() => {
  fetchAccounts()
})
</script>
