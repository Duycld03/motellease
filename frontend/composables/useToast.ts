export const useToast = () => {
  const toastStore = useToastStore()
  return {
    toasts: computed(() => toastStore.toasts),
    addToast: toastStore.addToast,
    removeToast: toastStore.removeToast,
    success: toastStore.success,
    error: toastStore.error,
    warning: toastStore.warning,
    info: toastStore.info,
  }
}
