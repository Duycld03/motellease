<template>
  <div class="w-full relative" ref="selectRef">
    <label v-if="label" class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
      {{ label }}
      <span v-if="required" class="text-red-500">*</span>
    </label>

    <div class="relative">
      <button
        type="button"
        :disabled="disabled"
        :class="[
          'flex items-center justify-between w-full rounded-lg border text-left text-sm py-2 px-3.5 transition-colors focus:outline-none focus:ring-2',
          error
            ? 'border-red-300 dark:border-red-800 text-red-900 dark:text-red-300 focus:border-red-500 focus:ring-red-500 bg-red-50/20 dark:bg-red-950/20'
            : 'border-slate-300 dark:border-slate-700 focus:border-primary-500 focus:ring-primary-500 bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100',
          disabled ? 'bg-slate-50 dark:bg-slate-800 text-slate-400 dark:text-slate-500 cursor-not-allowed' : 'cursor-pointer hover:border-slate-400 dark:hover:border-slate-600',
        ]"
        @click="toggleDropdown"
      >
        <span v-if="selectedLabel" class="font-medium text-slate-800 dark:text-slate-200 text-xs truncate">
          {{ selectedLabel }}
        </span>
        <span v-else class="text-slate-400 dark:text-slate-500 text-xs truncate">
          {{ placeholder || 'Chọn...' }}
        </span>

        <svg
          :class="['w-4 h-4 text-slate-400 dark:text-slate-500 transition-transform duration-200', isOpen ? 'rotate-180' : '']"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      <!-- Custom Dropdown Menu -->
      <Transition
        enter-active-class="transition duration-150 ease-out"
        enter-from-class="opacity-0 scale-95 translate-y-1"
        enter-to-class="opacity-100 scale-100 translate-y-0"
        leave-active-class="transition duration-100 ease-in"
        leave-from-class="opacity-100 scale-100 translate-y-0"
        leave-to-class="opacity-0 scale-95 translate-y-1"
      >
        <div
          v-if="isOpen"
          class="absolute z-50 left-0 right-0 mt-1.5 max-h-60 overflow-y-auto bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl shadow-xl py-1 text-xs"
        >
          <div
            v-for="opt in options"
            :key="String(opt.value)"
            :class="[
              'flex items-center justify-between px-3.5 py-2 cursor-pointer transition-colors',
              isSelected(opt.value)
                ? 'bg-primary-50 dark:bg-primary-950/40 text-primary-600 dark:text-primary-400 font-semibold'
                : 'text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800',
              opt.disabled ? 'opacity-40 cursor-not-allowed hover:bg-transparent' : '',
            ]"
            @click="selectOption(opt)"
          >
            <span>{{ opt.label }}</span>
            <svg
              v-if="isSelected(opt.value)"
              class="w-4 h-4 text-primary-600 dark:text-primary-400 shrink-0"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
            </svg>
          </div>
        </div>
      </Transition>
    </div>

    <p v-if="error" class="mt-1 text-xs text-red-600">
      {{ error }}
    </p>
    <p v-else-if="hint" class="mt-1 text-xs text-slate-500">
      {{ hint }}
    </p>
  </div>
</template>

<script setup lang="ts">
import { onClickOutside } from '@vueuse/core'

export interface SelectOption {
  label: string
  value: string | number
  disabled?: boolean
}

const props = withDefaults(
  defineProps<{
    modelValue?: string | number | null
    options: SelectOption[]
    id?: string
    label?: string
    placeholder?: string
    error?: string
    hint?: string
    required?: boolean
    disabled?: boolean
  }>(),
  {
    modelValue: '',
    id: undefined,
    label: undefined,
    placeholder: undefined,
    error: undefined,
    hint: undefined,
    required: false,
    disabled: false,
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | number): void
  (e: 'change', value: string | number): void
}>()

const selectRef = ref<HTMLElement | null>(null)
const isOpen = ref(false)

const toggleDropdown = () => {
  if (props.disabled) return
  isOpen.value = !isOpen.value
}

onClickOutside(selectRef, () => {
  isOpen.value = false
})

const selectedLabel = computed(() => {
  const match = props.options.find((opt) => String(opt.value) === String(props.modelValue))
  return match?.label || ''
})

const isSelected = (val: string | number) => {
  return String(val) === String(props.modelValue)
}

const selectOption = (opt: SelectOption) => {
  if (opt.disabled) return
  emit('update:modelValue', opt.value)
  emit('change', opt.value)
  isOpen.value = false
}
</script>
