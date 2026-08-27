<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">Báo cáo Vi phạm & Khiếu nại</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Tiếp nhận và xử lý các báo cáo vi phạm nội dung đối với tin đăng khu trọ và đánh giá của người dùng
        </p>
      </div>
      <BaseButton variant="outline" size="sm" @click="fetchReports">
        🔄 Làm mới
      </BaseButton>
    </div>

    <!-- Filters -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
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
          Tất cả ({{ reports.length }})
        </button>
        <button
          v-for="st in ['Pending', 'Resolved', 'Dismissed']"
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
          {{ $t(`enums.ReportStatus.${st}`) }}
        </button>
      </div>

      <select v-model="filterTargetType" class="input-field !text-xs !py-1.5 w-44" @change="fetchReports">
        <option value="">Tất cả đối tượng</option>
        <option value="Listing">Bài đăng (Listing)</option>
        <option value="Review">Đánh giá (Review)</option>
      </select>
    </div>

    <!-- Reports List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="filteredReports.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <p class="font-medium text-slate-500 dark:text-slate-400">Không có báo cáo vi phạm nào.</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="rep in filteredReports"
        :key="rep.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="px-2 py-0.5 rounded-md text-[10px] font-bold bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 uppercase">
                {{ rep.targetType }}
              </span>
              <span class="text-sm font-bold text-slate-900 dark:text-white">{{ rep.reason }}</span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              Người báo cáo: <strong class="text-slate-800 dark:text-slate-200">{{ rep.reporterFullName }}</strong> · Gửi lúc: {{ formatRelativeTime(rep.createdAt) }}
            </p>
          </div>

          <StatusBadge type="ReportStatus" :status="rep.status" />
        </div>

        <p v-if="rep.details" class="text-xs text-slate-700 dark:text-slate-300 bg-slate-50 dark:bg-slate-800/60 p-3.5 rounded-xl">
          {{ rep.details }}
        </p>

        <div v-if="rep.resolution" class="p-3 bg-emerald-50 dark:bg-emerald-950/30 rounded-xl text-xs text-emerald-800 dark:text-emerald-300">
          <strong>Kết quả xử lý:</strong> {{ rep.resolution }}
          <span v-if="rep.processedByFullName" class="block text-[11px] text-emerald-600 dark:text-emerald-400 mt-0.5">
            Bởi: {{ rep.processedByFullName }} ({{ rep.processedAt ? formatRelativeTime(rep.processedAt) : '' }})
          </span>
        </div>

        <!-- Actions -->
        <div v-if="rep.status === 'Pending'" class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800">
          <BaseButton
            variant="outline"
            size="sm"
            class="!text-xs"
            @click="openResolutionModal(rep, 'dismiss')"
          >
            ✕ Bác bỏ báo cáo
          </BaseButton>
          <BaseButton
            variant="danger"
            size="sm"
            class="!text-xs"
            @click="openResolutionModal(rep, 'resolve')"
          >
            ⚡ Xử lý vi phạm
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Resolve / Dismiss -->
    <BaseModal
      v-model="isModalOpen"
      :title="actionType === 'resolve' ? 'Xử lý Vi phạm Báo cáo' : 'Bác bỏ Báo cáo'"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitResolution" class="space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Ghi chú kết quả xử lý
          </label>
          <textarea
            v-model="resolutionNote"
            rows="3"
            class="input-field !text-xs !py-2"
            :placeholder="actionType === 'resolve' ? 'VD: Đã ẩn bài đăng và cảnh cáo chủ phòng trọ...' : 'VD: Báo cáo không đủ căn cứ vi phạm...'"
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton
            :variant="actionType === 'resolve' ? 'danger' : 'primary'"
            size="sm"
            type="submit"
            :loading="isSubmitting"
          >
            {{ actionType === 'resolve' ? 'Xác nhận xử lý' : 'Xác nhận bác bỏ' }}
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
import type { PagedResult, ReportResponse } from '~/types/api'

definePageMeta({
  layout: 'admin',
})

const { get, put } = useApi()
const { formatRelativeTime } = useFormat()
const toast = useToast()

const isLoading = ref(true)
const reports = ref<ReportResponse[]>([])
const filterStatus = ref('')
const filterTargetType = ref('')

const filteredReports = computed(() => {
  let list = reports.value
  if (filterStatus.value) list = list.filter((r) => r.status === filterStatus.value)
  if (filterTargetType.value) list = list.filter((r) => r.targetType === filterTargetType.value)
  return list
})

const fetchReports = async () => {
  isLoading.value = true
  try {
    const data = await get<PagedResult<ReportResponse>>('/reports', {
      targetType: filterTargetType.value || undefined,
      status: filterStatus.value || undefined,
      pageSize: 50,
    })
    reports.value = data.items || []
  } catch {
    reports.value = []
  } finally {
    isLoading.value = false
  }
}

// Resolution Modal
const isModalOpen = ref(false)
const selectedReport = ref<ReportResponse | null>(null)
const actionType = ref<'resolve' | 'dismiss'>('resolve')
const resolutionNote = ref('')
const isSubmitting = ref(false)

const openResolutionModal = (r: ReportResponse, type: 'resolve' | 'dismiss') => {
  selectedReport.value = r
  actionType.value = type
  resolutionNote.value = ''
  isModalOpen.value = true
}

const handleSubmitResolution = async () => {
  if (!selectedReport.value) return
  isSubmitting.value = true
  try {
    const endpoint = actionType.value === 'resolve' ? 'resolve' : 'dismiss'
    await put(`/reports/${selectedReport.value.id}/${endpoint}`, {
      resolution: resolutionNote.value || undefined,
    })
    toast.success(actionType.value === 'resolve' ? 'Đã xử lý vi phạm thành công!' : 'Đã bác bỏ báo cáo.')
    isModalOpen.value = false
    await fetchReports()
  } catch (err: any) {
    toast.error(err.message || 'Không thể xử lý báo cáo.')
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  fetchReports()
})
</script>
