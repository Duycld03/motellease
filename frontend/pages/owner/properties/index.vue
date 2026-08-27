<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.properties') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">Danh sách các khu trọ bạn sở hữu hoặc được phân công quản lý</p>
      </div>

      <NuxtLink to="/owner/properties/create" class="btn-primary !text-xs !py-2.5 !px-4 !rounded-xl">
        + Thêm khu trọ mới
      </NuxtLink>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="lg" :text="$t('common.loading')" />
    </div>

    <!-- Empty state -->
    <div v-else-if="items.length === 0" class="py-16 text-center bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm p-8">
      <div class="w-14 h-14 rounded-2xl bg-primary-50 dark:bg-primary-950/40 text-primary-600 dark:text-primary-400 flex items-center justify-center mx-auto mb-4">
        <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
        </svg>
      </div>
      <h3 class="text-sm font-bold text-slate-800 dark:text-slate-200">Bạn chưa có khu trọ nào</h3>
      <p class="text-xs text-slate-500 dark:text-slate-400 mt-1 max-w-sm mx-auto">
        Tạo khu trọ đầu tiên để bắt đầu thêm loại phòng, danh sách phòng và tiếp cận khách thuê trực tuyến.
      </p>
      <div class="mt-6">
        <NuxtLink to="/owner/properties/create" class="btn-primary !text-xs !py-2 !px-4">
          + Thêm khu trọ ngay
        </NuxtLink>
      </div>
    </div>

    <!-- Properties Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="house in items"
        :key="house.id"
        class="bg-white dark:bg-slate-900 rounded-2xl overflow-hidden border border-slate-200 dark:border-slate-800 shadow-sm hover:shadow-md transition-shadow flex flex-col justify-between"
      >
        <div>
          <!-- Thumbnail & Status -->
          <div class="aspect-[16/9] bg-slate-100 dark:bg-slate-800 relative overflow-hidden flex items-center justify-center text-slate-400">
            <img
              v-if="house.primaryImageUrl"
              :src="house.primaryImageUrl"
              :alt="house.name"
              class="w-full h-full object-cover"
            />
            <svg v-else class="w-10 h-10 text-slate-300 dark:text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>

            <div class="absolute top-3 right-3">
              <StatusBadge type="ListingStatus" :status="house.listingStatus" />
            </div>

            <div class="absolute bottom-3 left-3 px-2 py-0.5 rounded-md bg-slate-900/70 backdrop-blur-sm text-white text-[10px] font-semibold">
              {{ $t(`enums.BoardingHouseType.${house.type}`) }}
            </div>
          </div>

          <!-- Content -->
          <div class="p-5 space-y-3">
            <div>
              <h3 class="text-sm font-bold text-slate-900 dark:text-white line-clamp-1">
                {{ house.name }}
              </h3>
              <p class="text-xs text-slate-500 dark:text-slate-400 mt-1 line-clamp-1 flex items-center gap-1">
                <svg class="w-3.5 h-3.5 flex-shrink-0 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                {{ house.addressLine }}, {{ house.district }}, {{ house.province }}
              </p>
            </div>

            <!-- Stats strip -->
            <div class="grid grid-cols-2 gap-2 p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-center">
              <div>
                <span class="text-[10px] text-slate-400 block">Tổng số phòng</span>
                <span class="text-xs font-bold text-slate-800 dark:text-slate-200">{{ house.roomCount }} phòng</span>
              </div>
              <div>
                <span class="text-[10px] text-slate-400 block">Phòng trống</span>
                <span class="text-xs font-bold text-emerald-600 dark:text-emerald-400">{{ house.availableRoomCount }} trống</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Footer Actions -->
        <div class="px-5 pb-5 pt-2 flex items-center justify-between gap-2 border-t border-slate-100 dark:border-slate-800 mt-2">
          <NuxtLink
            :to="`/owner/properties/${house.id}`"
            class="flex-1 btn-primary !text-xs !py-2 text-center"
          >
            Quản lý khu trọ
          </NuxtLink>

          <NuxtLink
            :to="`/owner/properties/${house.id}/edit`"
            class="p-2 text-slate-500 hover:text-slate-900 dark:hover:text-white rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
            title="Chỉnh sửa thông tin"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </NuxtLink>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
import type { BoardingHouseSummaryResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get } = useApi()

const isLoading = ref(true)
const items = ref<BoardingHouseSummaryResponse[]>([])

const fetchProperties = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<BoardingHouseSummaryResponse>>('/my/boarding-houses')
    items.value = data?.items || []
  } catch {
    items.value = []
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchProperties()
})
</script>
