<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.savedListings') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">Danh sách các khu trọ bạn đã lưu để theo dõi</p>
      </div>

      <NuxtLink to="/search" class="btn-primary !text-xs !py-2 !px-4">
        🔍 Tìm thêm phòng mới
      </NuxtLink>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="lg" :text="$t('common.loading')" />
    </div>

    <!-- Empty State -->
    <div v-else-if="items.length === 0" class="py-16 text-center bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm p-8">
      <div class="w-14 h-14 rounded-2xl bg-rose-50 dark:bg-rose-950/40 text-rose-500 flex items-center justify-center mx-auto mb-4">
        <svg class="w-7 h-7 fill-current" viewBox="0 0 24 24">
          <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
        </svg>
      </div>
      <h3 class="text-sm font-bold text-slate-800 dark:text-slate-200">Bạn chưa lưu khu trọ nào</h3>
      <p class="text-xs text-slate-400 mt-1 max-w-sm mx-auto">
        Khi tìm kiếm, nhấn biểu tượng trái tim trên các thẻ trọ để lưu lại và so sánh giá phòng sau.
      </p>
      <div class="mt-6">
        <NuxtLink to="/search" class="btn-primary !text-xs !py-2 !px-4">
          Khám phá phòng trọ ngay
        </NuxtLink>
      </div>
    </div>

    <!-- Saved Listings Grid -->
    <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="item in items"
        :key="item.id"
        class="bg-white dark:bg-slate-900 rounded-2xl overflow-hidden border border-slate-200 dark:border-slate-800 shadow-sm hover:shadow-md transition-shadow group flex flex-col justify-between cursor-pointer"
        @click="navigateTo(`/boarding-houses/${item.boardingHouseId}`)"
      >
        <div>
          <!-- Thumbnail & Delete Action -->
          <div class="aspect-[16/10] bg-slate-100 dark:bg-slate-800 relative overflow-hidden flex items-center justify-center text-slate-400">
            <img
              v-if="item.boardingHouse?.primaryImageUrl"
              :src="item.boardingHouse.primaryImageUrl"
              :alt="item.boardingHouse.name"
              class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            />
            <svg v-else class="w-10 h-10 text-slate-300 dark:text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>

            <!-- Remove Button -->
            <button
              type="button"
              class="absolute top-3 right-3 p-2 rounded-full bg-white/90 dark:bg-slate-900/90 text-rose-500 hover:bg-rose-500 hover:text-white shadow-sm transition-colors"
              title="Bỏ lưu tin này"
              @click.stop="handleRemove(item.boardingHouseId)"
            >
              <svg class="w-4 h-4 fill-current" viewBox="0 0 24 24">
                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z" />
              </svg>
            </button>
          </div>

          <!-- Content -->
          <div class="p-5 space-y-2">
            <div class="flex items-center justify-between">
              <span class="text-[11px] font-bold px-2 py-0.5 rounded-full bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400">
                {{ item.boardingHouse?.availableRoomsCount > 0 ? `Còn ${item.boardingHouse.availableRoomsCount} phòng trống` : 'Hết phòng' }}
              </span>

              <div class="flex items-center gap-1 text-xs text-amber-500 font-semibold">
                <span>★</span>
                <span>{{ item.boardingHouse?.rating ? item.boardingHouse.rating.toFixed(1) : '5.0' }}</span>
              </div>
            </div>

            <h3 class="text-sm font-bold text-slate-900 dark:text-white group-hover:text-primary-600 transition-colors line-clamp-1">
              {{ item.boardingHouse?.name }}
            </h3>

            <p class="text-xs text-slate-500 dark:text-slate-400 line-clamp-1 flex items-center gap-1">
              <svg class="w-3.5 h-3.5 flex-shrink-0 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              </svg>
              {{ item.boardingHouse?.addressLine }}, {{ item.boardingHouse?.district }}, {{ item.boardingHouse?.province }}
            </p>
          </div>
        </div>

        <!-- Footer -->
        <div class="px-5 pb-5 pt-2 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <span class="text-xs font-bold text-primary-600 dark:text-primary-400">
            {{ item.boardingHouse?.minPrice ? formatCurrency(item.boardingHouse.minPrice) : 'Liên hệ' }}
          </span>

          <span class="text-xs font-semibold text-primary-600 dark:text-primary-400">
            Xem chi tiết →
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import type { SavedListingResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'tenant',
})

const { get, delete: deleteApi } = useApi()
const { formatCurrency } = useFormat()
const toast = useToast()

const isLoading = ref(true)
const items = ref<SavedListingResponse[]>([])

const fetchSaved = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<SavedListingResponse>>('/me/saved-listings', { page: 1, pageSize: 50 })
    items.value = data?.items || []
  } catch {
    items.value = []
  } finally {
    isLoading.value = false
  }
}

const handleRemove = async (houseId: string) => {
  try {
    await deleteApi(`/me/saved-listings/${houseId}`)
    items.value = items.value.filter((i) => i.boardingHouseId !== houseId)
    toast.success('Đã xóa khỏi danh sách yêu thích!')
  } catch (err: any) {
    toast.error(err.message || 'Không thể xóa tin đã lưu.')
  }
}

onMounted(() => {
  fetchSaved()
})
</script>
