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

function getNestedValue(obj: any, path: string, params?: Record<string, any>): string {
  const parts = path.split('.')
  let curr = obj
  for (const part of parts) {
    if (curr && typeof curr === 'object' && part in curr) {
      curr = curr[part]
    } else {
      return path
    }
  }
  let result = typeof curr === 'string' ? curr : path
  if (params && typeof params === 'object') {
    for (const [k, v] of Object.entries(params)) {
      result = result.replace(new RegExp(`\\{${k}\\}`, 'g'), String(v))
    }
  }
  return result
}

function hasNestedValue(obj: any, path: string): boolean {
  const parts = path.split('.')
  let curr = obj
  for (const part of parts) {
    if (curr && typeof curr === 'object' && part in curr) {
      curr = curr[part]
    } else {
      return false
    }
  }
  return typeof curr === 'string'
}

config.global.mocks = {
  $t: (key: string, params?: Record<string, any>) => getNestedValue(translations, key, params),
  $te: (key: string) => hasNestedValue(translations, key),
  $localePath: (path: string) => path,
}

config.global.stubs = {
  NuxtLink: {
    template: '<a><slot /></a>',
    props: ['to'],
  },
  NuxtLinkLocale: {
    template: '<a><slot /></a>',
    props: ['to', 'href', 'locale'],
  },
  ClientOnly: {
    template: '<div><slot /></div>',
  },
  Teleport: true,
}

// Global Nuxt helper mocks
;(globalThis as any).confirm = vi.fn(() => true)
;(globalThis as any).alert = vi.fn()
;(globalThis as any).navigateTo = vi.fn()
;(globalThis as any).definePageMeta = vi.fn()
;(globalThis as any).useRoute = () => ({ query: {}, params: { id: '01a047f0-a19c-72aa-99df-5dcae5b61001' } })
;(globalThis as any).useRouter = () => ({ push: vi.fn(), replace: vi.fn() })
;(globalThis as any).useLocalePath = () => (p: any) => typeof p === 'string' ? p : p?.path || '/'
;(globalThis as any).useSwitchLocalePath = () => (loc: string) => `/${loc}`
;(globalThis as any).useCookieLocale = () => ({ value: 'vi' })
;(globalThis as any).useI18n = () => ({
  t: (key: string, params?: Record<string, any>) => getNestedValue(translations, key, params),
  te: (key: string) => hasNestedValue(translations, key),
  locale: { value: 'vi' },
  setLocale: vi.fn(),
})
const mockToastInstance = {
  success: vi.fn(),
  error: vi.fn(),
  info: vi.fn(),
  warning: vi.fn(),
}
;(globalThis as any).useToast = () => mockToastInstance
;(globalThis as any).useFormat = () => ({
  formatCurrency: (val: number) => `${Number(val || 0).toLocaleString('vi-VN')} đ`,
  formatRelativeTime: (d: any) => '1 ngày trước',
  formatDateTime: (d: any) => '2026-08-29 09:00',
  formatDate: (d: any) => '29/08/2026',
})
;(globalThis as any).useRuntimeConfig = () => ({
  public: {
    apiBase: 'http://localhost:5004/api/v1',
  },
})
;(globalThis as any).useCookie = () => ({ value: 'test-token' })
;(globalThis as any).useAuthStore = () => ({
  accessToken: 'test-access-token',
  refreshToken: 'test-refresh-token',
  user: { id: 'test-user', fullName: 'Test User', role: 'Owner' },
  isAuthenticated: true,
  isOwner: true,
  isTenant: false,
  isStaff: false,
  isAdmin: false,
  setAuth: vi.fn(),
  setTokens: vi.fn(),
  setUser: vi.fn(),
  clearAuth: vi.fn(),
  logout: vi.fn(),
})

