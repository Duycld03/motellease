<template>
  <aside class="w-64 bg-white dark:bg-slate-950 border-r border-slate-200 dark:border-slate-800 min-h-[calc(100vh-4rem)] flex flex-col justify-between py-6 px-4 transition-colors">
    <div class="space-y-6">
      <!-- Portal Tag -->
      <div class="px-3 py-2 bg-primary-50 dark:bg-primary-950/40 rounded-xl flex items-center gap-2 text-primary-800 dark:text-primary-300">
        <div class="w-2 h-2 rounded-full bg-primary-600 animate-pulse" />
        <span class="text-xs font-bold uppercase tracking-wider">{{ $t('nav.ownerPortal') }}</span>
      </div>

      <!-- Nav Items -->
      <nav class="space-y-1 text-sm font-medium">
        <NuxtLinkLocale
          to="/owner/dashboard"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
          </svg>
          {{ $t('nav.dashboard') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/properties"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
          {{ $t('nav.properties') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/appointments"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          {{ $t('nav.appointments') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/deposits"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          {{ $t('nav.myDeposits') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/leases"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          {{ $t('nav.myLeases') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/bills"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
          </svg>
          {{ $t('nav.myBills') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/expenses"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
          </svg>
          {{ $t('nav.expenses') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/analytics"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
          {{ $t('nav.analytics') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/staff"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
          </svg>
          {{ $t('nav.staff') }}
        </NuxtLinkLocale>

        <NuxtLinkLocale
          to="/owner/withdraw"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold shadow-sm shadow-primary-500/5"
        >
          <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
          </svg>
          {{ $t('nav.withdrawals') }}
        </NuxtLinkLocale>
      </nav>
    </div>
  </aside>
</template>

<script setup lang="ts">
</script>
