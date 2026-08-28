import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import OwnerExpensesPage from '../../pages/owner/expenses.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/my/boarding-houses') && !url.includes('/expenses')) {
    return Promise.resolve({
      items: [
        { id: 'house-1', name: 'Nhà trọ Cầu Giấy' }
      ]
    })
  }
  return Promise.resolve({
    items: [
      {
        id: 'exp-1',
        boardingHouseId: 'house-1',
        boardingHouseName: 'Nhà trọ Cầu Giấy',
        month: 8,
        year: 2026,
        electricityOld: 1000,
        electricityNew: 1500,
        electricityQty: 500,
        electricityAmount: 1500000,
        waterOld: 100,
        waterNew: 130,
        waterQty: 30,
        waterAmount: 600000,
        otherExpenses: [],
        otherExpensesTotal: 0,
        totalExpense: 2100000,
        createdAt: '2026-08-28T10:00:00Z',
      }
    ],
    totalCount: 1,
  })
})

const mockPost = vi.fn().mockResolvedValue({ id: 'exp-new-1' })
const mockPut = vi.fn().mockResolvedValue({})
const mockDelete = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
  put: mockPut,
  delete: mockDelete,
}))

describe('OwnerExpensesPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('records master utility and operating expense for a property', async () => {
    const wrapper = mount(OwnerExpensesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.selectedHouseId = 'house-1'
    vm.openCreateExpenseModal()
    await wrapper.vm.$nextTick()
    vm.form.month = 8
    vm.form.year = 2026
    vm.form.electricityOld = 1000
    vm.form.electricityNew = 1500
    vm.form.electricityQty = 500
    vm.form.electricityAmount = 1500000
    vm.form.waterOld = 100
    vm.form.waterNew = 130
    vm.form.waterQty = 30
    vm.form.waterAmount = 600000

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPost).toHaveBeenCalledWith('/my/boarding-houses/house-1/expenses', expect.objectContaining({
      month: 8,
      year: 2026,
      electricityAmount: 1500000,
      waterAmount: 600000,
    }))
  })
})
