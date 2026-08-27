<template>
  <div class="min-h-screen flex flex-col bg-slate-50 text-slate-800 antialiased">
    <AppHeader />
    <div class="flex-1 flex">
      <!-- Desktop Sidebar -->
      <div class="hidden md:block">
        <OwnerSidebar />
      </div>

      <!-- Main Content -->
      <main class="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl">
        <slot />
      </main>
    </div>
    <ToastContainer />
  </div>
</template>

<script setup lang="ts">
import AppHeader from '~/components/common/AppHeader.vue'
import OwnerSidebar from '~/components/layout/OwnerSidebar.vue'
import ToastContainer from '~/components/common/ToastContainer.vue'
</script>
