<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <!-- Header -->
    <div>
      <NuxtLink :to="`/owner/properties/${houseId}`" class="inline-flex items-center text-xs font-medium text-slate-500 hover:text-primary-600 mb-3 transition-colors">
        ← Quay lại chi tiết khu trọ
      </NuxtLink>
      <h1 class="text-xl font-bold text-slate-900">Chỉnh sửa thông tin khu trọ</h1>
      <p class="text-xs text-slate-500 mt-1">Cập nhật tên, mô tả, loại hình và vị trí ghim bản đồ của khu trọ</p>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="lg" :text="$t('common.loading')" />
    </div>

    <!-- Form Container -->
    <form v-else @submit.prevent="handleSubmit" class="space-y-6">
      <!-- Section 1: Basic Info -->
      <BaseCard title="1. Thông tin cơ bản">
        <div class="space-y-4">
          <BaseInput
            v-model="form.name"
            label="Tên khu trọ / Tòa nhà"
            placeholder="VD: Nhà trọ MotelLease An Phú"
            required
          />

          <BaseSelect
            v-model="form.type"
            label="Mô hình nhà trọ"
            :options="typeOptions"
            required
          />

          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">
              Mô tả chi tiết khu trọ
            </label>
            <textarea
              v-model="form.description"
              rows="4"
              class="input-field !text-xs !py-2"
              placeholder="Giới thiệu về an ninh, giờ giấc tự do, chỗ để xe, môi trường xung quanh..."
            />
          </div>
        </div>
      </BaseCard>

      <!-- Section 2: Location -->
      <BaseCard title="2. Địa chỉ & Vị trí bản đồ">
        <LocationPicker
          v-model:province="form.province"
          v-model:district="form.district"
          v-model:ward="form.ward"
          v-model:address-line="form.addressLine"
          v-model:latitude="form.latitude"
          v-model:longitude="form.longitude"
        />
      </BaseCard>

      <!-- Submit Footer -->
      <div class="flex items-center justify-end gap-3 pt-4">
        <NuxtLink :to="`/owner/properties/${houseId}`" class="btn-secondary !text-xs !py-2.5 !px-5">
          {{ $t('common.cancel') }}
        </NuxtLink>
        <BaseButton
          type="submit"
          variant="primary"
          size="md"
          :loading="isSubmitting"
          class="!py-2.5 !px-6"
        >
          Lưu thay đổi
        </BaseButton>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import BaseSelect from '~/components/common/BaseSelect.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import LocationPicker from '~/components/common/LocationPicker.vue'
import { BoardingHouseType } from '~/types/enums'
import type { BoardingHouseDetailResponse } from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const route = useRoute()
const houseId = route.params.id as string

const { get, put } = useApi()
const toast = useToast()

const typeOptions = [
  { label: 'Phòng trọ truyền thống (Traditional)', value: BoardingHouseType.Traditional },
  { label: 'Căn hộ mini / Chung cư mini (MiniHouse)', value: BoardingHouseType.MiniHouse },
  { label: 'Ký túc xá / Sleepbox (DormStyle)', value: BoardingHouseType.DormStyle },
]

const form = reactive({
  name: '',
  description: '',
  type: BoardingHouseType.Traditional,
  province: '',
  district: '',
  ward: '',
  addressLine: '',
  latitude: 21.0285,
  longitude: 105.8542,
})

const isLoading = ref(true)
const isSubmitting = ref(false)

const fetchHouse = async () => {
  isLoading.value = true
  try {
    const data = await get<BoardingHouseDetailResponse>(`/my/boarding-houses/${houseId}`)
    form.name = data.name
    form.description = data.description || ''
    form.type = data.type
    form.province = data.province
    form.district = data.district
    form.ward = data.ward
    form.addressLine = data.addressLine
    form.latitude = data.latitude
    form.longitude = data.longitude
  } catch (err: any) {
    toast.error('Không thể tải thông tin khu trọ.')
  } finally {
    isLoading.value = false
  }
}

const handleSubmit = async () => {
  if (!form.name || !form.province || !form.district || !form.ward || !form.addressLine) {
    toast.warning('Vui lòng điền đầy đủ các trường thông tin bắt buộc (*).')
    return
  }

  isSubmitting.value = true
  try {
    await put(`/my/boarding-houses/${houseId}`, form)
    toast.success('Cập nhật khu trọ thành công!')
    navigateTo(`/owner/properties/${houseId}`)
  } catch (err: any) {
    toast.error(err.message || 'Cập nhật khu trọ thất bại.')
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  fetchHouse()
})
</script>
