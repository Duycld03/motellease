import { useColorMode } from '@vueuse/core'

export const useTheme = () => {
  const mode = useColorMode({
    attribute: 'class',
    modes: {
      light: '',
      dark: 'dark',
    },
    storageKey: 'motellease-theme',
  })

  const isDark = computed(() => {
    if (mode.value === 'dark') return true
    if (mode.value === 'light') return false
    if (typeof window !== 'undefined' && window.matchMedia) {
      return window.matchMedia('(prefers-color-scheme: dark)').matches
    }
    return false
  })

  const toggleTheme = () => {
    if (isDark.value) {
      mode.value = 'light'
    } else {
      mode.value = 'dark'
    }
  }

  const setTheme = (theme: 'light' | 'dark' | 'auto') => {
    mode.value = theme
  }

  return {
    mode,
    isDark,
    toggleTheme,
    setTheme,
  }
}
