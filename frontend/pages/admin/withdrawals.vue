<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900">Duyệt yêu cầu rút tiền</h1>
        <p class="text-xs text-slate-500 mt-1">Kiểm tra và duyệt chuyển khoản số dư cho chủ nhà trọ</p>
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
  layout: 'admin',
})
</script>
