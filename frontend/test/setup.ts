import { vi } from 'vitest'
import { config } from '@vue/test-utils'
import * as vueExports from 'vue'
import viLocale from '../locales/vi.json'

// Expose all Vue exports globally (to mirror Nuxt auto-imports in test environment)
for (const [key, value] of Object.entries(vueExports)) {
  ;(globalThis as any)[key] = value
}

// Global i18n mock for Vue Test Utils
const translations: Record<string, any> = viLocale

function getNestedValue(obj: any, path: string): string {
  const parts = path.split('.')
  let curr = obj
  for (const part of parts) {
    if (curr && typeof curr === 'object' && part in curr) {
      curr = curr[part]
    } else {
      return path
    }
  }
  return typeof curr === 'string' ? curr : path
}

config.global.mocks = {
  $t: (key: string) => getNestedValue(translations, key),
  $localePath: (path: string) => path,
}

config.global.stubs = {
  NuxtLink: {
    template: '<a><slot /></a>',
    props: ['to'],
  },
  ClientOnly: {
    template: '<div><slot /></div>',
  },
  Teleport: true,
}

// Global Nuxt helper mocks
;(globalThis as any).navigateTo = vi.fn()
;(globalThis as any).definePageMeta = vi.fn()
;(globalThis as any).useRoute = () => ({ query: {}, params: { id: '01a047f0-a19c-72aa-99df-5dcae5b61001' } })
;(globalThis as any).useRouter = () => ({ push: vi.fn(), replace: vi.fn() })
;(globalThis as any).useI18n = () => ({
  t: (key: string) => getNestedValue(translations, key),
  locale: { value: 'vi' },
})
;(globalThis as any).useToast = () => ({
  success: vi.fn(),
  error: vi.fn(),
  info: vi.fn(),
  warning: vi.fn(),
})
;(globalThis as any).useFormat = () => ({
  formatCurrency: (val: number) => `${Number(val || 0).toLocaleString('vi-VN')} đ`,
  formatRelativeTime: (d: any) => '1 ngày trước',
  formatDateTime: (d: any) => '2026-08-29 09:00',
  formatDate: (d: any) => '29/08/2026',
})
