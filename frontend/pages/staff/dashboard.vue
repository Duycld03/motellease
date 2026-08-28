<template>
  <div class="space-y-8">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">
          {{ $t('staffDashboard.title') }}
        </h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('staffDashboard.subtitle') }}
        </p>
      </div>

      <div class="flex items-center gap-3">
        <BaseButton variant="outline" size="sm" @click="fetchDashboardData">
          🔄 {{ $t('common.refresh') }}
        </BaseButton>
      </div>
    </div>

    <!-- Quick Stats Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-sky-100 dark:bg-sky-950/60 text-sky-700 dark:text-sky-300 flex items-center justify-center font-bold text-xl">
          🏢
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('staffDashboard.assignedProperties') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ $t('staffDashboard.propertiesCount', { count: propertiesCount }) }}
          </span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-amber-100 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 flex items-center justify-center font-bold text-xl">
          ⚡
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('staffDashboard.pendingTasks') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ $t('staffDashboard.tasksCount', { count: activeTasks.length }) }}
          </span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 flex items-center justify-center font-bold text-xl">
          📅
        </div>
        <div>
          <span class="text-xs text-slate-500 dark:text-slate-400 block">{{ $t('staffDashboard.pendingAppointments') }}</span>
          <span class="text-lg font-bold text-slate-900 dark:text-white mt-0.5 block">
            {{ $t('staffDashboard.appointmentsCount', { count: pendingAppointmentsCount }) }}
          </span>
        </div>
      </BaseCard>
    </div>

    <!-- Active Tasks Section -->
    <BaseCard :title="$t('staffDashboard.todayTasksTitle')">
      <div v-if="isLoading" class="py-12 text-center">
        <LoadingSpinner size="md" />
      </div>

      <div v-else-if="activeTasks.length === 0" class="py-12 text-center text-slate-400 text-xs">
        <svg class="w-10 h-10 text-slate-300 dark:text-slate-700 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>{{ $t('staffDashboard.noPendingTasks') }}</p>
      </div>

      <div v-else class="space-y-3 pt-2">
        <div
          v-for="t in activeTasks"
          :key="t.id"
          class="p-4 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs"
        >
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="font-bold text-slate-900 dark:text-white">{{ t.title }}</span>
              <span class="text-[10px] font-bold px-1.5 py-0.2 rounded bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-300">
                {{ $t(`enums.TaskPriority.${t.priority}`) }}
              </span>
            </div>
            <p class="text-[11px] text-slate-500 dark:text-slate-400">
              🏢 {{ t.boardingHouseName }}
              <span v-if="t.dueDate"> · {{ $t('staffDashboard.dueDatePrefix') }} <strong>{{ t.dueDate }}</strong></span>
            </p>
          </div>

          <div class="flex items-center gap-2">
            <BaseButton
              v-if="t.status === 'Pending'"
              variant="outline"
              size="sm"
              class="!text-xs"
              @click="handleQuickStatus(t.id, 'InProgress')"
            >
              ▶ {{ $t('staffDashboard.startTask') }}
            </BaseButton>
            <BaseButton
              v-if="t.status === 'InProgress'"
              variant="primary"
              size="sm"
              class="!text-xs"
              @click="handleQuickStatus(t.id, 'Completed')"
            >
              ✓ {{ $t('staffDashboard.completeTask') }}
            </BaseButton>
          </div>
        </div>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import type {
  AppointmentResponse,
  BoardingHouse,
  PagedResult,
  TaskResponse,
  WorkTaskStatus,
} from '~/types/api'

definePageMeta({
  layout: 'staff',
})

const { get, put } = useApi()
const { t } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const propertiesCount = ref(0)
const activeTasks = ref<TaskResponse[]>([])
const pendingAppointmentsCount = ref(0)

const fetchDashboardData = async () => {
  isLoading.value = true
  try {
    const [housesData, tasksData, apptsData] = await Promise.all([
      get<PagedResult<BoardingHouse>>('/my/boarding-houses', { pageSize: 50 }),
      get<PagedResult<TaskResponse>>('/tasks', { pageSize: 50 }),
      get<PagedResult<AppointmentResponse>>('/appointments', { status: 'Pending', pageSize: 50 }),
    ])

    propertiesCount.value = housesData.totalCount || housesData.items?.length || 0
    const allTasks = tasksData.items || []
    activeTasks.value = allTasks.filter((t) => t.status === 'Pending' || t.status === 'InProgress')
    pendingAppointmentsCount.value = apptsData.totalCount || apptsData.items?.length || 0
  } catch {
    // Keep defaults
  } finally {
    isLoading.value = false
  }
}

const handleQuickStatus = async (taskId: string, newStatus: WorkTaskStatus) => {
  try {
    await put(`/tasks/${taskId}/status`, { status: newStatus })
    toast.success(t('messages.updateTaskStatusSuccess'))
    await fetchDashboardData()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

onMounted(() => {
  fetchDashboardData()
})
</script>
