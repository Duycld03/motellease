import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AdminFacilitiesPage from '../../pages/admin/facilities.vue'

const mockGet = vi.fn().mockResolvedValue({
  items: [
    {
      id: 'fac-1',
      name: 'Điều hòa',
      codeName: 'air_conditioner',
      iconKey: 'ac',
      description: 'Điều hòa hai chiều Inverter',
      inUseByRoomTypesCount: 5,
    }
  ],
  totalCount: 1,
})

const mockPost = vi.fn().mockResolvedValue({ id: 'fac-new-1' })
const mockPut = vi.fn().mockResolvedValue({})
const mockDelete = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
  put: mockPut,
  delete: mockDelete,
}))

describe('AdminFacilitiesPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('creates a new system facility with POST /admin/facilities', async () => {
    const wrapper = mount(AdminFacilitiesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    vm.openCreateModal()
    await wrapper.vm.$nextTick()
    vm.facilityForm.name = 'Máy giặt riêng'
    vm.facilityForm.codeName = 'washing_machine'
    vm.facilityForm.iconKey = 'washer'
    vm.facilityForm.description = 'Máy giặt cửa trước lồng ngang'

    await wrapper.find('form').trigger('submit.prevent')

    expect(mockPost).toHaveBeenCalledWith('/admin/facilities', {
      name: 'Máy giặt riêng',
      codeName: 'washing_machine',
      iconKey: 'washer',
      description: 'Máy giặt cửa trước lồng ngang',
    })
  })

  it('deletes a facility with DELETE /admin/facilities/{id}', async () => {
    const wrapper = mount(AdminFacilitiesPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleDelete({
      id: 'fac-1',
      name: 'Điều hòa',
    })

    expect(mockDelete).toHaveBeenCalledWith('/admin/facilities/fac-1')
  })
})
