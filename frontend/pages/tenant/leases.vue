<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.myLeases') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">Danh sách hợp đồng thuê phòng, thông tin thành viên cùng phòng và yêu cầu gia hạn</p>
      </div>
      <BaseButton variant="outline" size="sm" @click="fetchLeases">
        🔄 Làm mới
      </BaseButton>
    </div>

    <!-- Filter status tabs -->
    <div class="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none">
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
        Tất cả ({{ leases.length }})
      </button>
      <button
        v-for="st in ['Active', 'Expiring', 'Ended', 'Terminated']"
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
        {{ $t(`enums.LeaseStatus.${st}`) }}
      </button>
    </div>

    <!-- Leases List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredLeases.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">Không có hợp đồng thuê nào.</p>
    </div>

    <div v-else class="space-y-6">
      <div
        v-for="l in filteredLeases"
        :key="l.id"
        class="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-5 transition-all"
      >
        <!-- Lease Header -->
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-slate-900 dark:text-white">{{ l.boardingHouseName }}</span>
              <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-primary-50 dark:bg-primary-950/50 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-800">
                Phòng {{ l.roomNumber }}
              </span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              Thời hạn: <strong class="text-slate-700 dark:text-slate-300">{{ l.startDate }}</strong> đến <strong class="text-slate-700 dark:text-slate-300">{{ l.endDate }}</strong>
              <span v-if="l.termMonths"> ({{ l.termMonths }} tháng)</span>
            </p>
          </div>

          <div class="flex items-center gap-3">
            <StatusBadge type="LeaseStatus" :status="l.status" />
          </div>
        </div>

        <!-- Expiring Alert -->
        <div
          v-if="l.status === 'Expiring'"
          class="p-3.5 bg-amber-50 dark:bg-amber-950/30 rounded-xl border border-amber-200 dark:border-amber-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs"
        >
          <div class="flex items-center gap-2 text-amber-800 dark:text-amber-300">
            <span>⚠️</span>
            <span>Hợp đồng của bạn sắp hết hạn vào ngày <strong>{{ l.endDate }}</strong>. Bạn có muốn tiếp tục gia hạn hợp đồng không?</span>
          </div>
          <BaseButton variant="primary" size="sm" class="shrink-0" @click="openExtensionModal(l)">
            📝 Gia hạn hợp đồng
          </BaseButton>
        </div>

        <!-- Financial Terms Summary Cards -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-left">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">Giá thuê cố định</span>
            <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block">{{ formatCurrency(l.monthlyRent) }} / th</span>
          </div>
          <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-left">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">Tiền cọc bảo đảm</span>
            <span class="text-sm font-bold text-emerald-600 dark:text-emerald-400 mt-0.5 block">{{ formatCurrency(l.depositHeld) }}</span>
          </div>
          <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-left">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">Người đứng tên HĐ</span>
            <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block truncate">{{ l.primaryTenantFullName || 'Khách thuê' }}</span>
          </div>
          <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-left">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">Số người ở cùng</span>
            <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block">{{ l.tenants?.length || 1 }} người</span>
          </div>
        </div>

        <!-- Co-tenants List Section -->
        <div class="space-y-2">
          <h4 class="text-xs font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wide">
            Danh sách người lưu trú ({{ l.tenants?.length || 0 }})
          </h4>
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            <div
              v-for="t in l.tenants"
              :key="t.id"
              class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800 flex items-center justify-between text-xs"
            >
              <div class="space-y-0.5">
                <div class="flex items-center gap-1.5">
                  <span class="font-bold text-slate-900 dark:text-white">{{ t.fullName }}</span>
                  <span
                    v-if="t.isPrimary"
                    class="px-1.5 py-0.2 rounded text-[9px] font-bold bg-primary-100 dark:bg-primary-950 text-primary-700 dark:text-primary-300"
                  >
                    Chủ hợp đồng
                  </span>
                </div>
                <p class="text-[11px] text-slate-500 dark:text-slate-400">
                  📞 {{ t.phoneNumber || 'Chưa cập nhật' }} · CCCD: {{ t.idCardNumber || 'Chưa cập nhật' }}
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Move-out Settlement Details if Ended/Terminated -->
        <div
          v-if="l.status === 'Ended' || l.status === 'Terminated'"
          class="p-4 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700 space-y-2 text-xs"
        >
          <div class="flex items-center justify-between font-bold text-slate-900 dark:text-white">
            <span>📋 Biên bản Quyết toán Bàn giao phòng khi trả phòng</span>
            <span class="text-slate-500 font-normal">Ngày kết thúc: {{ l.endedAt ? formatRelativeTime(l.endedAt) : l.endDate }}</span>
          </div>
          <p v-if="l.endReason" class="text-slate-600 dark:text-slate-400">
            <strong>Lý do kết thúc:</strong> {{ l.endReason }}
          </p>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-2 border-t border-slate-200 dark:border-slate-700 text-[11px]">
            <div>⚡ Số điện chốt: <strong>{{ l.finalElectricityReading ?? 0 }} kWh</strong></div>
            <div>💧 Số nước chốt: <strong>{{ l.finalWaterReading ?? 0 }} m³</strong></div>
            <div>Trừ cọc: <strong class="text-red-600 dark:text-red-400">{{ formatCurrency(l.depositDeducted || 0) }}</strong></div>
            <div>Hoàn lại cọc: <strong class="text-emerald-600 dark:text-emerald-400">{{ formatCurrency(l.depositRefunded || 0) }}</strong></div>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex flex-wrap items-center justify-end gap-2 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton
            variant="outline"
            size="sm"
            @click="navigateTo('/tenant/bills')"
          >
            💳 Xem hóa đơn tiền nhà
          </BaseButton>
          <BaseButton
            v-if="l.status === 'Active' || l.status === 'Expiring'"
            variant="primary"
            size="sm"
            @click="openExtensionModal(l)"
          >
            📝 Yêu cầu gia hạn hợp đồng
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Request Extension -->
    <BaseModal
      v-model="isExtensionModalOpen"
      title="Gửi yêu cầu Gia hạn Hợp đồng"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitExtension" class="space-y-4">
        <div v-if="selectedLease" class="p-3.5 bg-primary-50 dark:bg-primary-950/40 rounded-xl text-xs space-y-1">
          <div class="flex items-center justify-between font-bold text-primary-900 dark:text-primary-200">
            <span>{{ selectedLease.boardingHouseName }} - P.{{ selectedLease.roomNumber }}</span>
            <span>{{ formatCurrency(selectedLease.monthlyRent) }}/tháng</span>
          </div>
          <p class="text-primary-700 dark:text-primary-400 text-[11px]">
            Ngày hết hạn hiện tại: <strong>{{ selectedLease.endDate }}</strong>
          </p>
        </div>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Ngày kết thúc hợp đồng mới mong muốn <span class="text-red-500">*</span>
          </label>
          <input
            v-model="extensionForm.requestedEndDate"
            type="date"
            class="input-field !text-xs !py-2"
            required
          />
        </div>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Ghi chú gửi chủ nhà (Tùy chọn)
          </label>
          <textarea
            v-model="extensionForm.tenantNote"
            rows="3"
            class="input-field !text-xs !py-2"
            placeholder="VD: Em muốn gia hạn thêm 6 tháng để tiếp tục học tập và làm việc..."
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isExtensionModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmittingExtension">
            Gửi yêu cầu gia hạn
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
import type { LeaseResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'tenant',
})

const { get, post } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const toast = useToast()

const isLoading = ref(true)
const leases = ref<LeaseResponse[]>([])
const filterStatus = ref('')

const filteredLeases = computed(() => {
  if (!filterStatus.value) return leases.value
  return leases.value.filter((l) => l.status === filterStatus.value)
})

const fetchLeases = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<LeaseResponse>>('/leases', { page: 1, pageSize: 50 })
    leases.value = data.items || []
  } catch {
    leases.value = []
  } finally {
    isLoading.value = false
  }
}

// Extension Modal
const isExtensionModalOpen = ref(false)
const selectedLease = ref<LeaseResponse | null>(null)
const isSubmittingExtension = ref(false)

const extensionForm = reactive({
  requestedEndDate: '',
  tenantNote: '',
})

const openExtensionModal = (l: LeaseResponse) => {
  selectedLease.value = l
  // Default to +6 months from current end date
  const currentEnd = new Date(l.endDate)
  currentEnd.setMonth(currentEnd.getMonth() + 6)
  extensionForm.requestedEndDate = currentEnd.toISOString().slice(0, 10)
  extensionForm.tenantNote = ''
  isExtensionModalOpen.value = true
}

const handleSubmitExtension = async () => {
  if (!selectedLease.value || !extensionForm.requestedEndDate) return
  isSubmittingExtension.value = true
  try {
    await post('/extension-requests', {
      leaseId: selectedLease.value.id,
      requestedEndDate: extensionForm.requestedEndDate,
      tenantNote: extensionForm.tenantNote || undefined,
    })
    toast.success('Gửi yêu cầu gia hạn hợp đồng thành công! Chủ nhà sẽ xem xét và phản hồi.')
    isExtensionModalOpen.value = false
  } catch (err: any) {
    toast.error(err.message || 'Không thể gửi yêu cầu gia hạn.')
  } finally {
    isSubmittingExtension.value = false
  }
}

onMounted(() => {
  fetchLeases()
})
</script>
