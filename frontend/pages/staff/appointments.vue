<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900">Lịch hẹn dẫn khách xem phòng</h1>
        <p class="text-xs text-slate-500 mt-1">Xử lý các cuộc hẹn xem phòng tại khu trọ bạn phụ trách</p>
      </div>
    </div>

    <BaseCard>
      <div class="py-12 text-center text-slate-400 text-xs">
        <p>{{ $t('common.noData') }}</p>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import BaseCard from '~/components/common/BaseCard.vue'

definePageMeta({
  layout: 'staff',
})
</script>
