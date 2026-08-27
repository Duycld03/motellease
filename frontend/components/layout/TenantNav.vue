<template>
  <div class="bg-white dark:bg-slate-950 border-b border-slate-200 dark:border-slate-800 sticky top-16 z-30 shadow-sm transition-colors">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <nav class="flex space-x-1 sm:space-x-4 overflow-x-auto py-2.5 text-xs font-medium scrollbar-none">
        <NuxtLink
          to="/tenant/dashboard"
          class="px-3 py-1.5 rounded-lg text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors whitespace-nowrap"
          active-class="!text-primary-700 dark:!text-primary-400 !bg-primary-50 dark:!bg-primary-950/50 font-semibold"
        >
          {{ $t('nav.dashboard') }}
        </NuxtLink>
        <NuxtLink
          to="/tenant/leases"
          class="px-3 py-1.5 rounded-lg text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors whitespace-nowrap"
          active-class="!text-primary-700 !bg-primary-50 font-semibold"
        >
          {{ $t('nav.myLeases') }}
        </NuxtLink>
        <NuxtLink
          to="/tenant/deposits"
          class="px-3 py-1.5 rounded-lg text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors whitespace-nowrap"
          active-class="!text-primary-700 !bg-primary-50 font-semibold"
        >
          {{ $t('nav.myDeposits') }}
        </NuxtLink>
        <NuxtLink
          to="/tenant/appointments"
          class="px-3 py-1.5 rounded-lg text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors whitespace-nowrap"
          active-class="!text-primary-700 !bg-primary-50 font-semibold"
        >
          {{ $t('nav.myAppointments') }}
        </NuxtLink>
        <NuxtLink
          to="/tenant/bills"
          class="px-3 py-1.5 rounded-lg text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors whitespace-nowrap"
          active-class="!text-primary-700 !bg-primary-50 font-semibold"
        >
          {{ $t('nav.myBills') }}
        </NuxtLink>
        <NuxtLink
          to="/tenant/maintenance"
          class="px-3 py-1.5 rounded-lg text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors whitespace-nowrap"
          active-class="!text-primary-700 !bg-primary-50 font-semibold"
        >
          {{ $t('nav.myMaintenance') }}
        </NuxtLink>
        <NuxtLink
          to="/tenant/saved"
          class="px-3 py-1.5 rounded-lg text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors whitespace-nowrap"
          active-class="!text-primary-700 !bg-primary-50 font-semibold"
        >
          {{ $t('nav.savedListings') }}
        </NuxtLink>
        <NuxtLink
          to="/tenant/profile"
          class="px-3 py-1.5 rounded-lg text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors whitespace-nowrap"
          active-class="!text-primary-700 !bg-primary-50 font-semibold"
        >
          {{ $t('nav.profile') }}
        </NuxtLink>
      </nav>
    </div>
  </div>
</template>

<script setup lang="ts">
</script>
