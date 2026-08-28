<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('staffProperties.title') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">{{ $t('staffProperties.subtitle') }}</p>
      </div>
      <BaseButton variant="outline" size="sm" @click="fetchProperties">
        🔄 {{ $t('common.refresh') }}
      </BaseButton>
    </div>

    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="properties.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">{{ $t('staffProperties.noProperties') }}</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div
        v-for="p in properties"
        :key="p.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <div class="flex items-start justify-between gap-3">
          <div class="space-y-1">
            <h3 class="text-base font-bold text-slate-900 dark:text-white">{{ p.name }}</h3>
            <p class="text-xs text-slate-500 dark:text-slate-400">📍 {{ p.address }}</p>
          </div>
          <span class="px-2.5 py-1 rounded-lg text-xs font-semibold bg-primary-50 dark:bg-primary-950/60 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-800">
            {{ $t('staffProperties.roomsCount', { count: p.totalRooms || 0 }) }}
          </span>
        </div>

        <div class="grid grid-cols-2 gap-2 text-xs text-slate-600 dark:text-slate-400 pt-2 border-t border-slate-100 dark:border-slate-800">
          <div>⚡ {{ $t('property.electricity') }}: <strong class="text-slate-900 dark:text-white">{{ formatCurrency(p.electricityPrice || 0) }}/kWh</strong></div>
          <div>💧 {{ $t('property.water') }}: <strong class="text-slate-900 dark:text-white">{{ formatCurrency(p.waterPrice || 0) }}/m³</strong></div>
        </div>

        <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800">
          <NuxtLinkLocale :to="`/staff/appointments?boardingHouseId=${p.id}`" class="btn-outline !text-xs !py-1.5 !px-3">
            📅 {{ $t('nav.appointments') }}
          </NuxtLinkLocale>
          <NuxtLinkLocale :to="`/staff/tasks?boardingHouseId=${p.id}`" class="btn-primary !text-xs !py-1.5 !px-3">
            ⚡ {{ $t('nav.tasks') }}
          </NuxtLinkLocale>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import type { BoardingHouse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'staff',
})

const { get } = useApi()
const { formatCurrency } = useFormat()

const isLoading = ref(true)
const properties = ref<BoardingHouse[]>([])

const fetchProperties = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<BoardingHouse>>('/my/boarding-houses', { pageSize: 50 })
    properties.value = data.items || []
  } catch {
    properties.value = []
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchProperties()
})
</script>
