<template>
  <header class="sticky top-0 z-40 w-full border-b border-slate-200/80 bg-white/90 backdrop-blur-md dark:bg-slate-950/90 dark:border-slate-800 transition-colors">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between gap-4">
      <!-- Logo -->
      <NuxtLink to="/" class="flex items-center gap-2.5 font-bold text-lg text-slate-900 dark:text-white group">
        <div class="w-9 h-9 rounded-xl bg-primary-600 flex items-center justify-center text-white shadow-sm shadow-primary-500/20 group-hover:bg-primary-700 transition-colors">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
        </div>
        <span class="tracking-tight">Motel<span class="text-primary-600">Lease</span></span>
      </NuxtLink>

      <!-- Nav links (Desktop) -->
      <nav class="hidden md:flex items-center gap-6 text-sm font-medium text-slate-600 dark:text-slate-300">
        <NuxtLink to="/" class="hover:text-primary-600 dark:hover:text-primary-400 transition-colors" active-class="text-primary-600 dark:text-primary-400 font-semibold">
          {{ $t('nav.home') }}
        </NuxtLink>
        <NuxtLink to="/search" class="hover:text-primary-600 dark:hover:text-primary-400 transition-colors" active-class="text-primary-600 dark:text-primary-400 font-semibold">
          {{ $t('nav.search') }}
        </NuxtLink>
        <NuxtLink v-if="isAuthenticated && isTenant" to="/tenant/saved" class="hover:text-primary-600 dark:hover:text-primary-400 transition-colors" active-class="text-primary-600 dark:text-primary-400 font-semibold">
          {{ $t('nav.savedListings') }}
        </NuxtLink>
      </nav>

      <!-- Right controls -->
      <div class="flex items-center gap-2 sm:gap-3">
        <!-- Theme Switcher -->
        <ThemeSwitcher />

        <!-- Language Switcher -->
        <LanguageSwitcher />

        <!-- Notifications -->
        <NotificationBell v-if="isAuthenticated" />

        <!-- User section / Auth buttons -->
        <div v-if="isAuthenticated" class="relative" ref="userDropdownRef">
          <button
            type="button"
            class="flex items-center gap-2 p-1.5 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors text-slate-700 dark:text-slate-100"
            @click="isUserMenuOpen = !isUserMenuOpen"
          >
            <div class="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-950/80 text-primary-700 dark:text-primary-300 flex items-center justify-center font-bold text-xs">
              {{ userInitials }}
            </div>
            <div class="hidden sm:block text-left">
              <span class="block text-xs font-semibold text-slate-800 dark:text-white leading-tight max-w-[120px] truncate">
                {{ user?.fullName }}
              </span>
              <span class="block text-[10px] text-slate-500 dark:text-slate-400 font-medium">
                {{ roleLabel }}
              </span>
            </div>
            <svg class="w-4 h-4 text-slate-400 dark:text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          <!-- User dropdown menu -->
          <div
            v-if="isUserMenuOpen"
            class="absolute right-0 mt-2 w-48 bg-white dark:bg-slate-900 rounded-2xl shadow-xl border border-slate-100 dark:border-slate-800 py-1.5 z-50 animate-in fade-in zoom-in-95 duration-100"
          >
            <NuxtLink
              :to="dashboardRoute"
              class="block px-4 py-2 text-xs font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
              @click="isUserMenuOpen = false"
            >
              {{ $t('nav.dashboard') }}
            </NuxtLink>
            <NuxtLink
              to="/tenant/profile"
              class="block px-4 py-2 text-xs font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
              @click="isUserMenuOpen = false"
            >
              {{ $t('nav.profile') }}
            </NuxtLink>
            <div class="border-t border-slate-100 dark:border-slate-800 my-1" />
            <button
              type="button"
              class="w-full text-left px-4 py-2 text-xs font-medium text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40 transition-colors"
              @click="handleLogout"
            >
              {{ $t('nav.logout') }}
            </button>
          </div>
        </div>

        <div v-else class="flex items-center gap-2">
          <NuxtLink to="/auth/login" class="px-3.5 py-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:text-primary-600 dark:hover:text-primary-400 transition-colors">
            {{ $t('nav.login') }}
          </NuxtLink>
          <NuxtLink to="/auth/register" class="btn-primary !px-3.5 !py-1.5 !text-xs !rounded-lg">
            {{ $t('nav.register') }}
          </NuxtLink>
        </div>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { onClickOutside } from '@vueuse/core'
import LanguageSwitcher from './LanguageSwitcher.vue'
import NotificationBell from './NotificationBell.vue'
import ThemeSwitcher from './ThemeSwitcher.vue'

const { user, isAuthenticated, isTenant, role, getDefaultRouteForRole, logout } = useAuth()
const { t } = useI18n()

const isUserMenuOpen = ref(false)
const userDropdownRef = ref<HTMLElement | null>(null)

onClickOutside(userDropdownRef, () => {
  isUserMenuOpen.value = false
})

const dashboardRoute = computed(() => getDefaultRouteForRole(role.value))

const roleLabel = computed(() => {
  if (!role.value) return ''
  return t(`roles.${role.value}`)
})

const userInitials = computed(() => {
  if (!user.value?.fullName) return 'U'
  const parts = user.value.fullName.trim().split(' ')
  return parts[parts.length - 1]?.charAt(0).toUpperCase() || 'U'
})

const handleLogout = async () => {
  isUserMenuOpen.value = false
  await logout()
}
</script>
