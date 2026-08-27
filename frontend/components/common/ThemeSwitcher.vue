<template>
  <ClientOnly>
    <button
      type="button"
      :class="[
        'p-2 rounded-xl text-slate-500 hover:text-slate-900 hover:bg-slate-100 dark:text-slate-400 dark:hover:text-slate-100 dark:hover:bg-slate-800 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500',
        customClass,
      ]"
      :title="isDark ? 'Chuyển sang giao diện Sáng' : 'Chuyển sang giao diện Tối'"
      @click="toggleTheme"
    >
      <!-- Sun icon (when dark mode is active, click to turn light) -->
      <svg
        v-if="isDark"
        class="w-5 h-5 text-amber-400 transition-transform duration-300 rotate-0 hover:rotate-45"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"
        />
      </svg>

      <!-- Moon icon (when light mode is active, click to turn dark) -->
      <svg
        v-else
        class="w-5 h-5 text-slate-600 transition-transform duration-300 -rotate-12 hover:rotate-0"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"
        />
      </svg>
    </button>
    <template #fallback>
      <div class="w-9 h-9" />
    </template>
  </ClientOnly>
</template>

<script setup lang="ts">
defineProps<{
  customClass?: string
}>()

const { isDark, toggleTheme } = useTheme()
</script>
