import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseSelect from '../../components/common/BaseSelect.vue'

describe('BaseSelect.vue', () => {
  const options = [
    { label: '3 tháng', value: 3 },
    { label: '6 tháng', value: 6 },
    { label: '12 tháng', value: 12 },
  ]

  it('renders select label and currently selected option', () => {
    const wrapper = mount(BaseSelect, {
      props: {
        modelValue: 6,
        options,
        label: 'Thời hạn hợp đồng',
      },
    })

    expect(wrapper.text()).toContain('Thời hạn hợp đồng')
    expect(wrapper.text()).toContain('6 tháng')
  })

  it('opens dropdown menu on click and emits selected value', async () => {
    const wrapper = mount(BaseSelect, {
      props: {
        modelValue: 3,
        options,
      },
    })

    await wrapper.find('button[type="button"]').trigger('click')
    const optionElements = wrapper.findAll('.cursor-pointer')
    expect(optionElements.length).toBeGreaterThan(0)

    // Click on 12 months
    const targetOpt = optionElements.find(opt => opt.text().includes('12 tháng'))
    if (targetOpt) {
      await targetOpt.trigger('click')
      expect(wrapper.emitted('update:modelValue')).toBeTruthy()
      expect(wrapper.emitted('update:modelValue')![0][0]).toBe(12)
    }
  })
})
