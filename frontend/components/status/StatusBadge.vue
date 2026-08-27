<template>
  <span :class="['inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium', badgeColorClass]">
    <span :class="['w-1.5 h-1.5 rounded-full mr-1.5', dotColorClass]" />
    {{ label }}
  </span>
</template>

<script setup lang="ts">
const props = defineProps<{
  type:
    | 'RoomStatus'
    | 'DepositStatus'
    | 'LeaseStatus'
    | 'BillStatus'
    | 'PaymentStatus'
    | 'RequestStatus'
    | 'ListingStatus'
    | 'TaskPriority'
    | 'MaintenanceStatus'
  status: string
}>()

const { t, te } = useI18n()

const label = computed(() => {
  const i18nKey = `enums.${props.type}.${props.status}`
  return te(i18nKey) ? t(i18nKey) : props.status
})

const badgeColorClass = computed(() => {
  const s = props.status
  switch (props.type) {
    case 'RoomStatus':
      if (s === 'Available') return 'bg-emerald-50 text-emerald-700 border border-emerald-200'
      if (s === 'Reserved') return 'bg-amber-50 text-amber-700 border border-amber-200'
      if (s === 'Occupied') return 'bg-blue-50 text-blue-700 border border-blue-200'
      if (s === 'Maintenance') return 'bg-red-50 text-red-700 border border-red-200'
      break

    case 'DepositStatus':
      if (s === 'Paid' || s === 'Completed') return 'bg-emerald-50 text-emerald-700 border border-emerald-200'
      if (s === 'Accepted' || s === 'Refunding') return 'bg-sky-50 text-sky-700 border border-sky-200'
      if (s === 'Pending') return 'bg-amber-50 text-amber-700 border border-amber-200'
      if (s === 'Rejected' || s === 'Expired') return 'bg-red-50 text-red-700 border border-red-200'
      break

    case 'LeaseStatus':
      if (s === 'Active') return 'bg-emerald-50 text-emerald-700 border border-emerald-200'
      if (s === 'Expiring') return 'bg-amber-50 text-amber-700 border border-amber-200'
      if (s === 'Ended') return 'bg-slate-100 text-slate-700 border border-slate-200'
      if (s === 'Terminated') return 'bg-red-50 text-red-700 border border-red-200'
      break

    case 'BillStatus':
      if (s === 'Paid') return 'bg-emerald-50 text-emerald-700 border border-emerald-200'
      if (s === 'Issued') return 'bg-sky-50 text-sky-700 border border-sky-200'
      if (s === 'Overdue') return 'bg-red-50 text-red-700 border border-red-200'
      if (s === 'Draft') return 'bg-slate-100 text-slate-700 border border-slate-200'
      break

    case 'ListingStatus':
      if (s === 'Published') return 'bg-emerald-50 text-emerald-700 border border-emerald-200'
      if (s === 'PendingReview') return 'bg-amber-50 text-amber-700 border border-amber-200'
      if (s === 'Rejected') return 'bg-red-50 text-red-700 border border-red-200'
      break

    case 'TaskPriority':
      if (s === 'High') return 'bg-red-50 text-red-700 border border-red-200'
      if (s === 'Medium') return 'bg-amber-50 text-amber-700 border border-amber-200'
      return 'bg-slate-100 text-slate-700 border border-slate-200'

    case 'MaintenanceStatus':
      if (s === 'Resolved') return 'bg-emerald-50 text-emerald-700 border border-emerald-200'
      if (s === 'InProgress') return 'bg-amber-50 text-amber-700 border border-amber-200'
      if (s === 'Open') return 'bg-sky-50 text-sky-700 border border-sky-200'
      if (s === 'Rejected') return 'bg-red-50 text-red-700 border border-red-200'
      break
  }

  return 'bg-slate-100 text-slate-700 border border-slate-200'
})

const dotColorClass = computed(() => {
  const s = props.status
  if (['Available', 'Paid', 'Completed', 'Active', 'Published', 'Resolved'].includes(s)) {
    return 'bg-emerald-500'
  }
  if (['Reserved', 'Pending', 'Expiring', 'InProgress', 'Medium', 'PendingReview'].includes(s)) {
    return 'bg-amber-500'
  }
  if (['Occupied', 'Accepted', 'Issued', 'Open'].includes(s)) {
    return 'bg-sky-500'
  }
  if (['Maintenance', 'Rejected', 'Overdue', 'Terminated', 'High', 'Expired'].includes(s)) {
    return 'bg-red-500'
  }
  return 'bg-slate-400'
})
</script>
