<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">Quản lý Tài khoản Người dùng</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Theo dõi toàn bộ tài khoản người dùng trên hệ thống: Khách thuê, Chủ trọ, Nhân viên và Quản trị viên
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchAccounts">
          🔄 Làm mới
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openCreateModal">
          + Thêm Quản trị viên
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
          placeholder="🔍 Tìm theo tên, email, SĐT..."
          @input="debounceFetch"
        />
      </div>

      <!-- Filters -->
      <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
        <select v-model="filterRole" class="input-field !text-xs !py-1.5 w-36" @change="fetchAccounts">
          <option value="">Tất cả vai trò</option>
          <option value="Tenant">Khách thuê (Tenant)</option>
          <option value="Owner">Chủ trọ (Owner)</option>
          <option value="Staff">Nhân viên (Staff)</option>
          <option value="Admin">Quản trị viên (Admin)</option>
        </select>

        <select v-model="filterLock" class="input-field !text-xs !py-1.5 w-36" @change="fetchAccounts">
          <option :value="null">Tất cả trạng thái</option>
          <option :value="false">Đang hoạt động</option>
          <option :value="true">Đã khóa</option>
        </select>
      </div>
    </div>

    <!-- Accounts Table -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="accounts.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <p class="font-medium text-slate-500 dark:text-slate-400">Không tìm thấy tài khoản nào phù hợp.</p>
    </div>

    <div v-else class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm overflow-x-auto">
      <table class="w-full text-left text-xs">
        <thead>
          <tr class="border-b border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400">
            <th class="pb-3 font-semibold">Người dùng</th>
            <th class="pb-3 font-semibold">Tên đăng nhập / SĐT</th>
            <th class="pb-3 font-semibold">Vai trò</th>
            <th class="pb-3 font-semibold">Trạng thái</th>
            <th class="pb-3 font-semibold">Ngày tạo</th>
            <th class="pb-3 font-semibold text-right">Thao tác</th>
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
              <div class="text-[11px] text-slate-400">{{ acc.phoneNumber || 'Chưa cập nhật SĐT' }}</div>
            </td>
            <td class="py-3">
              <span
                :class="[
                  'px-2 py-0.5 rounded-md text-[10px] font-bold uppercase tracking-wider',
                  getRoleBadgeClass(acc.role),
                ]"
              >
                {{ acc.role }}
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
                {{ acc.isLocked ? 'Đã khóa' : 'Hoạt động' }}
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
                {{ acc.isLocked ? 'Mở khóa' : 'Khóa' }}
              </BaseButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- MODAL: Create Admin Account -->
    <BaseModal
      v-model="isCreateModalOpen"
      title="Tạo Tài khoản Quản trị viên (Admin)"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitCreateAdmin" class="space-y-4">
        <BaseInput
          v-model="createForm.fullName"
          label="Họ và tên"
          placeholder="VD: Quản trị viên 1"
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
            label="Số điện thoại"
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
          label="Mật khẩu"
          type="password"
          placeholder="Tối thiểu 6 ký tự"
          required
        />

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCreateModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmittingCreate">
            Tạo tài khoản Admin
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
  const actionName = acc.isLocked ? 'mở khóa' : 'khóa'
  if (!confirm(`Bạn có chắc chắn muốn ${actionName} tài khoản "${acc.fullName}"?`)) return
  try {
    await post(`/admin/accounts/${acc.id}/${action}`, {
      reason: acc.isLocked ? undefined : 'Khóa bởi Quản trị viên',
    })
    toast.success(`Đã ${actionName} tài khoản thành công!`)
    await fetchAccounts()
  } catch (err: any) {
    toast.error(err.message || `Không thể ${actionName} tài khoản.`)
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
    toast.success('Tạo tài khoản Quản trị viên thành công!')
    isCreateModalOpen.value = false
    await fetchAccounts()
  } catch (err: any) {
    toast.error(err.message || 'Không thể tạo tài khoản admin.')
  } finally {
    isSubmittingCreate.value = false
  }
}

onMounted(() => {
  fetchAccounts()
})
</script>
