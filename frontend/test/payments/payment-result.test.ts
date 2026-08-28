import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import PaymentResultPage from '../../pages/payments/result.vue'

const mockGet = vi.fn().mockResolvedValue({
  id: 'txn-123',
  providerOrderId: 'ORDER_998877',
  provider: 'MoMo',
  amount: 3500000,
  status: 'Succeeded',
  initiatedAt: '2026-08-28T12:00:00Z',
  completedAt: '2026-08-28T12:01:00Z',
})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
}))

describe('PaymentResultPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders success state when outcome=Succeeded', async () => {
    vi.stubGlobal('useRoute', () => ({
      query: { outcome: 'Succeeded', transactionId: 'txn-123' }
    }))

    const wrapper = mount(PaymentResultPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('Thanh toán Thành công!')
    expect(wrapper.text()).toContain('ORDER_998877')
    expect(wrapper.text()).toContain('MoMo')
  })

  it('renders failed state when outcome=Failed', async () => {
    vi.stubGlobal('useRoute', () => ({
      query: { outcome: 'Failed', transactionId: 'txn-123' }
    }))

    const wrapper = mount(PaymentResultPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('Thanh toán Không thành công')
    expect(wrapper.text()).toContain('Thử thanh toán lại')
  })
})
