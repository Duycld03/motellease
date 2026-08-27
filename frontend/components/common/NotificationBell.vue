<template>
  <div class="relative" ref="bellRef">
    <button
      type="button"
      class="relative p-2 text-slate-600 hover:text-slate-900 rounded-lg hover:bg-slate-100 transition-colors"
      @click="isOpen = !isOpen"
      :aria-label="$t('nav.notifications')"
    >
      <!-- Bell Icon -->
      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
      </svg>

      <!-- Badge -->
      <span
        v-if="unreadCount > 0"
        class="absolute top-1.5 right-1.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white shadow-sm ring-2 ring-white"
      >
        {{ unreadCount > 99 ? '99+' : unreadCount }}
      </span>
    </button>

    <!-- Dropdown -->
    <div
      v-if="isOpen"
      class="absolute right-0 mt-2 w-80 sm:w-96 bg-white rounded-2xl shadow-xl border border-slate-100 py-2 z-50 animate-in fade-in zoom-in-95 duration-100"
    >
      <div class="flex items-center justify-between px-4 py-2 border-b border-slate-100">
        <div class="flex items-center gap-2">
          <h4 class="text-sm font-semibold text-slate-800">
            {{ $t('notification.title') }}
          </h4>
          <span
            v-if="unreadCount > 0"
            class="px-1.5 py-0.5 text-[10px] font-bold bg-primary-100 text-primary-700 rounded-full"
          >
            {{ unreadCount }}
          </span>
        </div>
        <button
          v-if="unreadCount > 0"
          type="button"
          class="text-xs text-primary-600 hover:text-primary-700 font-medium"
          @click="markAllAsRead"
        >
          {{ $t('notification.markAllAsRead') }}
        </button>
      </div>

      <div class="max-h-80 overflow-y-auto divide-y divide-slate-50">
        <div
          v-if="notifications.length === 0"
          class="py-8 text-center text-xs text-slate-400"
        >
          {{ $t('notification.empty') }}
        </div>

        <div
          v-for="item in notifications"
          :key="item.id"
          :class="[
            'p-3.5 hover:bg-slate-50 transition-colors cursor-pointer flex items-start gap-3',
            !item.isRead ? 'bg-primary-50/30' : '',
          ]"
          @click="handleClickNotification(item)"
        >
          <div
            :class="[
              'w-2 h-2 mt-1.5 rounded-full flex-shrink-0',
              !item.isRead ? 'bg-primary-500' : 'bg-transparent',
            ]"
          />
          <div class="flex-1 min-w-0">
            <h5 class="text-xs font-semibold text-slate-800 truncate">
              {{ item.title }}
            </h5>
            <p class="text-xs text-slate-600 mt-0.5 line-clamp-2 leading-relaxed">
              {{ item.body }}
            </p>
            <span class="text-[10px] text-slate-400 mt-1 block">
              {{ formatRelativeTime(item.createdAt) }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onClickOutside } from '@vueuse/core'
import type { InAppNotification } from '~/types/api'

const notificationStore = useNotificationStore()
const { formatRelativeTime } = useFormat()
const { put } = useApi()

const isOpen = ref(false)
const bellRef = ref<HTMLElement | null>(null)

onClickOutside(bellRef, () => {
  isOpen.value = false
})

const notifications = computed(() => notificationStore.notifications)
const unreadCount = computed(() => notificationStore.unreadCount)

const markAllAsRead = async () => {
  try {
    await put('/notifications/read-all')
  } catch {}
  notificationStore.markAllAsReadLocally()
}

const handleClickNotification = async (item: InAppNotification) => {
  if (!item.isRead) {
    try {
      await put(`/notifications/${item.id}/read`)
    } catch {}
    notificationStore.markAsReadLocally(item.id)
  }
  if (item.targetUrl) {
    isOpen.value = false
    navigateTo(item.targetUrl)
  }
}
</script>
