import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import LoginPage from '../../pages/auth/login.vue'

// Mock useAuth
const mockLogin = vi.fn()
const mockLoginWithGoogle = vi.fn()
vi.stubGlobal('useAuth', () => ({
  login: mockLogin,
  loginWithGoogle: mockLoginWithGoogle,
}))

describe('LoginPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders email and password inputs, submit button, and Google sign-in button', () => {
    const wrapper = mount(LoginPage)
    expect(wrapper.find('input[type="email"]').exists()).toBe(true)
    expect(wrapper.find('input[type="password"]').exists()).toBe(true)
    expect(wrapper.find('button[type="submit"]').exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'GoogleSignInButton' }).exists()).toBe(true)
  })

  it('does not submit when fields are empty', async () => {
    const wrapper = mount(LoginPage)
    await wrapper.find('form').trigger('submit.prevent')
    expect(mockLogin).not.toHaveBeenCalled()
  })

  it('submits form with entered email and password', async () => {
    const wrapper = mount(LoginPage)
    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')

    await emailInput.setValue('tenant@motellease.vn')
    await passwordInput.setValue('Secret123!')
    await wrapper.find('form').trigger('submit.prevent')

    expect(mockLogin).toHaveBeenCalledTimes(1)
    expect(mockLogin).toHaveBeenCalledWith('tenant@motellease.vn', 'Secret123!')
  })
})
