<template>
  <div>
    <!-- Hero Section -->
    <section class="relative bg-gradient-to-b from-primary-50/60 via-white to-slate-50 py-16 sm:py-24 border-b border-slate-200/60">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
        <!-- Badge -->
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary-100 text-primary-800 text-xs font-semibold mb-6">
          <span class="w-2 h-2 rounded-full bg-primary-600 animate-ping" />
          <span>{{ $t('home.whyUsTitle') }}</span>
        </div>

        <!-- Headline -->
        <h1 class="text-3xl sm:text-5xl font-extrabold text-slate-900 tracking-tight max-w-3xl mx-auto leading-tight sm:leading-tight">
          {{ $t('home.heroTitle') }}
        </h1>
        <p class="mt-4 text-base sm:text-lg text-slate-600 max-w-2xl mx-auto leading-relaxed">
          {{ $t('home.heroSubtitle') }}
        </p>

        <!-- Search Bar Card -->
        <div class="mt-10 max-w-3xl mx-auto bg-white p-3 sm:p-4 rounded-2xl shadow-xl shadow-slate-200/60 border border-slate-200 flex flex-col sm:flex-row items-center gap-3">
          <div class="relative flex-1 w-full">
            <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
            <input
              v-model="searchQuery"
              type="text"
              class="w-full pl-10 pr-4 py-2.5 text-sm text-slate-800 placeholder-slate-400 bg-slate-50 sm:bg-transparent rounded-xl border border-slate-200 sm:border-0 focus:outline-none focus:ring-2 sm:focus:ring-0 focus:ring-primary-500"
              :placeholder="$t('home.searchPrompt')"
              @keyup.enter="handleSearch"
            />
          </div>

          <BaseButton
            variant="primary"
            size="md"
            class="w-full sm:w-auto !py-3 !px-6 whitespace-nowrap !rounded-xl"
            @click="handleSearch"
          >
            <template #icon-left>
              <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </template>
            {{ $t('home.searchButton') }}
          </BaseButton>
        </div>
      </div>
    </section>

    <!-- Key Features Section -->
    <section class="py-16 bg-white border-b border-slate-200/60">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div class="p-6 rounded-2xl bg-slate-50 border border-slate-100 flex flex-col items-start">
            <div class="w-12 h-12 rounded-xl bg-primary-100 text-primary-700 flex items-center justify-center mb-4">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <h3 class="text-base font-bold text-slate-900 mb-2">
              {{ $t('home.feature1Title') }}
            </h3>
            <p class="text-xs text-slate-600 leading-relaxed">
              {{ $t('home.feature1Desc') }}
            </p>
          </div>

          <div class="p-6 rounded-2xl bg-slate-50 border border-slate-100 flex flex-col items-start">
            <div class="w-12 h-12 rounded-xl bg-primary-100 text-primary-700 flex items-center justify-center mb-4">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <h3 class="text-base font-bold text-slate-900 mb-2">
              {{ $t('home.feature2Title') }}
            </h3>
            <p class="text-xs text-slate-600 leading-relaxed">
              {{ $t('home.feature2Desc') }}
            </p>
          </div>

          <div class="p-6 rounded-2xl bg-slate-50 border border-slate-100 flex flex-col items-start">
            <div class="w-12 h-12 rounded-xl bg-primary-100 text-primary-700 flex items-center justify-center mb-4">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
            <h3 class="text-base font-bold text-slate-900 mb-2">
              {{ $t('home.feature3Title') }}
            </h3>
            <p class="text-xs text-slate-600 leading-relaxed">
              {{ $t('home.feature3Desc') }}
            </p>
          </div>
        </div>
      </div>
    </section>

    <!-- Featured Boarding Houses -->
    <section class="py-16 bg-slate-50">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between mb-8">
          <div>
            <h2 class="text-2xl font-bold text-slate-900">
              {{ $t('home.featuredTitle') }}
            </h2>
            <p class="text-xs text-slate-500 mt-1">
              {{ $t('home.featuredSubtitle') }}
            </p>
          </div>
          <NuxtLink to="/search" class="text-xs font-semibold text-primary-600 hover:text-primary-700 flex items-center gap-1">
            {{ $t('home.viewAllListings') }}
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </NuxtLink>
        </div>

        <!-- Placeholder Cards Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          <div
            v-for="i in 3"
            :key="i"
            class="bg-white rounded-2xl overflow-hidden border border-slate-200 shadow-sm hover:shadow-md transition-shadow group flex flex-col"
          >
            <div class="aspect-[16/10] bg-slate-200 relative overflow-hidden flex items-center justify-center text-slate-400">
              <svg class="w-12 h-12" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <div class="absolute top-3 right-3">
                <StatusBadge type="ListingStatus" status="Published" />
              </div>
            </div>
            <div class="p-5 flex-1 flex flex-col justify-between">
              <div>
                <div class="flex items-center justify-between mb-1.5">
                  <span class="text-xs font-semibold text-primary-600">Phòng trọ mini</span>
                  <div class="flex items-center gap-1 text-xs text-amber-500 font-semibold">
                    <span>★</span>
                    <span>4.9</span>
                    <span class="text-slate-400 font-normal">(18)</span>
                  </div>
                </div>
                <h3 class="text-sm font-bold text-slate-900 group-hover:text-primary-600 transition-colors line-clamp-1">
                  Nhà trọ MotelLease An Phú - Phòng cao cấp khép kín
                </h3>
                <p class="text-xs text-slate-500 mt-1 line-clamp-1 flex items-center gap-1">
                  <svg class="w-3.5 h-3.5 flex-shrink-0 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  Quận Cầu Giấy, Hà Nội
                </p>
              </div>

              <div class="mt-4 pt-4 border-t border-slate-100 flex items-center justify-between">
                <div>
                  <span class="text-xs text-slate-400">Từ</span>
                  <span class="text-sm font-bold text-slate-900 ml-1">3.500.000 ₫</span>
                  <span class="text-xs text-slate-500">/tháng</span>
                </div>
                <NuxtLink to="/search" class="text-xs font-semibold text-primary-600 hover:text-primary-700">
                  {{ $t('common.detail') }} →
                </NuxtLink>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'

const searchQuery = ref('')

const handleSearch = () => {
  if (searchQuery.value.trim()) {
    navigateTo({
      path: '/search',
      query: { q: searchQuery.value.trim() },
    })
  } else {
    navigateTo('/search')
  }
}
</script>
