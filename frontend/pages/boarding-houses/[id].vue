<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <div v-if="isLoading" class="py-20 text-center">
      <LoadingSpinner size="lg" :text="$t('common.loading')" />
    </div>

    <div v-else-if="!house" class="py-16 text-center bg-white rounded-2xl border border-slate-200">
      <p class="text-sm font-semibold text-slate-700">Không tìm thấy thông tin nhà trọ</p>
      <NuxtLink to="/search" class="mt-3 inline-block text-xs font-semibold text-primary-600">
        ← {{ $t('common.back') }}
      </NuxtLink>
    </div>

    <div v-else class="space-y-8">
      <!-- Breadcrumb & Header -->
      <div>
        <NuxtLink to="/search" class="inline-flex items-center text-xs font-medium text-slate-500 hover:text-primary-600 mb-3 transition-colors">
          ← {{ $t('common.back') }}
        </NuxtLink>
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <div class="flex items-center gap-2 mb-1.5">
              <StatusBadge type="ListingStatus" :status="house.status" />
              <span class="text-xs font-semibold text-slate-500">{{ house.type }}</span>
            </div>
            <h1 class="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
              {{ house.name }}
            </h1>
            <p class="text-xs text-slate-500 mt-1 flex items-center gap-1">
              <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {{ house.address }}, {{ house.district }}, {{ house.province }}
            </p>
          </div>

          <div class="flex items-center gap-3">
            <BaseButton variant="outline" size="md" @click="handleBookViewing">
              {{ $t('property.bookAppointment') }}
            </BaseButton>
            <BaseButton variant="primary" size="md" @click="handleDeposit">
              {{ $t('property.depositRoom') }}
            </BaseButton>
          </div>
        </div>
      </div>

      <!-- Image Gallery -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 rounded-2xl overflow-hidden">
        <div class="md:col-span-2 aspect-[16/10] bg-slate-200 relative">
          <img
            v-if="house.images && house.images.length > 0"
            :src="house.images[0].url"
            :alt="house.name"
            class="w-full h-full object-cover"
          />
          <div v-else class="w-full h-full flex items-center justify-center text-slate-400">
            Hình ảnh khu trọ
          </div>
        </div>
        <div class="hidden md:grid grid-rows-2 gap-4">
          <div class="bg-slate-100 rounded-xl overflow-hidden flex items-center justify-center text-slate-400 text-xs">
            <img
              v-if="house.images && house.images.length > 1"
              :src="house.images[1].url"
              class="w-full h-full object-cover"
            />
            <span v-else>Ảnh 2</span>
          </div>
          <div class="bg-slate-100 rounded-xl overflow-hidden flex items-center justify-center text-slate-400 text-xs">
            <img
              v-if="house.images && house.images.length > 2"
              :src="house.images[2].url"
              class="w-full h-full object-cover"
            />
            <span v-else>Ảnh 3</span>
          </div>
        </div>
      </div>

      <!-- Main info grid -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div class="lg:col-span-2 space-y-8">
          <!-- Description -->
          <BaseCard :title="$t('property.details')">
            <p class="text-sm text-slate-600 leading-relaxed whitespace-pre-line">
              {{ house.description || 'Chưa có mô tả chi tiết.' }}
            </p>
          </BaseCard>

          <!-- Utilities & Prices -->
          <BaseCard title="Bảng giá dịch vụ">
            <div class="grid grid-cols-2 sm:grid-cols-3 gap-4">
              <div class="p-3.5 bg-slate-50 rounded-xl border border-slate-100">
                <span class="text-xs text-slate-400 block">{{ $t('property.electricity') }}</span>
                <span class="text-sm font-bold text-slate-900 mt-1 block">
                  {{ formatCurrency(house.electricityPrice) }}/kWh
                </span>
              </div>
              <div class="p-3.5 bg-slate-50 rounded-xl border border-slate-100">
                <span class="text-xs text-slate-400 block">{{ $t('property.water') }}</span>
                <span class="text-sm font-bold text-slate-900 mt-1 block">
                  {{ formatCurrency(house.waterPrice) }}/m³
                </span>
              </div>
            </div>
          </BaseCard>

          <!-- Facilities -->
          <BaseCard v-if="house.facilities && house.facilities.length > 0" :title="$t('property.facilities')">
            <div class="flex flex-wrap gap-2">
              <span
                v-for="fac in house.facilities"
                :key="fac.id"
                class="px-3 py-1.5 rounded-lg bg-slate-100 text-slate-700 text-xs font-medium"
              >
                {{ fac.name }}
              </span>
            </div>
          </BaseCard>
        </div>

        <!-- Right Card (Quick booking / host info) -->
        <div>
          <BaseCard title="Thông tin chủ trọ & Hỗ trợ">
            <div class="space-y-4">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-full bg-primary-100 text-primary-700 flex items-center justify-center font-bold text-sm">
                  {{ house.ownerName ? house.ownerName.charAt(0) : 'O' }}
                </div>
                <div>
                  <h4 class="text-xs font-bold text-slate-900">{{ house.ownerName || 'Chủ trọ MotelLease' }}</h4>
                  <span class="text-[10px] text-slate-400">Đã xác minh thông tin</span>
                </div>
              </div>

              <div class="pt-4 border-t border-slate-100 space-y-2">
                <BaseButton variant="primary" size="md" full-width @click="handleDeposit">
                  {{ $t('property.depositRoom') }}
                </BaseButton>
                <BaseButton variant="outline" size="md" full-width @click="handleBookViewing">
                  {{ $t('property.bookAppointment') }}
                </BaseButton>
              </div>
            </div>
          </BaseCard>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
import type { BoardingHouse } from '~/types/api'

const route = useRoute()
const { get } = useApi()
const { formatCurrency } = useFormat()
const toast = useToast()

const houseId = route.params.id as string
const house = ref<BoardingHouse | null>(null)
const isLoading = ref(true)

const fetchHouse = async () => {
  isLoading.value = true
  try {
    house.value = await get<BoardingHouse>(`/boarding-houses/${houseId}`)
  } catch {
    house.value = null
  } finally {
    isLoading.value = false
  }
}

const handleBookViewing = () => {
  toast.info('Tính năng đặt lịch hẹn xem phòng trực tuyến')
}

const handleDeposit = () => {
  toast.info('Tính năng đặt cọc phòng trực tuyến qua MoMo / VNPay')
}

onMounted(() => {
  fetchHouse()
})
</script>
