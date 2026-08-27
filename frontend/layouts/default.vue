<template>
  <div class="min-h-screen flex flex-col bg-slate-50 text-slate-800 antialiased">
    <AppHeader />
    <main class="flex-1">
      <slot />
    </main>
    <AppFooter />
    <ToastContainer />
  </div>
</template>

<script setup lang="ts">
import AppHeader from '~/components/common/AppHeader.vue'
import AppFooter from '~/components/common/AppFooter.vue'
import ToastContainer from '~/components/common/ToastContainer.vue'
</script>
