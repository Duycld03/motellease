<template>
  <div class="w-full flex flex-col items-center justify-center">
    <!-- Google GIS rendered container (active when googleClientId is configured and script loaded) -->
    <div
      v-show="isGisReady && hasClientId"
      ref="googleButtonRef"
      class="w-full flex justify-center min-h-[44px]"
    />

    <!-- Custom / Fallback Google Button (used when loading, when GIS render is not ready, or in dev mode without clientId) -->
    <button
      v-if="!isGisReady || !hasClientId"
      type="button"
      :disabled="disabled || isLoading"
      class="w-full flex items-center justify-center gap-3 px-4 py-2.5 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 text-sm font-medium hover:bg-slate-50 dark:hover:bg-slate-750 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 transition-all shadow-sm disabled:opacity-60 disabled:cursor-not-allowed cursor-pointer"
      @click="handleCustomButtonClick"
    >
      <svg class="w-5 h-5 flex-shrink-0" viewBox="0 0 24 24">
        <path
          fill="#4285F4"
          d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.66-5.17 3.66-9.17z"
        />
        <path
          fill="#34A853"
          d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.25v3.15C3.26 21.36 7.34 24 12 24z"
        />
        <path
          fill="#FBBC05"
          d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.25C.45 8.17 0 9.98 0 12s.45 3.83 1.25 5.42l4.03-3.15z"
        />
        <path
          fill="#EA4335"
          d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.34 0 3.26 2.64 1.25 6.58l4.03 3.15c.95-2.83 3.6-4.98 6.72-4.98z"
        />
      </svg>
      <span v-if="isLoading" class="inline-flex items-center gap-1.5">
        <svg class="animate-spin h-4 w-4 text-slate-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
        </svg>
        <span>{{ $t('common.loading') }}</span>
      </span>
      <span v-else>
        {{ $t('auth.loginWithGoogle') }}
      </span>
    </button>
  </div>
</template>

<script setup lang="ts">
import type { UserRole } from '~/types/enums'

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: any) => void
          renderButton: (parent: HTMLElement, options: any) => void
          prompt: () => void
        }
      }
    }
  }
}

const props = withDefaults(
  defineProps<{
    role?: UserRole
    disabled?: boolean
    text?: 'signin_with' | 'signup_with' | 'continue_with'
    shape?: 'rectangular' | 'pill' | 'circle' | 'square'
    size?: 'large' | 'medium' | 'small'
    theme?: 'outline' | 'filled_blue' | 'filled_black'
  }>(),
  {
    role: undefined,
    disabled: false,
    text: 'continue_with',
    shape: 'rectangular',
    size: 'large',
    theme: 'outline',
  }
)

const emit = defineEmits<{
  (e: 'success', credential: string): void
  (e: 'error', error: any): void
}>()

const { loginWithGoogle } = useAuth()
const config = useRuntimeConfig()
const toast = useToast()
const { t, locale } = useI18n()

const googleButtonRef = ref<HTMLElement | null>(null)
const isGisReady = ref(false)
const isLoading = ref(false)

const googleClientId = computed(() => (config.public?.googleClientId as string) || '')
const hasClientId = computed(() => Boolean(googleClientId.value && googleClientId.value.trim() !== ''))

const handleCredentialResponse = async (response: { credential: string }) => {
  if (!response?.credential) return
  isLoading.value = true
  try {
    await loginWithGoogle(response.credential, props.role)
    emit('success', response.credential)
  } catch (err: any) {
    emit('error', err)
    toast.error(err?.message || t('auth.loginFailed'))
  } finally {
    isLoading.value = false
  }
}

const initializeGoogleSignIn = () => {
  if (typeof window === 'undefined' || !window.google?.accounts?.id) return
  if (!hasClientId.value) return

  try {
    window.google.accounts.id.initialize({
      client_id: googleClientId.value,
      callback: handleCredentialResponse,
      auto_select: false,
      cancel_on_tap_outside: true,
    })

    if (googleButtonRef.value) {
      googleButtonRef.value.innerHTML = ''
      window.google.accounts.id.renderButton(googleButtonRef.value, {
        type: 'standard',
        theme: props.theme,
        size: props.size,
        text: props.text,
        shape: props.shape,
        logo_alignment: 'left',
        width: googleButtonRef.value.clientWidth || 320,
        locale: locale.value || 'vi',
      })
      isGisReady.value = true
    }
  } catch (err) {
    isGisReady.value = false
  }
}

const loadGisScript = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (typeof window === 'undefined' || 'happyDOM' in window || (process as any)?.env?.NODE_ENV === 'test') {
      return resolve()
    }
    if (window.google?.accounts?.id) {
      return resolve()
    }

    try {
      const existingScript = document.querySelector('script[src="https://accounts.google.com/gsi/client"]')
      if (existingScript) {
        existingScript.addEventListener('load', () => resolve())
        existingScript.addEventListener('error', (e) => reject(e))
        return
      }

      const script = document.createElement('script')
      script.src = 'https://accounts.google.com/gsi/client'
      script.async = true
      script.defer = true
      script.onload = () => resolve()
      script.onerror = (e) => reject(e)
      document.head.appendChild(script)
    } catch {
      // In testing environments (e.g. happy-dom) or environments where external script loading is blocked
      resolve()
    }
  })
}

const handleCustomButtonClick = () => {
  if (props.disabled || isLoading.value) return
  if (!hasClientId.value) {
    toast.info(t('auth.googleNotConfigured'))
    return
  }

  if (window.google?.accounts?.id) {
    window.google.accounts.id.prompt()
  } else {
    toast.error(t('auth.googleSignInUnavailable'))
  }
}

onMounted(async () => {
  if (typeof window === 'undefined') return
  try {
    await loadGisScript()
    initializeGoogleSignIn()
  } catch {
    // If script loading fails, fallback button will remain visible
  }
})

watch(
  () => locale.value,
  () => {
    if (isGisReady.value) {
      initializeGoogleSignIn()
    }
  }
)
</script>
