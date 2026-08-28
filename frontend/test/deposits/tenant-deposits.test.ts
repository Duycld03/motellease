import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import TenantDepositsPage from '../../pages/tenant/deposits.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/contract-preview')) {
    return Promise.resolve({
      depositId: 'dep-101',
      boardingHouseName: 'Nhà trọ Cầu Giấy',
      addressLine: '123 Cầu Giấy',
      ward: 'Dịch Vọng',
      district: 'Cầu Giấy',
      province: 'Hà Nội',
      roomNumber: '101',
      tenantFullName: 'Nguyen Van A',
      monthlyRent: 3500000,
      depositHeld: 3500000,
      termMonths: 6,
      startDate: '2026-09-01',
      endDate: '2027-03-01',
    })
  }
  return Promise.resolve({
    items: [
      {
        id: 'dep-101',
        boardingHouseName: 'Nhà trọ Cầu Giấy',
        roomNumber: '101',
        amount: 3500000,
        status: 'Accepted',
        requestedStartDate: '2026-09-01',
        requestedTermMonths: 6,
        expiresAt: '2026-08-29T18:00:00Z',
        createdAt: '2026-08-28T10:00:00Z',
      }
    ],
    totalCount: 1,
  })
})

const mockPost = vi.fn().mockResolvedValue({
  transactionId: 'txn-123',
  paymentUrl: 'https://payment.momo.vn/pay/test',
})

const mockPut = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
  put: mockPut,
}))

describe('TenantDepositsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('opens contract preview modal and loads contract preview details', async () => {
    const wrapper = mount(TenantDepositsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const previewBtn = wrapper.findAll('button').find(b => b.text().includes('Xem dự thảo hợp đồng'))
    expect(previewBtn).toBeDefined()
    await previewBtn!.trigger('click')
    await wrapper.vm.$nextTick()

    expect(mockGet).toHaveBeenCalledWith('/deposits/dep-101/contract-preview')
  })

  it('opens checkout modal and initiates payment with selected provider', async () => {
    const wrapper = mount(TenantDepositsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openCheckoutModal({
      id: 'dep-101',
      boardingHouseName: 'Nhà trọ Cầu Giấy',
      roomNumber: '101',
      amount: 3500000,
    })
    vm.selectedGateway = 'VNPay'

    await vm.handleStartCheckout()

    expect(mockPost).toHaveBeenCalledWith('/deposits/dep-101/checkout', {
      provider: 'VNPay',
    })
  })

  it('cancels pending deposit request with reason payload', async () => {
    const wrapper = mount(TenantDepositsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openCancelModal({
      id: 'dep-101',
      boardingHouseName: 'Nhà trọ Cầu Giấy',
      roomNumber: '101',
    })
    vm.cancelReason = 'Đổi ý không thuê nữa'

    await vm.handleConfirmCancel()

    expect(mockPut).toHaveBeenCalledWith('/deposits/dep-101/cancel', {
      reason: 'Đổi ý không thuê nữa',
    })
  })
})
