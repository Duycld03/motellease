<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
    <!-- Loading State -->
    <div v-if="isLoading" class="py-24 text-center">
      <LoadingSpinner size="lg" :text="$t('common.loading')" />
    </div>

    <!-- Not Found State -->
    <div v-else-if="!house" class="py-20 text-center bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800">
      <h3 class="text-base font-bold text-slate-800 dark:text-slate-200">{{ $t('common.houseNotFound') }}</h3>
      <p class="text-xs text-slate-500 mt-1">{{ $t('common.houseNotFoundHint') }}</p>
      <NuxtLinkLocale to="/search" class="mt-4 inline-block btn-primary !text-xs !py-2 !px-4">
        {{ $t('common.backToSearch') }}
      </NuxtLinkLocale>
    </div>

    <!-- Main Detail View -->
    <div v-else class="space-y-8">
      <!-- Breadcrumb & Title Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <NuxtLinkLocale to="/search" class="inline-flex items-center text-xs font-medium text-slate-500 hover:text-primary-600 mb-2 transition-colors">
            {{ $t('common.backToSearchResults') }}
          </NuxtLinkLocale>
          <div class="flex items-center gap-2 mb-1">
            <span class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-primary-50 dark:bg-primary-950/50 text-primary-700 dark:text-primary-400">
              {{ $t(`enums.BoardingHouseType.${house.type}`) }}
            </span>
            <span
              :class="[
                'text-xs font-semibold px-2.5 py-0.5 rounded-full',
                house.availableRoomsCount > 0
                  ? 'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-500',
              ]"
            >
              {{ house.availableRoomsCount > 0 ? $t('property.availableRoomsBadge', { count: house.availableRoomsCount }) : $t('property.outOfRooms') }}
            </span>
          </div>

          <h1 class="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
            {{ house.name }}
          </h1>

          <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 mt-1.5 flex items-center gap-1.5">
            <svg class="w-4 h-4 text-slate-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
            </svg>
            {{ house.addressLine }}, {{ house.ward }}, {{ house.district }}, {{ house.province }}
          </p>
        </div>

        <!-- Rating & Bookmark Action -->
        <div class="flex items-center gap-3">
          <div class="p-3 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm text-right">
            <div class="flex items-center justify-end gap-1 text-sm font-bold text-amber-500">
              <span>★</span>
              <span>{{ house.rating ? house.rating.toFixed(1) : '5.0' }}</span>
            </div>
            <span class="text-[11px] text-slate-400 block mt-0.5">{{ $t('common.reviewCountVerified', { count: house.reviewCount || 0 }) }}</span>
          </div>

          <button
            v-if="isAuthenticated"
            type="button"
            :class="[
              'p-3 rounded-2xl border transition-colors shadow-sm',
              isSaved
                ? 'bg-red-50 dark:bg-red-950/40 border-red-200 dark:border-red-800 text-red-600'
                : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:text-red-500',
            ]"
            :title="isSaved ? $t('common.unsaveListing') : $t('common.saveListing')"
            @click="toggleSaved"
          >
            <svg class="w-5 h-5 fill-current" viewBox="0 0 24 24">
              <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Image Gallery Hero -->
      <div class="space-y-3">
        <!-- Main Large Image -->
        <div class="aspect-[21/9] max-h-[480px] w-full rounded-2xl overflow-hidden bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-800 shadow-sm relative flex items-center justify-center text-slate-400">
          <img
            v-if="selectedImage || (house.images && house.images.length > 0)"
            :src="selectedImage || house.images[0].url"
            :alt="house.name"
            class="w-full h-full object-cover"
          />
          <svg v-else class="w-16 h-16 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        </div>

        <!-- Thumbnails row -->
        <div v-if="house.images && house.images.length > 1" class="flex gap-2 overflow-x-auto pb-1 scrollbar-none">
          <button
            v-for="img in house.images"
            :key="img.id"
            type="button"
            :class="[
              'relative w-20 h-14 rounded-xl overflow-hidden flex-shrink-0 border-2 transition-all',
              (selectedImage || house.images[0].url) === img.url
                ? 'border-primary-600 scale-95'
                : 'border-transparent opacity-70 hover:opacity-100',
            ]"
            @click="selectedImage = img.url"
          >
            <img :src="img.url" alt="Thumbnail" class="w-full h-full object-cover" />
          </button>
        </div>
      </div>

      <!-- Main 2-Column Content Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- Left 2 Columns -->
        <div class="lg:col-span-2 space-y-8">
          <!-- Description -->
          <BaseCard :title="$t('property.description')">
            <p class="text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed whitespace-pre-line">
              {{ house.description || $t('property.noDescription') }}
            </p>
          </BaseCard>

          <!-- Room Types & Features -->
          <div class="space-y-4">
            <h2 class="text-lg font-bold text-slate-900 dark:text-white">{{ $t('property.roomTypesAndPrices') }}</h2>

            <div v-if="!house.roomTypes || house.roomTypes.length === 0" class="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
              {{ $t('property.noRoomTypes') }}
            </div>

            <div v-else class="space-y-4">
              <div
                v-for="rt in house.roomTypes"
                :key="rt.id"
                class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-3"
              >
                <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                  <div>
                    <h3 class="text-sm font-bold text-slate-900 dark:text-white">{{ rt.typeName }}</h3>
                    <span class="text-xs text-slate-500 dark:text-slate-400">
                      📐 {{ rt.roomSizeM2 }} m² · 👥 {{ $t('property.maxOccupants', { n: rt.maxOccupants }) }} · {{ $t('property.availableRoomsBadge', { count: rt.availableRoomsCount }) }}
                    </span>
                  </div>
                  <div class="text-right">
                    <span class="text-base font-extrabold text-primary-600 dark:text-primary-400 block">
                      {{ formatCurrency(rt.price) }} <span class="text-xs text-slate-400 font-normal">{{ $t('property.perMonth') }}</span>
                    </span>
                  </div>
                </div>

                <p v-if="rt.description" class="text-xs text-slate-600 dark:text-slate-400 leading-relaxed">
                  {{ rt.description }}
                </p>

                <!-- Facilities -->
                <div v-if="rt.facilities && rt.facilities.length > 0" class="flex flex-wrap gap-1.5 pt-1">
                  <span
                    v-for="f in rt.facilities"
                    :key="f.id"
                    class="px-2 py-0.5 rounded-md text-[11px] bg-primary-50 dark:bg-primary-950/40 text-primary-700 dark:text-primary-300 font-medium"
                  >
                    ✓ {{ f.name }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Vacant Rooms List & Booking -->
          <div class="space-y-4">
            <div class="flex items-center justify-between">
              <div>
                <h2 class="text-lg font-bold text-slate-900 dark:text-white">{{ $t('common.vacantRoomsList') }}</h2>
                <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ $t('common.vacantRoomsSelectHint') }}</p>
              </div>
              <span class="text-xs font-bold text-emerald-600 dark:text-emerald-400">
                {{ $t('common.availableRoomsCountLabel', { count: vacantRooms.length }) }}
              </span>
            </div>

            <div v-if="vacantRooms.length === 0" class="p-8 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
              {{ $t('common.fullyBookedNotice') }}
            </div>

            <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div
                v-for="rm in vacantRooms"
                :key="rm.id"
                class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-3"
              >
                <div>
                  <div class="flex items-center justify-between">
                    <span class="text-sm font-bold text-slate-900 dark:text-white">{{ $t('property.room') }} {{ rm.roomNumber }}</span>
                    <span class="text-xs font-bold text-primary-600 dark:text-primary-400">{{ formatCurrency(rm.price) }}/th</span>
                  </div>
                  <span class="text-[11px] text-slate-500 block mt-0.5">{{ rm.roomTypeName }} ({{ rm.roomSizeM2 }} m²)</span>
                  <p v-if="rm.description" class="text-xs text-slate-500 mt-1 line-clamp-1">{{ rm.description }}</p>
                </div>

                <div class="pt-2 border-t border-slate-100 dark:border-slate-800 grid grid-cols-2 gap-2">
                  <BaseButton
                    variant="outline"
                    size="sm"
                    class="w-full !py-1.5 !text-xs"
                    @click="openAppointmentModal(rm)"
                  >
                    {{ $t('common.viewRoomBtn') }}
                  </BaseButton>
                  <BaseButton
                    variant="primary"
                    size="sm"
                    class="w-full !py-1.5 !text-xs"
                    @click="openDepositModal(rm)"
                  >
                    {{ $t('common.depositRoomBtn') }}
                  </BaseButton>
                </div>
              </div>
            </div>
          </div>

          <!-- Location Map -->
          <BaseCard :title="$t('property.mapLocation')">
            <div class="h-72 rounded-xl overflow-hidden border border-slate-200 dark:border-slate-800">
              <ClientOnly>
                <MapView
                  :latitude="house.latitude"
                  :longitude="house.longitude"
                  :zoom="16"
                  :markers="[{ id: house.id, name: house.name, latitude: house.latitude, longitude: house.longitude, address: house.addressLine }]"
                />
                <template #fallback>
                  <div class="w-full h-full bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-xs text-slate-400">
                    {{ $t('location.loadingMap') }}
                  </div>
                </template>
              </ClientOnly>
            </div>
          </BaseCard>

          <!-- Verified Reviews Section -->
          <div class="space-y-4">
            <div class="flex items-center justify-between">
              <h2 class="text-lg font-bold text-slate-900 dark:text-white">
                {{ $t('property.verifiedReviewsTitle') }}
                <span class="text-xs font-normal text-slate-500 ml-1">({{ reviews.length }})</span>
              </h2>
            </div>

            <div v-if="reviews.length === 0" class="p-8 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
              {{ $t('property.noReviews') }}
            </div>

            <div v-else class="space-y-4">
              <div
                v-for="rev in reviews"
                :key="rev.id"
                class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-3"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="flex items-center gap-3">
                    <div class="w-9 h-9 rounded-full bg-primary-100 dark:bg-primary-950 text-primary-700 dark:text-primary-300 font-bold text-xs flex items-center justify-center">
                      {{ rev.userFullName ? rev.userFullName.charAt(0).toUpperCase() : 'U' }}
                    </div>
                    <div>
                      <div class="flex items-center gap-2">
                        <span class="text-xs font-bold text-slate-900 dark:text-white">{{ rev.userFullName }}</span>
                        <span v-if="rev.isVerified" class="px-1.5 py-0.2 rounded text-[10px] bg-emerald-50 text-emerald-700 font-semibold">
                          ✓ {{ $t('property.verifiedTenant') }}
                        </span>
                      </div>
                      <span class="text-[10px] text-slate-400">{{ formatRelativeTime(rev.createdAt) }}</span>
                    </div>
                  </div>

                  <!-- Stars -->
                  <div class="flex items-center text-amber-400 text-xs">
                    <span v-for="star in 5" :key="star">{{ star <= rev.rating ? '★' : '☆' }}</span>
                  </div>
                </div>

                <p class="text-xs text-slate-700 dark:text-slate-300 leading-relaxed">
                  {{ rev.content }}
                </p>

                <!-- Host Replies -->
                <div v-if="rev.replies && rev.replies.length > 0" class="mt-3 pl-4 border-l-2 border-primary-200 dark:border-primary-800 space-y-2">
                  <div v-for="rep in rev.replies" :key="rep.id" class="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl text-xs space-y-1">
                    <div class="flex items-center gap-2">
                      <span class="font-bold text-slate-800 dark:text-slate-200">{{ $t('property.hostReply') }}</span>
                      <span class="text-[10px] text-slate-400">· {{ formatRelativeTime(rep.createdAt) }}</span>
                    </div>
                    <p class="text-slate-600 dark:text-slate-400 leading-relaxed">{{ rep.content }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right 1 Column (Host info, Utilities, Quick Booking Box) -->
        <div class="space-y-6">
          <!-- Host Contact Card -->
          <BaseCard :title="$t('property.hostInfo')">
            <div class="space-y-4">
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 rounded-2xl bg-primary-100 dark:bg-primary-950 text-primary-700 dark:text-primary-300 flex items-center justify-center font-bold text-base shadow-sm">
                  {{ house.owner?.fullName ? house.owner.fullName.charAt(0).toUpperCase() : 'H' }}
                </div>
                <div>
                  <h4 class="text-sm font-bold text-slate-900 dark:text-white">{{ house.owner?.fullName || $t('roles.Owner') }}</h4>
                  <span class="text-xs text-slate-400 block mt-0.5">{{ $t('property.ownerAndManager') }}</span>
                </div>
              </div>

              <div v-if="house.owner?.phoneNumber" class="p-3 bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-100 dark:border-slate-700 flex items-center justify-between">
                <span class="text-xs text-slate-500">{{ $t('property.contactPhone') }}</span>
                <a :href="`tel:${house.owner.phoneNumber}`" class="text-xs font-bold text-primary-600 dark:text-primary-400 hover:underline">
                  {{ house.owner.phoneNumber }}
                </a>
              </div>
            </div>
          </BaseCard>

          <!-- Utilities Rate Card -->
          <BaseCard :title="$t('property.utilityPricing')">
            <div class="space-y-3">
              <div class="p-3 bg-slate-50 dark:bg-slate-800 rounded-xl flex items-center justify-between">
                <span class="text-xs text-slate-600 dark:text-slate-400">⚡ {{ $t('property.electricity') }}</span>
                <span class="text-xs font-bold text-slate-900 dark:text-white">{{ formatCurrency(house.electricityUnitPrice) }} / kWh</span>
              </div>
              <div class="p-3 bg-slate-50 dark:bg-slate-800 rounded-xl flex items-center justify-between">
                <span class="text-xs text-slate-600 dark:text-slate-400">💧 {{ $t('property.water') }}</span>
                <span class="text-xs font-bold text-slate-900 dark:text-white">{{ formatCurrency(house.waterUnitPrice) }} / m³</span>
              </div>
            </div>
          </BaseCard>
        </div>
      </div>
    </div>

    <!-- MODAL: Book Viewing Appointment -->
    <BaseModal
      v-model="isAppointmentModalOpen"
      :title="$t('property.bookAppointmentModalTitle', { room: selectedRoom?.roomNumber || '' })"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitAppointment" class="space-y-4">
        <div v-if="selectedRoom" class="p-3.5 bg-primary-50 dark:bg-primary-950/40 rounded-xl text-xs space-y-1">
          <div class="flex items-center justify-between font-bold text-primary-900 dark:text-primary-200">
            <span>{{ $t('property.room') }} {{ selectedRoom.roomNumber }} ({{ selectedRoom.roomTypeName }})</span>
            <span>{{ formatCurrency(selectedRoom.price) }}/{{ $t('common.unitMonth') }}</span>
          </div>
          <p class="text-primary-700 dark:text-primary-400 text-[11px]">{{ $t('property.house') }}: {{ house?.name }}</p>
        </div>

        <BaseDatePicker
          v-model="appointmentForm.appointmentDate"
          :label="$t('property.appointmentDateTime')"
          enable-time
          required
          :min="todayStr"
        />

        <div>
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('property.noteToHostOptional') }}
          </label>
          <textarea
            v-model="appointmentForm.note"
            rows="3"
            class="input-field !text-xs !py-2"
            :placeholder="$t('property.appointmentNotePlaceholder')"
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isAppointmentModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isBookingAppointment">
            {{ $t('property.sendAppointment') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>

    <!-- MODAL: Request Deposit ({{ $t('property.depositRoom') }}) -->
    <BaseModal
      v-model="isDepositModalOpen"
      :title="$t('property.depositModalTitle', { room: selectedRoom?.roomNumber || '' })"
      max-width="lg"
    >
      <form @submit.prevent="handleSubmitDeposit" class="space-y-4">
        <div v-if="selectedRoom" class="p-4 bg-primary-50 dark:bg-primary-950/40 rounded-xl border border-primary-200 dark:border-primary-800 text-xs space-y-2">
          <div class="flex items-center justify-between font-bold text-primary-900 dark:text-primary-200">
            <span>{{ $t('property.room') }} {{ selectedRoom.roomNumber }} - {{ selectedRoom.roomTypeName }}</span>
            <span class="text-sm text-primary-700 dark:text-primary-300">{{ formatCurrency(selectedRoom.price) }}/{{ $t('common.unitMonth') }}</span>
          </div>
          <div class="flex items-center justify-between text-slate-600 dark:text-slate-400 text-[11px] pt-1 border-t border-primary-200/60 dark:border-primary-800/60">
            <span>{{ $t('property.holdingDepositNote') }}</span>
            <span class="font-bold text-slate-900 dark:text-white">{{ formatCurrency(selectedRoom.price) }}</span>
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <BaseDatePicker
            v-model="depositForm.requestedStartDate"
            :label="$t('property.expectedMoveInDate')"
            required
            :min="todayStr"
          />

          <BaseSelect
            v-model="depositForm.requestedTermMonths"
            :options="termOptions"
            :label="$t('property.desiredContractTerm')"
            required
          />
        </div>

        <div class="p-3 bg-amber-50 dark:bg-amber-950/30 rounded-xl border border-amber-200 dark:border-amber-800 text-[11px] text-amber-800 dark:text-amber-300 space-y-1">
          <p class="font-bold">{{ $t('property.safeDepositProcessTitle') }}</p>
          <ul class="list-disc list-inside space-y-0.5 text-amber-700 dark:text-amber-400">
            <li>{{ $t('property.safeDepositStep1') }}</li>
            <li>{{ $t('property.safeDepositStep2') }}</li>
            <li>{{ $t('property.safeDepositStep3') }}</li>
          </ul>
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isDepositModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmittingDeposit">
            {{ $t('property.submitDepositRequest') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'
import BaseDatePicker from '~/components/common/BaseDatePicker.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import BaseSelect from '~/components/common/BaseSelect.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import MapView from '~/components/common/MapView.client.vue'
import type {
  PublicBoardingHouseDetailResponse,
  PublicVacantRoomResponse,
  PublicReviewResponse,
  PagedResult,
  SavedListingResponse,
} from '~/types/api'

const route = useRoute()
const houseId = route.params.id as string

const { get, post, delete: deleteApi } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const { isAuthenticated, isTenant } = useAuth()
const { t } = useI18n()
const localePath = useLocalePath()
const toast = useToast()

const isLoading = ref(true)
const house = ref<PublicBoardingHouseDetailResponse | null>(null)
const vacantRooms = ref<PublicVacantRoomResponse[]>([])
const reviews = ref<PublicReviewResponse[]>([])
const selectedImage = ref<string | null>(null)
const isSaved = ref(false)

// Fetch detail, vacant rooms, and reviews
const fetchData = async () => {
  isLoading.value = true
  try {
    const [hData, rmData, revData] = await Promise.all([
      get<PublicBoardingHouseDetailResponse>(`/boarding-houses/${houseId}`),
      get<PublicVacantRoomResponse[]>(`/boarding-houses/${houseId}/rooms`).catch(() => []),
      get<PagedResult<PublicReviewResponse>>(`/boarding-houses/${houseId}/reviews`).catch(() => null),
    ])
    house.value = hData
    vacantRooms.value = rmData || []
    reviews.value = revData?.items || []
  } catch {
    house.value = null
  } finally {
    isLoading.value = false
  }
}

// Check saved status
const checkSavedStatus = async () => {
  if (!isAuthenticated.value) return
  try {
    const data = await get<PagedResult<SavedListingResponse>>('/me/saved-listings', { page: 1, pageSize: 100 })
    if (data?.items) {
      isSaved.value = data.items.some((s) => s.boardingHouseId === houseId)
    }
  } catch {
    // Ignore
  }
}

const toggleSaved = async () => {
  try {
    if (isSaved.value) {
      await deleteApi(`/me/saved-listings/${houseId}`)
      isSaved.value = false
      toast.success(t('messages.unsaveListingSuccess'))
    } else {
      await post('/me/saved-listings', { boardingHouseId: houseId })
      isSaved.value = true
      toast.success(t('messages.saveListingSuccess'))
    }
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

// Appointment Booking
const isAppointmentModalOpen = ref(false)
const selectedRoom = ref<PublicVacantRoomResponse | null>(null)
const isBookingAppointment = ref(false)

const appointmentForm = reactive({
  appointmentDate: '',
  note: '',
})

const openAppointmentModal = (rm: PublicVacantRoomResponse) => {
  if (!isAuthenticated.value) {
    toast.info(t('messages.actionFailed'))
    navigateTo(localePath('/auth/login'))
    return
  }
  if (!isTenant.value) {
    toast.warning(t('messages.actionFailed'))
    return
  }

  selectedRoom.value = rm
  // Pre-fill tomorrow 9:00 AM
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  tomorrow.setHours(9, 0, 0, 0)
  appointmentForm.appointmentDate = tomorrow.toISOString().slice(0, 16)
  appointmentForm.note = ''
  isAppointmentModalOpen.value = true
}

const handleSubmitAppointment = async () => {
  if (!selectedRoom.value || !appointmentForm.appointmentDate) return
  isBookingAppointment.value = true
  try {
    await post('/appointments', {
      roomId: selectedRoom.value.id,
      appointmentDate: new Date(appointmentForm.appointmentDate).toISOString(),
      note: appointmentForm.note || undefined,
    })
    toast.success(t('messages.createAppointmentSuccess'))
    isAppointmentModalOpen.value = false
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isBookingAppointment.value = false
  }
}

// Deposit Booking (Đặt cọc giữ phòng)
const isDepositModalOpen = ref(false)
const isSubmittingDeposit = ref(false)

const todayStr = computed(() => {
  const d = new Date()
  return d.toISOString().slice(0, 10)
})

const termOptions = computed(() => [
  { label: `3 ${t('common.unitMonth')}`, value: 3 },
  { label: `6 ${t('common.unitMonth')}`, value: 6 },
  { label: `12 ${t('common.unitMonth')}`, value: 12 },
  { label: `24 ${t('common.unitMonth')}`, value: 24 },
])

const depositForm = reactive({
  requestedStartDate: '',
  requestedTermMonths: 6,
})

const openDepositModal = (rm: PublicVacantRoomResponse) => {
  if (!isAuthenticated.value) {
    toast.info(t('messages.actionFailed'))
    navigateTo(localePath('/auth/login'))
    return
  }
  if (!isTenant.value) {
    toast.warning(t('messages.actionFailed'))
    return
  }

  selectedRoom.value = rm
  // Pre-fill tomorrow as default start date
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  depositForm.requestedStartDate = tomorrow.toISOString().slice(0, 10)
  depositForm.requestedTermMonths = 6
  isDepositModalOpen.value = true
}

const handleSubmitDeposit = async () => {
  if (!selectedRoom.value || !depositForm.requestedStartDate) return
  isSubmittingDeposit.value = true
  try {
    await post('/deposits', {
      roomId: selectedRoom.value.id,
      requestedStartDate: depositForm.requestedStartDate,
      requestedTermMonths: Number(depositForm.requestedTermMonths),
    })
    toast.success(t('messages.createDepositSuccess'))
    isDepositModalOpen.value = false
    navigateTo(localePath('/tenant/deposits'))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSubmittingDeposit.value = false
  }
}

onMounted(async () => {
  await fetchData()
  await checkSavedStatus()
})
</script>
