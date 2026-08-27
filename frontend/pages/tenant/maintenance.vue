<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.myMaintenance') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Gửi yêu cầu sửa chữa cơ sở vật chất (điện, nước, khóa cửa, wifi, điều hòa) đến chủ nhà
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchRequests">
          🔄 Làm mới
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openCreateModal">
          + Báo sự cố mới
        </BaseButton>
      </div>
    </div>

    <!-- Status Filters -->
    <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
      <button
        type="button"
        :class="[
          'px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
          filterStatus === ''
            ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
            : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
        ]"
        @click="filterStatus = ''"
      >
        Tất cả ({{ requests.length }})
      </button>
      <button
        v-for="st in ['Pending', 'Accepted', 'Resolved', 'Rejected']"
        :key="st"
        type="button"
        :class="[
          'px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
          filterStatus === st
            ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
            : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
        ]"
        @click="filterStatus = st"
      >
        {{ $t(`enums.MaintenanceStatus.${st}`) }}
      </button>
    </div>

    <!-- Requests List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredRequests.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M11 4a2 2 0 114 0v1a1 1 0 001 1h3a1 1 0 011 1v3a1 1 0 01-1 1h-1a2 2 0 100 4h1a1 1 0 011 1v3a1 1 0 01-1 1h-3a1 1 0 01-1-1v-1a2 2 0 10-4 0v1a1 1 0 01-1 1H7a1 1 0 01-1-1v-3a1 1 0 00-1-1H4a2 2 0 110-4h1a1 1 0 001-1V7a1 1 0 011-1h3a1 1 0 001-1V4z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">Không có yêu cầu sự cố nào.</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="r in filteredRequests"
        :key="r.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-slate-900 dark:text-white">{{ r.boardingHouseName }}</span>
              <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                Phòng {{ r.roomNumber }}
              </span>
              <span class="text-xs font-semibold px-2 py-0.5 rounded-md bg-primary-50 dark:bg-primary-950 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-800">
                {{ getCategoryLabel(r.category) }}
              </span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              Gửi lúc: {{ formatRelativeTime(r.createdAt) }}
            </p>
          </div>

          <StatusBadge type="MaintenanceStatus" :status="r.status" />
        </div>

        <p class="text-xs text-slate-700 dark:text-slate-300 bg-slate-50 dark:bg-slate-800/60 p-3.5 rounded-xl whitespace-pre-wrap">
          {{ r.description }}
        </p>

        <!-- Images if any -->
        <div v-if="r.images && r.images.length > 0" class="flex items-center gap-2 overflow-x-auto pb-1">
          <img
            v-for="img in r.images"
            :key="img.id"
            :src="img.thumbnailUrl || img.url"
            alt="Incident photo"
            class="w-16 h-16 rounded-lg object-cover border border-slate-200 dark:border-slate-700 shrink-0"
          />
        </div>
      </div>
    </div>

    <!-- MODAL: Create Maintenance Request -->
    <BaseModal
      v-model="isCreateModalOpen"
      title="Báo cáo Sự cố Phòng trọ"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitCreate" class="space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Chọn Phòng trọ bạn đang thuê <span class="text-red-500">*</span>
          </label>
          <select v-model="form.leaseId" class="input-field !text-xs !py-2" required>
            <option value="">-- Chọn phòng --</option>
            <option v-for="l in activeLeases" :key="l.id" :value="l.id">
              {{ l.boardingHouseName }} - Phòng {{ l.roomNumber }}
            </option>
          </select>
        </div>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Loại sự cố <span class="text-red-500">*</span>
          </label>
          <select v-model="form.category" class="input-field !text-xs !py-2" required>
            <option value="Electrical">⚡ Điện (Mất điện, cháy bóng, chập ổ cắm)</option>
            <option value="Plumbing">💧 Nước (Rò rỉ nước, vòi hỏng, nghẹt bồn cầu)</option>
            <option value="Furniture">🪑 Nội thất (Hỏng giường, tủ, cửa, ổ khóa)</option>
            <option value="Appliances">❄️ Thiết bị gia dụng (Điều hòa, nóng lạnh, máy giặt)</option>
            <option value="Internet">🌐 Mạng / Wifi (Mất kết nối mạng, wifi yếu)</option>
            <option value="Other">🛠️ Khác (Tường ẩm mốc, dột, vệ sinh...)</option>
          </select>
        </div>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Mô tả chi tiết tình trạng sự cố <span class="text-red-500">*</span>
          </label>
          <textarea
            v-model="form.description"
            rows="4"
            class="input-field !text-xs !py-2"
            placeholder="Mô tả cụ thể hiện tượng gặp phải để chủ nhà / nhân viên dễ dàng chuẩn bị đồ nghề sửa chữa..."
            required
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCreateModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmitting">
            Gửi báo cáo sự cố
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
import type {
  LeaseResponse,
  MaintenanceCategory,
  MaintenanceRequestResponse,
  PagedResult,
} from '~/types/api'

definePageMeta({
  layout: 'tenant',
})

const { get, post } = useApi()
const { formatRelativeTime } = useFormat()
const toast = useToast()

const isLoading = ref(true)
const requests = ref<MaintenanceRequestResponse[]>([])
const filterStatus = ref('')
const activeLeases = ref<LeaseResponse[]>([])

const getCategoryLabel = (cat: MaintenanceCategory) => {
  switch (cat) {
    case 'Electrical':
      return '⚡ Điện'
    case 'Plumbing':
      return '💧 Nước'
    case 'Furniture':
      return '🪑 Nội thất'
    case 'Appliances':
      return '❄️ Thiết bị'
    case 'Internet':
      return '🌐 Wifi'
    default:
      return '🛠️ Khác'
  }
}

const filteredRequests = computed(() => {
  if (!filterStatus.value) return requests.value
  return requests.value.filter((r) => r.status === filterStatus.value)
})

const fetchRequests = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<MaintenanceRequestResponse>>('/maintenance-requests', { pageSize: 50 })
    requests.value = data.items || []
  } catch {
    requests.value = []
  } finally {
    isLoading.value = false
  }
}

// Modal Create
const isCreateModalOpen = ref(false)
const isSubmitting = ref(false)
const form = reactive({
  leaseId: '',
  category: 'Electrical' as MaintenanceCategory,
  description: '',
})

const openCreateModal = async () => {
  try {
    const leasesData = await get<PagedResult<LeaseResponse>>('/leases', { pageSize: 20 })
    activeLeases.value = (leasesData.items || []).filter((l) => l.status === 'Active' || l.status === 'Expiring')
    if (activeLeases.value.length > 0) {
      form.leaseId = activeLeases.value[0].id
    }
  } catch {
    activeLeases.value = []
  }

  form.category = 'Electrical'
  form.description = ''
  isCreateModalOpen.value = true
}

const handleSubmitCreate = async () => {
  if (!form.leaseId) {
    toast.error('Bạn cần có hợp đồng thuê phòng đang hiệu lực để gửi báo cáo.')
    return
  }
  isSubmitting.value = true
  try {
    await post('/maintenance-requests', {
      leaseId: form.leaseId,
      category: form.category,
      description: form.description,
    })
    toast.success('Gửi báo cáo sự cố thành công! Chủ trọ sẽ liên hệ hỗ trợ sớm.')
    isCreateModalOpen.value = false
    await fetchRequests()
  } catch (err: any) {
    toast.error(err.message || 'Không thể gửi báo cáo sự cố.')
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  fetchRequests()
})
</script>
