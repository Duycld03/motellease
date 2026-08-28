<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <!-- Header -->
    <div>
      <NuxtLinkLocale to="/owner/properties" class="inline-flex items-center text-xs font-medium text-slate-500 hover:text-primary-600 mb-3 transition-colors">
        {{ $t('ownerProperties.backToList') }}
      </NuxtLinkLocale>
      <h1 class="text-xl font-bold text-slate-900">{{ $t('ownerProperties.createTitle') }}</h1>
      <p class="text-xs text-slate-500 mt-1">{{ $t('common.createPropertyHint') }}</p>
    </div>

    <!-- Form Container -->
    <form @submit.prevent="handleSubmit" class="space-y-6">
      <!-- Section 1: Basic Info -->
      <BaseCard :title="$t('ownerProperties.section1Basic')">
        <div class="space-y-4">
          <BaseInput
            v-model="form.name"
            :label="$t('ownerProperties.buildingName')"
            :placeholder="$t('ownerProperties.buildingNamePlaceholder')"
            required
          />

          <BaseSelect
            v-model="form.type"
            :label="$t('ownerProperties.houseType')"
            :options="typeOptions"
            required
          />

          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">
              {{ $t('ownerProperties.houseDesc') }}
            </label>
            <textarea
              v-model="form.description"
              rows="4"
              class="input-field !text-xs !py-2"
              :placeholder="$t('ownerProperties.houseDescPlaceholder')"
            />
          </div>
        </div>
      </BaseCard>

      <!-- Section 2: Location -->
      <BaseCard :title="$t('ownerProperties.section2Address')">
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
      <BaseCard :title="$t('ownerProperties.section3Utility')">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <BaseInput
            v-model.number="form.electricityUnitPrice"
            type="number"
            min="0"
            :label="$t('ownerProperties.elecUnitLabel')"
            placeholder="VD: 3500"
            required
          />

          <BaseInput
            v-model.number="form.waterUnitPrice"
            type="number"
            min="0"
            :label="$t('common.waterPriceUnitHint')"
            placeholder="VD: 30000"
            required
          />
        </div>
      </BaseCard>

      <!-- Submit Footer -->
      <div class="flex items-center justify-end gap-3 pt-4">
        <NuxtLinkLocale to="/owner/properties" class="btn-secondary !text-xs !py-2.5 !px-5">
          {{ $t('common.cancel') }}
        </NuxtLinkLocale>
        <BaseButton
          type="submit"
          variant="primary"
          size="md"
          :loading="isSubmitting"
          class="!py-2.5 !px-6"
        >
          {{ $t('common.createAndContinue') }}
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
const { t } = useI18n()
const localePath = useLocalePath()
const toast = useToast()

const typeOptions = computed(() => [
  { label: t('enums.BoardingHouseType.Traditional'), value: BoardingHouseType.Traditional },
  { label: t('enums.BoardingHouseType.MiniHouse'), value: BoardingHouseType.MiniHouse },
  { label: t('enums.BoardingHouseType.DormStyle'), value: BoardingHouseType.DormStyle },
])

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
    toast.warning(t('messages.actionFailed'))
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

    toast.success(t('messages.createPropertySuccess'))
    navigateTo(localePath(`/owner/properties/${created.id}`))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSubmitting.value = false
  }
}
</script>
