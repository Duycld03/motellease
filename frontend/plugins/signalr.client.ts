export default defineNuxtPlugin(() => {
  const authStore = useAuthStore()
  const { startConnection, stopConnection } = useSignalR()

  // Watch authentication state to start or stop SignalR hub connection
  watch(
    () => authStore.isAuthenticated,
    (isAuth) => {
      if (isAuth) {
        startConnection()
      } else {
        stopConnection()
      }
    },
    { immediate: true }
  )
})
