<template>
  <div class="min-h-screen flex flex-col bg-slate-50 text-slate-800 antialiased">
    <AppHeader />
    <TenantNav />
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <slot />
    </main>
    <AppFooter />
    <ToastContainer />
  </div>
</template>

<script setup lang="ts">
import AppHeader from '~/components/common/AppHeader.vue'
import AppFooter from '~/components/common/AppFooter.vue'
import TenantNav from '~/components/layout/TenantNav.vue'
import ToastContainer from '~/components/common/ToastContainer.vue'
</script>
