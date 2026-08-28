import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import OwnerAnalyticsPage from '../../pages/owner/analytics.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/my/boarding-houses')) {
    return Promise.resolve({
      items: [
        { id: 'house-1', name: 'Nhà trọ Cầu Giấy' }
      ]
    })
  }
  if (url.includes('/my/stats/revenue/years')) {
    return Promise.resolve({ years: [2025, 2026] })
  }
  if (url.includes('/my/stats/revenue')) {
    return Promise.resolve({
      year: 2026,
      totalRevenue: 240000000,
      totalRentRevenue: 200000000,
      totalUtilityRevenue: 40000000,
      totalPaidBills: 60,
      monthlyBreakdown: [
        { month: 8, revenue: 30000000, rentRevenue: 25000000, utilityRevenue: 5000000, paidBillsCount: 8 }
      ]
    })
  }
  if (url.includes('/my/stats/profit')) {
    return Promise.resolve({
      year: 2026,
      totalRevenue: 240000000,
      totalExpense: 40000000,
      totalNetProfit: 200000000,
      monthlyBreakdown: [
        { month: 8, revenue: 30000000, expense: 5000000, netProfit: 25000000 }
      ]
    })
  }
  if (url.includes('/my/stats/occupancy')) {
    return Promise.resolve({
      totalRooms: 10,
      rentedRooms: 8,
      reservedRooms: 1,
      vacantRooms: 1,
      overallOccupancyRate: 80.0,
      houses: []
    })
  }
  return Promise.resolve({})
})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
}))

describe('OwnerAnalyticsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders revenue, profit and occupancy metrics', async () => {
    const wrapper = mount(OwnerAnalyticsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('Thống kê doanh thu')
    expect(wrapper.text()).toContain('240.000.000 đ')
    expect(wrapper.text()).toContain('40.000.000 đ')
  })
})
