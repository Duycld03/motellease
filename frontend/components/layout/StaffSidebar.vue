<template>
  <aside class="w-64 bg-white dark:bg-slate-950 border-r border-slate-200 dark:border-slate-800 min-h-[calc(100vh-4rem)] flex flex-col justify-between py-6 px-4 transition-colors">
    <div class="space-y-6">
      <!-- Portal Tag -->
      <div class="px-3 py-2 bg-sky-50 dark:bg-sky-950/40 rounded-xl flex items-center gap-2 text-sky-800 dark:text-sky-300">
        <div class="w-2 h-2 rounded-full bg-sky-600 animate-pulse" />
        <span class="text-xs font-bold uppercase tracking-wider">{{ $t('nav.staffPortal') }}</span>
      </div>

      <!-- Nav Items -->
      <nav class="space-y-1 text-sm font-medium">
        <NuxtLinkLocale
          to="/staff/dashboard"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-sky-700 dark:!text-sky-400 !bg-sky-50 dark:!bg-sky-950/50 font-semibold shadow-sm shadow-sky-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
          </svg>
          {{ $t('nav.dashboard') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/staff/properties"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-sky-700 dark:!text-sky-400 !bg-sky-50 dark:!bg-sky-950/50 font-semibold shadow-sm shadow-sky-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
          {{ $t('nav.properties') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/staff/tasks"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-sky-700 dark:!text-sky-400 !bg-sky-50 dark:!bg-sky-950/50 font-semibold shadow-sm shadow-sky-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
          </svg>
          {{ $t('nav.tasks') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/staff/appointments"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-sky-700 dark:!text-sky-400 !bg-sky-50 dark:!bg-sky-950/50 font-semibold shadow-sm shadow-sky-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          {{ $t('nav.myAppointments') }}
        </NuxtLinkLocale>
      </nav>
    </div>
  </aside>
</template>

<script setup lang="ts">
</script>
