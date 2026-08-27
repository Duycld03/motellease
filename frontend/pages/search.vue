<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
    <!-- Search & Filter Controls -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl p-5 sm:p-6 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-colors">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Keyword -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('common.search') }}
          </label>
          <input
            v-model="filters.q"
            type="text"
            class="input-field !text-xs !py-2"
            :placeholder="$t('home.searchPrompt')"
            @keyup.enter="fetchResults"
          />
        </div>

        <!-- Province / District -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">Tỉnh / Thành phố</label>
          <select
            v-model="filters.province"
            class="input-field !text-xs !py-2"
            @change="onProvinceChange"
          >
            <option value="">Tất cả Tỉnh/Thành</option>
            <option v-for="p in provinces" :key="p.code" :value="p.name">
              {{ p.fullName || p.name }}
            </option>
          </select>
        </div>

        <!-- District -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">Quận / Huyện</label>
          <select
            v-model="filters.district"
            :disabled="!filters.province || districts.length === 0"
            class="input-field !text-xs !py-2"
            @change="fetchResults"
          >
            <option value="">Tất cả Quận/Huyện</option>
            <option v-for="d in districts" :key="d.code" :value="d.name">
              {{ d.fullName || d.name }}
            </option>
          </select>
        </div>

        <!-- Property Type -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">Mô hình nhà trọ</label>
          <select v-model="filters.type" class="input-field !text-xs !py-2" @change="fetchResults">
            <option value="">Tất cả mô hình</option>
            <option value="Traditional">Phòng trọ truyền thống</option>
            <option value="MiniHouse">Căn hộ mini / CC mini</option>
            <option value="DormStyle">Ký túc xá / Sleepbox</option>
          </select>
        </div>
      </div>

      <!-- Advanced Filter Row: Price, Sort, Nearby & Facility Pills -->
      <div class="pt-3 border-t border-slate-100 dark:border-slate-800 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 items-end">
        <!-- Price Range Select -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">Khoảng giá thuê</label>
          <select v-model="selectedPriceRange" class="input-field !text-xs !py-2 h-[38px]" @change="onPriceRangeChange">
            <option value="all">Tất cả mức giá</option>
            <option value="under_2m">Dưới 2 triệu</option>
            <option value="2m_4m">2 triệu - 4 triệu</option>
            <option value="4m_7m">4 triệu - 7 triệu</option>
            <option value="above_7m">Trên 7 triệu</option>
          </select>
        </div>

        <!-- Sort -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">Sắp xếp theo</label>
          <select v-model="filters.sort" class="input-field !text-xs !py-2 h-[38px]" @change="fetchResults">
            <option value="newest">Mới nhất</option>
            <option value="rating">Đánh giá cao nhất</option>
            <option value="price_asc">Giá tăng dần</option>
            <option value="price_desc">Giá giảm dần</option>
          </select>
        </div>

        <!-- Geolocation "Near Me" button -->
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">Vị trí của bạn</label>
          <button
            type="button"
            :class="[
              'w-full h-[38px] px-3 rounded-lg text-xs font-semibold flex items-center justify-center gap-1.5 border transition-colors',
              isNearbyActive
                ? 'bg-emerald-50 dark:bg-emerald-950/40 border-emerald-300 dark:border-emerald-700 text-emerald-700 dark:text-emerald-300'
                : 'bg-white dark:bg-slate-900 border-slate-300 dark:border-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
            ]"
            :disabled="isLocating"
            @click="toggleNearMe"
          >
            <span>📍</span>
            <span>{{ isLocating ? 'Đang định vị...' : isNearbyActive ? 'Đang tìm gần tôi (5km)' : 'Tìm trọ gần tôi' }}</span>
          </button>
        </div>

        <!-- Search Action Buttons -->
        <div>
          <label class="block text-xs font-semibold text-transparent select-none mb-1">Thao tác</label>
          <div class="flex items-center gap-2">
            <BaseButton variant="primary" size="sm" class="flex-1 h-[38px] !text-xs !py-0" @click="fetchResults">
              {{ $t('common.search') }}
            </BaseButton>
            <BaseButton variant="outline" size="sm" class="h-[38px] px-4 !text-xs !py-0" @click="resetFilters">
              {{ $t('common.reset') }}
            </BaseButton>
          </div>
        </div>
      </div>

      <!-- Facilities Pills Filter -->
      <div v-if="facilitiesList.length > 0" class="pt-2">
        <label class="block text-[11px] font-medium text-slate-500 dark:text-slate-400 mb-1.5">Lọc theo tiện ích:</label>
        <div class="flex flex-wrap gap-1.5">
          <button
            v-for="fac in facilitiesList"
            :key="fac.id"
            type="button"
            :class="[
              'px-2.5 py-1 rounded-full text-xs font-medium border transition-colors',
              selectedFacilityIds.includes(fac.id)
                ? 'bg-primary-600 text-white border-primary-600'
                : 'bg-slate-50 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700 hover:border-slate-300',
            ]"
            @click="toggleFacility(fac.id)"
          >
            {{ fac.name }}
          </button>
        </div>
      </div>
    </div>

    <!-- Header bar: Results count & Map/List switch -->
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-bold text-slate-900 dark:text-white">
        Kết quả tìm kiếm
        <span class="text-xs font-normal text-slate-500 dark:text-slate-400 ml-2">({{ total }} khu trọ)</span>
      </h1>

      <div class="flex items-center bg-slate-100 dark:bg-slate-800 p-1 rounded-xl">
        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition-all',
            viewMode === 'list' ? 'bg-white dark:bg-slate-900 text-slate-900 dark:text-white shadow-sm' : 'text-slate-500 hover:text-slate-900 dark:hover:text-white',
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
            viewMode === 'map' ? 'bg-white dark:bg-slate-900 text-slate-900 dark:text-white shadow-sm' : 'text-slate-500 hover:text-slate-900 dark:hover:text-white',
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

    <!-- MAP VIEW (Interactive Leaflet Bounding Box) -->
    <div v-if="viewMode === 'map'" class="h-[600px] rounded-2xl overflow-hidden border border-slate-200 dark:border-slate-800 shadow-sm">
      <ClientOnly>
        <MapView
          :markers="mapMarkers"
          :latitude="userLat || 21.0285"
          :longitude="userLon || 105.8542"
          :zoom="14"
          @bounds-changed="onMapBoundsChanged"
          @click-marker="(id) => navigateTo(`/boarding-houses/${id}`)"
        />
        <template #fallback>
          <div class="w-full h-full bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-xs text-slate-400">
            Đang tải bản đồ...
          </div>
        </template>
      </ClientOnly>
    </div>

    <!-- LIST VIEW -->
    <div v-if="viewMode === 'list'">
      <!-- Loading State -->
      <div v-if="isLoading" class="py-20 text-center">
        <LoadingSpinner size="lg" :text="$t('common.loading')" />
      </div>

      <!-- Empty State -->
      <div v-else-if="items.length === 0" class="py-16 text-center bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800">
        <svg class="w-12 h-12 text-slate-300 dark:text-slate-600 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
        </svg>
        <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ $t('common.noData') }}</p>
        <p class="text-xs text-slate-400 mt-1">Không tìm thấy khu trọ nào phù hợp với bộ lọc</p>
      </div>

      <!-- Cards Grid -->
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        <div
          v-for="house in items"
          :key="house.id"
          class="bg-white dark:bg-slate-900 rounded-2xl overflow-hidden border border-slate-200 dark:border-slate-800 shadow-sm hover:shadow-md transition-shadow group flex flex-col justify-between cursor-pointer"
          @click="navigateTo(`/boarding-houses/${house.id}`)"
        >
          <div>
            <!-- Image & Badges -->
            <div class="aspect-[16/10] bg-slate-100 dark:bg-slate-800 relative overflow-hidden flex items-center justify-center text-slate-400">
              <img
                v-if="house.primaryImageUrl"
                :src="house.primaryImageUrl"
                :alt="house.name"
                class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
              />
              <svg v-else class="w-10 h-10 text-slate-300 dark:text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>

              <!-- Property Type Badge -->
              <div class="absolute top-3 left-3 px-2 py-0.5 rounded-md bg-slate-900/70 backdrop-blur-sm text-white text-[10px] font-semibold">
                {{ $t(`enums.BoardingHouseType.${house.type}`) }}
              </div>

              <!-- Bookmark Toggle Button -->
              <button
                v-if="isAuthenticated"
                type="button"
                :class="[
                  'absolute top-3 right-3 p-2 rounded-full backdrop-blur-md shadow-sm transition-colors',
                  savedIds.has(house.id)
                    ? 'bg-red-500 text-white hover:bg-red-600'
                    : 'bg-white/80 dark:bg-slate-900/80 text-slate-600 dark:text-slate-300 hover:text-red-500',
                ]"
                title="Lưu tin yêu thích"
                @click.stop="toggleBookmark(house.id)"
              >
                <svg class="w-4 h-4 fill-current" viewBox="0 0 24 24">
                  <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
                </svg>
              </button>
            </div>

            <!-- Content -->
            <div class="p-5 space-y-2">
              <div class="flex items-center justify-between">
                <!-- Available rooms indicator -->
                <span
                  :class="[
                    'text-[11px] font-bold px-2 py-0.5 rounded-full',
                    house.availableRoomsCount > 0
                      ? 'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400'
                      : 'bg-slate-100 dark:bg-slate-800 text-slate-500',
                  ]"
                >
                  {{ house.availableRoomsCount > 0 ? `Còn ${house.availableRoomsCount} phòng trống` : 'Hết phòng' }}
                </span>

                <!-- Rating -->
                <div class="flex items-center gap-1 text-xs text-amber-500 font-semibold">
                  <span>★</span>
                  <span>{{ house.rating ? house.rating.toFixed(1) : '5.0' }}</span>
                  <span class="text-slate-400 font-normal">({{ house.reviewCount || 0 }})</span>
                </div>
              </div>

              <h3 class="text-sm font-bold text-slate-900 dark:text-white group-hover:text-primary-600 transition-colors line-clamp-1">
                {{ house.name }}
              </h3>

              <p class="text-xs text-slate-500 dark:text-slate-400 line-clamp-1 flex items-center gap-1">
                <svg class="w-3.5 h-3.5 flex-shrink-0 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                </svg>
                {{ house.addressLine }}, {{ house.district }}, {{ house.province }}
              </p>

              <!-- Facilities tags -->
              <div v-if="house.facilities && house.facilities.length > 0" class="flex flex-wrap gap-1 pt-1">
                <span
                  v-for="fac in house.facilities.slice(0, 3)"
                  :key="fac.id"
                  class="px-1.5 py-0.5 rounded text-[10px] bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400"
                >
                  {{ fac.name }}
                </span>
                <span v-if="house.facilities.length > 3" class="text-[10px] text-slate-400 self-center">
                  +{{ house.facilities.length - 3 }}
                </span>
              </div>
            </div>
          </div>

          <!-- Price & CTA -->
          <div class="px-5 pb-5 pt-2 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
            <div>
              <span class="text-[10px] text-slate-400 block">Giá từ</span>
              <span class="text-xs font-bold text-primary-600 dark:text-primary-400">
                {{ house.minPrice ? formatCurrency(house.minPrice) : 'Liên hệ' }}
                <span v-if="house.maxPrice && house.maxPrice !== house.minPrice" class="text-[11px] text-slate-400 font-normal">
                  - {{ formatCurrency(house.maxPrice) }}
                </span>
                /tháng
              </span>
            </div>

            <span class="text-xs font-semibold text-primary-600 dark:text-primary-400 group-hover:translate-x-0.5 transition-transform">
              {{ $t('common.view') }} →
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import MapView, { type MapMarker } from '~/components/common/MapView.client.vue'
import type {
  PublicBoardingHouseCardResponse,
  BoardingHouseMapMarkerResponse,
  ProvinceResponse,
  DistrictResponse,
  FacilityResponse,
  PagedResult,
  SavedListingResponse,
} from '~/types/api'

const route = useRoute()
const { get, post, delete: deleteApi } = useApi()
const { formatCurrency } = useFormat()
const { isAuthenticated } = useAuth()
const toast = useToast()

const viewMode = ref<'list' | 'map'>('list')
const isLoading = ref(false)
const items = ref<PublicBoardingHouseCardResponse[]>([])
const mapMarkers = ref<MapMarker[]>([])
const total = ref(0)

const provinces = ref<ProvinceResponse[]>([])
const districts = ref<DistrictResponse[]>([])
const facilitiesList = ref<FacilityResponse[]>([])
const selectedFacilityIds = ref<string[]>([])
const selectedPriceRange = ref('all')

const isNearbyActive = ref(false)
const isLocating = ref(false)
const userLat = ref<number | null>(null)
const userLon = ref<number | null>(null)
const savedIds = ref<Set<string>>(new Set())

const filters = reactive({
  q: (route.query.q as string) || '',
  province: (route.query.province as string) || '',
  district: (route.query.district as string) || '',
  type: (route.query.type as string) || '',
  minPrice: undefined as number | undefined,
  maxPrice: undefined as number | undefined,
  sort: (route.query.sort as string) || 'newest',
  page: 1,
  pageSize: 18,
})

const fetchLookups = async () => {
  try {
    const [pList, fList] = await Promise.all([
      get<ProvinceResponse[]>('/provinces').catch(() => []),
      get<FacilityResponse[]>('/facilities').catch(() => []),
    ])
    provinces.value = pList || []
    facilitiesList.value = fList || []
  } catch {
    // Ignore lookup errors
  }
}

const onProvinceChange = async () => {
  filters.district = ''
  if (!filters.province) {
    districts.value = []
  } else {
    const found = provinces.value.find((p) => p.name === filters.province || p.fullName === filters.province)
    if (found) {
      districts.value = (await get<DistrictResponse[]>(`/provinces/${found.code}/districts`)) || []
    }
  }
  fetchResults()
}

const onPriceRangeChange = () => {
  switch (selectedPriceRange.value) {
    case 'under_2m':
      filters.minPrice = undefined
      filters.maxPrice = 2000000
      break
    case '2m_4m':
      filters.minPrice = 2000000
      filters.maxPrice = 4000000
      break
    case '4m_7m':
      filters.minPrice = 4000000
      filters.maxPrice = 7000000
      break
    case 'above_7m':
      filters.minPrice = 7000000
      filters.maxPrice = undefined
      break
    default:
      filters.minPrice = undefined
      filters.maxPrice = undefined
  }
  fetchResults()
}

const toggleFacility = (id: string) => {
  const index = selectedFacilityIds.value.indexOf(id)
  if (index >= 0) {
    selectedFacilityIds.value.splice(index, 1)
  } else {
    selectedFacilityIds.value.push(id)
  }
  fetchResults()
}

const fetchResults = async () => {
  isLoading.value = true
  try {
    if (isNearbyActive.value && userLat.value && userLon.value) {
      const nearbyData = await get<PublicBoardingHouseCardResponse[]>('/boarding-houses/nearby', {
        lat: userLat.value,
        lon: userLon.value,
        radiusKm: 5,
        limit: 20,
      })
      items.value = nearbyData || []
      total.value = nearbyData?.length || 0
    } else {
      const data = await get<PagedResult<PublicBoardingHouseCardResponse>>('/boarding-houses', {
        q: filters.q || undefined,
        province: filters.province || undefined,
        district: filters.district || undefined,
        type: filters.type || undefined,
        minPrice: filters.minPrice,
        maxPrice: filters.maxPrice,
        facilities: selectedFacilityIds.value.length > 0 ? selectedFacilityIds.value : undefined,
        sort: filters.sort || undefined,
        page: filters.page,
        pageSize: filters.pageSize,
      })
      items.value = data?.items || []
      total.value = data?.total || 0
    }

    mapMarkers.value = items.value.map((h) => ({
      id: h.id,
      name: h.name,
      latitude: h.latitude,
      longitude: h.longitude,
      price: h.minPrice,
      address: `${h.addressLine}, ${h.district}`,
    }))
  } catch {
    items.value = []
    total.value = 0
  } finally {
    isLoading.value = false
  }
}

// Nearby geolocation
const toggleNearMe = () => {
  if (isNearbyActive.value) {
    isNearbyActive.value = false
    fetchResults()
    return
  }

  if (!navigator.geolocation) {
    toast.error('Trình duyệt không hỗ trợ định vị GPS.')
    return
  }

  isLocating.value = true
  navigator.geolocation.getCurrentPosition(
    (pos) => {
      userLat.value = pos.coords.latitude
      userLon.value = pos.coords.longitude
      isNearbyActive.value = true
      isLocating.value = false
      fetchResults()
      toast.success('Đã tìm thấy vị trí của bạn!')
    },
    () => {
      isLocating.value = false
      toast.error('Không thể truy cập vị trí hiện tại của bạn.')
    }
  )
}

// Bounding box map dynamic loader
const onMapBoundsChanged = async (bounds: { swLat: number; swLon: number; neLat: number; neLon: number }) => {
  try {
    const markers = await get<BoardingHouseMapMarkerResponse[]>('/boarding-houses/map', {
      swLat: bounds.swLat,
      swLon: bounds.swLon,
      neLat: bounds.neLat,
      neLon: bounds.neLon,
      limit: 100,
    })
    if (markers) {
      mapMarkers.value = markers.map((m) => ({
        id: m.id,
        name: m.name,
        latitude: m.latitude,
        longitude: m.longitude,
        price: m.minPrice,
        address: m.addressLine,
      }))
    }
  } catch {
    // Ignore map marker errors
  }
}

// Bookmarking
const fetchSavedListings = async () => {
  if (!isAuthenticated.value) return
  try {
    const data = await get<PagedResult<SavedListingResponse>>('/me/saved-listings', { page: 1, pageSize: 100 })
    if (data?.items) {
      savedIds.value = new Set(data.items.map((s) => s.boardingHouseId))
    }
  } catch {
    // Ignore
  }
}

const toggleBookmark = async (houseId: string) => {
  try {
    if (savedIds.value.has(houseId)) {
      await deleteApi(`/me/saved-listings/${houseId}`)
      savedIds.value.delete(houseId)
      toast.success('Đã bỏ lưu tin!')
    } else {
      await post('/me/saved-listings', { boardingHouseId: houseId })
      savedIds.value.add(houseId)
      toast.success('Đã lưu tin trọ vào mục Yêu thích!')
    }
  } catch (err: any) {
    toast.error(err.message || 'Không thể lưu tin.')
  }
}

const resetFilters = () => {
  filters.q = ''
  filters.province = ''
  filters.district = ''
  filters.type = ''
  filters.sort = 'newest'
  filters.minPrice = undefined
  filters.maxPrice = undefined
  selectedFacilityIds.value = []
  selectedPriceRange.value = 'all'
  isNearbyActive.value = false
  fetchResults()
}

onMounted(async () => {
  await fetchLookups()
  await fetchSavedListings()
  await fetchResults()
})
</script>
