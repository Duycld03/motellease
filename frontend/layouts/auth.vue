<template>
  <div class="min-h-screen flex flex-col justify-between bg-slate-50 dark:bg-slate-950 text-slate-800 dark:text-slate-100 antialiased transition-colors">
    <!-- Simple Topbar with Logo & Language Switcher -->
    <header class="w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 flex items-center justify-between">
      <NuxtLinkLocale to="/" class="flex items-center gap-2.5 font-bold text-lg text-slate-900 dark:text-white group">
        <div class="w-9 h-9 rounded-xl bg-primary-600 flex items-center justify-center text-white shadow-sm shadow-primary-500/20 group-hover:bg-primary-700 transition-colors">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
        </div>
        <span class="tracking-tight">Motel<span class="text-primary-600 dark:text-primary-400">Lease</span></span>
      </NuxtLinkLocale>

      <div class="flex items-center gap-3">
        <ThemeSwitcher />
        <LanguageSwitcher />
      </div>
    </header>

    <!-- Centered Form Container -->
    <main class="flex-1 flex items-center justify-center p-4 sm:p-6 lg:p-8">
      <div class="w-full max-w-md">
        <slot />
      </div>
    </main>

    <!-- Simple Bottom Footer -->
    <footer class="py-6 text-center text-xs text-slate-400">
      <p>© 2026 MotelLease. All rights reserved.</p>
    </footer>

    <ToastContainer />
  </div>
</template>

<script setup lang="ts">
import LanguageSwitcher from '~/components/common/LanguageSwitcher.vue'
import ToastContainer from '~/components/common/ToastContainer.vue'
</script>
