import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import TenantMaintenancePage from '../../pages/tenant/maintenance.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/leases')) {
    return Promise.resolve({
      items: [
        { id: 'lease-101', roomNumber: '101', boardingHouseName: 'Nhà trọ Cầu Giấy' }
      ]
    })
  }
  return Promise.resolve({
    items: [
      {
        id: 'mr-1',
        boardingHouseName: 'Nhà trọ Cầu Giấy',
        roomNumber: '101',
        category: 'Electricity',
        description: 'Bóng đèn trần bị cháy và ổ cắm lỏng',
        status: 'Open',
        createdAt: '2026-08-28T10:00:00Z',
        images: [],
      }
    ],
    totalCount: 1,
  })
})

const mockPost = vi.fn().mockResolvedValue({ id: 'mr-new-1' })

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
}))

describe('TenantMaintenancePage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('submits a maintenance report with POST /maintenance-requests', async () => {
    const wrapper = mount(TenantMaintenancePage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.openCreateModal()
    await wrapper.vm.$nextTick()
    vm.form.leaseId = 'lease-101'
    vm.form.category = 'Water'
    vm.form.description = 'Vòi nước bồn rửa mặt bị rò rỉ liên tục'

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPost).toHaveBeenCalledWith('/maintenance-requests', {
      leaseId: 'lease-101',
      category: 'Water',
      description: 'Vòi nước bồn rửa mặt bị rò rỉ liên tục',
    })
  })
})
