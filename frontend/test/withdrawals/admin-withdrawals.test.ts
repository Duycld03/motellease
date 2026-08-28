import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AdminWithdrawalsPage from '../../pages/admin/withdrawals.vue'

const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'w-101',
      ownerFullName: 'Tran Van ChuTro',
      amount: 10000000,
      bankName: 'Techcombank',
      bankAccountNumber: '19033445566',
      bankAccountHolder: 'TRAN VAN CHUTRO',
      status: 'Pending',
      createdAt: '2026-08-28T11:00:00Z',
    }
  ],
  totalCount: 1,
})

const mockPut = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  put: mockPut,
}))

describe('AdminWithdrawalsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('approves a withdrawal request with PUT /withdraw-requests/{id}/approve', async () => {
    const wrapper = mount(AdminWithdrawalsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleApprove('w-101')

    expect(mockPut).toHaveBeenCalledWith('/withdraw-requests/w-101/approve', {})
  })

  it('rejects a withdrawal request with reason payload', async () => {
    const wrapper = mount(AdminWithdrawalsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openRejectModal({
      id: 'w-101',
      ownerFullName: 'Tran Van ChuTro',
      amount: 10000000,
    })
    vm.rejectReason = 'Thông tin số tài khoản không khớp với tên chủ tài khoản'

    await vm.handleConfirmReject()

    expect(mockPut).toHaveBeenCalledWith('/withdraw-requests/w-101/reject', {
      reason: 'Thông tin số tài khoản không khớp với tên chủ tài khoản',
    })
  })
})
