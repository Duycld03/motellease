import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import ForgotPasswordPage from '../../pages/auth/forgot-password.vue'

const mockPost = vi.fn().mockResolvedValue({})
vi.stubGlobal('useApi', () => ({
  post: mockPost,
}))

describe('ForgotPasswordPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('starts at step 1 and sends OTP request with email', async () => {
    const wrapper = mount(ForgotPasswordPage)
    const emailInput = wrapper.find('input[type="email"]')
    await emailInput.setValue('user@example.com')

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPost).toHaveBeenCalledWith('/auth/password/forgot', { email: 'user@example.com' })
  })

  it('advances to step 2 and submits code and newPassword payload', async () => {
    const wrapper = mount(ForgotPasswordPage)
    const emailInput = wrapper.find('input[type="email"]')
    await emailInput.setValue('user@example.com')
    await wrapper.find('form').trigger('submit.prevent')

    // Wait for step 2 form
    await wrapper.vm.$nextTick()

    const textInput = wrapper.find('input[type="text"]')
    const passwordInput = wrapper.find('input[type="password"]')
    await textInput.setValue('654321')
    await passwordInput.setValue('NewSecretPass123!')

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPost).toHaveBeenCalledWith('/auth/password/reset', {
      email: 'user@example.com',
      code: '654321',
      newPassword: 'NewSecretPass123!',
    })
  })
})
