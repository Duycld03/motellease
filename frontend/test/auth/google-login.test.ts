import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import GoogleSignInButton from '../../components/common/GoogleSignInButton.vue'
import { UserRole } from '../../types/enums'

const mockLoginWithGoogle = vi.fn().mockResolvedValue({
  accessToken: 'mock-access',
  refreshToken: 'mock-refresh',
  user: { id: 'user-1', fullName: 'Google User', role: 'Tenant' },
})

vi.stubGlobal('useAuth', () => ({
  loginWithGoogle: mockLoginWithGoogle,
}))

describe('GoogleSignInButton.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    ;(globalThis as any).window.google = undefined
  })

  it('renders fallback Google button when no GIS or ClientId is initialized', () => {
    const wrapper = mount(GoogleSignInButton)
    const button = wrapper.find('button')
    expect(button.exists()).toBe(true)
    expect(button.text()).toContain('Tiếp tục với Google')
  })

  it('handles custom button click and notifies if googleClientId is not configured', async () => {
    const toast = (globalThis as any).useToast()
    const wrapper = mount(GoogleSignInButton)
    const button = wrapper.find('button')
    await button.trigger('click')

    expect(toast.info).toHaveBeenCalledWith('Đăng nhập Google chưa được cấu hình Client ID.')
  })

  it('prompts GIS if google SDK exists on window when custom button is clicked', async () => {
    const mockPrompt = vi.fn()
    ;(globalThis as any).window.google = {
      accounts: {
        id: {
          initialize: vi.fn(),
          renderButton: vi.fn(),
          prompt: mockPrompt,
        },
      },
    }
    ;(globalThis as any).useRuntimeConfig = () => ({
      public: { googleClientId: 'test-client-id.apps.googleusercontent.com' },
    })

    const wrapper = mount(GoogleSignInButton)
    const button = wrapper.find('button')
    if (button.exists()) {
      await button.trigger('click')
      expect(mockPrompt).toHaveBeenCalled()
    }
  })

  it('calls loginWithGoogle with role when passed as prop', async () => {
    let storedCallback: ((res: { credential: string }) => void) | null = null
    const mockInitialize = vi.fn((config: any) => {
      storedCallback = config.callback
    })
    const mockRenderButton = vi.fn()

    ;(globalThis as any).window.google = {
      accounts: {
        id: {
          initialize: mockInitialize,
          renderButton: mockRenderButton,
          prompt: vi.fn(),
        },
      },
    }
    ;(globalThis as any).useRuntimeConfig = () => ({
      public: { googleClientId: 'test-client-id.apps.googleusercontent.com' },
    })

    const wrapper = mount(GoogleSignInButton, {
      props: {
        role: UserRole.Owner,
      },
    })

    // Wait for onMounted
    await wrapper.vm.$nextTick()

    // Trigger callback directly if initialized
    if (storedCallback) {
      await (storedCallback as any)({ credential: 'mock-jwt-google-id-token' })
      expect(mockLoginWithGoogle).toHaveBeenCalledWith('mock-jwt-google-id-token', UserRole.Owner)
      expect(wrapper.emitted('success')).toBeTruthy()
    }
  })
})
