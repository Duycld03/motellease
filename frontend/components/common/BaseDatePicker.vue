<template>
  <div class="w-full relative" ref="datePickerRef">
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
        @click="togglePicker"
      >
        <span v-if="displayValue" class="font-medium text-slate-800 dark:text-slate-200 text-xs">
          {{ displayValue }}
        </span>
        <span v-else class="text-slate-400 dark:text-slate-500 text-xs">
          {{ placeholder || (enableTime ? 'Chọn ngày & giờ...' : 'Chọn ngày...') }}
        </span>

        <div class="flex items-center gap-1.5 text-slate-400 dark:text-slate-500">
          <span
            v-if="modelValue && !disabled"
            class="hover:text-slate-600 dark:hover:text-slate-300 p-0.5 rounded transition-colors"
            title="Xóa"
            @click.stop="clearDate"
          >
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </span>
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        </div>
      </button>

      <!-- Calendar Popover Menu -->
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
          :class="[
            'absolute z-50 left-0 mt-1.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-2xl p-4 text-xs select-none',
            enableTime ? 'w-80 sm:w-84' : 'w-72',
          ]"
        >
          <!-- Header (Month & Year Selector) -->
          <div class="flex items-center justify-between mb-3">
            <button
              type="button"
              class="p-1 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-300 transition-colors"
              @click="prevMonth"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
              </svg>
            </button>

            <div class="font-bold text-sm text-slate-800 dark:text-slate-100">
              Tháng {{ currentMonth + 1 }}, {{ currentYear }}
            </div>

            <button
              type="button"
              class="p-1 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-300 transition-colors"
              @click="nextMonth"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            </button>
          </div>

          <!-- Week Days Header -->
          <div class="grid grid-cols-7 gap-1 text-center font-semibold text-[11px] text-slate-400 dark:text-slate-500 mb-1">
            <span v-for="w in weekDays" :key="w">{{ w }}</span>
          </div>

          <!-- Calendar Days Grid -->
          <div class="grid grid-cols-7 gap-1 text-center">
            <button
              v-for="d in calendarDays"
              :key="d.dateStr"
              type="button"
              :disabled="d.isDisabled"
              :class="[
                'h-8 w-8 mx-auto flex items-center justify-center rounded-xl font-medium transition-all text-xs',
                d.isCurrentMonth
                  ? d.isSelected
                    ? 'bg-primary-600 text-white font-bold shadow-md shadow-primary-500/30'
                    : d.isToday
                    ? 'border border-primary-500 text-primary-600 dark:text-primary-400 font-bold hover:bg-primary-50 dark:hover:bg-slate-800'
                    : 'text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800'
                  : 'text-slate-300 dark:text-slate-600 hover:bg-slate-50 dark:hover:bg-slate-800/40',
                d.isDisabled ? 'opacity-30 cursor-not-allowed hover:bg-transparent dark:hover:bg-transparent' : 'cursor-pointer',
              ]"
              @click="selectDay(d)"
            >
              {{ d.day }}
            </button>
          </div>

          <!-- Time Picker (if enabled) -->
          <div v-if="enableTime" class="pt-3 mt-3 border-t border-slate-100 dark:border-slate-800">
            <div class="flex items-center justify-between gap-2">
              <span class="text-[11px] font-semibold text-slate-600 dark:text-slate-400">Giờ hẹn:</span>
              <div class="flex items-center gap-1.5">
                <select
                  v-model="selectedHour"
                  class="bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-xs px-2 py-1 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-primary-500"
                  @change="onTimeChange"
                >
                  <option v-for="h in 24" :key="h - 1" :value="String(h - 1).padStart(2, '0')">
                    {{ String(h - 1).padStart(2, '0') }}
                  </option>
                </select>
                <span class="text-slate-400 font-bold">:</span>
                <select
                  v-model="selectedMinute"
                  class="bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-xs px-2 py-1 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-primary-500"
                  @change="onTimeChange"
                >
                  <option v-for="m in ['00', '15', '30', '45']" :key="m" :value="m">
                    {{ m }}
                  </option>
                </select>
              </div>
            </div>
          </div>

          <!-- Quick Action Footer -->
          <div class="flex items-center justify-between pt-3 mt-3 border-t border-slate-100 dark:border-slate-800 text-[11px]">
            <button
              type="button"
              class="text-primary-600 dark:text-primary-400 hover:underline font-semibold"
              @click="selectToday"
            >
              Hôm nay
            </button>
            <button
              type="button"
              class="text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200"
              @click="isOpen = false"
            >
              Đóng
            </button>
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

const props = withDefaults(
  defineProps<{
    modelValue?: string | null
    label?: string
    placeholder?: string
    error?: string
    hint?: string
    required?: boolean
    disabled?: boolean
    min?: string
    max?: string
    enableTime?: boolean
  }>(),
  {
    modelValue: '',
    label: undefined,
    placeholder: undefined,
    error: undefined,
    hint: undefined,
    required: false,
    disabled: false,
    min: undefined,
    max: undefined,
    enableTime: false,
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const datePickerRef = ref<HTMLElement | null>(null)
const isOpen = ref(false)

const weekDays = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']

// Initial current view year and month
const currentDate = new Date()
const currentYear = ref(currentDate.getFullYear())
const currentMonth = ref(currentDate.getMonth())
const selectedDateOnly = ref('')
const selectedHour = ref('09')
const selectedMinute = ref('00')

// Parse modelValue into date and time
watch(
  () => props.modelValue,
  (val) => {
    if (val) {
      if (val.includes('T')) {
        const [dPart, tPart] = val.split('T')
        selectedDateOnly.value = dPart
        if (tPart) {
          const [h, m] = tPart.split(':')
          if (h) selectedHour.value = h.slice(0, 2)
          if (m) selectedMinute.value = m.slice(0, 2)
        }
        const parsed = new Date(val)
        if (!isNaN(parsed.getTime())) {
          currentYear.value = parsed.getFullYear()
          currentMonth.value = parsed.getMonth()
        }
      } else {
        selectedDateOnly.value = val
        const parsed = new Date(val)
        if (!isNaN(parsed.getTime())) {
          currentYear.value = parsed.getFullYear()
          currentMonth.value = parsed.getMonth()
        }
      }
    } else {
      selectedDateOnly.value = ''
    }
  },
  { immediate: true }
)

const togglePicker = () => {
  if (props.disabled) return
  isOpen.value = !isOpen.value
}

onClickOutside(datePickerRef, () => {
  isOpen.value = false
})

const displayValue = computed(() => {
  if (!props.modelValue) return ''
  if (props.modelValue.includes('T')) {
    const [dPart, tPart] = props.modelValue.split('T')
    const [y, m, d] = dPart.split('-')
    const timeStr = tPart ? tPart.slice(0, 5) : ''
    return `${timeStr} ngày ${d}/${m}/${y}`
  }
  const parts = props.modelValue.split('-')
  if (parts.length === 3) {
    const [y, m, d] = parts
    return `${d}/${m}/${y}`
  }
  return props.modelValue
})

const prevMonth = () => {
  if (currentMonth.value === 0) {
    currentMonth.value = 11
    currentYear.value--
  } else {
    currentMonth.value--
  }
}

const nextMonth = () => {
  if (currentMonth.value === 11) {
    currentMonth.value = 0
    currentYear.value++
  } else {
    currentMonth.value++
  }
}

interface CalendarDay {
  day: number
  dateStr: string
  isCurrentMonth: boolean
  isSelected: boolean
  isToday: boolean
  isDisabled: boolean
}

const calendarDays = computed<CalendarDay[]>(() => {
  const days: CalendarDay[] = []
  const year = currentYear.value
  const month = currentMonth.value

  const firstDayOfMonth = new Date(year, month, 1)
  const lastDayOfMonth = new Date(year, month + 1, 0)
  
  let startDayOfWeek = (firstDayOfMonth.getDay() + 6) % 7
  
  const prevMonthLastDay = new Date(year, month, 0).getDate()
  for (let i = startDayOfWeek - 1; i >= 0; i--) {
    const d = prevMonthLastDay - i
    const prevMonthIdx = month === 0 ? 11 : month - 1
    const prevYear = month === 0 ? year - 1 : year
    const dateStr = formatDateStr(prevYear, prevMonthIdx, d)
    days.push({
      day: d,
      dateStr,
      isCurrentMonth: false,
      isSelected: dateStr === selectedDateOnly.value,
      isToday: isToday(prevYear, prevMonthIdx, d),
      isDisabled: checkDisabled(dateStr),
    })
  }

  for (let d = 1; d <= lastDayOfMonth.getDate(); d++) {
    const dateStr = formatDateStr(year, month, d)
    days.push({
      day: d,
      dateStr,
      isCurrentMonth: true,
      isSelected: dateStr === selectedDateOnly.value,
      isToday: isToday(year, month, d),
      isDisabled: checkDisabled(dateStr),
    })
  }

  const remaining = 42 - days.length
  for (let d = 1; d <= remaining; d++) {
    const nextMonthIdx = month === 11 ? 0 : month + 1
    const nextYear = month === 11 ? year + 1 : year
    const dateStr = formatDateStr(nextYear, nextMonthIdx, d)
    days.push({
      day: d,
      dateStr,
      isCurrentMonth: false,
      isSelected: dateStr === selectedDateOnly.value,
      isToday: isToday(nextYear, nextMonthIdx, d),
      isDisabled: checkDisabled(dateStr),
    })
  }

  return days
})

const formatDateStr = (y: number, m: number, d: number) => {
  const mm = String(m + 1).padStart(2, '0')
  const dd = String(d).padStart(2, '0')
  return `${y}-${mm}-${dd}`
}

const isToday = (y: number, m: number, d: number) => {
  const today = new Date()
  return today.getFullYear() === y && today.getMonth() === m && today.getDate() === d
}

const checkDisabled = (dateStr: string) => {
  const minDateOnly = props.min ? (props.min.includes('T') ? props.min.split('T')[0] : props.min) : null
  const maxDateOnly = props.max ? (props.max.includes('T') ? props.max.split('T')[0] : props.max) : null
  if (minDateOnly && dateStr < minDateOnly) return true
  if (maxDateOnly && dateStr > maxDateOnly) return true
  return false
}

const emitValue = (dateStr: string) => {
  if (props.enableTime) {
    emit('update:modelValue', `${dateStr}T${selectedHour.value}:${selectedMinute.value}`)
  } else {
    emit('update:modelValue', dateStr)
  }
}

const selectDay = (day: CalendarDay) => {
  if (day.isDisabled) return
  selectedDateOnly.value = day.dateStr
  emitValue(day.dateStr)
  if (!props.enableTime) {
    isOpen.value = false
  }
}

const onTimeChange = () => {
  if (selectedDateOnly.value) {
    emitValue(selectedDateOnly.value)
  }
}

const selectToday = () => {
  const today = new Date()
  const dateStr = formatDateStr(today.getFullYear(), today.getMonth(), today.getDate())
  if (!checkDisabled(dateStr)) {
    selectedDateOnly.value = dateStr
    emitValue(dateStr)
    if (!props.enableTime) {
      isOpen.value = false
    }
  }
}

const clearDate = () => {
  selectedDateOnly.value = ''
  emit('update:modelValue', '')
}
</script>
