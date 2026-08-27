<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900">{{ $t('nav.facilities') }}</h1>
        <p class="text-xs text-slate-500 mt-1">Danh mục tiện ích dùng chung trên toàn hệ thống</p>
      </div>
      <BaseButton variant="primary" size="md">
        + Thêm tiện ích
      </BaseButton>
    </div>

    <BaseCard>
      <div class="py-12 text-center text-slate-400 text-xs">
        <p>{{ $t('common.noData') }}</p>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'

definePageMeta({
  layout: 'admin',
})
</script>
