import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import StaffTasksPage from '../../pages/staff/tasks.vue'

const mockGet = vi.fn().mockImplementation((url: string) => {
  if (url.includes('/staff/properties')) {
    return Promise.resolve([
      { id: 'house-1', name: 'Nhà trọ Cầu Giấy' }
    ])
  }
  return Promise.resolve({
    items: [
      {
        id: 'task-1',
        boardingHouseName: 'Nhà trọ Cầu Giấy',
        assignedToFullName: 'Nguyen Van B',
        title: 'Sửa vòi nước phòng 101',
        details: 'Khách báo rò rỉ',
        priority: 'High',
        status: 'InProgress',
        dueDate: '2026-08-30',
        createdAt: '2026-08-28T10:00:00Z',
      }
    ],
    totalCount: 1,
  })
})

const mockPost = vi.fn().mockResolvedValue({ id: 'task-new-1' })
const mockPut = vi.fn().mockResolvedValue({})

vi.stubGlobal('useApi', () => ({
  get: mockGet,
  post: mockPost,
  put: mockPut,
}))

describe('StaffTasksPage.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('updates task status with PUT /tasks/{id}/status', async () => {
    const wrapper = mount(StaffTasksPage)
    await new Promise(r => setTimeout(r, 50))
    await wrapper.vm.$nextTick()

    const vm = wrapper.vm as any
    await vm.handleUpdateStatus('task-1', 'Completed')

    expect(mockPut).toHaveBeenCalledWith('/tasks/task-1/status', {
      status: 'Completed',
    })
  })
})
