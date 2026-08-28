import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import ProfilePage from '../../pages/tenant/profile.vue'

const mockPut = vi.fn().mockResolvedValue({ fullName: 'Updated Name', phoneNumber: '0987654321', gender: 'Female' })
const mockGet = vi.fn().mockResolvedValue([])
const mockDelete = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  put: mockPut,
  get: mockGet,
  delete: mockDelete,
}))

vi.stubGlobal('useAuth', () => ({
  user: { value: { fullName: 'Nguyen Van B', email: 'tenant@motellease.vn', phoneNumber: '0912345678', isEmailVerified: true } },
  role: { value: 'Tenant' },
}))

vi.stubGlobal('useAuthStore', () => ({
  setUser: vi.fn(),
}))

vi.stubGlobal('useFormat', () => ({
  formatRelativeTime: (d: any) => 'vừa xong',
}))

describe('ProfilePage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('submits updated profile with fullName, phoneNumber and gender payload', async () => {
    const wrapper = mount(ProfilePage)
    const nameInput = wrapper.find('input[type="text"]')
    await nameInput.setValue('Nguyen Van C')

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPut).toHaveBeenCalledWith('/me', {
      fullName: 'Nguyen Van C',
      phoneNumber: '0912345678',
      gender: 'Other',
    })
  })

  it('submits password change with currentPassword and newPassword payload', async () => {
    const wrapper = mount(ProfilePage)

    // Switch to security tab
    const tabs = wrapper.findAll('nav button')
    await tabs[1].trigger('click')

    const passwordInputs = wrapper.findAll('input[type="password"]')
    await passwordInputs[0].setValue('OldPass123!')
    await passwordInputs[1].setValue('NewPass456!')
    await passwordInputs[2].setValue('NewPass456!')

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPut).toHaveBeenCalledWith('/auth/password', {
      currentPassword: 'OldPass123!',
      newPassword: 'NewPass456!',
    })
  })
})
