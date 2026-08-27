import { defineStore } from 'pinia'
import type { InAppNotification } from '~/types/api'

export const useNotificationStore = defineStore('notification', () => {
  const notifications = ref<InAppNotification[]>([])
  const unreadCount = ref(0)
  const isLoading = ref(false)

  const setNotifications = (items: InAppNotification[], unread: number) => {
    notifications.value = items
    unreadCount.value = unread
  }

  const addNotification = (item: InAppNotification) => {
    notifications.value.unshift(item)
    if (!item.isRead) {
      unreadCount.value += 1
    }
  }

  const markAsReadLocally = (id: string) => {
    const item = notifications.value.find(n => n.id === id)
    if (item && !item.isRead) {
      item.isRead = true
      unreadCount.value = Math.max(0, unreadCount.value - 1)
    }
  }

  const markAllAsReadLocally = () => {
    notifications.value.forEach(n => {
      n.isRead = true
    })
    unreadCount.value = 0
  }

  return {
    notifications,
    unreadCount,
    isLoading,
    setNotifications,
    addNotification,
    markAsReadLocally,
    markAllAsReadLocally,
  }
})
