<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.facilities') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Danh mục tiện ích chuẩn của toàn hệ thống phục vụ tìm kiếm và niêm yết phòng trọ
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchFacilities">
          🔄 Làm mới
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openCreateModal">
          + Thêm tiện ích
        </BaseButton>
      </div>
    </div>

    <!-- Facilities List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="facilities.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <p class="font-medium text-slate-500 dark:text-slate-400">Chưa có tiện ích nào trong danh mục.</p>
    </div>

    <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="f in facilities"
        :key="f.id"
        class="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-3 transition-all"
      >
        <div class="flex items-start justify-between gap-3">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-primary-50 dark:bg-primary-950/60 text-primary-700 dark:text-primary-300 flex items-center justify-center font-bold text-lg">
              ✨
            </div>
            <div>
              <h3 class="text-sm font-bold text-slate-900 dark:text-white">{{ f.name }}</h3>
              <span class="text-[11px] text-slate-400 font-mono">#{{ f.codeName }}</span>
            </div>
          </div>

          <span class="px-2 py-0.5 rounded-md text-[10px] font-bold bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400">
            {{ f.inUseByRoomTypesCount }} loại phòng
          </span>
        </div>

        <p v-if="f.description" class="text-xs text-slate-500 dark:text-slate-400 line-clamp-2">
          {{ f.description }}
        </p>

        <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" class="!text-xs" @click="openEditModal(f)">
            ✏️ Sửa
          </BaseButton>
          <BaseButton variant="ghost" size="sm" class="!text-xs text-red-500 hover:text-red-700" @click="handleDelete(f)">
            Xóa
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Create / Edit Facility -->
    <BaseModal
      v-model="isModalOpen"
      :title="isEditing ? 'Chỉnh sửa Tiện ích' : 'Thêm Tiện ích mới vào Danh mục'"
      max-width="md"
    >
      <form @submit.prevent="handleSubmitFacility" class="space-y-4">
        <BaseInput
          v-model="facilityForm.name"
          label="Tên tiện ích"
          placeholder="VD: Điều hòa hai chiều"
          required
        />

        <div class="grid grid-cols-2 gap-3">
          <BaseInput
            v-model="facilityForm.codeName"
            label="Mã định danh (CodeName)"
            placeholder="air_conditioner"
            required
          />
          <BaseInput
            v-model="facilityForm.iconKey"
            label="Icon Key"
            placeholder="ac"
          />
        </div>

        <div>
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Mô tả tiện ích
          </label>
          <textarea
            v-model="facilityForm.description"
            rows="3"
            class="input-field !text-xs !py-2"
            placeholder="Mô tả công năng của tiện ích..."
          />
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmitting">
            {{ isEditing ? 'Cập nhật' : 'Tạo tiện ích' }}
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
import type { FacilityDetailResponse } from '~/types/api'

definePageMeta({
  layout: 'admin',
})

const { get, post, put, delete: deleteApi } = useApi()
const toast = useToast()

const isLoading = ref(true)
const facilities = ref<FacilityDetailResponse[]>([])

const fetchFacilities = async () => {
  isLoading.value = true
  try {
    const data = await get<FacilityDetailResponse[]>('/admin/facilities')
    facilities.value = data || []
  } catch {
    facilities.value = []
  } finally {
    isLoading.value = false
  }
}

// Modal State
const isModalOpen = ref(false)
const isEditing = ref(false)
const editingFacilityId = ref<string | null>(null)
const isSubmitting = ref(false)

const facilityForm = reactive({
  name: '',
  codeName: '',
  iconKey: '',
  description: '',
})

const openCreateModal = () => {
  isEditing.value = false
  editingFacilityId.value = null
  facilityForm.name = ''
  facilityForm.codeName = ''
  facilityForm.iconKey = ''
  facilityForm.description = ''
  isModalOpen.value = true
}

const openEditModal = (f: FacilityDetailResponse) => {
  isEditing.value = true
  editingFacilityId.value = f.id
  facilityForm.name = f.name
  facilityForm.codeName = f.codeName
  facilityForm.iconKey = f.iconKey || ''
  facilityForm.description = f.description || ''
  isModalOpen.value = true
}

const handleSubmitFacility = async () => {
  isSubmitting.value = true
  try {
    if (isEditing.value && editingFacilityId.value) {
      await put(`/admin/facilities/${editingFacilityId.value}`, {
        name: facilityForm.name,
        codeName: facilityForm.codeName,
        iconKey: facilityForm.iconKey || undefined,
        description: facilityForm.description || undefined,
      })
      toast.success('Cập nhật tiện ích thành công!')
    } else {
      await post('/admin/facilities', {
        name: facilityForm.name,
        codeName: facilityForm.codeName,
        iconKey: facilityForm.iconKey || undefined,
        description: facilityForm.description || undefined,
      })
      toast.success('Thêm tiện ích mới thành công!')
    }
    isModalOpen.value = false
    await fetchFacilities()
  } catch (err: any) {
    toast.error(err.message || 'Không thể lưu tiện ích.')
  } finally {
    isSubmitting.value = false
  }
}

const handleDelete = async (f: FacilityDetailResponse) => {
  if (!confirm(`Xóa tiện ích "${f.name}" khỏi danh mục hệ thống?`)) return
  try {
    await deleteApi(`/admin/facilities/${f.id}`)
    toast.success('Đã xóa tiện ích.')
    await fetchFacilities()
  } catch (err: any) {
    toast.error(err.message || 'Không thể xóa tiện ích.')
  }
}

onMounted(() => {
  fetchFacilities()
})
</script>
