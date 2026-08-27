<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <!-- Header -->
    <div>
      <NuxtLink to="/owner/properties" class="inline-flex items-center text-xs font-medium text-slate-500 hover:text-primary-600 mb-3 transition-colors">
        ← Quay lại danh sách khu trọ
      </NuxtLink>
      <h1 class="text-xl font-bold text-slate-900">Thêm khu trọ mới</h1>
      <p class="text-xs text-slate-500 mt-1">Điền thông tin chi tiết khu trọ, ghim vị trí bản đồ và bảng giá điện nước</p>
    </div>

    <!-- Form Container -->
    <form @submit.prevent="handleSubmit" class="space-y-6">
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

      <!-- Section 3: Utility Prices -->
      <BaseCard title="3. Bảng giá dịch vụ điện nước cơ bản">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <BaseInput
            v-model.number="form.electricityUnitPrice"
            type="number"
            min="0"
            label="Đơn giá điện (VNĐ / kWh)"
            placeholder="VD: 3500"
            required
          />

          <BaseInput
            v-model.number="form.waterUnitPrice"
            type="number"
            min="0"
            label="Đơn giá nước (VNĐ / m³ hoặc người)"
            placeholder="VD: 30000"
            required
          />
        </div>
      </BaseCard>

      <!-- Submit Footer -->
      <div class="flex items-center justify-end gap-3 pt-4">
        <NuxtLink to="/owner/properties" class="btn-secondary !text-xs !py-2.5 !px-5">
          {{ $t('common.cancel') }}
        </NuxtLink>
        <BaseButton
          type="submit"
          variant="primary"
          size="md"
          :loading="isSubmitting"
          class="!py-2.5 !px-6"
        >
          Tạo khu trọ & Tiếp tục
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
import LocationPicker from '~/components/common/LocationPicker.vue'
import { BoardingHouseType } from '~/types/enums'
import type { BoardingHouseDetailResponse } from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { post, put } = useApi()
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
  electricityUnitPrice: 3500,
  waterUnitPrice: 30000,
})

const isSubmitting = ref(false)

const handleSubmit = async () => {
  if (!form.name || !form.province || !form.district || !form.ward || !form.addressLine) {
    toast.warning('Vui lòng điền đầy đủ các trường thông tin bắt buộc (*).')
    return
  }

  isSubmitting.value = true
  try {
    const created = await post<BoardingHouseDetailResponse>('/my/boarding-houses', {
      name: form.name,
      description: form.description || undefined,
      type: form.type,
      province: form.province,
      district: form.district,
      ward: form.ward,
      addressLine: form.addressLine,
      latitude: form.latitude,
      longitude: form.longitude,
    })

    // Set utility prices
    if (form.electricityUnitPrice > 0 || form.waterUnitPrice > 0) {
      await put(`/my/boarding-houses/${created.id}/utility-prices`, {
        electricityUnitPrice: form.electricityUnitPrice,
        waterUnitPrice: form.waterUnitPrice,
      })
    }

    toast.success('Tạo khu trọ thành công!')
    navigateTo(`/owner/properties/${created.id}`)
  } catch (err: any) {
    toast.error(err.message || 'Tạo khu trọ thất bại. Vui lòng thử lại.')
  } finally {
    isSubmitting.value = false
  }
}
</script>
