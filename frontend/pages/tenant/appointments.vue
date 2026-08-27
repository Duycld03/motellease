<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.myAppointments') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">Danh sách các lịch hẹn xem phòng trọ bạn đã gửi</p>
      </div>

      <NuxtLink to="/search" class="btn-primary !text-xs !py-2 !px-4">
        🔍 Tìm phòng mới
      </NuxtLink>
    </div>

    <!-- Filter status pills -->
    <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
      <button
        type="button"
        :class="[
          'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all',
          selectedStatus === '' ? 'bg-slate-900 text-white' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
        ]"
        @click="changeStatus('')"
      >
        Tất cả
      </button>
      <button
        v-for="st in ['Pending', 'Approved', 'Rejected', 'Cancelled']"
        :key="st"
        type="button"
        :class="[
          'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all',
          selectedStatus === st ? 'bg-slate-900 text-white' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
        ]"
        @click="changeStatus(st)"
      >
        {{ $t(`enums.RequestStatus.${st}`) }}
      </button>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="lg" :text="$t('common.loading')" />
    </div>

    <!-- Empty state -->
    <div v-else-if="items.length === 0" class="py-16 text-center bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">Không có lịch hẹn nào</p>
      <p class="text-xs text-slate-400 mt-1">Hãy khám phá các khu trọ và đặt lịch xem trực tiếp</p>
    </div>

    <!-- Appointments list -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="item in items"
        :key="item.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-4"
      >
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <StatusBadge type="RequestStatus" :status="item.status" />
            <span class="text-[11px] text-slate-400">Tạo: {{ formatRelativeTime(item.createdAt) }}</span>
          </div>

          <div>
            <h3 class="text-sm font-bold text-slate-900 dark:text-white">
              {{ item.boardingHouseName }}
            </h3>
            <span class="text-xs font-semibold text-primary-600 dark:text-primary-400 block mt-0.5">
              Phòng {{ item.roomNumber }}
            </span>
          </div>

          <!-- Appointment date block -->
          <div class="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-100 dark:border-slate-800 space-y-1">
            <span class="text-[10px] text-slate-400 uppercase font-bold block">Thời gian xem phòng</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 flex items-center gap-1.5">
              <span>📅</span>
              <span>{{ formatDate(item.appointmentDate, { dateStyle: 'full', timeStyle: 'short' }) }}</span>
            </span>
          </div>

          <!-- Note -->
          <p v-if="item.note" class="text-xs text-slate-500 dark:text-slate-400 italic">
            "{{ item.note }}"
          </p>

          <!-- Reason for cancel / rejection -->
          <div v-if="item.reasonForCancel" class="p-2.5 bg-red-50 dark:bg-red-950/30 rounded-xl text-xs text-red-700 dark:text-red-400">
            <span class="font-bold">Lý do từ chối/hủy:</span> {{ item.reasonForCancel }}
          </div>
        </div>

        <!-- Action buttons -->
        <div class="pt-3 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between gap-2">
          <NuxtLink
            :to="`/boarding-houses/${item.boardingHouseId}`"
            class="text-xs font-semibold text-primary-600 dark:text-primary-400 hover:underline"
          >
            Xem khu trọ →
          </NuxtLink>

          <BaseButton
            v-if="item.status === 'Pending'"
            variant="outline"
            size="sm"
            class="!text-xs !py-1 text-red-600 border-red-200 hover:bg-red-50"
            @click="openCancelModal(item)"
          >
            Hủy lịch hẹn
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Cancel Appointment -->
    <BaseModal
      v-model="isCancelModalOpen"
      title="Hủy lịch hẹn xem phòng"
      max-width="sm"
    >
      <form @submit.prevent="handleConfirmCancel" class="space-y-4">
        <p class="text-xs text-slate-600 dark:text-slate-300">
          Bạn có chắc chắn muốn hủy lịch hẹn xem phòng <strong>{{ selectedAppointment?.roomNumber }}</strong> tại khu trọ <strong>{{ selectedAppointment?.boardingHouseName }}</strong>?
        </p>

        <div>
          <label class="block text-xs font-medium text-slate-700 dark:text-slate-300 mb-1">
            Lý do hủy lịch (Tùy chọn)
          </label>
          <textarea
            v-model="cancelReason"
            rows="3"
            class="input-field !text-xs !py-2"
            placeholder="VD: Em bận việc đột xuất, xin phép hẹn lại dịp khác..."
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isCancelModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="danger" size="sm" type="submit" :loading="isCancelling">
            Xác nhận hủy lịch
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
import type { AppointmentResponse, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'tenant',
})

const { get, put } = useApi()
const { formatDate, formatRelativeTime } = useFormat()
const toast = useToast()

const isLoading = ref(true)
const items = ref<AppointmentResponse[]>([])
const selectedStatus = ref('')

const isCancelModalOpen = ref(false)
const selectedAppointment = ref<AppointmentResponse | null>(null)
const cancelReason = ref('')
const isCancelling = ref(false)

const fetchAppointments = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<AppointmentResponse>>('/appointments', {
      status: selectedStatus.value || undefined,
      page: 1,
      pageSize: 50,
    })
    items.value = data?.items || []
  } catch {
    items.value = []
  } finally {
    isLoading.value = false
  }
}

const changeStatus = (st: string) => {
  selectedStatus.value = st
  fetchAppointments()
}

const openCancelModal = (apt: AppointmentResponse) => {
  selectedAppointment.value = apt
  cancelReason.value = ''
  isCancelModalOpen.value = true
}

const handleConfirmCancel = async () => {
  if (!selectedAppointment.value) return
  isCancelling.value = true
  try {
    await put(`/appointments/${selectedAppointment.value.id}/cancel`, {
      reason: cancelReason.value || undefined,
    })
    toast.success('Đã hủy lịch hẹn xem phòng!')
    isCancelModalOpen.value = false
    await fetchAppointments()
  } catch (err: any) {
    toast.error(err.message || 'Không thể hủy lịch hẹn.')
  } finally {
    isCancelling.value = false
  }
}

onMounted(() => {
  fetchAppointments()
})
</script>
