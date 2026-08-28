import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import TenantAppointmentsPage from '../../pages/tenant/appointments.vue'

const mockPut = vi.fn().mockResolvedValue({})
const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'apt-2',
      roomNumber: '202',
      boardingHouseName: 'Chung cư mini Bạch Mai',
      appointmentDate: '2026-08-30T14:00:00Z',
      status: 'Pending',
      createdAt: '2026-08-28T10:00:00Z',
    }
  ],
  totalCount: 1,
})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  put: mockPut,
}))

describe('TenantAppointmentsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('cancels viewing appointment with PUT /appointments/{id}/cancel and reason payload', async () => {
    const wrapper = mount(TenantAppointmentsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openCancelModal({
      id: 'apt-2',
      roomNumber: '202',
      boardingHouseName: 'Chung cư mini Bạch Mai',
    })
    vm.cancelReason = 'Đã tìm được phòng khác gần trường hơn'

    await vm.handleConfirmCancel()

    expect(mockPut).toHaveBeenCalledWith('/appointments/apt-2/cancel', {
      reason: 'Đã tìm được phòng khác gần trường hơn',
    })
  })
})
