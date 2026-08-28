<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.staff') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('common.ownerStaffSubtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchStaffList">
          🔄 {{ $t('common.refresh') }}
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openCreateStaffModal">
          {{ $t('staff.addNewStaffBtn') }}
        </BaseButton>
      </div>
    </div>

    <!-- Staff List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="staffList.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('staff.emptyStaffList') }}</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div
        v-for="s in staffList"
        :key="s.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <!-- Header -->
        <div class="flex items-start justify-between gap-3">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-primary-100 dark:bg-primary-950/60 text-primary-700 dark:text-primary-300 flex items-center justify-center font-bold text-sm">
              {{ s.fullName?.charAt(0) || 'NV' }}
            </div>
            <div>
              <div class="flex items-center gap-2">
                <span class="text-sm font-bold text-slate-900 dark:text-white">{{ s.fullName }}</span>
                <span
                  :class="[
                    'px-2 py-0.5 rounded-md text-[10px] font-bold',
                    s.isLocked
                      ? 'bg-red-100 dark:bg-red-950 text-red-700 dark:text-red-300'
                      : 'bg-emerald-100 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-300',
                  ]"
                >
                  {{ s.isLocked ? $t('common.statusLocked') : $t('common.statusActive') }}
                </span>
              </div>
              <p class="text-xs text-slate-500 dark:text-slate-400">@{{ s.username }} · {{ s.email }}</p>
            </div>
          </div>
        </div>

        <!-- Info Grid -->
        <div class="grid grid-cols-2 gap-2 text-xs text-slate-600 dark:text-slate-400 pt-2 border-t border-slate-100 dark:border-slate-800">
          <div>📞 {{ $t('auth.phoneNumber') }}: <strong class="text-slate-900 dark:text-white">{{ s.phoneNumber || $t('common.noPhoneProvided') }}</strong></div>
          <div>📅 {{ $t('admin.colJoinedDate') }}: <strong class="text-slate-900 dark:text-white">{{ s.hireDate }}</strong></div>
          <div class="col-span-2 text-primary-600 dark:text-primary-400 font-semibold">
            🏢 {{ $t('staff.assignedHouses') }} {{ s.activeAssignmentsCount }}
          </div>
        </div>

        <!-- Actions -->
        <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800">
          <BaseButton
            variant="outline"
            size="sm"
            @click="openAssignmentModal(s)"
          >
            {{ $t('staff.assignHouseModalTitle') }}
          </BaseButton>

          <BaseButton
            variant="ghost"
            size="sm"
            :class="s.isLocked ? 'text-emerald-600' : 'text-red-600 hover:text-red-700'"
            @click="handleToggleLockStaff(s)"
          >
            {{ s.isLocked ? $t('common.unlockAction') : $t('common.lockAction') }}
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL 1: Create Staff -->
    <BaseModal
      v-model="isCreateModalOpen"
      :title="$t('staff.createStaffModalTitle')"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitCreateStaff" class="space-y-4">
        <BaseInput
          v-model="createForm.fullName"
          :label="$t('staff.staffFullName')"
          :placeholder="$t('auth.fullNamePlaceholder')"
          required
        />

        <div class="grid grid-cols-2 gap-3">
          <BaseInput
            v-model="createForm.username"
            :label="$t('auth.email')"
            placeholder="nhanvien_01"
            required
          />
          <BaseInput
            v-model="createForm.phoneNumber"
            :label="$t('staff.staffPhone')"
            placeholder="0912345678"
          />
        </div>

        <BaseInput
          v-model="createForm.email"
          :label="$t('staff.staffEmail')"
          type="email"
          placeholder="staff@example.com"
          required
        />

        <BaseInput
          v-model="createForm.password"
          :label="$t('staff.staffPassword')"
          type="password"
          :placeholder="$t('common.adminMinPasswordLength')"
          required
        />

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">{{ $t('auth.gender') }}</label>
            <select v-model="createForm.gender" class="input-field !text-xs !py-2">
              <option value="Male">{{ $t('enums.Gender.Male') }}</option>
              <option value="Female">{{ $t('enums.Gender.Female') }}</option>
              <option value="Other">{{ $t('enums.Gender.Other') }}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">{{ $t('admin.colJoinedDate') }}</label>
            <input
              v-model="createForm.hireDate"
              type="date"
              class="input-field !text-xs !py-2"
              required
            />
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCreateModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmittingCreate">
            {{ $t('staff.createStaff') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>

    <!-- MODAL 2: Manage Property Assignments -->
    <BaseModal
      v-model="isAssignmentModalOpen"
      :title="$t('staff.assignHouseModalTitle')"
      max-width="lg"
    >
      <div v-if="selectedStaff" class="space-y-5">
        <!-- Add new assignment section -->
        <div class="p-4 bg-slate-50 dark:bg-slate-800/60 rounded-2xl border border-slate-200 dark:border-slate-700 space-y-3">
          <span class="text-xs font-bold text-slate-900 dark:text-white block">
            {{ $t('staff.selectHouseToAssign') }}:
          </span>
          <div class="flex items-center gap-3">
            <select v-model="selectedHouseToAssign" class="input-field !text-xs !py-2 flex-1">
              <option value="">-- {{ $t('common.select') }} --</option>
              <option
                v-for="h in availableHousesToAssign"
                :key="h.id"
                :value="h.id"
              >
                {{ h.name }} ({{ h.address }})
              </option>
            </select>
            <BaseButton
              variant="primary"
              size="sm"
              :disabled="!selectedHouseToAssign"
              :loading="isAssigning"
              @click="handleAssignHouse"
            >
              {{ $t('staff.confirmAssign') }}
            </BaseButton>
          </div>
        </div>

        <!-- Currently assigned houses list -->
        <div class="space-y-2">
          <h4 class="text-xs font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wide">
            {{ $t('staff.assignedHouses') }} ({{ staffAssignments.length }})
          </h4>

          <div v-if="staffAssignments.length === 0" class="p-6 bg-slate-50 dark:bg-slate-800/40 rounded-xl text-center text-xs text-slate-400">
            {{ $t('common.noData') }}
          </div>

          <div v-else class="space-y-2">
            <div
              v-for="assign in staffAssignments"
              :key="assign.id"
              class="p-3 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs"
            >
              <div>
                <span class="font-bold text-slate-900 dark:text-white block">{{ assign.boardingHouseName }}</span>
                <span class="text-[10px] text-slate-400">{{ $t('common.createdAt', { time: formatRelativeTime(assign.assignedAt) }) }}</span>
              </div>

              <BaseButton
                variant="ghost"
                size="sm"
                class="text-red-500 hover:text-red-700 !text-xs !py-1"
                @click="handleUnassignHouse(assign)"
              >
                {{ $t('staff.unassignBtn') }}
              </BaseButton>
            </div>
          </div>
        </div>

        <div class="flex justify-end pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" @click="isAssignmentModalOpen = false">
            {{ $t('common.close') }}
          </BaseButton>
        </div>
      </div>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import type {
  BoardingHouse,
  Gender,
  PagedResult,
  StaffAssignmentResponse,
  StaffDetailResponse,
  StaffSummaryResponse,
} from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get, post, delete: deleteApi } = useApi()
const { formatRelativeTime } = useFormat()
const { t } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const staffList = ref<StaffSummaryResponse[]>([])
const allBoardingHouses = ref<BoardingHouse[]>([])

const fetchStaffList = async () => {
  isLoading.value = true
  try {
    const [staffData, housesData] = await Promise.all([
      get<StaffSummaryResponse[]>('/my/staff'),
      get<PagedResult<BoardingHouse>>('/my/boarding-houses', { pageSize: 100 }),
    ])
    staffList.value = staffData || []
    allBoardingHouses.value = housesData.items || []
  } catch {
    staffList.value = []
  } finally {
    isLoading.value = false
  }
}

// Modal 1: Create Staff
const isCreateModalOpen = ref(false)
const isSubmittingCreate = ref(false)
const createForm = reactive({
  fullName: '',
  username: '',
  email: '',
  password: '',
  phoneNumber: '',
  gender: 'Male' as Gender,
  hireDate: new Date().toISOString().slice(0, 10),
})

const openCreateStaffModal = () => {
  createForm.fullName = ''
  createForm.username = ''
  createForm.email = ''
  createForm.password = ''
  createForm.phoneNumber = ''
  createForm.gender = 'Male'
  createForm.hireDate = new Date().toISOString().slice(0, 10)
  isCreateModalOpen.value = true
}

const handleSubmitCreateStaff = async () => {
  isSubmittingCreate.value = true
  try {
    await post('/my/staff', {
      fullName: createForm.fullName,
      username: createForm.username,
      email: createForm.email,
      password: createForm.password,
      phoneNumber: createForm.phoneNumber || undefined,
      gender: createForm.gender,
      hireDate: createForm.hireDate,
    })
    toast.success(t('messages.createStaffSuccess'))
    isCreateModalOpen.value = false
    await fetchStaffList()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSubmittingCreate.value = false
  }
}

// Lock staff
const handleToggleLockStaff = async (s: StaffSummaryResponse) => {
  if (!confirm(t('messages.confirmAction'))) return
  try {
    await deleteApi(`/my/staff/${s.id}`)
    toast.success(t('messages.toggleStaffLockSuccess'))
    await fetchStaffList()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

// Modal 2: Assignments
const isAssignmentModalOpen = ref(false)
const selectedStaff = ref<StaffSummaryResponse | null>(null)
const staffAssignments = ref<StaffAssignmentResponse[]>([])
const selectedHouseToAssign = ref('')
const isAssigning = ref(false)

const availableHousesToAssign = computed(() => {
  const assignedHouseIds = new Set(staffAssignments.value.map((a) => a.boardingHouseId))
  return allBoardingHouses.value.filter((h) => !assignedHouseIds.has(h.id))
})

const openAssignmentModal = async (s: StaffSummaryResponse) => {
  selectedStaff.value = s
  selectedHouseToAssign.value = ''
  isAssignmentModalOpen.value = true

  // Fetch full details of staff including assignments
  try {
    const detail = await get<StaffDetailResponse>(`/my/staff/${s.id}`)
    staffAssignments.value = (detail.assignments as StaffAssignmentResponse[]) || []
  } catch {
    staffAssignments.value = []
  }
}

const handleAssignHouse = async () => {
  if (!selectedStaff.value || !selectedHouseToAssign.value) return
  isAssigning.value = true
  try {
    await post(`/my/boarding-houses/${selectedHouseToAssign.value}/staff`, {
      staffUserId: selectedStaff.value.id,
    })
    toast.success(t('messages.assignStaffSuccess'))
    selectedHouseToAssign.value = ''

    // Refresh assignments
    const detail = await get<StaffDetailResponse>(`/my/staff/${selectedStaff.value.id}`)
    staffAssignments.value = (detail.assignments as StaffAssignmentResponse[]) || []
    await fetchStaffList()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isAssigning.value = false
  }
}

const handleUnassignHouse = async (assign: StaffAssignmentResponse) => {
  if (!confirm(t('messages.confirmAction'))) return
  try {
    await deleteApi(`/my/boarding-houses/${assign.boardingHouseId}/staff/${assign.staffUserId}`)
    toast.success(t('messages.unassignStaffSuccess'))
    if (selectedStaff.value) {
      const detail = await get<StaffDetailResponse>(`/my/staff/${selectedStaff.value.id}`)
      staffAssignments.value = (detail.assignments as StaffAssignmentResponse[]) || []
    }
    await fetchStaffList()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

onMounted(() => {
  fetchStaffList()
})
</script>
