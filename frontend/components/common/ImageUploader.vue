<template>
  <div class="space-y-4">
    <!-- Dropzone -->
    <div
      v-if="!disabled && (maxCount === 0 || images.length < maxCount)"
      :class="[
        'border-2 border-dashed rounded-2xl p-6 text-center transition-colors cursor-pointer',
        isDragging ? 'border-primary-500 bg-primary-50/50' : 'border-slate-300 hover:border-primary-400 bg-slate-50/50',
      ]"
      @dragover.prevent="isDragging = true"
      @dragleave.prevent="isDragging = false"
      @drop.prevent="handleDrop"
      @click="triggerFileInput"
    >
      <input
        ref="fileInputRef"
        type="file"
        multiple
        accept="image/png, image/jpeg, image/webp"
        class="hidden"
        @change="handleFileSelect"
      />

      <div class="flex flex-col items-center justify-center gap-2">
        <div class="w-12 h-12 rounded-full bg-primary-100 text-primary-600 flex items-center justify-center">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        </div>
        <div>
          <p class="text-sm font-semibold text-slate-700">
            {{ $t('common.dragDropImages') }} <span class="text-primary-600">{{ $t('common.chooseFromDevice') }}</span>
          </p>
          <p class="text-xs text-slate-400 mt-0.5">
            {{ $t('common.uploadFormatsHint') }}
          </p>
        </div>
      </div>
    </div>

    <!-- Uploading Progress / Indicator -->
    <div v-if="isUploading" class="p-4 bg-primary-50 rounded-xl flex items-center gap-3 text-primary-800 text-xs font-medium">
      <LoadingSpinner size="sm" />
      <span>{{ $t('common.uploadingImages') }}</span>
    </div>

    <!-- Preview Grid -->
    <div v-if="images.length > 0" class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
      <div
        v-for="img in images"
        :key="img.id"
        class="group relative aspect-[4/3] rounded-xl overflow-hidden border border-slate-200 bg-slate-100 shadow-sm"
      >
        <img :src="img.url" alt="Property image" class="w-full h-full object-cover" />

        <!-- Primary Badge -->
        <div v-if="img.isPrimary" class="absolute top-2 left-2 px-2 py-0.5 rounded-md bg-primary-600 text-white text-[10px] font-bold shadow-sm">
          {{ $t('common.primaryImage') }}
        </div>

        <!-- Action Overlay -->
        <div
          v-if="!disabled"
          class="absolute inset-0 bg-slate-900/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2"
        >
          <button
            v-if="!img.isPrimary && boardingHouseId"
            type="button"
            class="p-1.5 bg-white/90 text-slate-700 hover:text-primary-600 rounded-lg text-xs font-semibold shadow-sm transition-colors"
            :title="$t('common.setAsPrimary')"
            @click.stop="setPrimary(img.id)"
          >
            ★
          </button>
          <button
            type="button"
            class="p-1.5 bg-red-600/90 text-white hover:bg-red-700 rounded-lg text-xs font-semibold shadow-sm transition-colors"
            :title="$t('common.deleteImage')"
            @click.stop="deleteImage(img.id)"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import LoadingSpinner from './LoadingSpinner.vue'
import type { ImageResponse } from '~/types/api'

const props = withDefaults(
  defineProps<{
    images: ImageResponse[]
    boardingHouseId?: string
    maxCount?: number
    disabled?: boolean
  }>(),
  {
    images: () => [],
    boardingHouseId: undefined,
    maxCount: 0,
    disabled: false,
  }
)

const emit = defineEmits<{
  (e: 'update:images', images: ImageResponse[]): void
  (e: 'uploaded', image: ImageResponse): void
  (e: 'deleted', id: string): void
}>()

const { $api, put, delete: deleteApi } = useApi()
const { t } = useI18n()
const toast = useToast()

const fileInputRef = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)
const isUploading = ref(false)

const triggerFileInput = () => {
  if (fileInputRef.value) {
    fileInputRef.value.click()
  }
}

const handleFileSelect = async (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files && target.files.length > 0) {
    await uploadFiles(Array.from(target.files))
    target.value = ''
  }
}

const handleDrop = async (event: DragEvent) => {
  isDragging.value = false
  if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
    await uploadFiles(Array.from(event.dataTransfer.files))
  }
}

const uploadFiles = async (files: File[]) => {
  isUploading.value = true
  try {
    for (const file of files) {
      if (file.size > 5 * 1024 * 1024) {
        toast.error(t('messages.actionFailed'))
        continue
      }

      const formData = new FormData()
      formData.append('file', file)

      if (props.boardingHouseId) {
        const result = await $api<ImageResponse>(
          `/my/boarding-houses/${props.boardingHouseId}/images`,
          {
            method: 'POST',
            body: formData,
          }
        )
        const updated = [...props.images, result]
        emit('update:images', updated)
        emit('uploaded', result)
      } else {
        const result = await $api<{ url: string; publicId: string }>('/images', {
          method: 'POST',
          body: formData,
        })
        const newImg: ImageResponse = {
          id: result.publicId,
          url: result.url,
          isPrimary: props.images.length === 0,
        }
        const updated = [...props.images, newImg]
        emit('update:images', updated)
        emit('uploaded', newImg)
      }
    }
    toast.success(t('messages.uploadImageSuccess'))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  } finally {
    isUploading.value = false
  }
}

const setPrimary = async (imageId: string) => {
  if (!props.boardingHouseId) return
  try {
    await put(`/my/boarding-houses/${props.boardingHouseId}/images/${imageId}/primary`)
    const updated = props.images.map((img) => ({
      ...img,
      isPrimary: img.id === imageId,
    }))
    emit('update:images', updated)
    toast.success(t('messages.setPrimaryImageSuccess'))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}

const deleteImage = async (imageId: string) => {
  try {
    if (props.boardingHouseId) {
      await deleteApi(`/my/boarding-houses/${props.boardingHouseId}/images/${imageId}`)
    } else {
      await deleteApi(`/images/${encodeURIComponent(imageId)}`)
    }
    const updated = props.images.filter((img) => img.id !== imageId)
    emit('update:images', updated)
    emit('deleted', imageId)
    toast.success(t('messages.deleteImageSuccess'))
  } catch (err: any) {
    toast.error(err.message || t('messages.actionFailed'))
  }
}
</script>
