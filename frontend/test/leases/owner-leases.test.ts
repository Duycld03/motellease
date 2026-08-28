import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import OwnerLeasesPage from '../../pages/owner/leases.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/termination-preview')) {
    return Promise.resolve({
      leaseId: 'lease-201',
      depositHeld: 3500000,
      electricityQty: 20,
      electricityAmount: 70000,
      waterQty: 3,
      waterAmount: 90000,
      depositDeducted: 0,
      depositRefunded: 3340000,
    })
  }
  if (url.includes('/extension-requests')) {
    return Promise.resolve({
      items: [
        {
          id: 'ext-1',
          roomNumber: '101',
          boardingHouseName: 'Nhà trọ Cầu Giấy',
          requesterFullName: 'Nguyen Van A',
          currentEndDate: '2026-09-01',
          requestedEndDate: '2027-03-01',
          status: 'Pending',
        }
      ],
      totalCount: 1,
    })
  }
  return Promise.resolve({
    items: [
      {
        id: 'lease-201',
        roomNumber: '101',
        boardingHouseName: 'Nhà trọ Cầu Giấy',
        primaryTenantFullName: 'Nguyen Van A',
        startDate: '2026-03-01',
        endDate: '2026-09-01',
        monthlyRent: 3500000,
        depositHeld: 3500000,
        status: 'Active',
        tenants: [
          { id: 't-1', fullName: 'Nguyen Van A', isPrimary: true },
          { id: 't-2', fullName: 'Tran Thi B', isPrimary: false, phoneNumber: '0987654321' }
        ]
      }
    ],
    totalCount: 1,
  })
})

const mockPost = vi.fn().mockResolvedValue({})
const mockPut = vi.fn().mockResolvedValue({})
const mockDelete = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
  put: mockPut,
  delete: mockDelete,
}))

describe('OwnerLeasesPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('adds a co-tenant to an active lease', async () => {
    const wrapper = mount(OwnerLeasesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openAddTenantModal({ id: 'lease-201' })
    vm.tenantForm.fullName = 'Le Van C'
    vm.tenantForm.phoneNumber = '0911223344'
    vm.tenantForm.idCardNumber = '001203009988'

    await vm.handleSaveTenant()

    expect(mockPost).toHaveBeenCalledWith('/leases/lease-201/tenants', {
      fullName: 'Le Van C',
      phoneNumber: '0911223344',
      idCardNumber: '001203009988',
    })
  })

  it('removes a co-tenant from a lease', async () => {
    const wrapper = mount(OwnerLeasesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleRemoveTenant('lease-201', 't-2', 'Tran Thi B')

    expect(mockDelete).toHaveBeenCalledWith('/leases/lease-201/tenants/t-2')
  })

  it('conducts move-out settlement and terminates lease', async () => {
    const wrapper = mount(OwnerLeasesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openTerminateModal({
      id: 'lease-201',
      roomNumber: '101',
      depositHeld: 3500000,
    })

    vm.terminateForm.finalElectricityReading = 150
    vm.terminateForm.finalWaterReading = 30
    vm.terminateForm.depositDeducted = 100000
    vm.terminateForm.endReason = 'Trả phòng đúng hạn'

    await vm.handleConfirmTerminate()

    expect(mockPost).toHaveBeenCalledWith('/leases/lease-201/terminate', {
      finalElectricityReading: 150,
      finalWaterReading: 30,
      depositDeducted: 100000,
      endReason: 'Trả phòng đúng hạn',
    })
  })

  it('approves lease extension request', async () => {
    const wrapper = mount(OwnerLeasesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleApproveExtension({
      id: 'ext-1',
      roomNumber: '101',
      requestedEndDate: '2027-03-01',
    })

    expect(mockPut).toHaveBeenCalledWith('/extension-requests/ext-1/approve', {})
  })

  it('rejects lease extension request with owner note', async () => {
    const wrapper = mount(OwnerLeasesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openRejectExtensionModal({
      id: 'ext-1',
    })
    vm.rejectExtReason = 'Chủ nhà chuẩn bị bán nhà'

    await vm.handleConfirmRejectExtension()

    expect(mockPut).toHaveBeenCalledWith('/extension-requests/ext-1/reject', {
      ownerNote: 'Chủ nhà chuẩn bị bán nhà',
    })
  })
})
