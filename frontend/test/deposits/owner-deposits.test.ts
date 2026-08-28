import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import OwnerDepositsPage from '../../pages/owner/deposits.vue'

const mockPut = vi.fn().mockResolvedValue({})
const mockPost = vi.fn().mockResolvedValue({ id: 'lease-new-1', roomNumber: '101' })
const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'dep-201',
      boardingHouseName: 'Nhà trọ Cầu Giấy',
      roomNumber: '101',
      tenantFullName: 'Le Van C',
      tenantPhoneNumber: '0988888888',
      amount: 3000000,
      status: 'Paid',
      requestedStartDate: '2026-09-01',
      requestedTermMonths: 12,
      createdAt: '2026-08-28T09:00:00Z',
    }
  ],
  totalCount: 1,
})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  put: mockPut,
  post: mockPost,
}))

describe('OwnerDepositsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('approves a pending deposit for 24 hours', async () => {
    const wrapper = mount(OwnerDepositsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleApprove({
      id: 'dep-pending-1',
      roomNumber: '102',
      tenantFullName: 'Tran Van D',
    })

    expect(mockPut).toHaveBeenCalledWith('/deposits/dep-pending-1/approve', {})
  })

  it('rejects deposit with reject reason payload', async () => {
    const wrapper = mount(OwnerDepositsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openRejectModal({
      id: 'dep-pending-1',
      roomNumber: '102',
      tenantFullName: 'Tran Van D',
    })
    vm.rejectReason = 'Không nhận khách có nuôi thú cưng'

    await vm.handleConfirmReject()

    expect(mockPut).toHaveBeenCalledWith('/deposits/dep-pending-1/reject', {
      reason: 'Không nhận khách có nuôi thú cưng',
    })
  })

  it('confirms paid deposit into active lease contract with POST /deposits/{id}/confirm-lease', async () => {
    const wrapper = mount(OwnerDepositsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const confirmBtn = wrapper.findAll('button').find(b => b.text().includes('Tạo Hợp đồng & Nhận phòng'))
    expect(confirmBtn).toBeDefined()
    await confirmBtn!.trigger('click')

    expect(mockPost).toHaveBeenCalledWith('/deposits/dep-201/confirm-lease', {})
  })
})
