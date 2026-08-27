import { defineStore } from 'pinia'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

export interface ToastItem {
  id: string
  type: ToastType
  title?: string
  message: string
  duration?: number
}

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<ToastItem[]>([])

  const addToast = (toast: Omit<ToastItem, 'id'>) => {
    const id = Math.random().toString(36).substring(2, 9)
    const duration = toast.duration ?? 4000

    const item: ToastItem = {
      id,
      type: toast.type,
      title: toast.title,
      message: toast.message,
      duration,
    }

    toasts.value.push(item)

    if (duration > 0) {
      setTimeout(() => {
        removeToast(id)
      }, duration)
    }

    return id
  }

  const removeToast = (id: string) => {
    const index = toasts.value.findIndex(t => t.id === id)
    if (index !== -1) {
      toasts.value.splice(index, 1)
    }
  }

  const success = (message: string, title?: string) => addToast({ type: 'success', message, title })
  const error = (message: string, title?: string) => addToast({ type: 'error', message, title })
  const warning = (message: string, title?: string) => addToast({ type: 'warning', message, title })
  const info = (message: string, title?: string) => addToast({ type: 'info', message, title })

  return {
    toasts,
    addToast,
    removeToast,
    success,
    error,
    warning,
    info,
  }
})
