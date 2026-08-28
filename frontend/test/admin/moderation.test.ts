import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AdminModerationPage from '../../pages/admin/moderation.vue'

const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'house-pending-1',
      name: 'Nhà trọ Mới Cầu Giấy',
      addressLine: '123 Cầu Giấy',
      ward: 'Dịch Vọng',
      district: 'Cầu Giấy',
      province: 'Hà Nội',
      ownerFullName: 'Nguyen Van Owner',
      ownerEmail: 'owner@test.com',
      roomsCount: 10,
      listingStatus: 'PendingApproval',
      createdAt: '2026-08-28T10:00:00Z',
    }
  ],
  totalCount: 1,
})

const mockPut = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  put: mockPut,
}))

describe('AdminModerationPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('approves a boarding house listing with PUT /admin/boarding-houses/{id}/approve', async () => {
    const wrapper = mount(AdminModerationPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleApprove('house-pending-1')

    expect(mockPut).toHaveBeenCalledWith('/admin/boarding-houses/house-pending-1/approve', {})
  })

  it('rejects a boarding house listing with reason', async () => {
    const wrapper = mount(AdminModerationPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openRejectModal({
      id: 'house-pending-1',
      name: 'Nhà trọ Mới Cầu Giấy',
    })
    vm.rejectReason = 'Hình ảnh chụp phòng không rõ ràng'

    await vm.handleConfirmReject()

    expect(mockPut).toHaveBeenCalledWith('/admin/boarding-houses/house-pending-1/reject', {
      reason: 'Hình ảnh chụp phòng không rõ ràng',
    })
  })
})
