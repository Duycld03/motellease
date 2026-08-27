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

  const isDark = computed(() => mode.value === 'dark')

  const toggleTheme = () => {
    mode.value = mode.value === 'dark' ? 'light' : 'dark'
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
