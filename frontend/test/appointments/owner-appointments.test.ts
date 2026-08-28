import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import OwnerAppointmentsPage from '../../pages/owner/appointments.vue'

const mockPut = vi.fn().mockResolvedValue({})
const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'apt-1',
      roomNumber: '101',
      boardingHouseName: 'Nhà trọ Cầu Giấy',
      tenantFullName: 'Nguyen Van A',
      tenantPhoneNumber: '0912345678',
      appointmentDate: '2026-08-29T09:00:00Z',
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

describe('OwnerAppointmentsPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('approves appointment with PUT /appointments/{id}/approve', async () => {
    const wrapper = mount(OwnerAppointmentsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const approveButton = wrapper.findAll('button').find(b => b.text().includes('Duyệt hẹn'))
    expect(approveButton).toBeDefined()
    await approveButton!.trigger('click')

    expect(mockPut).toHaveBeenCalledWith('/appointments/apt-1/approve')
  })

  it('rejects appointment with PUT /appointments/{id}/reject and reason payload', async () => {
    const wrapper = mount(OwnerAppointmentsPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openRejectModal({
      id: 'apt-1',
      tenantFullName: 'Nguyen Van A',
    })
    vm.rejectReason = 'Chủ nhà bận khung giờ này'

    await vm.handleConfirmReject()

    expect(mockPut).toHaveBeenCalledWith('/appointments/apt-1/reject', {
      reason: 'Chủ nhà bận khung giờ này',
    })
  })
})
