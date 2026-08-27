<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-slate-900 dark:text-white">{{ $t('nav.expenses') }}</h1>
        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
          Theo dõi hóa đơn điện nước tổng đầu vào và các khoản chi phí vận hành khu trọ
        </p>
      </div>
      <div class="flex items-center gap-2">
        <BaseButton variant="outline" size="sm" @click="fetchExpenses">
          🔄 Làm mới
        </BaseButton>
        <BaseButton variant="primary" size="sm" @click="openCreateExpenseModal">
          + Ghi nhận chi phí
        </BaseButton>
      </div>
    </div>

    <!-- Filter Bar: Property, Month, Year -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="flex items-center gap-3 w-full sm:w-auto">
        <label class="text-xs font-semibold text-slate-600 dark:text-slate-400 shrink-0">Khu trọ:</label>
        <select v-model="selectedHouseId" class="input-field !text-xs !py-1.5 w-full sm:w-64" @change="fetchExpenses">
          <option v-for="h in boardingHouses" :key="h.id" :value="h.id">
            {{ h.name }}
          </option>
        </select>
      </div>

      <div class="flex items-center gap-2 shrink-0">
        <select v-model="filterMonth" class="input-field !text-xs !py-1.5 w-28" @change="fetchExpenses">
          <option :value="null">Tất cả tháng</option>
          <option v-for="m in 12" :key="m" :value="m">Tháng {{ m }}</option>
        </select>
        <select v-model="filterYear" class="input-field !text-xs !py-1.5 w-24" @change="fetchExpenses">
          <option :value="2025">2025</option>
          <option :value="2026">2026</option>
          <option :value="2027">2027</option>
        </select>
      </div>
    </div>

    <!-- Quick Stats Cards for filtered expenses -->
    <div v-if="expenses.length > 0" class="grid grid-cols-2 sm:grid-cols-4 gap-3">
      <div class="p-3.5 bg-rose-50 dark:bg-rose-950/30 rounded-xl border border-rose-100 dark:border-rose-900/40">
        <span class="text-[10px] text-rose-500 font-semibold uppercase block">Tổng chi phí vận hành</span>
        <span class="text-base font-extrabold text-rose-700 dark:text-rose-300 mt-0.5 block">
          {{ formatCurrency(totalFilteredExpense) }}
        </span>
      </div>
      <div class="p-3.5 bg-amber-50 dark:bg-amber-950/30 rounded-xl border border-amber-100 dark:border-amber-900/40">
        <span class="text-[10px] text-amber-500 font-semibold uppercase block">⚡ Tiền điện tổng</span>
        <span class="text-base font-extrabold text-amber-700 dark:text-amber-300 mt-0.5 block">
          {{ formatCurrency(totalElectricityExpense) }}
        </span>
      </div>
      <div class="p-3.5 bg-blue-50 dark:bg-blue-950/30 rounded-xl border border-blue-100 dark:border-blue-900/40">
        <span class="text-[10px] text-blue-500 font-semibold uppercase block">💧 Tiền nước tổng</span>
        <span class="text-base font-extrabold text-blue-700 dark:text-blue-300 mt-0.5 block">
          {{ formatCurrency(totalWaterExpense) }}
        </span>
      </div>
      <div class="p-3.5 bg-purple-50 dark:bg-purple-950/30 rounded-xl border border-purple-100 dark:border-purple-900/40">
        <span class="text-[10px] text-purple-500 font-semibold uppercase block">🛠️ Chi phí khác</span>
        <span class="text-base font-extrabold text-purple-700 dark:text-purple-300 mt-0.5 block">
          {{ formatCurrency(totalOtherExpense) }}
        </span>
      </div>
    </div>

    <!-- Expenses List -->
    <div v-if="isLoading" class="py-16 text-center">
      <LoadingSpinner size="md" />
    </div>

    <div v-else-if="expenses.length === 0" class="p-12 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center text-xs text-slate-400">
      <svg class="w-12 h-12 text-slate-300 dark:text-slate-700 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>
      <p class="font-medium text-slate-500 dark:text-slate-400">Chưa ghi nhận chi phí nào cho khu trọ này.</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="e in expenses"
        :key="e.id"
        class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4 transition-all"
      >
        <!-- Header -->
        <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <span class="text-base font-bold text-slate-900 dark:text-white">{{ e.boardingHouseName }}</span>
              <span class="text-xs font-bold px-2 py-0.5 rounded-md bg-rose-50 dark:bg-rose-950/50 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800">
                Chi phí Tháng {{ e.month }}/{{ e.year }}
              </span>
            </div>
            <p class="text-xs text-slate-500 dark:text-slate-400">
              Ghi nhận lúc: {{ formatRelativeTime(e.createdAt) }}
            </p>
          </div>

          <div class="flex items-center gap-3">
            <div class="text-right">
              <span class="text-xs text-slate-400 block">Tổng chi</span>
              <span class="text-base font-extrabold text-rose-600 dark:text-rose-400">{{ formatCurrency(e.totalExpense) }}</span>
            </div>
          </div>
        </div>

        <!-- Breakdown Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs">
          <!-- Electricity -->
          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">⚡ Điện tổng đầu vào</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(e.electricityAmount) }}</span>
            <span class="text-[10px] text-slate-400 block mt-0.5">
              {{ e.electricityOld }} ➔ {{ e.electricityNew }} ({{ e.electricityQty }} kWh)
            </span>
          </div>

          <!-- Water -->
          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">💧 Nước tổng đầu vào</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(e.waterAmount) }}</span>
            <span class="text-[10px] text-slate-400 block mt-0.5">
              {{ e.waterOld }} ➔ {{ e.waterNew }} ({{ e.waterQty }} m³)
            </span>
          </div>

          <!-- Other Fees -->
          <div class="p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-800">
            <span class="text-[10px] text-slate-400 uppercase font-semibold block">🛠️ Các khoản chi khác</span>
            <span class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-0.5 block">{{ formatCurrency(e.otherExpensesTotal) }}</span>
            <span class="text-[10px] text-slate-400 block mt-0.5">{{ e.otherExpenses?.length || 0 }} khoản mục</span>
          </div>
        </div>

        <!-- Other Expenses List if any -->
        <div v-if="e.otherExpenses && e.otherExpenses.length > 0" class="p-3 bg-slate-50/50 dark:bg-slate-800/30 rounded-xl text-xs space-y-1">
          <div class="font-semibold text-slate-700 dark:text-slate-300 text-[11px] mb-1">Chi tiết các khoản chi khác:</div>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-2 text-[11px] text-slate-600 dark:text-slate-400">
            <div v-for="(fee, idx) in e.otherExpenses" :key="idx" class="flex justify-between">
              <span>• {{ fee.feeName }}:</span>
              <span class="font-medium text-slate-900 dark:text-white">{{ formatCurrency(fee.feeAmount) }}</span>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" @click="openEditExpenseModal(e)">
            ✏️ Chỉnh sửa
          </BaseButton>
          <BaseButton variant="ghost" size="sm" class="text-red-600 hover:text-red-700" @click="handleDeleteExpense(e.id)">
            Xóa
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- MODAL: Create / Edit Expense -->
    <BaseModal
      v-model="isModalOpen"
      :title="isEditing ? 'Chỉnh sửa Chi phí Vận hành' : 'Ghi nhận Chi phí Vận hành Khu trọ'"
      max-width="lg"
    >
      <form @submit.prevent="handleSubmitExpense" class="space-y-4">
        <!-- Property Selector (only when creating) -->
        <div v-if="!isEditing">
          <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
            Khu trọ <span class="text-red-500">*</span>
          </label>
          <select v-model="form.boardingHouseId" class="input-field !text-xs !py-2" required>
            <option v-for="h in boardingHouses" :key="h.id" :value="h.id">
              {{ h.name }}
            </option>
          </select>
        </div>

        <!-- Month & Year -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              Tháng <span class="text-red-500">*</span>
            </label>
            <select v-model.number="form.month" class="input-field !text-xs !py-2" :disabled="isEditing" required>
              <option v-for="m in 12" :key="m" :value="m">Tháng {{ m }}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              Năm <span class="text-red-500">*</span>
            </label>
            <input
              v-model.number="form.year"
              type="number"
              class="input-field !text-xs !py-2"
              :disabled="isEditing"
              required
            />
          </div>
        </div>

        <!-- Electricity master bill -->
        <div class="p-3 bg-slate-50 dark:bg-slate-800/40 rounded-xl space-y-2">
          <span class="text-xs font-bold text-slate-800 dark:text-slate-200 block">⚡ Hóa đơn Điện tổng đầu vào</span>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
            <div>
              <label class="block text-[10px] text-slate-500">Số cũ</label>
              <input v-model.number="form.electricityOld" type="number" step="0.1" min="0" class="input-field !text-xs !py-1.5" @input="calcElectricityQty" />
            </div>
            <div>
              <label class="block text-[10px] text-slate-500">Số mới</label>
              <input v-model.number="form.electricityNew" type="number" step="0.1" min="0" class="input-field !text-xs !py-1.5" @input="calcElectricityQty" />
            </div>
            <div>
              <label class="block text-[10px] text-slate-500">Tiêu thụ (kWh)</label>
              <input v-model.number="form.electricityQty" type="number" step="0.1" min="0" class="input-field !text-xs !py-1.5" />
            </div>
            <div>
              <label class="block text-[10px] text-slate-500">Tiền điện (VNĐ)</label>
              <input v-model.number="form.electricityAmount" type="number" min="0" class="input-field !text-xs !py-1.5" required />
            </div>
          </div>
        </div>

        <!-- Water master bill -->
        <div class="p-3 bg-slate-50 dark:bg-slate-800/40 rounded-xl space-y-2">
          <span class="text-xs font-bold text-slate-800 dark:text-slate-200 block">💧 Hóa đơn Nước tổng đầu vào</span>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
            <div>
              <label class="block text-[10px] text-slate-500">Số cũ</label>
              <input v-model.number="form.waterOld" type="number" step="0.1" min="0" class="input-field !text-xs !py-1.5" @input="calcWaterQty" />
            </div>
            <div>
              <label class="block text-[10px] text-slate-500">Số mới</label>
              <input v-model.number="form.waterNew" type="number" step="0.1" min="0" class="input-field !text-xs !py-1.5" @input="calcWaterQty" />
            </div>
            <div>
              <label class="block text-[10px] text-slate-500">Tiêu thụ (m³)</label>
              <input v-model.number="form.waterQty" type="number" step="0.1" min="0" class="input-field !text-xs !py-1.5" />
            </div>
            <div>
              <label class="block text-[10px] text-slate-500">Tiền nước (VNĐ)</label>
              <input v-model.number="form.waterAmount" type="number" min="0" class="input-field !text-xs !py-1.5" required />
            </div>
          </div>
        </div>

        <!-- Other Expenses List -->
        <div class="space-y-2">
          <div class="flex items-center justify-between">
            <label class="text-xs font-bold text-slate-800 dark:text-slate-200">
              🛠️ Các khoản chi khác (Bảo trì, vệ sinh, rác, camera...)
            </label>
            <button type="button" class="text-xs font-semibold text-primary-600 dark:text-primary-400 hover:underline" @click="addOtherExpenseRow">
              + Thêm khoản chi
            </button>
          </div>

          <div v-for="(item, idx) in form.otherExpenses" :key="idx" class="flex items-center gap-2">
            <input
              v-model="item.feeName"
              type="text"
              class="input-field !text-xs !py-1.5 flex-1"
              placeholder="Tên khoản chi (VD: Thay bóng đèn hành lang)"
              required
            />
            <input
              v-model.number="item.feeAmount"
              type="number"
              min="0"
              class="input-field !text-xs !py-1.5 w-36"
              placeholder="Số tiền (VNĐ)"
              required
            />
            <button type="button" class="text-red-500 hover:text-red-700 text-xs px-1" @click="removeOtherExpenseRow(idx)">
              ✕
            </button>
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800">
          <BaseButton variant="outline" size="sm" type="button" @click="isModalOpen = false">
            {{ $t('common.cancel') }}
          </BaseButton>
          <BaseButton variant="primary" size="sm" type="submit" :loading="isSubmitting">
            {{ isEditing ? 'Cập nhật chi phí' : 'Ghi nhận chi phí' }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import BaseButton from '~/components/common/BaseButton.vue'
import BaseModal from '~/components/common/BaseModal.vue'
import LoadingSpinner from '~/components/common/LoadingSpinner.vue'
import type { BoardingHouse, ExpenseResponse, OtherExpenseItem, PagedResult } from '~/types/api'

definePageMeta({
  layout: 'owner',
})

const { get, post, put, delete: deleteApi } = useApi()
const { formatCurrency, formatRelativeTime } = useFormat()
const toast = useToast()

const isLoading = ref(true)
const boardingHouses = ref<BoardingHouse[]>([])
const selectedHouseId = ref<string>('')
const expenses = ref<ExpenseResponse[]>([])

const filterMonth = ref<number | null>(null)
const filterYear = ref<number>(new Date().getFullYear())

const totalFilteredExpense = computed(() => expenses.value.reduce((s, e) => s + e.totalExpense, 0))
const totalElectricityExpense = computed(() => expenses.value.reduce((s, e) => s + e.electricityAmount, 0))
const totalWaterExpense = computed(() => expenses.value.reduce((s, e) => s + e.waterAmount, 0))
const totalOtherExpense = computed(() => expenses.value.reduce((s, e) => s + e.otherExpensesTotal, 0))

const fetchHouses = async () => {
  try {
    const data = await get<PagedResult<BoardingHouse>>('/my/boarding-houses', { pageSize: 50 })
    boardingHouses.value = data.items || []
    if (boardingHouses.value.length > 0 && !selectedHouseId.value) {
      selectedHouseId.value = boardingHouses.value[0].id
    }
  } catch {
    boardingHouses.value = []
  }
}

const fetchExpenses = async () => {
  if (!selectedHouseId.value) return
  isLoading.value = true
  try {
    const data = await get<PagedResult<ExpenseResponse>>(`/my/boarding-houses/${selectedHouseId.value}/expenses`, {
      month: filterMonth.value || undefined,
      year: filterYear.value || undefined,
      page: 1,
      pageSize: 50,
    })
    expenses.value = data.items || []
  } catch {
    expenses.value = []
  } finally {
    isLoading.value = false
  }
}

// Modal State
const isModalOpen = ref(false)
const isEditing = ref(false)
const editingExpenseId = ref<string | null>(null)
const isSubmitting = ref(false)

const form = reactive({
  boardingHouseId: '',
  month: new Date().getMonth() + 1,
  year: new Date().getFullYear(),
  electricityOld: 0,
  electricityNew: 0,
  electricityQty: 0,
  electricityAmount: 0,
  waterOld: 0,
  waterNew: 0,
  waterQty: 0,
  waterAmount: 0,
  otherExpenses: [] as OtherExpenseItem[],
})

const calcElectricityQty = () => {
  form.electricityQty = Math.max(0, form.electricityNew - form.electricityOld)
}

const calcWaterQty = () => {
  form.waterQty = Math.max(0, form.waterNew - form.waterOld)
}

const addOtherExpenseRow = () => {
  form.otherExpenses.push({ feeName: '', feeAmount: 0 })
}

const removeOtherExpenseRow = (idx: number) => {
  form.otherExpenses.splice(idx, 1)
}

const openCreateExpenseModal = () => {
  isEditing.value = false
  editingExpenseId.value = null
  form.boardingHouseId = selectedHouseId.value
  form.month = new Date().getMonth() + 1
  form.year = new Date().getFullYear()
  form.electricityOld = 0
  form.electricityNew = 0
  form.electricityQty = 0
  form.electricityAmount = 0
  form.waterOld = 0
  form.waterNew = 0
  form.waterQty = 0
  form.waterAmount = 0
  form.otherExpenses = []
  isModalOpen.value = true
}

const openEditExpenseModal = (e: ExpenseResponse) => {
  isEditing.value = true
  editingExpenseId.value = e.id
  form.boardingHouseId = e.boardingHouseId
  form.month = e.month
  form.year = e.year
  form.electricityOld = e.electricityOld
  form.electricityNew = e.electricityNew
  form.electricityQty = e.electricityQty
  form.electricityAmount = e.electricityAmount
  form.waterOld = e.waterOld
  form.waterNew = e.waterNew
  form.waterQty = e.waterQty
  form.waterAmount = e.waterAmount
  form.otherExpenses = (e.otherExpenses || []).map((o) => ({ ...o }))
  isModalOpen.value = true
}

const handleSubmitExpense = async () => {
  isSubmitting.value = true
  try {
    if (isEditing.value && editingExpenseId.value) {
      await put(`/my/boarding-houses/${form.boardingHouseId}/expenses/${editingExpenseId.value}`, {
        electricityOld: form.electricityOld,
        electricityNew: form.electricityNew,
        electricityQty: form.electricityQty,
        electricityAmount: form.electricityAmount,
        waterOld: form.waterOld,
        waterNew: form.waterNew,
        waterQty: form.waterQty,
        waterAmount: form.waterAmount,
        otherExpenses: form.otherExpenses.filter((o) => o.feeName.trim() && o.feeAmount > 0),
      })
      toast.success('Cập nhật chi phí thành công!')
    } else {
      await post(`/my/boarding-houses/${form.boardingHouseId}/expenses`, {
        month: form.month,
        year: form.year,
        electricityOld: form.electricityOld,
        electricityNew: form.electricityNew,
        electricityQty: form.electricityQty,
        electricityAmount: form.electricityAmount,
        waterOld: form.waterOld,
        waterNew: form.waterNew,
        waterQty: form.waterQty,
        waterAmount: form.waterAmount,
        otherExpenses: form.otherExpenses.filter((o) => o.feeName.trim() && o.feeAmount > 0),
      })
      toast.success('Ghi nhận chi phí vận hành thành công!')
    }
    isModalOpen.value = false
    await fetchExpenses()
  } catch (err: any) {
    toast.error(err.message || 'Không thể lưu chi phí.')
  } finally {
    isSubmitting.value = false
  }
}

const handleDeleteExpense = async (expenseId: string) => {
  if (!confirm('Bạn có chắc chắn muốn xóa bản ghi chi phí này không?')) return
  try {
    await deleteApi(`/my/boarding-houses/${selectedHouseId.value}/expenses/${expenseId}`)
    toast.success('Đã xóa chi phí.')
    await fetchExpenses()
  } catch (err: any) {
    toast.error(err.message || 'Không thể xóa chi phí.')
  }
}

onMounted(async () => {
  await fetchHouses()
  await fetchExpenses()
})
</script>
