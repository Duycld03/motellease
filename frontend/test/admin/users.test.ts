import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AdminUsersPage from '../../pages/admin/users.vue'

const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'usr-1',
      username: 'tenant1',
      email: 'tenant1@test.com',
      fullName: 'Le Van Thue',
      phoneNumber: '0988111222',
      role: 'Tenant',
      isLocked: false,
      createdAt: '2026-08-28T10:00:00Z',
    }
  ],
  totalCount: 1,
})

const mockPost = vi.fn().mockResolvedValue({ id: 'adm-new-1' })

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
}))

describe('AdminUsersPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('creates an admin account with POST /admin/accounts', async () => {
    const wrapper = mount(AdminUsersPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openCreateModal()
    await wrapper.vm.$nextTick()
    vm.createForm.fullName = 'Admin Tong'
    vm.createForm.username = 'superadmin'
    vm.createForm.email = 'admin@motellease.vn'
    vm.createForm.password = 'Admin12345@'
    vm.createForm.gender = 'Male'

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPost).toHaveBeenCalledWith('/admin/accounts', {
      fullName: 'Admin Tong',
      username: 'superadmin',
      email: 'admin@motellease.vn',
      password: 'Admin12345@',
      phoneNumber: undefined,
      gender: 'Male',
      role: 'Admin',
    })
  })

  it('locks a user account with POST /admin/accounts/{id}/lock', async () => {
    const wrapper = mount(AdminUsersPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleToggleLock({
      id: 'usr-1',
      fullName: 'Le Van Thue',
      isLocked: false,
    })

    expect(mockPost).toHaveBeenCalledWith('/admin/accounts/usr-1/lock', {
      reason: 'Khóa bởi Quản trị viên',
    })
  })
})
