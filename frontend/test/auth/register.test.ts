import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import RegisterPage from '../../pages/auth/register.vue'
import { UserRole } from '../../types/enums'

const mockSendRegistrationOtp = vi.fn().mockResolvedValue({ message: 'OK', expiresInMinutes: 10 })
const mockLoginWithGoogle = vi.fn()
vi.stubGlobal('useAuth', () => ({
  sendRegistrationOtp: mockSendRegistrationOtp,
  loginWithGoogle: mockLoginWithGoogle,
}))

describe('RegisterPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    sessionStorage.clear()
  })

  it('renders all registration form fields and Google sign-in button', () => {
    const wrapper = mount(RegisterPage)
    expect(wrapper.find('input[type="text"]').exists()).toBe(true)
    expect(wrapper.find('input[type="email"]').exists()).toBe(true)
    expect(wrapper.find('input[type="password"]').exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'GoogleSignInButton' }).exists()).toBe(true)
  })

  it('submits registration form, calls sendRegistrationOtp and saves pending state in sessionStorage', async () => {
    const wrapper = mount(RegisterPage)
    const inputs = wrapper.findAll('input')
    // Full name
    await inputs[0].setValue('Nguyen Van A')
    // Email
    await inputs[1].setValue('nguyenvana@gmail.com')
    // Password
    await inputs[3].setValue('Password123!')

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockSendRegistrationOtp).toHaveBeenCalledWith('nguyenvana@gmail.com')
    const saved = sessionStorage.getItem('pending_registration')
    expect(saved).not.toBeNull()
    const parsed = JSON.parse(saved!)
    expect(parsed.email).toBe('nguyenvana@gmail.com')
    expect(parsed.fullName).toBe('Nguyen Van A')
  })
})
