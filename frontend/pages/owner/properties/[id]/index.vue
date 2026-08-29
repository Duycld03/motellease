<template>
  <div class="space-y-6">
    <!-- Top Header -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="lg" :text="$t('common.loading')" />
    </div>

    <div v-else-if="!house" class="py-16 text-center bg-white rounded-2xl border border-slate-200">
      <p class="text-sm font-semibold text-slate-700">{{ $t('common.houseNotFound') }}</p>
      <NuxtLinkLocale to="/owner/properties" class="mt-3 inline-block text-xs font-semibold text-primary-600">
        {{ $t('ownerProperties.backToList') }}
      </NuxtLinkLocale>
    </div>

    <div v-else class="space-y-6">
      <!-- Breadcrumb & Header Title -->
      <div>
        <NuxtLinkLocale to="/owner/properties" class="inline-flex items-center text-xs font-medium text-slate-500 hover:text-primary-600 mb-3 transition-colors">
          {{ $t('ownerProperties.backToList') }}
        </NuxtLinkLocale>
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <div class="flex items-center gap-2 mb-1.5">
              <StatusBadge type="ListingStatus" :status="house.listingStatus" />
              <span class="text-xs font-semibold text-slate-500">{{ $t(`enums.BoardingHouseType.${house.type}`) }}</span>
            </div>
            <h1 class="text-2xl font-extrabold text-slate-900 tracking-tight">
              {{ house.name }}
            </h1>
            <p class="text-xs text-slate-500 mt-1 flex items-center gap-1">
              <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {{ house.addressLine }}, {{ house.ward }}, {{ house.district }}, {{ house.province }}
            </p>
          </div>

          <div class="flex items-center gap-3">
            <BaseButton
              v-if="house.listingStatus === 'Draft' || house.listingStatus === 'Rejected'"
              variant="primary"
              size="md"
              :loading="isSubmittingReview"
              @click="handleSubmitReview"
            >
              {{ $t('ownerProperties.submitReview') }}
            </BaseButton>

            <NuxtLinkLocale :to="`/owner/properties/${houseId}/edit`" class="btn-secondary !text-xs !py-2 !px-4">
              {{ $t('common.edit') }}
            </NuxtLinkLocale>
          </div>
        </div>
      </div>

      <!-- Rejection Warning Banner -->
      <div
        v-if="house.listingStatus === 'Rejected' && house.rejectionReason"
        class="p-4 bg-red-50 dark:bg-red-950/40 rounded-2xl border border-red-200 dark:border-red-800 flex items-start gap-3"
      >
        <div class="text-red-600 mt-0.5 font-bold">⚠️</div>
        <div>
          <h4 class="text-xs font-bold text-red-900 dark:text-red-300">{{ $t('common.rejectedListingBanner') }}</h4>
          <p class="text-xs text-red-700 dark:text-red-400 mt-0.5">{{ house.rejectionReason }}</p>
        </div>
      </div>

      <!-- Navigation Tabs -->
      <div class="border-b border-slate-200 dark:border-slate-800">
        <nav class="flex space-x-6">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            type="button"
            :class="[
              'py-3 text-xs font-semibold border-b-2 transition-colors -mb-px flex items-center gap-1.5',
              activeTab === tab.id
                ? 'border-primary-600 text-primary-600 dark:text-primary-400'
                : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 hover:border-slate-300 dark:hover:border-slate-700',
            ]"
            @click="activeTab = tab.id"
          >
            <span>{{ tab.label }}</span>
            <span
              v-if="tab.count !== undefined"
              :class="[
                'px-1.5 py-0.5 rounded-full text-[10px]',
                activeTab === tab.id ? 'bg-primary-100 dark:bg-primary-950 text-primary-800 dark:text-primary-300' : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400',
              ]"
            >
              {{ tab.count }}
            </span>
          </button>
        </nav>
      </div>

      <!-- TAB 1: OVERVIEW -->
      <div v-if="activeTab === 'overview'" class="space-y-6">
        <!-- Room Counts Summary -->
        <div class="grid grid-cols-2 sm:grid-cols-5 gap-3">
          <div class="p-4 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm text-center">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">{{ $t('common.totalRoomsCount') }}</span>
            <span class="text-lg font-bold text-slate-800 dark:text-slate-100 mt-0.5 block">{{ house.roomCounts?.total || 0 }}</span>
          </div>
          <div class="p-4 bg-emerald-50/50 dark:bg-emerald-950/30 rounded-xl border border-emerald-200/60 dark:border-emerald-800 shadow-sm text-center">
            <span class="text-[10px] text-emerald-600 dark:text-emerald-400 uppercase font-semibold block">{{ $t('common.availableCount') }}</span>
            <span class="text-lg font-bold text-emerald-700 dark:text-emerald-300 mt-0.5 block">{{ house.roomCounts?.available || 0 }}</span>
          </div>
          <div class="p-4 bg-amber-50/50 dark:bg-amber-950/30 rounded-xl border border-amber-200/60 dark:border-amber-800 shadow-sm text-center">
            <span class="text-[10px] text-amber-600 dark:text-amber-400 uppercase font-semibold block">{{ $t('ownerProperties.statusReserved') }}</span>
            <span class="text-lg font-bold text-amber-700 dark:text-amber-300 mt-0.5 block">{{ house.roomCounts?.reserved || 0 }}</span>
          </div>
          <div class="p-4 bg-blue-50/50 dark:bg-blue-950/30 rounded-xl border border-blue-200/60 dark:border-blue-800 shadow-sm text-center">
            <span class="text-[10px] text-blue-600 dark:text-blue-400 uppercase font-semibold block">{{ $t('ownerProperties.statusOccupied') }}</span>
            <span class="text-lg font-bold text-blue-700 dark:text-blue-300 mt-0.5 block">{{ house.roomCounts?.occupied || 0 }}</span>
          </div>
          <div class="p-4 bg-red-50/50 dark:bg-red-950/30 rounded-xl border border-red-200/60 dark:border-red-800 shadow-sm text-center">
            <span class="text-[10px] text-red-600 dark:text-red-400 uppercase font-semibold block">{{ $t('ownerProperties.statusMaintenance') }}</span>
            <span class="text-lg font-bold text-red-700 dark:text-red-300 mt-0.5 block">{{ house.roomCounts?.maintenance || 0 }}</span>
          </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2 space-y-6">
            <BaseCard :title="$t('ownerProperties.descriptionTitle')">
              <p class="text-xs text-slate-600 dark:text-slate-300 leading-relaxed whitespace-pre-line">
                {{ house.description || $t('property.noDescription') }}
              </p>
            </BaseCard>

            <BaseCard :title="$t('ownerProperties.mapTitle')">
              <div class="h-64 rounded-xl overflow-hidden border border-slate-200 dark:border-slate-800">
                <ClientOnly>
                  <MapView
                    :latitude="house.latitude"
                    :longitude="house.longitude"
                    :zoom="16"
                    :markers="[{ id: house.id, name: house.name, latitude: house.latitude, longitude: house.longitude, address: house.addressLine }]"
                  />
                </ClientOnly>
              </div>
            </BaseCard>
          </div>

          <div>
            <BaseCard :title="$t('ownerProperties.utilityTitle')">
              <div class="space-y-3">
                <div class="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 flex items-center justify-between">
                  <span class="text-xs text-slate-600 dark:text-slate-400 font-medium">{{ $t('ownerProperties.elecPrice') }}</span>
                  <span class="text-xs font-bold text-slate-900 dark:text-white">{{ formatCurrency(house.electricityUnitPrice) }} / kWh</span>
                </div>
                <div class="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 flex items-center justify-between">
                  <span class="text-xs text-slate-600 dark:text-slate-400 font-medium">{{ $t('ownerProperties.waterPrice') }}</span>
                  <span class="text-xs font-bold text-slate-900 dark:text-white">{{ formatCurrency(house.waterUnitPrice) }} / m³</span>
                </div>
                <button
                  type="button"
                  class="w-full text-center text-xs font-semibold text-primary-600 dark:text-primary-400 hover:text-primary-700 pt-2"
                  @click="activeTab = 'utilities'"
                >
                  {{ $t('ownerProperties.editUtilityPrices') }}
                </button>
              </div>
            </BaseCard>
          </div>
        </div>
      </div>

      <!-- TAB 2: ROOM TYPES -->
      <div v-if="activeTab === 'room-types'" class="space-y-6">
        <div class="flex items-center justify-between">
          <div>
            <h3 class="text-base font-bold text-slate-900 dark:text-white">{{ $t('ownerProperties.roomTypesTitle') }}</h3>
            <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ $t('ownerProperties.roomTypesSubtitle') }}</p>
          </div>
          <BaseButton variant="primary" size="sm" @click="openCreateRoomTypeModal">
            {{ $t('ownerProperties.addRoomTypeBtn') }}
          </BaseButton>
        </div>

        <div v-if="roomTypes.length === 0" class="py-12 text-center bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-slate-400 text-xs">
          {{ $t('ownerProperties.noRoomTypesYet') }}
        </div>

        <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div
            v-for="rt in roomTypes"
            :key="rt.id"
            class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col justify-between"
          >
            <div>
              <div class="flex items-center justify-between mb-2">
                <h4 class="text-sm font-bold text-slate-900 dark:text-white">{{ rt.typeName }}</h4>
                <span class="text-xs font-extrabold text-primary-600 dark:text-primary-400">{{ formatCurrency(rt.price) }}{{ $t('property.perMonth') }}</span>
              </div>
              <p class="text-xs text-slate-500 dark:text-slate-400 line-clamp-2 mb-3">{{ rt.description || $t('property.noDescription') }}</p>

              <div class="flex flex-wrap gap-2 text-[11px] text-slate-600 dark:text-slate-400 mb-3">
                <span class="px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800">📐 {{ rt.roomSizeM2 }} m²</span>
                <span class="px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800">{{ $t('ownerProperties.maxOccupantsCount', { count: rt.maxOccupants }) }}</span>
                <span class="px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800">{{ $t('ownerProperties.roomCountLabel', { count: rt.roomCount }) }}</span>
              </div>

              <!-- Facilities list -->
              <div v-if="rt.facilities && rt.facilities.length > 0" class="flex flex-wrap gap-1">
                <span
                  v-for="f in rt.facilities"
                  :key="f.id"
                  class="px-1.5 py-0.5 rounded text-[10px] bg-primary-50 dark:bg-primary-950/40 text-primary-700 dark:text-primary-300"
                >
                  {{ f.name }}
                </span>
              </div>
            </div>

            <div class="pt-4 mt-4 border-t border-slate-100 dark:border-slate-800 flex items-center justify-end gap-2">
              <button
                type="button"
                class="text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 px-2 py-1"
                @click="openEditRoomTypeModal(rt)"
              >
                {{ $t('common.edit') }}
              </button>
              <button
                type="button"
                class="text-xs font-semibold text-red-600 dark:text-red-400 hover:text-red-700 px-2 py-1"
                @click="handleDeleteRoomType(rt.id)"
              >
                {{ $t('common.delete') }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- TAB 3: ROOMS -->
      <div v-if="activeTab === 'rooms'" class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h3 class="text-base font-bold text-slate-900 dark:text-white">{{ $t('ownerProperties.roomsTitle') }}</h3>
            <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ $t('ownerProperties.roomsSubtitle') }}</p>
          </div>
          <BaseButton
            variant="primary"
            size="sm"
            :disabled="roomTypes.length === 0"
            @click="openCreateRoomModal"
          >
            {{ $t('ownerProperties.addRoomBtn') }}
          </BaseButton>
        </div>

        <div v-if="roomTypes.length === 0" class="p-4 bg-amber-50 dark:bg-amber-950/40 rounded-xl border border-amber-200 dark:border-amber-800 text-xs text-amber-800 dark:text-amber-300">
          {{ $t('ownerProperties.roomTypeRequiredNotice') }}
        </div>

        <!-- Filter buttons -->
        <div class="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none">
          <button
            type="button"
            :class="[
              'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all',
              roomFilterStatus === '' ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
            ]"
            @click="roomFilterStatus = ''"
          >
            {{ $t('common.allCount', { count: rooms.length }) }}
          </button>
          <button
            v-for="st in ['Available', 'Reserved', 'Occupied', 'Maintenance']"
            :key="st"
            type="button"
            :class="[
              'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all',
              roomFilterStatus === st ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
            ]"
            @click="roomFilterStatus = st"
          >
            {{ $t(`enums.RoomStatus.${st}`) }}
          </button>
        </div>

        <!-- Rooms Table / List -->
        <div v-if="filteredRooms.length === 0" class="py-12 text-center bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-slate-400 text-xs">
          {{ $t('ownerProperties.noRoomsMatchFilter') }}
        </div>

        <div v-else class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 dark:bg-slate-950 border-b border-slate-200 dark:border-slate-800 text-slate-500 dark:text-slate-400 font-semibold">
                <tr>
                  <th class="py-3 px-4">{{ $t('ownerProperties.colRoomNumber') }}</th>
                  <th class="py-3 px-4">{{ $t('ownerProperties.colRoomType') }}</th>
                  <th class="py-3 px-4">{{ $t('ownerProperties.colPrice') }}</th>
                  <th class="py-3 px-4">{{ $t('ownerProperties.colStatus') }}</th>
                  <th class="py-3 px-4">{{ $t('ownerProperties.colMeters') }}</th>
                  <th class="py-3 px-4 text-right">{{ $t('ownerProperties.colActions') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-700 dark:text-slate-300">
                <tr v-for="rm in filteredRooms" :key="rm.id" class="hover:bg-slate-50/60 dark:hover:bg-slate-800/50 transition-colors">
                  <td class="py-3.5 px-4 font-bold text-slate-900 dark:text-white">
                    {{ $t('property.room') }} {{ rm.roomNumber }}
                  </td>
                  <td class="py-3.5 px-4">
                    {{ rm.roomTypeName }}
                  </td>
                  <td class="py-3.5 px-4 font-semibold text-slate-900 dark:text-white">
                    {{ formatCurrency(rm.price) }}
                  </td>
                  <td class="py-3.5 px-4">
                    <StatusBadge type="RoomStatus" :status="rm.status" />
                  </td>
                  <td class="py-3.5 px-4 text-slate-500 dark:text-slate-400">
                    <span>⚡ {{ rm.currentElectricityReading }} kWh</span>
                    <span class="mx-1.5">·</span>
                    <span>💧 {{ rm.currentWaterReading }} m³</span>
                  </td>
                  <td class="py-3.5 px-4 text-right space-x-2">
                    <button
                      type="button"
                      class="text-xs font-semibold text-primary-600 dark:text-primary-400 hover:text-primary-700"
                      @click="openMeterModal(rm)"
                    >
                      {{ $t('ownerProperties.recordMeters') }}
                    </button>
                    <button
                      v-if="rm.status === 'Available' || rm.status === 'Maintenance'"
                      type="button"
                      class="text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
                      @click="toggleMaintenanceStatus(rm)"
                    >
                      {{ rm.status === 'Available' ? $t('ownerProperties.toggleMaintenance') : $t('ownerProperties.toggleAvailable') }}
                    </button>
                    <button
                      type="button"
                      class="text-xs font-semibold text-red-600 dark:text-red-400 hover:text-red-700"
                      @click="handleDeleteRoom(rm.id)"
                    >
                      {{ $t('common.delete') }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- TAB 4: UTILITY PRICES -->
      <div v-if="activeTab === 'utilities'" class="max-w-2xl space-y-6">
        <BaseCard :title="$t('ownerProperties.updateUtilityTitle')">
          <form @submit.prevent="handleUpdateUtilityPrices" class="space-y-4">
            <BaseInput
              v-model.number="utilityForm.electricityUnitPrice"
              type="number"
              min="0"
              :label="$t('ownerProperties.elecUnitLabel')"
              required
            />
            <BaseInput
              v-model.number="utilityForm.waterUnitPrice"
              type="number"
              min="0"
              :label="$t('ownerProperties.waterUnitLabel')"
              required
            />
            <div class="pt-2">
              <BaseButton type="submit" variant="primary" size="md" :loading="isSavingUtilities">
                {{ $t('ownerProperties.saveUtilityBtn') }}
              </BaseButton>
            </div>
          </form>
        </BaseCard>
      </div>

      <!-- TAB 5: GALLERY -->
      <div v-if="activeTab === 'gallery'" class="space-y-6">
        <BaseCard :title="$t('ownerProperties.galleryTitle')">
          <ImageUploader
            :images="house.images || []"
            :boarding-house-id="houseId"
            @update:images="handleImagesUpdated"
          />
        </BaseCard>
      </div>
    </div>

    <!-- MODAL 1: Create / Edit Room Type -->
    <BaseModal
      v-model="isRoomTypeModalOpen"
      :title="editingRoomTypeId ? $t('ownerProperties.editRoomType') : $t('ownerProperties.addRoomType')"
      max-width="lg"
    >
      <form @submit.prevent="handleSaveRoomType" class="space-y-4">
        <BaseInput
          v-model="roomTypeForm.typeName"
          :label="$t('ownerProperties.roomTypeNameLabel')"
          :placeholder="$t('ownerProperties.roomTypeNamePlaceholder')"
          required
        />

        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <BaseInput
            v-model.number="roomTypeForm.price"
            type="number"
            min="0"
            :label="$t('ownerProperties.roomTypePriceLabel')"
            required
          />
          <BaseInput
            v-model.number="roomTypeForm.roomSizeM2"
            type="number"
            min="1"
            :label="$t('ownerProperties.roomTypeAreaLabel')"
            required
          />
          <BaseInput
            v-model.number="roomTypeForm.maxOccupants"
            type="number"
            min="1"
            :label="$t('ownerProperties.roomTypeMaxOccupantsLabel')"
            required
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">{{ $t('ownerProperties.roomTypeDescLabel') }}</label>
          <textarea
            v-model="roomTypeForm.description"
            rows="3"
            class="input-field !text-xs !py-2"
            :placeholder="$t('ownerProperties.roomTypeDescPlaceholder')"
          />
        </div>

        <!-- Facilities checkboxes -->
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-2">{{ $t('ownerProperties.roomTypeFacilitiesLabel') }}</label>
          <div class="grid grid-cols-2 sm:grid-cols-3 gap-2 max-h-40 overflow-y-auto p-2 border border-slate-200 rounded-xl bg-slate-50/50">
            <label
              v-for="fac in availableFacilities"
              :key="fac.id"
              class="flex items-center gap-2 text-xs text-slate-700 cursor-pointer select-none"
            >
              <input
                type="checkbox"
                :value="fac.id"
                v-model="roomTypeForm.facilityIds"
                class="rounded border-slate-300 text-primary-600 focus:ring-primary-500"
              />
              <span>{{ fac.name }}</span>
            </label>
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
          <BaseButton variant="outline" size="sm" type="button" @click="isRoomTypeModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSavingRoomType">
            {{ $t('common.save') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>

    <!-- MODAL 2: Create Room -->
    <BaseModal
      v-model="isRoomModalOpen"
      :title="$t('ownerProperties.addRoom')"
      max-width="md"
    >
      <form @submit.prevent="handleSaveRoom" class="space-y-4">
        <BaseSelect
          v-model="roomForm.roomTypeId"
          :label="$t('ownerProperties.roomBelongsToType')"
          :options="roomTypeOptions"
          required
        />

        <BaseInput
          v-model="roomForm.roomNumber"
          :label="$t('ownerProperties.roomNumberLabel')"
          placeholder="VD: 101, 202, A3..."
          required
        />

        <BaseInput
          v-model="roomForm.description"
          :label="$t('ownerProperties.roomNoteLabel')"
          :placeholder="$t('ownerProperties.roomNotePlaceholder')"
        />

        <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
          <BaseButton variant="outline" size="sm" type="button" @click="isRoomModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSavingRoom">
            {{ $t('common.save') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>

    <!-- MODAL 3: Update Meter Readings -->
    <BaseModal
      v-model="isMeterModalOpen"
      :title="$t('ownerProperties.updateMetersModalTitle', { room: selectedRoom?.roomNumber })"
      max-width="md"
    >
      <form @submit.prevent="handleSaveMeterReadings" class="space-y-4">
        <BaseInput
          v-model.number="meterForm.electricityReading"
          type="number"
          step="0.1"
          min="0"
          :label="$t('ownerProperties.currentElecMeter')"
          required
        />

        <BaseInput
          v-model.number="meterForm.waterReading"
          type="number"
          step="0.1"
          min="0"
          :label="$t('ownerProperties.currentWaterMeter')"
          required
        />

        <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
          <BaseButton variant="outline" size="sm" type="button" @click="isMeterModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSavingMeter">
            {{ $t('common.save') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseCard from '~/components/common/BaseCard.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import BaseSelect from '~/components/common/BaseSelect.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
const MapView = defineAsyncComponent(() => import('~/components/common/MapView.client.vue'))
import ImageUploader from '~/components/common/ImageUploader.vue'
import { RoomStatus } from '~/types/enums'
import type {
  BoardingHouseDetailResponse,
  RoomTypeResponse,
  RoomResponse,
  FacilityResponse,
  ImageResponse,
} from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const route = useRoute()
const houseId = route.params.id as string

const { get, post, put, delete: deleteApi } = useApi()
const { formatCurrency } = useFormat()
const { t } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const house = ref<BoardingHouseDetailResponse | null>(null)
const roomTypes = ref<RoomTypeResponse[]>([])
const rooms = ref<RoomResponse[]>([])
const availableFacilities = ref<FacilityResponse[]>([])

const activeTab = ref('overview')
const roomFilterStatus = ref('')
const isSubmittingReview = ref(false)

const tabs = computed(() => [
  { id: 'overview', label: t('property.tabOverview') },
  { id: 'room-types', label: t('property.tabRoomTypes'), count: roomTypes.value.length },
  { id: 'rooms', label: t('property.tabRoomsList'), count: rooms.value.length },
  { id: 'utilities', label: t('property.tabUtilityPricing') },
  { id: 'gallery', label: t('property.tabGallery'), count: house.value?.images?.length || 0 },
])

const filteredRooms = computed(() => {
  if (!roomFilterStatus.value) return rooms.value
  return rooms.value.filter((r) => r.status === roomFilterStatus.value)
})

const roomTypeOptions = computed(() =>
  roomTypes.value.map((rt) => ({
    label: `${rt.typeName} (${formatCurrency(rt.price)}${t('property.perMonth')})`,
    value: rt.id,
  }))
)

// Fetch Data
const fetchData = async () => {
  isLoading.value = true
  try {
    const [houseData, rtData, rmData, facData] = await Promise.all([
      get<BoardingHouseDetailResponse>(`/my/boarding-houses/${houseId}`),
      get<RoomTypeResponse[]>(`/my/boarding-houses/${houseId}/room-types`),
      get<RoomResponse[]>(`/my/boarding-houses/${houseId}/rooms`),
      get<FacilityResponse[]>('/facilities').catch(() => []),
    ])

    house.value = houseData
    roomTypes.value = rtData || []
    rooms.value = rmData || []
    availableFacilities.value = facData || []

    utilityForm.electricityUnitPrice = houseData.electricityUnitPrice || 3500
    utilityForm.waterUnitPrice = houseData.waterUnitPrice || 30000
  } catch (err: any) {
    house.value = null
  } finally {
    isLoading.value = false
  }
}

// Review Submission
const handleSubmitReview = async () => {
  isSubmittingReview.value = true
  try {
    const updated = await put<BoardingHouseDetailResponse>(`/my/boarding-houses/${houseId}/submit-review`)
    house.value = updated
    toast.success(t('messages.submitForModerationSuccess'))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSubmittingReview.value = false
  }
}

// Room Types Management
const isRoomTypeModalOpen = ref(false)
const editingRoomTypeId = ref<string | null>(null)
const isSavingRoomType = ref(false)

const roomTypeForm = reactive({
  typeName: '',
  price: 3000000,
  roomSizeM2: 25,
  maxOccupants: 2,
  description: '',
  facilityIds: [] as string[],
})

const openCreateRoomTypeModal = () => {
  editingRoomTypeId.value = null
  roomTypeForm.typeName = ''
  roomTypeForm.price = 3000000
  roomTypeForm.roomSizeM2 = 25
  roomTypeForm.maxOccupants = 2
  roomTypeForm.description = ''
  roomTypeForm.facilityIds = []
  isRoomTypeModalOpen.value = true
}

const openEditRoomTypeModal = (rt: RoomTypeResponse) => {
  editingRoomTypeId.value = rt.id
  roomTypeForm.typeName = rt.typeName
  roomTypeForm.price = rt.price
  roomTypeForm.roomSizeM2 = rt.roomSizeM2
  roomTypeForm.maxOccupants = rt.maxOccupants
  roomTypeForm.description = rt.description || ''
  roomTypeForm.facilityIds = rt.facilities?.map((f) => f.id) || []
  isRoomTypeModalOpen.value = true
}

const handleSaveRoomType = async () => {
  if (!roomTypeForm.typeName || roomTypeForm.price <= 0) return
  isSavingRoomType.value = true
  try {
    if (editingRoomTypeId.value) {
      await put(`/my/boarding-houses/${houseId}/room-types/${editingRoomTypeId.value}`, roomTypeForm)
      toast.success(t('messages.updateRoomTypeSuccess'))
    } else {
      await post(`/my/boarding-houses/${houseId}/room-types`, roomTypeForm)
      toast.success(t('messages.createRoomTypeSuccess'))
    }
    isRoomTypeModalOpen.value = false
    await fetchData()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSavingRoomType.value = false
  }
}

const handleDeleteRoomType = async (typeId: string) => {
  if (!confirm(t('messages.confirmAction'))) return
  try {
    await deleteApi(`/my/boarding-houses/${houseId}/room-types/${typeId}`)
    toast.success(t('messages.deleteRoomTypeSuccess'))
    await fetchData()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

// Rooms Management
const isRoomModalOpen = ref(false)
const isSavingRoom = ref(false)

const roomForm = reactive({
  roomTypeId: '',
  roomNumber: '',
  description: '',
})

const openCreateRoomModal = () => {
  roomForm.roomTypeId = roomTypes.value[0]?.id || ''
  roomForm.roomNumber = ''
  roomForm.description = ''
  isRoomModalOpen.value = true
}

const handleSaveRoom = async () => {
  if (!roomForm.roomTypeId || !roomForm.roomNumber) return
  isSavingRoom.value = true
  try {
    await post(`/my/boarding-houses/${houseId}/rooms`, roomForm)
    toast.success(t('messages.createRoomSuccess'))
    isRoomModalOpen.value = false
    await fetchData()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSavingRoom.value = false
  }
}

const toggleMaintenanceStatus = async (rm: RoomResponse) => {
  const nextStatus = rm.status === RoomStatus.Available ? RoomStatus.Maintenance : RoomStatus.Available
  try {
    await put(`/my/rooms/${rm.id}/status`, { status: nextStatus })
    toast.success(t('messages.updateRoomStatusSuccess'))
    await fetchData()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

const handleDeleteRoom = async (roomId: string) => {
  if (!confirm(t('messages.confirmAction'))) return
  try {
    await deleteApi(`/my/boarding-houses/${houseId}/rooms/${roomId}`)
    toast.success(t('messages.deleteRoomSuccess'))
    await fetchData()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

// Meter Readings
const isMeterModalOpen = ref(false)
const selectedRoom = ref<RoomResponse | null>(null)
const isSavingMeter = ref(false)

const meterForm = reactive({
  electricityReading: 0,
  waterReading: 0,
})

const openMeterModal = (rm: RoomResponse) => {
  selectedRoom.value = rm
  meterForm.electricityReading = rm.currentElectricityReading || 0
  meterForm.waterReading = rm.currentWaterReading || 0
  isMeterModalOpen.value = true
}

const handleSaveMeterReadings = async () => {
  if (!selectedRoom.value) return
  isSavingMeter.value = true
  try {
    await put(`/my/rooms/${selectedRoom.value.id}/meter-readings`, meterForm)
    toast.success(t('messages.updateMetersSuccess'))
    isMeterModalOpen.value = false
    await fetchData()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSavingMeter.value = false
  }
}

// Utilities Update
const utilityForm = reactive({
  electricityUnitPrice: 3500,
  waterUnitPrice: 30000,
})
const isSavingUtilities = ref(false)

const handleUpdateUtilityPrices = async () => {
  isSavingUtilities.value = true
  try {
    const updated = await put<BoardingHouseDetailResponse>(`/my/boarding-houses/${houseId}/utility-prices`, utilityForm)
    house.value = updated
    toast.success(t('messages.updatePricingSuccess'))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isSavingUtilities.value = false
  }
}

const handleImagesUpdated = (newImages: ImageResponse[]) => {
  if (house.value) {
    house.value.images = newImages
  }
}

onMounted(() => {
  fetchData()
})
</script>
