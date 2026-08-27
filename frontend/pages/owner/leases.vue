<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">Quản lý Hợp đồng thuê & Gia hạn</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Theo dõi hợp đồng đang thuê, quản lý người lưu trú, duyệt gia hạn và thực hiện thủ tục trả phòng quyết toán cọc
        </p>
      </div>
      <BaseButton variant="outline" size="sm" @click="refreshCurrentTab">
        🔄 Làm mới dữ liệu
      </BaseButton>
    </div>

    <!-- Main Navigation Tabs -->
    <div class="border-b border-slate-200 dark:border-slate-800">
      <nav class="flex space-x-6">
        <button
          type="button"
          :class="[
            'py-3 text-xs font-semibold border-b-2 transition-colors -mb-px flex items-center gap-1.5',
            activeTab === 'leases'
              ? 'border-primary-600 text-primary-600 dark:text-primary-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300',
          ]"
          @click="activeTab = 'leases'"
        >
          <span>Danh sách Hợp đồng</span>
          <span class="px-1.5 py-0.5 rounded-full text-[10px] bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400">
            {{ leases.length }}
          </span>
        </button>

        <button
          type="button"
          :class="[
            'py-3 text-xs font-semibold border-b-2 transition-colors -mb-px flex items-center gap-1.5',
            activeTab === 'extensions'
              ? 'border-primary-600 text-primary-600 dark:text-primary-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300',
          ]"
          @click="activeTab = 'extensions'"
        >
          <span>Yêu cầu Gia hạn</span>
          <span
            v-if="pendingExtensionsCount > 0"
            class="px-1.5 py-0.5 rounded-full text-[10px] bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 font-bold"
          >
            {{ pendingExtensionsCount }}
          </span>
        </button>
      </nav>
    </div>

    <!-- ================= TAB 1: LEASES ================= -->
    <div v-if="activeTab === 'leases'" class="space-y-6">
      <!-- Status Filters -->
      <div class="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none">
        <button
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            leaseFilterStatus === ''
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
          ]"
          @click="leaseFilterStatus = ''"
        >
          Tất cả ({{ leases.length }})
        </button>
        <button
          v-for="st in ['Active', 'Expiring', 'Ended', 'Terminated']"
          :key="st"
          type="button"
          :class="[
            'px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all',
            leaseFilterStatus === st
              ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm'
              : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
          ]"
          @click="leaseFilterStatus = st"
        >
          {{ $t(`enums.LeaseStatus.${st}`) }}
        </button>
      </div>

      <div v-if="isLoadingLeases" class="py-16 text-center">
        <LoadingSpinner size="md" />
      </div>

      <div v-else-if="filteredLeases.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
        <p class="font-medium text-slate-500 dark:text-slate-400">Không tìm thấy hợp đồng nào.</p>
      </div>

      <div v-else class="space-y-6">
        <div
          v-for="l in filteredLeases"
          :key="l.id"
          class="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-5 transition-all"
        >
          <!-- Header -->
          <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="text-base font-bold text-slate-900 dark:text-white">{{ l.boardingHouseName }}</span>
                <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-primary-50 dark:bg-primary-950/50 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-800">
                  Phòng {{ l.roomNumber }}
                </span>
              </div>
              <p class="text-xs text-slate-500 dark:text-slate-400">
                Thời hạn hợp đồng: <strong class="text-slate-700 dark:text-slate-300">{{ l.startDate }}</strong> đến <strong class="text-slate-700 dark:text-slate-300">{{ l.endDate }}</strong>
              </p>
            </div>

            <div class="flex items-center gap-3">
              <StatusBadge type="LeaseStatus" :status="l.status" />
            </div>
          </div>

          <!-- Financial Terms Summary Cards -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-left">
              <span class="text-[10px] text-slate-400 uppercase font-semibold block">Giá thuê cố định</span>
              <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block">{{ formatCurrency(l.monthlyRent) }} / th</span>
            </div>
            <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-left">
              <span class="text-[10px] text-slate-400 uppercase font-semibold block">Tiền cọc giữ</span>
              <span class="text-sm font-bold text-emerald-600 dark:text-emerald-400 mt-0.5 block">{{ formatCurrency(l.depositHeld) }}</span>
            </div>
            <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-left">
              <span class="text-[10px] text-slate-400 uppercase font-semibold block">Chủ hợp đồng</span>
              <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block truncate">{{ l.primaryTenantFullName || 'Khách thuê' }}</span>
            </div>
            <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 text-left">
              <span class="text-[10px] text-slate-400 uppercase font-semibold block">Người lưu trú</span>
              <span class="text-sm font-bold text-slate-900 dark:text-white mt-0.5 block">{{ l.tenants?.length || 1 }} người</span>
            </div>
          </div>

          <!-- Co-tenants list with Add/Remove action -->
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <h4 class="text-xs font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wide">
                Danh sách thành viên ở cùng phòng ({{ l.tenants?.length || 0 }})
              </h4>
              <button
                v-if="l.status === 'Active' || l.status === 'Expiring'"
                type="button"
                class="text-xs font-semibold text-primary-600 dark:text-primary-400 hover:underline"
                @click="openAddTenantModal(l)"
              >
                + Thêm thành viên
              </button>
            </div>

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

                <button
                  v-if="!t.isPrimary && (l.status === 'Active' || l.status === 'Expiring')"
                  type="button"
                  class="text-xs text-red-500 hover:text-red-700 font-semibold px-2 py-1"
                  @click="handleRemoveTenant(l.id, t.id, t.fullName)"
                >
                  Xóa
                </button>
              </div>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex flex-wrap items-center justify-end gap-2 pt-3 border-t border-slate-100 dark:border-slate-800">
            <BaseButton
              variant="outline"
              size="sm"
              @click="navigateTo('/owner/bills')"
            >
              ⚡ Tạo hóa đơn tháng
            </BaseButton>

            <BaseButton
              v-if="l.status === 'Active' || l.status === 'Expiring'"
              variant="danger"
              size="sm"
              @click="openTerminateModal(l)"
            >
              🚪 Trả phòng & Quyết toán cọc
            </BaseButton>
          </div>
        </div>
      </div>
    </div>

    <!-- ================= TAB 2: EXTENSIONS ================= -->
    <div v-if="activeTab === 'extensions'" class="space-y-6">
      <div v-if="isLoadingExtensions" class="py-16 text-center">
        <LoadingSpinner size="md" />
      </div>

      <div v-else-if="extensions.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
        <p class="font-medium text-slate-500 dark:text-slate-400">Không có yêu cầu gia hạn nào.</p>
      </div>

      <div v-else class="space-y-4">
        <div
          v-for="ext in extensions"
          :key="ext.id"
          class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
        >
          <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="text-base font-bold text-slate-900 dark:text-white">{{ ext.boardingHouseName }}</span>
                <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                  Phòng {{ ext.roomNumber }}
                </span>
              </div>
              <p class="text-xs text-slate-600 dark:text-slate-400">
                Người yêu cầu: <strong class="text-slate-900 dark:text-white">{{ ext.requesterFullName }}</strong> · Gửi lúc: {{ formatRelativeTime(ext.createdAt) }}
              </p>
              <p class="text-xs text-slate-700 dark:text-slate-300">
                Hạn hợp đồng cũ: <strong class="text-slate-900 dark:text-white">{{ ext.currentEndDate }}</strong> ➔ Đề xuất gia hạn đến: <strong class="text-primary-600 dark:text-primary-400 text-sm">{{ ext.requestedEndDate }}</strong>
              </p>
            </div>

            <StatusBadge type="RequestStatus" :status="ext.status" />
          </div>

          <div v-if="ext.tenantNote" class="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl text-xs text-slate-700 dark:text-slate-300">
            <strong>Ghi chú của khách:</strong> {{ ext.tenantNote }}
          </div>

          <div v-if="ext.ownerNote" class="p-3 bg-red-50 dark:bg-red-950/30 rounded-xl text-xs text-red-700 dark:text-red-300">
            <strong>Phản hồi của chủ nhà:</strong> {{ ext.ownerNote }}
          </div>

          <!-- Extension Actions -->
          <div v-if="ext.status === 'Pending'" class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800">
            <BaseButton
              variant="outline"
              size="sm"
              class="text-red-600 dark:text-red-400 border-red-200 dark:border-red-800 hover:bg-red-50 dark:hover:bg-red-950/40"
              @click="openRejectExtensionModal(ext)"
            >
              ✕ Từ chối
            </BaseButton>
            <BaseButton
              variant="primary"
              size="sm"
              :loading="isApprovingExtensionId === ext.id"
              @click="handleApproveExtension(ext)"
            >
              ✓ Duyệt gia hạn hợp đồng
            </BaseButton>
          </div>
        </div>
      </div>
    </div>

    <!-- MODAL 1: Add Co-tenant -->
    <BaseModal
      v-model="isAddTenantModalOpen"
      title="Thêm Người lưu trú vào Hợp đồng"
      max-width="md"
    >
      <form @submit.prevent="handleSaveTenant" class="space-y-4">
        <BaseInput
          v-model="tenantForm.fullName"
          label="Họ và tên thành viên"
          placeholder="VD: Nguyễn Văn B"
          required
        />
        <BaseInput
          v-model="tenantForm.phoneNumber"
          label="Số điện thoại"
          placeholder="0912345678"
        />
        <BaseInput
          v-model="tenantForm.idCardNumber"
          label="Số CCCD / CMND"
          placeholder="001203004567"
        />

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isAddTenantModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSavingTenant">
            {{ $t('common.save') }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>

    <!-- MODAL 2: Terminate Lease & Move-out Settlement -->
    <BaseModal
      v-model="isTerminateModalOpen"
      title="Thủ tục Trả phòng & Quyết toán tiền cọc"
      max-width="lg"
    >
      <div v-if="selectedLease" class="space-y-5">
        <div class="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-xl text-xs space-y-1">
          <div class="flex items-center justify-between font-bold text-slate-900 dark:text-white">
            <span>{{ selectedLease.boardingHouseName }} - Phòng {{ selectedLease.roomNumber }}</span>
            <span>Khách: {{ selectedLease.primaryTenantFullName }}</span>
          </div>
          <p class="text-slate-500 dark:text-slate-400 text-[11px]">
            Tiền cọc ban đầu đang giữ: <strong class="text-emerald-600 dark:text-emerald-400">{{ formatCurrency(selectedLease.depositHeld) }}</strong>
          </p>
        </div>

        <!-- Meter inputs & deductions -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              Chỉ số điện chốt cuối (kWh) <span class="text-red-500">*</span>
            </label>
            <input
              v-model.number="terminateForm.finalElectricityReading"
              type="number"
              step="0.1"
              min="0"
              class="input-field !text-xs !py-2"
              @input="fetchTerminationPreview"
              required
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              Chỉ số nước chốt cuối (m³) <span class="text-red-500">*</span>
            </label>
            <input
              v-model.number="terminateForm.finalWaterReading"
              type="number"
              step="0.1"
              min="0"
              class="input-field !text-xs !py-2"
              @input="fetchTerminationPreview"
              required
            />
          </div>
        </div>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Khấu trừ thêm (Tiền hỏng hóc thiết bị, vệ sinh...) (VNĐ)
          </label>
          <input
            v-model.number="terminateForm.depositDeducted"
            type="number"
            min="0"
            class="input-field !text-xs !py-2"
            @input="fetchTerminationPreview"
          />
        </div>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Lý do kết thúc hợp đồng
          </label>
          <input
            v-model="terminateForm.endReason"
            type="text"
            class="input-field !text-xs !py-2"
            placeholder="VD: Hết hạn hợp đồng, khách chuyển công tác..."
          />
        </div>

        <!-- Real-time settlement preview -->
        <div v-if="terminationPreview" class="p-4 bg-emerald-50 dark:bg-emerald-950/30 rounded-2xl border border-emerald-200 dark:border-emerald-800 text-xs space-y-2">
          <h5 class="font-bold text-emerald-900 dark:text-emerald-200 uppercase text-[11px] tracking-wide">
            Bảng dự tính quyết toán hoàn cọc:
          </h5>
          <div class="space-y-1 text-slate-700 dark:text-slate-300 text-[11px]">
            <div class="flex justify-between">
              <span>⚡ Tiền điện phát sinh ({{ terminationPreview.electricityQty }} kWh):</span>
              <span>- {{ formatCurrency(terminationPreview.electricityAmount) }}</span>
            </div>
            <div class="flex justify-between">
              <span>💧 Tiền nước phát sinh ({{ terminationPreview.waterQty }} m³):</span>
              <span>- {{ formatCurrency(terminationPreview.waterAmount) }}</span>
            </div>
            <div v-if="terminationPreview.depositDeducted > 0" class="flex justify-between">
              <span>🛠️ Khấu trừ hư hại / dịch vụ:</span>
              <span>- {{ formatCurrency(terminationPreview.depositDeducted) }}</span>
            </div>
          </div>
          <div class="flex justify-between items-center pt-2 border-t border-emerald-200 dark:border-emerald-800">
            <span class="font-bold text-slate-900 dark:text-white">Số tiền cọc hoàn lại cho khách:</span>
            <span class="text-base font-extrabold text-emerald-700 dark:text-emerald-300">{{ formatCurrency(terminationPreview.depositRefunded) }}</span>
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isTerminateModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="danger" size="sm" :loading="isTerminating" @click="handleConfirmTerminate">
            Xác nhận trả phòng & Thanh lý
          </BaseButton>
        </div>
      </div>
    </BaseModal>

    <!-- MODAL 3: Reject Extension -->
    <BaseModal
      v-model="isRejectExtModalOpen"
      title="Từ chối yêu cầu gia hạn hợp đồng"
      max-width="md"
    >
      <form @submit.prevent="handleConfirmRejectExtension" class="space-y-4">
        <p class="text-xs text-slate-600 dark:text-slate-400">
          Nhập phản hồi từ chối gia hạn phòng <strong>{{ selectedExt?.roomNumber }}</strong> của khách <strong>{{ selectedExt?.requesterFullName }}</strong>:
        </p>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Lý do từ chối (Tùy chọn)
          </label>
          <textarea
            v-model="rejectExtReason"
            rows="3"
            class="input-field !text-xs !py-2"
            placeholder="VD: Chủ nhà dự kiến lấy lại phòng để sửa chữa toàn diện..."
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isRejectExtModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="danger" size="sm" type="submit" :loading="isRejectingExt">
            Xác nhận từ chối
          </BaseButton>
        </div>
      </form>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseInput from '~/components/common/BaseInput.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import StatusBadge from '~/components/status/StatusBadge.vue'
import type {
  LeaseResponse,
  ExtensionRequestResponse,
  LeaseTerminationPreviewResponse,
  PagedResult,
} from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get, post, put, delete: deleteApi } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const toast = useToast()

const activeTab = ref<'leases' | 'extensions'>('leases')

// Tab 1: Leases state
const isLoadingLeases = ref(true)
const leases = ref<LeaseResponse[]>([])
const leaseFilterStatus = ref('')

const filteredLeases = computed(() => {
  if (!leaseFilterStatus.value) return leases.value
  return leases.value.filter((l) => l.status === leaseFilterStatus.value)
})

const fetchLeases = async () => {
  isLoadingLeases.value = true
  try {
    const data = await get<PagedResult<LeaseResponse>>('/leases', { page: 1, pageSize: 50 })
    leases.value = data.items || []
  } catch {
    leases.value = []
  } finally {
    isLoadingLeases.value = false
  }
}

// Tab 2: Extensions state
const isLoadingExtensions = ref(true)
const extensions = ref<ExtensionRequestResponse[]>([])

const pendingExtensionsCount = computed(() =>
  extensions.value.filter((e) => e.status === 'Pending').length
)

const fetchExtensions = async () => {
  isLoadingExtensions.value = true
  try {
    const data = await get<PagedResult<ExtensionRequestResponse>>('/extension-requests', { page: 1, pageSize: 50 })
    extensions.value = data.items || []
  } catch {
    extensions.value = []
  } finally {
    isLoadingExtensions.value = false
  }
}

const refreshCurrentTab = () => {
  if (activeTab.value === 'leases') fetchLeases()
  else fetchExtensions()
}

// Add Co-tenant
const isAddTenantModalOpen = ref(false)
const selectedLease = ref<LeaseResponse | null>(null)
const isSavingTenant = ref(false)
const tenantForm = reactive({
  fullName: '',
  phoneNumber: '',
  idCardNumber: '',
})

const openAddTenantModal = (l: LeaseResponse) => {
  selectedLease.value = l
  tenantForm.fullName = ''
  tenantForm.phoneNumber = ''
  tenantForm.idCardNumber = ''
  isAddTenantModalOpen.value = true
}

const handleSaveTenant = async () => {
  if (!selectedLease.value || !tenantForm.fullName) return
  isSavingTenant.value = true
  try {
    await post(`/leases/${selectedLease.value.id}/tenants`, {
      fullName: tenantForm.fullName,
      phoneNumber: tenantForm.phoneNumber || undefined,
      idCardNumber: tenantForm.idCardNumber || undefined,
    })
    toast.success('Thêm người lưu trú thành công!')
    isAddTenantModalOpen.value = false
    await fetchLeases()
  } catch (err: any) {
    toast.error(err.message || 'Không thể thêm người lưu trú.')
  } finally {
    isSavingTenant.value = false
  }
}

const handleRemoveTenant = async (leaseId: string, tenantId: string, name: string) => {
  if (!confirm(`Xác nhận xóa thành viên "${name}" khỏi hợp đồng?`)) return
  try {
    await deleteApi(`/leases/${leaseId}/tenants/${tenantId}`)
    toast.success('Đã xóa người lưu trú.')
    await fetchLeases()
  } catch (err: any) {
    toast.error(err.message || 'Không thể xóa người lưu trú.')
  }
}

// Terminate Lease & Settlement
const isTerminateModalOpen = ref(false)
const isTerminating = ref(false)
const terminationPreview = ref<LeaseTerminationPreviewResponse | null>(null)

const terminateForm = reactive({
  finalElectricityReading: 0,
  finalWaterReading: 0,
  depositDeducted: 0,
  endReason: '',
})

const openTerminateModal = (l: LeaseResponse) => {
  selectedLease.value = l
  terminateForm.finalElectricityReading = 0
  terminateForm.finalWaterReading = 0
  terminateForm.depositDeducted = 0
  terminateForm.endReason = 'Hết hạn hợp đồng'
  terminationPreview.value = null
  isTerminateModalOpen.value = true
  fetchTerminationPreview()
}

const fetchTerminationPreview = async () => {
  if (!selectedLease.value) return
  try {
    const data = await get<LeaseTerminationPreviewResponse>(`/leases/${selectedLease.value.id}/termination-preview`, {
      finalElectricityReading: terminateForm.finalElectricityReading || 0,
      finalWaterReading: terminateForm.finalWaterReading || 0,
      depositDeducted: terminateForm.depositDeducted || 0,
    })
    terminationPreview.value = data
  } catch {
    // Ignore preview errors during typing
  }
}

const handleConfirmTerminate = async () => {
  if (!selectedLease.value) return
  if (!confirm(`Xác nhận hoàn tất trả phòng và thanh lý hợp đồng cho phòng ${selectedLease.value.roomNumber}? Phòng sẽ được chuyển về trạng thái Trống.`)) return
  isTerminating.value = true
  try {
    await post(`/leases/${selectedLease.value.id}/terminate`, {
      finalElectricityReading: terminateForm.finalElectricityReading,
      finalWaterReading: terminateForm.finalWaterReading,
      depositDeducted: terminateForm.depositDeducted || 0,
      endReason: terminateForm.endReason || undefined,
    })
    toast.success('Thanh lý hợp đồng thành công! Phòng đã chuyển sang trạng thái Trống.')
    isTerminateModalOpen.value = false
    await fetchLeases()
  } catch (err: any) {
    toast.error(err.message || 'Không thể kết thúc hợp đồng.')
  } finally {
    isTerminating.value = false
  }
}

// Extension Requests Approval / Rejection
const isApprovingExtensionId = ref<string | null>(null)
const handleApproveExtension = async (ext: ExtensionRequestResponse) => {
  isApprovingExtensionId.value = ext.id
  try {
    await put(`/extension-requests/${ext.id}/approve`, {})
    toast.success(`Đã duyệt gia hạn hợp đồng phòng ${ext.roomNumber} đến ngày ${ext.requestedEndDate}!`)
    await fetchExtensions()
  } catch (err: any) {
    toast.error(err.message || 'Không thể duyệt gia hạn.')
  } finally {
    isApprovingExtensionId.value = null
  }
}

const isRejectExtModalOpen = ref(false)
const selectedExt = ref<ExtensionRequestResponse | null>(null)
const rejectExtReason = ref('')
const isRejectingExt = ref(false)

const openRejectExtensionModal = (ext: ExtensionRequestResponse) => {
  selectedExt.value = ext
  rejectExtReason.value = ''
  isRejectExtModalOpen.value = true
}

const handleConfirmRejectExtension = async () => {
  if (!selectedExt.value) return
  isRejectingExt.value = true
  try {
    await put(`/extension-requests/${selectedExt.value.id}/reject`, {
      ownerNote: rejectExtReason.value || undefined,
    })
    toast.success('Đã từ chối yêu cầu gia hạn.')
    isRejectExtModalOpen.value = false
    await fetchExtensions()
  } catch (err: any) {
    toast.error(err.message || 'Không thể từ chối gia hạn.')
  } finally {
    isRejectingExt.value = false
  }
}

onMounted(() => {
  fetchLeases()
  fetchExtensions()
})
</script>
