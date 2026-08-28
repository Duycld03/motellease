import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import TenantLeasesPage from '../../pages/tenant/leases.vue'

const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'lease-101',
      roomNumber: '101',
      boardingHouseName: 'Nhà trọ Cầu Giấy',
      primaryTenantFullName: 'Nguyen Van A',
      startDate: '2026-03-01',
      endDate: '2026-09-01',
      termMonths: 6,
      monthlyRent: 3500000,
      depositHeld: 3500000,
      status: 'Expiring',
      tenants: [
        { id: 't-1', fullName: 'Nguyen Van A', isPrimary: true, phoneNumber: '0912345678' }
      ]
    }
  ],
  totalCount: 1,
})

const mockPost = vi.fn().mockResolvedValue({ id: 'ext-req-1' })

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
}))

describe('TenantLeasesPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('submits a lease extension request with POST /extension-requests', async () => {
    const wrapper = mount(TenantLeasesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openExtensionModal({
      id: 'lease-101',
      endDate: '2026-09-01',
    })

    vm.extensionForm.requestedEndDate = '2027-03-01'
    vm.extensionForm.tenantNote = 'Muốn ở thêm 6 tháng'

    await vm.handleSubmitExtension()

    expect(mockPost).toHaveBeenCalledWith('/extension-requests', {
      leaseId: 'lease-101',
      requestedEndDate: '2027-03-01',
      tenantNote: 'Muốn ở thêm 6 tháng',
    })
  })
})
