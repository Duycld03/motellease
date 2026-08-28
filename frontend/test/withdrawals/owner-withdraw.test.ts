import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import OwnerWithdrawPage from '../../pages/owner/withdraw.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/my/stats/summary')) {
    return Promise.resolve({
      availableBalance: 15000000,
      revenueThisMonth: 20000000,
      profitThisMonth: 18000000,
    })
  }
  return Promise.resolve({
    items: [
      {
        id: 'w-1',
        amount: 5000000,
        bankName: 'Vietcombank',
        bankAccountNumber: '001100223344',
        bankAccountHolder: 'NGUYEN VAN OWNER',
        status: 'Pending',
        createdAt: '2026-08-28T10:00:00Z',
      }
    ],
    totalCount: 1,
  })
})

const mockPost = vi.fn().mockResolvedValue({ id: 'w-new-1' })

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
}))

describe('OwnerWithdrawPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('submits a withdrawal request with POST /withdraw-requests', async () => {
    const wrapper = mount(OwnerWithdrawPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openWithdrawModal()
    vm.withdrawForm.amount = 5000000
    vm.withdrawForm.bankName = 'MBBank'
    vm.withdrawForm.bankAccountNumber = '999988887777'
    vm.withdrawForm.bankAccountHolder = 'NGUYEN VAN OWNER'

    await vm.handleSubmitWithdraw()

    expect(mockPost).toHaveBeenCalledWith('/withdraw-requests', {
      amount: 5000000,
      bankName: 'MBBank',
      bankAccountNumber: '999988887777',
      bankAccountHolder: 'NGUYEN VAN OWNER',
    })
  })
})
