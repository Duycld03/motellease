import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import TenantBillsPage from '../../pages/tenant/bills.vue'

const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'bill-101',
      roomNumber: '101',
      boardingHouseName: 'Nhà trọ Cầu Giấy',
      month: 8,
      year: 2026,
      rentAmount: 3500000,
      electricityAmount: 140000,
      waterAmount: 60000,
      additionalFeeTotal: 50000,
      totalAmount: 3750000,
      status: 'Issued',
      issuedAt: '2026-08-25T10:00:00Z',
      dueDate: '2026-09-05',
    }
  ],
  totalCount: 1,
})

const mockPost = vi.fn().mockResolvedValue({
  transactionId: 'txn-bill-1',
  paymentUrl: 'https://payment.momo.vn/pay/bill1',
})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
}))

describe('TenantBillsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders bill details and total payable amount', async () => {
    const wrapper = mount(TenantBillsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('Hóa đơn Tháng 8/2026')
    expect(wrapper.text()).toContain('3.750.000 đ')
  })

  it('initiates bill payment with POST /payments/bills/{id}/checkout', async () => {
    const wrapper = mount(TenantBillsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openCheckoutModal({
      id: 'bill-101',
      totalAmount: 3750000,
    })
    vm.selectedGateway = 'MoMo'

    await vm.handleStartCheckout()

    expect(mockPost).toHaveBeenCalledWith('/payments/bills/bill-101/checkout', {
      provider: 'MoMo',
    })
  })
})
