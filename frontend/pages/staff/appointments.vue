<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('staffAppointments.title') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">{{ $t('staffAppointments.subtitle') }}</p>
      </div>
    </div>

    <!-- Status filter pills -->
    <div class="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
      <button
        type="button"
        :class="[
          'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all',
          selectedStatus === '' ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
        ]"
        @click="changeStatus('')"
      >
        {{ $t('common.allCount', { count: totalCount }) }}
      </button>
      <button
        v-for="st in ['Pending', 'Accepted', 'Rejected', 'Cancelled']"
        :key="st"
        type="button"
        :class="[
          'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all',
          selectedStatus === st ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-sm' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800',
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
      <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ $t('staffAppointments.emptyTitle') }}</p>
      <p class="text-xs text-slate-400 mt-1">{{ $t('staffAppointments.emptySubtitle') }}</p>
    </div>

    <!-- Appointments grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="apt in items"
        :key="apt.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col justify-between space-y-4"
      >
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <StatusBadge type="RequestStatus" :status="apt.status" />
            <span class="text-[11px] text-slate-400">{{ formatRelativeTime(apt.createdAt) }}</span>
          </div>

          <!-- Tenant Info -->
          <div class="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl space-y-1">
            <span class="text-[10px] text-slate-400 uppercase font-bold block">{{ $t('staffAppointments.tenantInfoTitle') }}</span>
            <div class="flex items-center justify-between">
              <span class="text-xs font-bold text-slate-900 dark:text-white">{{ apt.tenantFullName }}</span>
              <a
                v-if="apt.tenantPhoneNumber"
                :href="`tel:${apt.tenantPhoneNumber}`"
                class="text-xs font-semibold text-sky-600 dark:text-sky-400 hover:underline flex items-center gap-1"
              >
                📞 {{ apt.tenantPhoneNumber }}
              </a>
            </div>
          </div>

          <!-- Room & House -->
          <div>
            <span class="text-xs text-slate-500 block">{{ apt.boardingHouseName }}</span>
            <span class="text-sm font-bold text-slate-900 dark:text-white block mt-0.5">
              {{ $t('property.room') }} {{ apt.roomNumber }}
            </span>
          </div>

          <!-- Appointment Date -->
          <div class="p-3 bg-sky-50/50 dark:bg-sky-950/30 rounded-xl border border-sky-100 dark:border-sky-900 space-y-0.5">
            <span class="text-[10px] text-sky-700 dark:text-sky-400 font-semibold block">{{ $t('staffAppointments.viewingTime') }}</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 block">
              📅 {{ formatDate(apt.appointmentDate, { dateStyle: 'full', timeStyle: 'short' }) }}
            </span>
          </div>

          <!-- Note -->
          <p v-if="apt.note" class="text-xs text-slate-500 dark:text-slate-400 italic">
            "{{ apt.note }}"
          </p>

          <!-- Rejection / Cancellation Reason -->
          <div v-if="apt.reasonForCancel" class="p-2.5 bg-red-50 dark:bg-red-950/30 rounded-xl text-xs text-red-700 dark:text-red-400">
            <span class="font-bold">{{ $t('staffAppointments.rejectionOrCancelReason') }}</span> {{ apt.reasonForCancel }}
          </div>
        </div>

        <!-- Approval / Rejection Actions -->
        <div v-if="apt.status === 'Pending'" class="pt-3 border-t border-slate-100 dark:border-slate-800 grid grid-cols-2 gap-2">
          <BaseButton
            variant="outline"
            size="sm"
            class="!text-xs !py-1.5 text-red-600 border-red-200 hover:bg-red-50"
            @click="openRejectModal(apt)"
          >
            {{ $t('staffAppointments.rejectBtn') }}
          </BaseButton>
          <BaseButton
            variant="primary"
            size="sm"
            class="!text-xs !py-1.5 !bg-sky-600 hover:!bg-sky-700"
            @click="handleApprove(apt.id)"
          >
            {{ $t('staffAppointments.approveBtn') }}
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Reject Appointment -->
    <BaseModal
      v-model="isRejectModalOpen"
      :title="$t('staffAppointments.rejectModalTitle')"
      max-width="sm"
    >
      <form @submit.prevent="handleConfirmReject" class="space-y-4">
        <p class="text-xs text-slate-600 dark:text-slate-300">
          {{ $t('staffAppointments.rejectModalPrompt', { name: selectedAppointment?.tenantFullName }) }}
        </p>

        <div>
          <label class="block text-xs font-medium text-slate-700 dark:text-slate-300 mb-1">
            {{ $t('staffAppointments.rejectReasonLabel') }} <span class="text-red-500">*</span>
          </label>
          <textarea
            v-model="rejectReason"
            rows="3"
            class="input-field !text-xs !py-2"
            :placeholder="$t('staffAppointments.rejectReasonPlaceholder')"
            required
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isRejectModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="danger" size="sm" type="submit" :loading="isRejecting">
            {{ $t('staffAppointments.confirmReject') }}
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
  layout: 'staff',
})

const { get, put } = useApi()
const { formatDate, formatRelativeTime } = useFormat()
const { t } = useI18n()
const toast = useToast()

const isLoading = ref(true)
const items = ref<AppointmentResponse[]>([])
const totalCount = ref(0)
const selectedStatus = ref('')

const isRejectModalOpen = ref(false)
const selectedAppointment = ref<AppointmentResponse | null>(null)
const rejectReason = ref('')
const isRejecting = ref(false)

const fetchAppointments = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<AppointmentResponse>>('/appointments', {
      status: selectedStatus.value || undefined,
      page: 1,
      pageSize: 50,
    })
    items.value = data?.items || []
    if (!selectedStatus.value) {
      totalCount.value = data?.totalCount ?? items.value.length
    }
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

const handleApprove = async (appointmentId: string) => {
  try {
    await put(`/appointments/${appointmentId}/approve`)
    toast.success(t('messages.approveAppointmentSuccess'))
    await fetchAppointments()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

const openRejectModal = (apt: AppointmentResponse) => {
  selectedAppointment.value = apt
  rejectReason.value = ''
  isRejectModalOpen.value = true
}

const handleConfirmReject = async () => {
  if (!selectedAppointment.value || !rejectReason.value) return
  isRejecting.value = true
  try {
    await put(`/appointments/${selectedAppointment.value.id}/reject`, {
      reason: rejectReason.value,
    })
    toast.success(t('messages.rejectAppointmentSuccess'))
    isRejectModalOpen.value = false
    await fetchAppointments()
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isRejecting.value = false
  }
}

onMounted(() => {
  fetchAppointments()
})
</script>
