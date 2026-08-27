<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900">Quản lý Hợp đồng thuê</h1>
        <p class="text-xs text-slate-500 mt-1">Danh sách hợp đồng đang hiệu lực, sắp hết hạn và thanh lý</p>
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
  layout: 'owner',
})
</script>
