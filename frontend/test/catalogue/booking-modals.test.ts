import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import PropertyDetailsPage from '../../pages/boarding-houses/[id].vue'

const mockPost = vi.fn().mockResolvedValue({ id: 'dep-1' })
const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.endsWith('/rooms')) {
    return Promise.resolve([
      {
        id: 'room-101',
        roomNumber: 'P.101',
        floor: 1,
        basePrice: 3500000,
        status: 'Available',
        roomTypeName: 'Phòng Studio',
        roomSizeM2: 25,
        maxOccupants: 2,
        facilities: []
      }
    ])
  }
  if (url.endsWith('/reviews')) {
    return Promise.resolve({ items: [], totalCount: 0 })
  }
  if (url.includes('/saved-listings')) {
    return Promise.resolve({ items: [], totalCount: 0 })
  }
  if (url.includes('/boarding-houses/')) {
    return Promise.resolve({
      id: '01a047f0-a19c-72aa-99df-5dcae5b61001',
      name: 'Nhà trọ Cầu Giấy',
      address: '123 Cầu Giấy, Hà Nội',
      primaryImageUrl: 'https://images.unsplash.com/photo-1',
      electricityPrice: 3500,
      waterPrice: 30000,
      roomTypes: [
        {
          id: 'rt-1',
          name: 'Phòng Studio',
          basePrice: 3500000,
          area: 25,
          maxOccupants: 2,
          vacantRooms: [],
          facilities: []
        }
      ],
      reviews: []
    })
  }
  return Promise.resolve([])
})

vi.stubGlobal('useApi', () => ({
  post: mockPost,
  get: mockGet,
  delete: vi.fn(),
}))

vi.stubGlobal('useAuth', () => ({
  isAuthenticated: { value: true },
  isTenant: { value: true },
  user: { value: { id: 'usr-1', fullName: 'Người Thuê Demo' } },
}))

describe('Boarding House Details - Booking Forms', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders property details and vacant rooms', async () => {
    const wrapper = mount(PropertyDetailsPage)
    // Wait for async fetch
    await new Promise(r => setTimeout(r, 100))
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('Nhà trọ Cầu Giấy')
    expect(wrapper.text()).toContain('P.101')
  })

  it('can open deposit modal and submit deposit request with requestedStartDate and requestedTermMonths', async () => {
    const wrapper = mount(PropertyDetailsPage)
    await new Promise(r => setTimeout(r, 100))
    await wrapper.vm.$nextTick()

    // Click on deposit room button
    const depositButtons = wrapper.findAll('button').filter(b => b.text().includes('Cọc giữ phòng'))
    expect(depositButtons.length).toBeGreaterThan(0)
    await depositButtons[0].trigger('click')
    await wrapper.vm.$nextTick()

    // Submit deposit form inside modal
    const forms = wrapper.findAll('form')
    const depositForm = forms.find(f => f.text().includes('Gửi yêu cầu đặt cọc'))
    expect(depositForm).toBeDefined()

    await depositForm!.trigger('submit.prevent')

    expect(mockPost).toHaveBeenCalledWith('/deposits', expect.objectContaining({
      roomId: 'room-101',
      requestedTermMonths: 6,
    }))
  })
})
