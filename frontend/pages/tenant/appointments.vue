<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900">{{ $t('nav.myAppointments') }}</h1>
        <p class="text-xs text-slate-500 mt-1">Lịch hẹn xem phòng trực tiếp cùng chủ trọ hoặc nhân viên</p>
      </div>
    </div>

    <BaseCard>
      <div class="py-12 text-center text-slate-400 text-xs">
        <svg class="w-10 h-10 text-slate-300 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        <p>{{ $t('common.noData') }}</p>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import BaseCard from '~/components/common/BaseCard.vue'

definePageMeta({
  layout: 'tenant',
})
</script>
