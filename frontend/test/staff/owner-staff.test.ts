import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import OwnerStaffPage from '../../pages/owner/staff.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/my/boarding-houses')) {
    return Promise.resolve({
      items: [
        { id: 'house-1', name: 'Nhà trọ Cầu Giấy' }
      ]
    })
  }
  if (url.includes('/my/staff/')) {
    return Promise.resolve({
      id: 'staff-1',
      fullName: 'Nguyen Van Staff',
      assignments: []
    })
  }
  return Promise.resolve([
    {
      id: 'staff-1',
      username: 'staff_hanoi',
      email: 'staff@motellease.vn',
      fullName: 'Nguyen Van Staff',
      phoneNumber: '0988776655',
      gender: 'Male',
      isLocked: false,
      hireDate: '2026-01-01',
      activeAssignmentsCount: 1,
    }
  ])
})

const mockPost = vi.fn().mockResolvedValue({ id: 'staff-new-1' })
const mockPut = vi.fn().mockResolvedValue({})
const mockDelete = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
  put: mockPut,
  delete: mockDelete,
}))

describe('OwnerStaffPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('creates a new staff member account', async () => {
    const wrapper = mount(OwnerStaffPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openCreateStaffModal()
    vm.createForm.username = 'staff_new'
    vm.createForm.email = 'staff_new@test.com'
    vm.createForm.password = 'Pass12345@'
    vm.createForm.fullName = 'Tran Thi Staff'
    vm.createForm.phoneNumber = '0912999888'
    vm.createForm.gender = 'Female'
    vm.createForm.hireDate = '2026-08-01'

    await vm.handleSubmitCreateStaff()

    expect(mockPost).toHaveBeenCalledWith('/my/staff', expect.objectContaining({
      username: 'staff_new',
      email: 'staff_new@test.com',
      fullName: 'Tran Thi Staff',
    }))
  })

  it('locks / unlocks staff account', async () => {
    const wrapper = mount(OwnerStaffPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleToggleLockStaff({
      id: 'staff-1',
      isLocked: false,
      fullName: 'Nguyen Van Staff',
    })

    expect(mockDelete).toHaveBeenCalledWith('/my/staff/staff-1')
  })
})
