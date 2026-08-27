<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <!-- Search & Filter Bar -->
    <div class="bg-white rounded-2xl p-4 sm:p-6 border border-slate-200 shadow-sm mb-8">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Keyword search -->
        <div>
          <label class="block text-xs font-semibold text-slate-600 mb-1">{{ $t('common.search') }}</label>
          <input
            v-model="filters.q"
            type="text"
            class="input-field !text-xs !py-2"
            :placeholder="$t('home.searchPrompt')"
            @keyup.enter="fetchResults"
          />
        </div>

        <!-- Property Type -->
        <div>
          <label class="block text-xs font-semibold text-slate-600 mb-1">Loại nhà trọ</label>
          <select v-model="filters.type" class="input-field !text-xs !py-2">
            <option value="">{{ $t('common.all') }}</option>
            <option value="Traditional">Phòng trọ truyền thống</option>
            <option value="MiniHouse">Căn hộ mini</option>
            <option value="DormStyle">Ký túc xá / Sleepbox</option>
          </select>
        </div>

        <!-- Sort -->
        <div>
          <label class="block text-xs font-semibold text-slate-600 mb-1">Sắp xếp theo</label>
          <select v-model="filters.sort" class="input-field !text-xs !py-2">
            <option value="newest">Mới nhất</option>
            <option value="rating">Đánh giá cao nhất</option>
            <option value="price_asc">Giá tăng dần</option>
            <option value="price_desc">Giá giảm dần</option>
          </select>
        </div>

        <!-- Search button -->
        <div class="flex items-end gap-2">
          <BaseButton variant="primary" size="md" class="flex-1 !py-2 !text-xs" @click="fetchResults">
            {{ $t('common.search') }}
          </BaseButton>
          <BaseButton variant="outline" size="md" class="!py-2 !text-xs" @click="resetFilters">
            {{ $t('common.reset') }}
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- Results count & map toggle -->
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-lg font-bold text-slate-900">
        Kết quả tìm kiếm
        <span class="text-xs font-normal text-slate-500 ml-2">({{ total }} khu trọ)</span>
      </h1>

      <div class="flex items-center bg-slate-100 p-1 rounded-xl">
        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition-all',
            viewMode === 'list' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-900',
          ]"
          @click="viewMode = 'list'"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
          </svg>
          Danh sách
        </button>
        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition-all',
            viewMode === 'map' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-900',
          ]"
          @click="viewMode = 'map'"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
          </svg>
          Bản đồ
        </button>
      </div>
    </div>

    <!-- Map View -->
    <div v-if="viewMode === 'map'" class="h-[600px] mb-8">
      <ClientOnly>
        <MapView
          :markers="mapMarkers"
          @click-marker="(id) => navigateTo(`/boarding-houses/${id}`)"
        />
        <template #fallback>
          <div class="w-full h-full bg-slate-100 rounded-2xl flex items-center justify-center text-slate-400 text-xs">
            Đang tải bản đồ...
          </div>
        </template>
      </ClientOnly>
    </div>

    <!-- Listings Grid (List View) -->
    <div v-if="viewMode === 'list'">
      <div v-if="isLoading" class="py-20 text-center">
        <LoadingSpinner size="lg" :text="$t('common.loading')" />
      </div>

      <div v-else-if="items.length === 0" class="py-16 text-center bg-white rounded-2xl border border-slate-200">
        <svg class="w-12 h-12 text-slate-300 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
        </svg>
        <p class="text-sm font-semibold text-slate-700">{{ $t('common.noData') }}</p>
        <p class="text-xs text-slate-400 mt-1">Hãy thử tìm kiếm với từ khóa hoặc bộ lọc khác</p>
      </div>

      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        <div
          v-for="house in items"
          :key="house.id"
          class="bg-white rounded-2xl overflow-hidden border border-slate-200 shadow-sm hover:shadow-md transition-shadow group flex flex-col cursor-pointer"
          @click="navigateTo(`/boarding-houses/${house.id}`)"
        >
          <div class="aspect-[16/10] bg-slate-100 relative overflow-hidden flex items-center justify-center text-slate-400">
            <img
              v-if="house.images && house.images.length > 0"
              :src="house.images[0].url"
              :alt="house.name"
              class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            />
            <svg v-else class="w-10 h-10 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <div class="absolute top-3 right-3">
              <StatusBadge type="ListingStatus" :status="house.status" />
            </div>
          </div>

          <div class="p-5 flex-1 flex flex-col justify-between">
            <div>
              <div class="flex items-center justify-between mb-1.5">
                <span class="text-xs font-semibold text-primary-600">{{ house.type }}</span>
                <div class="flex items-center gap-1 text-xs text-amber-500 font-semibold">
                  <span>★</span>
                  <span>{{ house.averageRating ? house.averageRating.toFixed(1) : '5.0' }}</span>
                  <span class="text-slate-400 font-normal">({{ house.reviewCount || 0 }})</span>
                </div>
              </div>
              <h3 class="text-sm font-bold text-slate-900 group-hover:text-primary-600 transition-colors line-clamp-1">
                {{ house.name }}
              </h3>
              <p class="text-xs text-slate-500 mt-1 line-clamp-1 flex items-center gap-1">
                <svg class="w-3.5 h-3.5 flex-shrink-0 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                {{ house.address }}, {{ house.district }}, {{ house.province }}
              </p>
            </div>

            <div class="mt-4 pt-4 border-t border-slate-100 flex items-center justify-between">
              <div>
                <span class="text-xs text-slate-400">Điện: {{ formatCurrency(house.electricityPrice) }}/kWh</span>
              </div>
              <span class="text-xs font-semibold text-primary-600">
                {{ $t('common.view') }} →
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
import MapView, { type MapMarker } from '~/components/common/MapView.client.vue'
import type { BoardingHouse, PagedResult } from '~/types/api'

const route = useRoute()
const { get } = useApi()
const { formatCurrency } = useFormat()

const viewMode = ref<'list' | 'map'>('list')
const isLoading = ref(false)
const items = ref<BoardingHouse[]>([])
const total = ref(0)

const mapMarkers = computed<MapMarker[]>(() =>
  items.value.map((h) => ({
    id: h.id,
    name: h.name,
    latitude: h.latitude,
    longitude: h.longitude,
    price: h.minPrice,
    address: `${h.address}, ${h.district}`,
  }))
)

const filters = reactive({
  q: (route.query.q as string) || '',
  type: (route.query.type as string) || '',
  sort: (route.query.sort as string) || 'newest',
  page: 1,
  pageSize: 12,
})

const fetchResults = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<BoardingHouse>>('/boarding-houses', {
      q: filters.q || undefined,
      type: filters.type || undefined,
      sort: filters.sort || undefined,
      page: filters.page,
      pageSize: filters.pageSize,
    })
    items.value = data?.items || []
    total.value = data?.total || 0
  } catch (err) {
    items.value = []
    total.value = 0
  } finally {
    isLoading.value = false
  }
}

const resetFilters = () => {
  filters.q = ''
  filters.type = ''
  filters.sort = 'newest'
  filters.page = 1
  fetchResults()
}

onMounted(() => {
  fetchResults()
})
</script>
