<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('admin.auditLogsTitle') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          {{ $t('admin.auditLogsSubtitle') }}
        </p>
      </div>
      <BaseButton variant="outline" size="sm" @click="fetchLogs">
        🔄 {{ $t('common.refresh') }}
      </BaseButton>
    </div>

    <!-- Filter Bar -->
    <div class="flex items-center gap-3 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <input
        v-model="filterEntityType"
        type="text"
        class="input-field !text-xs !py-1.5 w-64"
        :placeholder="$t('admin.auditLogsSearchPlaceholder')"
        @input="debounceFetch"
      />
    </div>

    <!-- Logs Table -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="logs.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('common.noData') }}</p>
    </div>

    <div v-else class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm overflow-x-auto">
      <table class="w-full text-left text-xs">
        <thead>
          <tr class="border-b border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400">
            <th class="pb-3 font-semibold">{{ $t('admin.colTimestamp') }}</th>
            <th class="pb-3 font-semibold">{{ $t('admin.colActor') }}</th>
            <th class="pb-3 font-semibold">{{ $t('admin.colAction') }}</th>
            <th class="pb-3 font-semibold">{{ $t('admin.colTarget') }}</th>
            <th class="pb-3 font-semibold">{{ $t('admin.colIp') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 dark:divide-slate-800 font-mono text-[11px]">
          <tr
            v-for="l in logs"
            :key="l.id"
            class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors"
          >
            <td class="py-3 text-slate-500 dark:text-slate-400 whitespace-nowrap">
              {{ formatRelativeTime(l.createdAt) }}
            </td>
            <td class="py-3 font-sans font-bold text-slate-900 dark:text-white">
              {{ l.actorFullName || 'System' }}
            </td>
            <td class="py-3">
              <span class="px-2 py-0.5 rounded-md font-sans text-[10px] font-bold bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                {{ l.action }}
              </span>
            </td>
            <td class="py-3 text-slate-700 dark:text-slate-300 font-sans">
              <span class="font-semibold text-primary-600 dark:text-primary-400">{{ l.entityType }}</span>
              <span v-if="l.entityId" class="text-[10px] text-slate-400 block font-mono">ID: {{ l.entityId }}</span>
            </td>
            <td class="py-3 text-slate-400">
              {{ l.ipAddress || '127.0.0.1' }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import type { AuditLogResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'admin',
})

const { get } = useApi()
const { formatRelativeTime } = useFormat()

const isLoading = ref(true)
const logs = ref<AuditLogResponse[]>([])
const filterEntityType = ref('')

let debounceTimer: any = null
const debounceFetch = () => {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    fetchLogs()
  }, 300)
}

const fetchLogs = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<AuditLogResponse>>('/admin/audit-logs', {
      entityType: filterEntityType.value || undefined,
      pageSize: 50,
    })
    logs.value = data.items || []
  } catch {
    logs.value = []
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchLogs()
})
</script>
