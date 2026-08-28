export const useFormat = () => {
  const { locale, t } = useI18n()

  const currentLocale = computed(() => (locale.value === 'en' ? 'en-US' : 'vi-VN'))

  const formatCurrency = (amount: number | null | undefined): string => {
    if (amount === null || amount === undefined || isNaN(amount)) return '0 ₫'
    return new Intl.NumberFormat(currentLocale.value, {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(amount)
  }

  const formatDate = (dateInput: string | Date | null | undefined): string => {
    if (!dateInput) return ''
    const d = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
    if (isNaN(d.getTime())) return ''
    return new Intl.DateTimeFormat(currentLocale.value, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(d)
  }

  const formatDateTime = (dateInput: string | Date | null | undefined): string => {
    if (!dateInput) return ''
    const d = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
    if (isNaN(d.getTime())) return ''
    return new Intl.DateTimeFormat(currentLocale.value, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    }).format(d)
  }

  const formatRelativeTime = (dateInput: string | Date | null | undefined): string => {
    if (!dateInput) return ''
    const d = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
    if (isNaN(d.getTime())) return ''

    const diffInSeconds = Math.floor((Date.now() - d.getTime()) / 1000)

    if (diffInSeconds < 60) return t('common.justNow')
    if (diffInSeconds < 3600) {
      const minutes = Math.floor(diffInSeconds / 60)
      return t('common.minutesAgo', { m: minutes })
    }
    if (diffInSeconds < 86400) {
      const hours = Math.floor(diffInSeconds / 3600)
      return t('common.hoursAgo', { h: hours })
    }
    if (diffInSeconds < 2592000) {
      const days = Math.floor(diffInSeconds / 86400)
      return t('common.daysAgo', { d: days })
    }
    return formatDate(d)
  }

  return {
    formatCurrency,
    formatDate,
    formatDateTime,
    formatRelativeTime,
  }
}
