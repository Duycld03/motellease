import { HubConnectionBuilder, HubConnection, HubConnectionState, LogLevel } from '@microsoft/signalr'
import type { InAppNotification } from '~/types/api'

let connection: HubConnection | null = null

export const useSignalR = () => {
  const config = useRuntimeConfig()
  const authStore = useAuthStore()
  const notificationStore = useNotificationStore()
  const toast = useToast()

  const isConnected = ref(false)

  const startConnection = async () => {
    if (!import.meta.client) return
    if (!authStore.accessToken) return

    if (connection && connection.state === HubConnectionState.Connected) {
      isConnected.value = true
      return
    }

    try {
      connection = new HubConnectionBuilder()
        .withUrl(config.public.hubUrl, {
          accessTokenFactory: () => authStore.accessToken || '',
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(LogLevel.Warning)
        .build()

      connection.on('ReceiveNotification', (notification: InAppNotification) => {
        notificationStore.addNotification(notification)
        toast.info(notification.body || notification.title, notification.title)
      })

      connection.onreconnecting(() => {
        isConnected.value = false
      })

      connection.onreconnected(() => {
        isConnected.value = true
      })

      connection.onclose(() => {
        isConnected.value = false
      })

      await connection.start()
      isConnected.value = true
    } catch (err) {
      isConnected.value = false
      // Silently fail connection on client if backend is not yet running
    }
  }

  const stopConnection = async () => {
    if (!import.meta.client || !connection) return
    try {
      if (connection.state === HubConnectionState.Connected) {
        await connection.stop()
      }
    } catch {
      // Ignore
    } finally {
      connection = null
      isConnected.value = false
    }
  }

  return {
    isConnected: readonly(isConnected),
    startConnection,
    stopConnection,
  }
}
