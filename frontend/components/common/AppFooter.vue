<template>
  <footer class="bg-slate-900 text-slate-400 text-sm mt-auto border-t border-slate-800">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
      <div class="grid grid-cols-1 md:grid-cols-4 gap-8 mb-8">
        <!-- Brand -->
        <div class="md:col-span-2">
          <div class="flex items-center gap-2.5 font-bold text-lg text-white mb-3">
            <div class="w-8 h-8 rounded-lg bg-primary-500 flex items-center justify-center text-white">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
            </div>
            <span>Motel<span class="text-primary-400">Lease</span></span>
          </div>
          <p class="text-xs text-slate-400 max-w-sm leading-relaxed mb-4">
            {{ $t('home.heroSubtitle') }}
          </p>
        </div>

        <!-- Quick Links -->
        <div>
          <h4 class="text-xs font-semibold text-white uppercase tracking-wider mb-3">
            {{ $t('nav.search') }}
          </h4>
          <ul class="space-y-2 text-xs">
            <li>
              <NuxtLink to="/search" class="hover:text-white transition-colors">{{ $t('nav.search') }}</NuxtLink>
            </li>
            <li>
              <NuxtLink to="/auth/login" class="hover:text-white transition-colors">{{ $t('nav.login') }}</NuxtLink>
            </li>
            <li>
              <NuxtLink to="/auth/register" class="hover:text-white transition-colors">{{ $t('nav.register') }}</NuxtLink>
            </li>
          </ul>
        </div>

        <!-- Portals -->
        <div>
          <h4 class="text-xs font-semibold text-white uppercase tracking-wider mb-3">
            {{ $t('common.actions') }}
          </h4>
          <ul class="space-y-2 text-xs">
            <li>
              <NuxtLink to="/tenant/dashboard" class="hover:text-white transition-colors">{{ $t('nav.tenantPortal') }}</NuxtLink>
            </li>
            <li>
              <NuxtLink to="/owner/dashboard" class="hover:text-white transition-colors">{{ $t('nav.ownerPortal') }}</NuxtLink>
            </li>
            <li>
              <NuxtLink to="/staff/dashboard" class="hover:text-white transition-colors">{{ $t('nav.staffPortal') }}</NuxtLink>
            </li>
          </ul>
        </div>
      </div>

      <div class="pt-8 border-t border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-slate-500">
        <p>© 2026 MotelLease. All rights reserved.</p>
        <p class="flex items-center gap-4">
          <span>Powered by ASP.NET Core & Nuxt</span>
        </p>
      </div>
    </div>
  </footer>
</template>

<script setup lang="ts">
</script>
