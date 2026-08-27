<template>
  <div class="space-y-8">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-slate-900">
          Tổng quan Quản trị viên
        </h1>
        <p class="text-xs text-slate-500 mt-1">
          Theo dõi toàn bộ nền tảng MotelLease: người dùng, bài đăng kiểm duyệt, báo cáo và nhật ký hệ thống
        </p>
      </div>
    </div>

    <!-- Quick Stats -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-primary-100 text-primary-700 flex items-center justify-center font-bold">
          👥
        </div>
        <div>
          <span class="text-xs text-slate-500 block">Tổng tài khoản</span>
          <span class="text-lg font-bold text-slate-900 mt-0.5 block">120</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-amber-100 text-amber-700 flex items-center justify-center font-bold">
          📑
        </div>
        <div>
          <span class="text-xs text-slate-500 block">Khu trọ chờ duyệt</span>
          <span class="text-lg font-bold text-slate-900 mt-0.5 block">3</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-red-100 text-red-700 flex items-center justify-center font-bold">
          ⚠️
        </div>
        <div>
          <span class="text-xs text-slate-500 block">Báo cáo vi phạm</span>
          <span class="text-lg font-bold text-slate-900 mt-0.5 block">1</span>
        </div>
      </BaseCard>

      <BaseCard no-padding custom-class="p-5 flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-emerald-100 text-emerald-700 flex items-center justify-center font-bold">
          💳
        </div>
        <div>
          <span class="text-xs text-slate-500 block">Yêu cầu rút tiền</span>
          <span class="text-lg font-bold text-slate-900 mt-0.5 block">2</span>
        </div>
      </BaseCard>
    </div>

    <!-- Quick Navigation -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <BaseCard title="Kiểm duyệt nhanh">
        <div class="space-y-3">
          <NuxtLink to="/admin/moderation" class="p-3.5 bg-slate-50 rounded-xl border border-slate-100 flex items-center justify-between hover:bg-slate-100 transition-colors">
            <span class="text-xs font-semibold text-slate-800">Duyệt bài đăng khu trọ mới</span>
            <span class="text-xs text-primary-600 font-bold">3 chờ duyệt →</span>
          </NuxtLink>
          <NuxtLink to="/admin/withdrawals" class="p-3.5 bg-slate-50 rounded-xl border border-slate-100 flex items-center justify-between hover:bg-slate-100 transition-colors">
            <span class="text-xs font-semibold text-slate-800">Duyệt rút tiền từ chủ nhà</span>
            <span class="text-xs text-emerald-600 font-bold">2 yêu cầu →</span>
          </NuxtLink>
        </div>
      </BaseCard>

      <BaseCard title="Bảo mật & Nhật ký">
        <div class="space-y-3">
          <NuxtLink to="/admin/reports" class="p-3.5 bg-slate-50 rounded-xl border border-slate-100 flex items-center justify-between hover:bg-slate-100 transition-colors">
            <span class="text-xs font-semibold text-slate-800">Xử lý báo cáo vi phạm</span>
            <span class="text-xs text-red-600 font-bold">1 báo cáo →</span>
          </NuxtLink>
          <NuxtLink to="/admin/audit-logs" class="p-3.5 bg-slate-50 rounded-xl border border-slate-100 flex items-center justify-between hover:bg-slate-100 transition-colors">
            <span class="text-xs font-semibold text-slate-800">Nhật ký hoạt động hệ thống (Audit Logs)</span>
            <span class="text-xs text-slate-500 font-bold">Xem logs →</span>
          </NuxtLink>
        </div>
      </BaseCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseCard from '~/components/common/BaseCard.vue'

definePageMeta({
  layout: 'admin',
})
</script>
