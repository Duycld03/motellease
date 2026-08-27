<template>
  <div class="relative" ref="dropdownRef">
    <button
      type="button"
      class="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors shadow-sm"
      @click="isOpen = !isOpen"
    >
      <span class="text-base">{{ currentLocaleItem?.code === 'vi' ? '🇻🇳' : '🇬🇧' }}</span>
      <span class="hidden sm:inline font-semibold">{{ currentLocaleItem?.name }}</span>
      <svg class="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <div
      v-if="isOpen"
      class="absolute right-0 mt-1.5 w-36 bg-white rounded-xl shadow-lg border border-slate-100 py-1 z-50 animate-in fade-in zoom-in-95 duration-100"
    >
      <button
        v-for="loc in availableLocales"
        :key="loc.code"
        type="button"
        :class="[
          'w-full text-left px-3 py-2 text-xs flex items-center gap-2 hover:bg-slate-50 transition-colors',
          locale === loc.code ? 'font-semibold text-primary-600 bg-primary-50/50' : 'text-slate-700',
        ]"
        @click="changeLanguage(loc.code)"
      >
        <span class="text-sm">{{ loc.code === 'vi' ? '🇻🇳' : '🇬🇧' }}</span>
        <span>{{ loc.name }}</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onClickOutside } from '@vueuse/core'

const { locale, locales, setLocale } = useI18n()
const isOpen = ref(false)
const dropdownRef = ref<HTMLElement | null>(null)

onClickOutside(dropdownRef, () => {
  isOpen.value = false
})

const availableLocales = computed(() => locales.value as { code: string; name: string }[])
const currentLocaleItem = computed(() =>
  availableLocales.value.find((l) => l.code === locale.value)
)

const changeLanguage = async (newLocale: string) => {
  await setLocale(newLocale as any)
  isOpen.value = false
}
</script>
