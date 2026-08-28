import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import CreatePropertyPage from '../../pages/owner/properties/create.vue'
import { BoardingHouseType } from '../../types/enums'

const mockPost = vi.fn().mockResolvedValue({ id: 'house-new-123' })
const mockPut = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  post: mockPost,
  put: mockPut,
}))

describe('CreatePropertyPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders all required form sections for new property creation', () => {
    const wrapper = mount(CreatePropertyPage)
    expect(wrapper.find('form').exists()).toBe(true)
    expect(wrapper.text()).toContain('Thông tin cơ bản')
    expect(wrapper.text()).toContain('Địa chỉ & Vị trí bản đồ')
  })

  it('submits property info and utility prices to the backend API', async () => {
    const wrapper = mount(CreatePropertyPage)
    
    // Fill basic info
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('Nhà trọ MotelLease An Phú')

    // Set address fields directly on component instance/form
    const vm = wrapper.vm as any
    vm.form.province = 'Thành phố Hà Nội'
    vm.form.district = 'Quận Cầu Giấy'
    vm.form.ward = 'Phường Dịch Vọng'
    vm.form.addressLine = '123 Cầu Giấy'
    vm.form.electricityUnitPrice = 3500
    vm.form.waterUnitPrice = 30000

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPost).toHaveBeenCalledWith('/my/boarding-houses', expect.objectContaining({
      name: 'Nhà trọ MotelLease An Phú',
      province: 'Thành phố Hà Nội',
      district: 'Quận Cầu Giấy',
      ward: 'Phường Dịch Vọng',
      addressLine: '123 Cầu Giấy',
    }))

    expect(mockPut).toHaveBeenCalledWith('/my/boarding-houses/house-new-123/utility-prices', {
      electricityUnitPrice: 3500,
      waterUnitPrice: 30000,
    })
  })
})
