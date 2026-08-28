import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseDatePicker from '../../components/common/BaseDatePicker.vue'

describe('BaseDatePicker.vue', () => {
  it('renders with placeholder and toggle menu on click', async () => {
    const wrapper = mount(BaseDatePicker, {
      props: {
        modelValue: '',
        placeholder: 'Chọn ngày thử nghiệm',
      },
    })

    expect(wrapper.text()).toContain('Chọn ngày thử nghiệm')

    // Click button to open calendar popover
    await wrapper.find('button[type="button"]').trigger('click')
    expect(wrapper.find('.grid-cols-7').exists()).toBe(true)
  })

  it('selects a date and emits formatted date', async () => {
    const wrapper = mount(BaseDatePicker, {
      props: {
        modelValue: '',
      },
    })

    await wrapper.find('button[type="button"]').trigger('click')
    
    // Find a day button inside the calendar
    const dayButtons = wrapper.findAll('.grid-cols-7 button')
    expect(dayButtons.length).toBeGreaterThan(0)

    // Click 15th day
    const dayBtn = dayButtons.find(b => b.text() === '15')
    if (dayBtn) {
      await dayBtn.trigger('click')
      expect(wrapper.emitted('update:modelValue')).toBeTruthy()
      const emitted = wrapper.emitted('update:modelValue')![0][0] as string
      expect(emitted).toMatch(/^\d{4}-\d{2}-15$/)
    }
  })

  it('supports enableTime mode with hour and minute selectors', async () => {
    const wrapper = mount(BaseDatePicker, {
      props: {
        modelValue: '',
        enableTime: true,
      },
    })

    await wrapper.find('button[type="button"]').trigger('click')
    expect(wrapper.find('select').exists()).toBe(true)
  })
})
