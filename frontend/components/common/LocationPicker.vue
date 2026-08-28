<template>
  <div class="space-y-4">
    <!-- Administrative Selects Grid -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <!-- Province -->
      <div>
        <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
          Tỉnh / Thành phố <span class="text-red-500">*</span>
        </label>
        <select
          :value="province"
          class="input-field !text-xs !py-2"
          @change="onProvinceChange"
        >
          <option value="" disabled selected>Chọn Tỉnh/Thành phố</option>
          <option v-for="p in provinces" :key="p.code" :value="p.name">
            {{ p.fullName || p.name }}
          </option>
        </select>
      </div>

      <!-- District -->
      <div>
        <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
          Quận / Huyện <span class="text-red-500">*</span>
        </label>
        <select
          :value="district"
          :disabled="!province || districts.length === 0"
          class="input-field !text-xs !py-2"
          @change="onDistrictChange"
        >
          <option value="" disabled selected>Chọn Quận/Huyện</option>
          <option v-for="d in districts" :key="d.code" :value="d.name">
            {{ d.fullName || d.name }}
          </option>
        </select>
      </div>

      <!-- Ward -->
      <div>
        <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
          Phường / Xã <span class="text-red-500">*</span>
        </label>
        <input
          :value="ward"
          type="text"
          class="input-field !text-xs !py-2"
          placeholder="VD: Phường Dịch Vọng Hậu"
          @input="$emit('update:ward', ($event.target as HTMLInputElement).value)"
        />
      </div>
    </div>

    <!-- Address Line -->
    <div>
      <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
        Địa chỉ chi tiết (Số nhà, tên đường, ngõ ngách) <span class="text-red-500">*</span>
      </label>
      <input
        :value="addressLine"
        type="text"
        class="input-field !text-xs !py-2"
        placeholder="VD: Số 18 Ngõ 86 Duy Tân"
        @input="$emit('update:addressLine', ($event.target as HTMLInputElement).value)"
      />
    </div>

    <!-- Map and Coordinates Picker -->
    <div class="space-y-2">
      <div class="flex items-center justify-between">
        <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
          Ghim tọa độ chính xác trên bản đồ (Latitude & Longitude)
        </label>
        <span class="text-[11px] text-slate-500 dark:text-slate-400">
          Tọa độ: {{ latitude.toFixed(5) }}, {{ longitude.toFixed(5) }}
        </span>
      </div>

      <div class="h-64 rounded-2xl overflow-hidden border border-slate-200 dark:border-slate-800 shadow-sm">
        <ClientOnly>
          <MapView
            :latitude="latitude || 21.0285"
            :longitude="longitude || 105.8542"
            :zoom="15"
            :selectable="true"
            @select-location="handleMapSelect"
          />
          <template #fallback>
            <div class="w-full h-full bg-slate-100 dark:bg-slate-900 flex items-center justify-center text-xs text-slate-400">
              Đang tải bản đồ...
            </div>
          </template>
        </ClientOnly>
      </div>

      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-[11px] text-slate-500 dark:text-slate-400 mb-0.5">Vĩ độ (Latitude)</label>
          <input
            :value="latitude"
            type="number"
            step="0.000001"
            class="input-field !text-xs !py-1.5"
            @input="$emit('update:latitude', parseFloat(($event.target as HTMLInputElement).value) || 0)"
          />
        </div>
        <div>
          <label class="block text-[11px] text-slate-500 dark:text-slate-400 mb-0.5">Kinh độ (Longitude)</label>
          <input
            :value="longitude"
            type="number"
            step="0.000001"
            class="input-field !text-xs !py-1.5"
            @input="$emit('update:longitude', parseFloat(($event.target as HTMLInputElement).value) || 0)"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import MapView from './MapView.client.vue'
import type { ProvinceResponse, DistrictResponse } from '~/types/api'

const props = withDefaults(
  defineProps<{
    province: string
    district: string
    ward: string
    addressLine: string
    latitude: number
    longitude: number
  }>(),
  {
    province: '',
    district: '',
    ward: '',
    addressLine: '',
    latitude: 21.0285,
    longitude: 105.8542,
  }
)

const emit = defineEmits<{
  (e: 'update:province', v: string): void
  (e: 'update:district', v: string): void
  (e: 'update:ward', v: string): void
  (e: 'update:addressLine', v: string): void
  (e: 'update:latitude', v: number): void
  (e: 'update:longitude', v: number): void
}>()

const { get } = useApi()

const provinces = ref<ProvinceResponse[]>([])
const districts = ref<DistrictResponse[]>([])

const fetchProvinces = async () => {
  try {
    provinces.value = await get<ProvinceResponse[]>('/provinces')
  } catch {
    provinces.value = []
  }
}

const fetchDistricts = async (provCode: string) => {
  try {
    districts.value = await get<DistrictResponse[]>(`/provinces/${provCode}/districts`)
  } catch {
    districts.value = []
  }
}

const onProvinceChange = (event: Event) => {
  const selectedName = (event.target as HTMLSelectElement).value
  emit('update:province', selectedName)
  emit('update:district', '')

  const found = provinces.value.find((p) => p.name === selectedName || p.fullName === selectedName)
  if (found) {
    fetchDistricts(found.code)
  } else {
    districts.value = []
  }
}

const onDistrictChange = (event: Event) => {
  const selectedName = (event.target as HTMLSelectElement).value
  emit('update:district', selectedName)
}

const handleMapSelect = (loc: { latitude: number; longitude: number }) => {
  emit('update:latitude', loc.latitude)
  emit('update:longitude', loc.longitude)
}

onMounted(async () => {
  await fetchProvinces()
  if (props.province) {
    const found = provinces.value.find((p) => p.name === props.province || p.fullName === props.province)
    if (found) {
      await fetchDistricts(found.code)
    }
  }
})
</script>
