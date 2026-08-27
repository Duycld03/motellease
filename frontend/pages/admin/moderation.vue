<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.moderation') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Duyệt hoặc từ chối bài đăng niêm yết khu trọ của chủ nhà trước khi hiển thị công khai trên hệ thống
        </p>
      </div>
      <BaseButton variant="outline" size="sm" @click="fetchHouses">
        🔄 Làm mới
      </BaseButton>
    </div>

    <!-- Filters & Search Bar -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="w-full sm:w-72">
        <input
          v-model="searchQuery"
          type="text"
          class="input-field !text-xs !py-1.5"
          placeholder="🔍 Tìm theo tên khu trọ, địa chỉ..."
          @input="debounceFetch"
        />
      </div>

      <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            filterStatus === ''
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700',
          ]"
          @click="filterStatus = ''"
        >
          Tất cả ({{ houses.length }})
        </button>
        <button
          v-for="st in ['PendingApproval', 'Approved', 'Rejected', 'Draft', 'Hidden']"
          :key="st"
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            filterStatus === st
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700',
          ]"
          @click="filterStatus = st"
        >
          {{ $t(`enums.ListingStatus.${st}`) }}
        </button>
      </div>
    </div>

    <!-- Properties Moderation List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredHouses.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <p class="font-medium text-slate-500 dark:text-slate-400">Không có khu trọ nào phù hợp.</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="h in filteredHouses"
        :key="h.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-slate-900 dark:text-white">{{ h.name }}</span>
              <span class="text-xs font-semibold px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400">
                {{ h.roomsCount }} phòng
              </span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              📍 {{ h.addressLine }}, {{ h.ward }}, {{ h.district }}, {{ h.province }}
            </p>
            <p class="text-xs text-slate-600 dark:text-slate-300">
              Chủ trọ: <strong class="text-slate-900 dark:text-white">{{ h.ownerFullName }}</strong> ({{ h.ownerEmail }})
            </p>
          </div>

          <StatusBadge type="ListingStatus" :status="h.listingStatus" />
        </div>

        <div v-if="h.rejectionReason" class="p-3 bg-red-50 dark:bg-red-950/30 rounded-xl text-xs text-red-800 dark:text-red-300">
          <strong>Lý do từ chối:</strong> {{ h.rejectionReason }}
        </div>

        <!-- Footer Actions -->
        <div class="flex items-center justify-between pt-2 border-t border-slate-100 dark:border-slate-800 text-xs">
          <span class="text-slate-400">Tạo lúc: {{ formatRelativeTime(h.createdAt) }}</span>

          <div class="flex items-center gap-2">
            <NuxtLink :to="`/boarding-houses/${h.id}`" target="_blank" class="btn-outline !text-xs !py-1.5 !px-3">
              👁️ Xem tin đăng
            </NuxtLink>

            <BaseButton
              v-if="h.listingStatus === 'PendingApproval' || h.listingStatus === 'Draft'"
              variant="outline"
              size="sm"
              class="text-red-600 hover:text-red-700 !text-xs !py-1.5"
              @click="openRejectModal(h)"
            >
              ✕ Từ chối
            </BaseButton>

            <BaseButton
              v-if="h.listingStatus === 'PendingApproval' || h.listingStatus === 'Draft' || h.listingStatus === 'Rejected'"
              variant="primary"
              size="sm"
              class="!text-xs !py-1.5"
              :loading="isApprovingId === h.id"
              @click="handleApprove(h.id)"
            >
              ✓ Duyệt bài
            </BaseButton>
          </div>
        </div>
      </div>
    </div>

    <!-- MODAL: Reject Listing -->
    <BaseModal
      v-model="isRejectModalOpen"
      title="Từ chối Niêm yết Khu trọ"
      max-width="md"
    >
      <form @submit.prevent="handleConfirmReject" class="space-y-4">
        <p class="text-xs text-slate-600 dark:text-slate-400">
          Nhập lý do từ chối kiểm duyệt cho khu trọ <strong>{{ selectedHouse?.name }}</strong> của chủ nhà <strong>{{ selectedHouse?.ownerFullName }}</strong>:
        </p>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Lý do từ chối (Tùy chọn)
          </label>
          <textarea
            v-model="rejectReason"
            rows="3"
            class="input-field !text-xs !py-2"
            placeholder="VD: Hình ảnh mờ, thông tin địa chỉ chưa chính xác..."
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isRejectModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="danger" size="sm" type="submit" :loading="isRejecting">
            Xác nhận từ chối
          </BaseButton>
        </div>
      </form>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
import type { AdminBoardingHouseResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'admin',
})

const { get, put } = useApi()
const { formatRelativeTime } = useFormat()
const toast = useToast()

const isLoading = ref(true)
const houses = ref<AdminBoardingHouseResponse[]>([])
const filterStatus = ref('')
const searchQuery = ref('')

let debounceTimer: any = null
const debounceFetch = () => {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    fetchHouses()
  }, 300)
}

const filteredHouses = computed(() => {
  let list = houses.value
  if (filterStatus.value) list = list.filter((h) => h.listingStatus === filterStatus.value)
  return list
})

const fetchHouses = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<AdminBoardingHouseResponse>>('/admin/boarding-houses', {
      listingStatus: filterStatus.value || undefined,
      search: searchQuery.value || undefined,
      pageSize: 50,
    })
    houses.value = data.items || []
  } catch {
    houses.value = []
  } finally {
    isLoading.value = false
  }
}

// Approve
const isApprovingId = ref<string | null>(null)
const handleApprove = async (id: string) => {
  if (!confirm('Duyệt niêm yết công khai cho khu trọ này?')) return
  isApprovingId.value = id
  try {
    await put(`/admin/boarding-houses/${id}/approve`, {})
    toast.success('Duyệt niêm yết khu trọ thành công!')
    await fetchHouses()
  } catch (err: any) {
    toast.error(err.message || 'Không thể duyệt khu trọ.')
  } finally {
    isApprovingId.value = null
  }
}

// Reject
const isRejectModalOpen = ref(false)
const selectedHouse = ref<AdminBoardingHouseResponse | null>(null)
const rejectReason = ref('')
const isRejecting = ref(false)

const openRejectModal = (h: AdminBoardingHouseResponse) => {
  selectedHouse.value = h
  rejectReason.value = ''
  isRejectModalOpen.value = true
}

const handleConfirmReject = async () => {
  if (!selectedHouse.value) return
  isRejecting.value = true
  try {
    await put(`/admin/boarding-houses/${selectedHouse.value.id}/reject`, {
      reason: rejectReason.value || undefined,
    })
    toast.success('Đã từ chối niêm yết khu trọ.')
    isRejectModalOpen.value = false
    await fetchHouses()
  } catch (err: any) {
    toast.error(err.message || 'Không thể từ chối niêm yết.')
  } finally {
    isRejecting.value = false
  }
}

onMounted(() => {
  fetchHouses()
})
</script>
