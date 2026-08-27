<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    :class="[
      'inline-flex items-center justify-center font-medium rounded-lg transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 select-none shadow-sm',
      sizeClasses,
      variantClasses,
      fullWidth ? 'w-full' : '',
      disabled || loading ? 'opacity-60 cursor-not-allowed' : 'active:scale-[0.98]',
    ]"
    @click="$emit('click', $event)"
  >
    <LoadingSpinner v-if="loading" size="sm" class="mr-2" />
    <slot name="icon-left" />
    <slot />
    <slot name="icon-right" />
  </button>
</template>

<script setup lang="ts">
import LoadingSpinner from './LoadingSpinner.vue'

const props = withDefaults(
  defineProps<{
    type?: 'button' | 'submit' | 'reset'
    variant?: 'primary' | 'secondary' | 'outline' | 'danger' | 'ghost'
    size?: 'sm' | 'md' | 'lg'
    loading?: boolean
    disabled?: boolean
    fullWidth?: boolean
  }>(),
  {
    type: 'button',
    variant: 'primary',
    size: 'md',
    loading: false,
    disabled: false,
    fullWidth: false,
  }
)

defineEmits<{
  (e: 'click', event: MouseEvent): void
}>()

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm':
      return 'px-3 py-1.5 text-xs'
    case 'lg':
      return 'px-6 py-3 text-base'
    default:
      return 'px-4 py-2 text-sm'
  }
})

const variantClasses = computed(() => {
  switch (props.variant) {
    case 'secondary':
      return 'bg-slate-100 text-slate-800 hover:bg-slate-200 focus:ring-slate-400 border border-slate-200'
    case 'outline':
      return 'bg-white text-slate-700 hover:bg-slate-50 focus:ring-primary-500 border border-slate-300'
    case 'danger':
      return 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500'
    case 'ghost':
      return 'bg-transparent shadow-none text-slate-600 hover:bg-slate-100 hover:text-slate-900 focus:ring-slate-300'
    default:
      return 'bg-primary-600 text-white hover:bg-primary-700 focus:ring-primary-500'
  }
})
</script>
