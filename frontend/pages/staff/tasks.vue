<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.tasks') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('common.staffTasksSubtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchTasks">
          🔄 {{ $t('common.refresh') }}
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openCreateTaskModal">
          + {{ $t('tasks.createTask') }}
        </BaseButton>
      </div>
    </div>

    <!-- Filters: Status & Priority -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <!-- Status Pills -->
      <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            filterStatus === ''
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700',
          ]"
          @click="filterStatus = ''"
        >
          {{ $t('common.allCount', { count: tasks.length }) }}
        </button>
        <button
          v-for="st in ['Pending', 'InProgress', 'Completed', 'Cancelled']"
          :key="st"
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            filterStatus === st
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700',
          ]"
          @click="filterStatus = st"
        >
          {{ $t(`enums.WorkTaskStatus.${st}`) }}
        </button>
      </div>

      <!-- Priority Selector -->
      <div class="flex items-center gap-2 shrink-0">
        <select v-model="filterPriority" class="input-field !text-xs !py-1.5 w-36" @change="fetchTasks">
          <option value="">{{ $t('common.all') }}</option>
          <option value="High">{{ $t('enums.TaskPriority.High') }}</option>
          <option value="Medium">{{ $t('enums.TaskPriority.Medium') }}</option>
          <option value="Low">{{ $t('enums.TaskPriority.Low') }}</option>
        </select>
      </div>
    </div>

    <!-- Tasks List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredTasks.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('tasks.emptyStaffTasks') }}</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="t in filteredTasks"
        :key="t.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <!-- Header -->
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex flex-wrap items-center gap-2">
              <span
                :class="[
                  'px-2 py-0.5 rounded-md text-[10px] font-bold uppercase tracking-wider',
                  getPriorityBadgeClass(t.priority),
                ]"
              >
                {{ $t(`enums.TaskPriority.${t.priority}`) }}
              </span>
              <span class="text-sm font-bold text-slate-900 dark:text-white">{{ t.title }}</span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              🏢 {{ t.boardingHouseName }} · {{ $t('staff.assignedHouses') }} <strong class="text-slate-800 dark:text-slate-200">{{ t.assignedToFullName }}</strong>
              <span v-if="t.dueDate"> · {{ $t('deposits.depositDateLabel') }}: <strong :class="isOverdue(t) ? 'text-red-600 dark:text-red-400' : 'text-slate-700 dark:text-slate-300'">{{ t.dueDate }}</strong></span>
            </p>
          </div>

          <StatusBadge type="WorkTaskStatus" :status="t.status" />
        </div>

        <!-- Details -->
        <p v-if="t.details" class="text-xs text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-slate-800/50 p-3 rounded-xl">
          {{ t.details }}
        </p>

        <!-- Footer / Actions -->
        <div class="flex items-center justify-between pt-2 border-t border-slate-100 dark:border-slate-800 text-[11px] text-slate-400">
          <span>{{ $t('common.createdAt', { time: formatRelativeTime(t.createdAt) }) }}</span>

          <div class="flex items-center gap-2">
            <BaseButton
              v-if="t.status === 'Pending'"
              variant="outline"
              size="sm"
              class="!text-xs text-amber-600 dark:text-amber-400 border-amber-200 dark:border-amber-800"
              @click="handleUpdateStatus(t.id, 'InProgress')"
            >
              {{ $t('tasks.markInProgressBtn') }}
            </BaseButton>

            <BaseButton
              v-if="t.status === 'InProgress'"
              variant="primary"
              size="sm"
              class="!text-xs"
              @click="handleUpdateStatus(t.id, 'Completed')"
            >
              {{ $t('tasks.markCompletedBtn') }}
            </BaseButton>

            <BaseButton
              v-if="t.status === 'Pending' || t.status === 'InProgress'"
              variant="ghost"
              size="sm"
              class="!text-xs text-red-500 hover:text-red-700"
              @click="handleUpdateStatus(t.id, 'Cancelled')"
            >
              {{ $t('common.cancel') }}
            </BaseButton>
          </div>
        </div>
      </div>
    </div>

    <!-- MODAL: Create Task -->
    <BaseModal
      v-model="isCreateModalOpen"
      :title="$t('tasks.createTask')"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitCreateTask" class="space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('ownerProperties.title') }} <span class="text-red-500">*</span>
          </label>
          <select v-model="taskForm.boardingHouseId" class="input-field !text-xs !py-2" @change="onHouseSelected" required>
            <option value="">-- {{ $t('common.select') }} --</option>
            <option v-for="h in boardingHouses" :key="h.id" :value="h.id">
              {{ h.name }}
            </option>
          </select>
        </div>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('staff.title') }} <span class="text-red-500">*</span>
          </label>
          <select v-model="taskForm.assignedToUserId" class="input-field !text-xs !py-2" required>
            <option value="">-- {{ $t('common.select') }} --</option>
            <option v-for="s in houseStaffList" :key="s.staffUserId" :value="s.staffUserId">
              {{ s.staffFullName }}
            </option>
          </select>
        </div>

        <BaseInput
          v-model="taskForm.title"
          :label="$t('tasks.title')"
          :placeholder="$t('tasks.title')"
          required
        />

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('common.detail') }}
          </label>
          <textarea
            v-model="taskForm.details"
            rows="3"
            class="input-field !text-xs !py-2"
            :placeholder="$t('common.detail')"
          />
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">{{ $t('tasks.taskPriorityLabel') }}</label>
            <select v-model="taskForm.priority" class="input-field !text-xs !py-2">
              <option value="Low">{{ $t('enums.TaskPriority.Low') }}</option>
              <option value="Medium">{{ $t('enums.TaskPriority.Medium') }}</option>
              <option value="High">{{ $t('enums.TaskPriority.High') }}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">{{ $t('deposits.depositDateLabel') }}</label>
            <input
              v-model="taskForm.dueDate"
              type="date"
              class="input-field !text-xs !py-2"
            />
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCreateModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmittingTask">
            {{ $t('tasks.createTask') }}
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
import StatusBadge from '~/components/status/StatusBadge.vue'
import type {
  BoardingHouse,
  PagedResult,
  StaffAssignmentResponse,
  TaskPriority,
  TaskResponse,
  WorkTaskStatus,
} from '~/types/api'

definePageMeta({
  layout: 'staff',
})

const { get, post, put } = useApi()
const { formatRelativeTime } = useFormat()
const { t } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const tasks = ref<TaskResponse[]>([])
const filterStatus = ref('')
const filterPriority = ref('')

const isOverdue = (t: TaskResponse) => {
  if (t.status === 'Completed' || t.status === 'Cancelled' || !t.dueDate) return false
  return new Date(t.dueDate) < new Date()
}

const getPriorityBadgeClass = (priority: TaskPriority) => {
  switch (priority) {
    case 'High':
      return 'bg-red-100 dark:bg-red-950 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800'
    case 'Medium':
      return 'bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800'
    default:
      return 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400'
  }
}

const filteredTasks = computed(() => {
  let list = tasks.value
  if (filterStatus.value) list = list.filter((t) => t.status === filterStatus.value)
  if (filterPriority.value) list = list.filter((t) => t.priority === filterPriority.value)
  return list
})

const fetchTasks = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<TaskResponse>>('/tasks', {
      status: filterStatus.value || undefined,
      priority: filterPriority.value || undefined,
      pageSize: 100,
    })
    tasks.value = data.items || []
  } catch {
    tasks.value = []
  } finally {
    isLoading.value = false
  }
}

const handleUpdateStatus = async (taskId: string, newStatus: WorkTaskStatus) => {
  try {
    await put(`/tasks/${taskId}/status`, { status: newStatus })
    toast.success(t('messages.updateTaskStatusSuccess'))
    await fetchTasks()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

// Create Task Modal State
const isCreateModalOpen = ref(false)
const isSubmittingTask = ref(false)
const boardingHouses = ref<BoardingHouse[]>([])
const houseStaffList = ref<StaffAssignmentResponse[]>([])

const taskForm = reactive({
  boardingHouseId: '',
  assignedToUserId: '',
  title: '',
  details: '',
  priority: 'Medium' as TaskPriority,
  dueDate: '',
})

const openCreateTaskModal = async () => {
  try {
    const housesData = await get<PagedResult<BoardingHouse>>('/my/boarding-houses', { pageSize: 50 })
    boardingHouses.value = housesData.items || []
  } catch {
    boardingHouses.value = []
  }

  taskForm.boardingHouseId = ''
  taskForm.assignedToUserId = ''
  taskForm.title = ''
  taskForm.details = ''
  taskForm.priority = 'Medium'
  taskForm.dueDate = ''
  houseStaffList.value = []
  isCreateModalOpen.value = true
}

const onHouseSelected = async () => {
  if (!taskForm.boardingHouseId) {
    houseStaffList.value = []
    return
  }
  try {
    const staffData = await get<StaffAssignmentResponse[]>(`/my/boarding-houses/${taskForm.boardingHouseId}/staff`)
    houseStaffList.value = staffData || []
  } catch {
    houseStaffList.value = []
  }
}

const handleSubmitCreateTask = async () => {
  isSubmittingTask.value = true
  try {
    await post('/tasks', {
      boardingHouseId: taskForm.boardingHouseId,
      assignedToUserId: taskForm.assignedToUserId,
      title: taskForm.title,
      details: taskForm.details || undefined,
      priority: taskForm.priority,
      dueDate: taskForm.dueDate || undefined,
    })
    toast.success(t('messages.createTaskSuccess'))
    isCreateModalOpen.value = false
    await fetchTasks()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSubmittingTask.value = false
  }
}

onMounted(() => {
  fetchTasks()
})
</script>
