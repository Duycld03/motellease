import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import OwnerBillsPage from '../../pages/owner/bills.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/leases')) {
    return Promise.resolve({
      items: [
        { id: 'lease-1', roomId: 'rm-101', roomNumber: '101', boardingHouseName: 'Nhà trọ Cầu Giấy' }
      ]
    })
  }
  return Promise.resolve({
    items: [
      {
        id: 'bill-draft-1',
        roomNumber: '101',
        boardingHouseName: 'Nhà trọ Cầu Giấy',
        month: 8,
        year: 2026,
        totalAmount: 3750000,
        status: 'Draft',
        dueDate: '2026-09-05',
      }
    ],
    totalCount: 1,
  })
})

const mockPost = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/preview')) {
    return Promise.resolve({
      rentAmount: 3500000,
      electricityOld: 100,
      electricityNew: 140,
      electricityQty: 40,
      electricityAmount: 140000,
      waterOld: 20,
      waterNew: 22,
      waterQty: 2,
      waterAmount: 60000,
      additionalFeeTotal: 0,
      totalAmount: 3700000,
    })
  }
  return Promise.resolve({ id: 'bill-created-1' })
})

const mockPut = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
  put: mockPut,
}))

describe('OwnerBillsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('previews bill calculation with POST /bills/preview', async () => {
    const wrapper = mount(OwnerBillsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.billForm.roomId = 'rm-101'
    vm.billForm.month = 8
    vm.billForm.year = 2026
    vm.billForm.electricityNew = 140
    vm.billForm.waterNew = 22

    await vm.fetchBillPreview()

    expect(mockPost).toHaveBeenCalledWith('/bills/preview', {
      roomId: 'rm-101',
      month: 8,
      year: 2026,
      electricityNew: 140,
      waterNew: 22,
    })
  })

  it('creates and immediately issues a monthly bill', async () => {
    const wrapper = mount(OwnerBillsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.billForm.roomId = 'rm-101'
    vm.billForm.month = 8
    vm.billForm.year = 2026
    vm.billForm.electricityNew = 140
    vm.billForm.waterNew = 22
    vm.billForm.dueDate = '2026-09-05'

    await vm.submitBillWithStatus('Issued')

    expect(mockPost).toHaveBeenCalledWith('/bills', {
      roomId: 'rm-101',
      month: 8,
      year: 2026,
      electricityNew: 140,
      waterNew: 22,
      dueDate: '2026-09-05',
      status: 'Issued',
    })
  })

  it('issues a draft bill with PUT /bills/{id}/issue', async () => {
    const wrapper = mount(OwnerBillsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleIssueDraft({
      id: 'bill-draft-1',
      dueDate: '2026-09-05',
    })

    expect(mockPut).toHaveBeenCalledWith('/bills/bill-draft-1/issue', {
      dueDate: '2026-09-05',
    })
  })

  it('cancels a bill with PUT /bills/{id}/cancel', async () => {
    const wrapper = mount(OwnerBillsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleCancelBill('bill-draft-1')

    expect(mockPut).toHaveBeenCalledWith('/bills/bill-draft-1/cancel', {})
  })
})
