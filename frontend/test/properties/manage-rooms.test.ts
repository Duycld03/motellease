import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import PropertyDashboardPage from '../../pages/owner/properties/[id]/index.vue'

const mockPost = vi.fn().mockResolvedValue({ id: 'res-id' })
const mockPut = vi.fn().mockResolvedValue({})
const mockDelete = vi.fn().mockResolvedValue({})

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/room-types')) {
    return Promise.resolve([
      { id: 'rt-1', typeName: 'Phòng Studio', price: 3500000, roomSizeM2: 25, maxOccupants: 2, facilities: [] }
    ])
  }
  if (url.includes('/rooms')) {
    return Promise.resolve([
      { id: 'rm-101', roomNumber: '101', status: 'Available', currentElectricityReading: 120, currentWaterReading: 45 }
    ])
  }
  if (url.includes('/facilities')) {
    return Promise.resolve([
      { id: 'fac-1', name: 'Điều hòa', codeName: 'ac' }
    ])
  }
  if (url.includes('/my/boarding-houses/')) {
    return Promise.resolve({
      id: '01a047f0-a19c-72aa-99df-5dcae5b61001',
      name: 'Nhà trọ Cầu Giấy',
      listingStatus: 'Draft',
      electricityUnitPrice: 3500,
      waterUnitPrice: 30000,
      roomCounts: { total: 1, available: 1, reserved: 0, occupied: 0, maintenance: 0 }
    })
  }
  return Promise.resolve([])
})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
  put: mockPut,
  delete: mockDelete,
}))

describe('Owner Property Detail - Rooms & Room Types Management', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('creates a new room type with SaveRoomTypeRequest contract', async () => {
    const wrapper = mount(PropertyDashboardPage)
    await new Promise(r => setTimeout(r, 100))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openCreateRoomTypeModal()
    vm.roomTypeForm.typeName = 'Phòng Gác Xép'
    vm.roomTypeForm.price = 2800000
    vm.roomTypeForm.roomSizeM2 = 20
    vm.roomTypeForm.maxOccupants = 2

    await vm.handleSaveRoomType()

    expect(mockPost).toHaveBeenCalledWith('/my/boarding-houses/01a047f0-a19c-72aa-99df-5dcae5b61001/room-types', expect.objectContaining({
      typeName: 'Phòng Gác Xép',
      price: 2800000,
      roomSizeM2: 20,
    }))
  })

  it('updates meter readings with electricityReading and waterReading', async () => {
    const wrapper = mount(PropertyDashboardPage)
    await new Promise(r => setTimeout(r, 100))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openMeterModal({
      id: 'rm-101',
      roomNumber: '101',
      currentElectricityReading: 120,
      currentWaterReading: 45,
    })

    vm.meterForm.electricityReading = 150
    vm.meterForm.waterReading = 55

    await vm.handleSaveMeterReadings()

    expect(mockPut).toHaveBeenCalledWith('/my/rooms/rm-101/meter-readings', {
      electricityReading: 150,
      waterReading: 55,
    })
  })
})
