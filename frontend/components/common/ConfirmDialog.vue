<template>
  <BaseModal
    :model-value="modelValue"
    :title="title"
    max-width="sm"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <div class="space-y-3">
      <p class="text-sm text-slate-600 leading-relaxed">
        {{ message }}
      </p>
    </div>

    <template #footer>
      <BaseButton
        variant="outline"
        size="sm"
        :disabled="loading"
        @click="$emit('update:modelValue', false)"
      >
        {{ cancelText || $t('common.cancel') }}
      </BaseButton>
      <BaseButton
        :variant="confirmVariant || 'danger'"
        size="sm"
        :loading="loading"
        @click="$emit('confirm')"
      >
        {{ confirmText || $t('common.confirm') }}
      </BaseButton>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import BaseModal from './BaseModal.vue'
import BaseButton from './BaseButton.vue'

withDefaults(
  defineProps<{
    modelValue: boolean
    title: string
    message: string
    confirmText?: string
    cancelText?: string
    confirmVariant?: 'primary' | 'danger'
    loading?: boolean
  }>(),
  {
    confirmText: '',
    cancelText: '',
    confirmVariant: 'danger',
    loading: false,
  }
)

defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'confirm'): void
}>()
</script>
