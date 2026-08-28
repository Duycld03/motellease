<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <!-- Header -->
    <div>
      <NuxtLinkLocale :to="`/owner/properties/${houseId}`" class="inline-flex items-center text-xs font-medium text-slate-500 hover:text-primary-600 mb-3 transition-colors">
        {{ $t('ownerProperties.backToDetail') }}
      </NuxtLinkLocale>
      <h1 class="text-xl font-bold text-slate-900">{{ $t('ownerProperties.editTitle') }}</h1>
      <p class="text-xs text-slate-500 mt-1">{{ $t('ownerProperties.subtitle') }}</p>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="lg" :text="$t('common.loading')" />
    </div>

    <!-- Form Container -->
    <form v-else @submit.prevent="handleSubmit" class="space-y-6">
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

      <!-- Submit Footer -->
      <div class="flex items-center justify-end gap-3 pt-4">
        <NuxtLinkLocale :to="`/owner/properties/${houseId}`" class="btn-secondary !text-xs !py-2.5 !px-5">
          {{ $t('common.cancel') }}
        </NuxtLinkLocale>
        <BaseButton
          type="submit"
          variant="primary"
          size="md"
          :loading="isSubmitting"
          class="!py-2.5 !px-6"
        >
          {{ $t('ownerProperties.saveChanges') }}
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
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isLoading.value = false
  }
}

const handleSubmit = async () => {
  if (!form.name || !form.province || !form.district || !form.ward || !form.addressLine) {
    toast.warning(t('messages.actionFailed'))
    return
  }

  isSubmitting.value = true
  try {
    await put(`/my/boarding-houses/${houseId}`, form)
    toast.success(t('messages.updatePropertySuccess'))
    navigateTo(localePath(`/owner/properties/${houseId}`))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  fetchHouse()
})
</script>
