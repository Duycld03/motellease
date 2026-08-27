<template>
  <div :class="['bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 rounded-xl shadow-sm transition-all', noPadding ? '' : 'p-6', customClass]">
    <div v-if="title || $slots.header" class="flex items-center justify-between pb-4 mb-4 border-b border-slate-100 dark:border-slate-800">
      <div v-if="title">
        <h3 class="text-base font-semibold text-slate-800 dark:text-slate-100">
          {{ title }}
        </h3>
        <p v-if="subtitle" class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
          {{ subtitle }}
        </p>
      </div>
      <slot name="header" />
    </div>

    <slot />

    <div v-if="$slots.footer" class="pt-4 mt-4 border-t border-slate-100 dark:border-slate-800">
      <slot name="footer" />
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    title?: string
    subtitle?: string
    noPadding?: boolean
    customClass?: string
  }>(),
  {
    title: undefined,
    subtitle: undefined,
    noPadding: false,
    customClass: '',
  }
)
</script>
