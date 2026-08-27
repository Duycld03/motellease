import { UserRole } from '~/types/enums'

export default defineNuxtRouteMiddleware((to) => {
  const authStore = useAuthStore()
  const path = to.path

  // Check if page is guest only (e.g. Login, Register)
  const isGuestOnly = to.meta.guestOnly || path.startsWith('/auth/login') || path.startsWith('/auth/register')

  if (isGuestOnly && authStore.isAuthenticated) {
    const defaultRoute = authStore.role === UserRole.Tenant ? '/tenant/dashboard'
      : authStore.role === UserRole.Owner ? '/owner/dashboard'
      : authStore.role === UserRole.Staff ? '/staff/dashboard'
      : authStore.role === UserRole.Admin ? '/admin/dashboard'
      : '/'
    return navigateTo(defaultRoute)
  }

  // Determine if path requires role protection
  let requiredRole: UserRole | null = null
  if (path.startsWith('/admin')) {
    requiredRole = UserRole.Admin
  } else if (path.startsWith('/owner')) {
    requiredRole = UserRole.Owner
  } else if (path.startsWith('/staff')) {
    requiredRole = UserRole.Staff
  } else if (path.startsWith('/tenant')) {
    requiredRole = UserRole.Tenant
  }

  // Also check explicit meta configuration
  const requiresAuth = to.meta.requiresAuth || !!requiredRole
  const allowedRoles = (to.meta.roles as UserRole[]) || (requiredRole ? [requiredRole] : null)

  if (requiresAuth && !authStore.isAuthenticated) {
    return navigateTo({
      path: '/auth/login',
      query: { redirect: to.fullPath },
    })
  }

  if (allowedRoles && allowedRoles.length > 0 && authStore.user) {
    if (!allowedRoles.includes(authStore.user.role)) {
      // User is authenticated but does not have the required role
      const defaultRoute = authStore.role === UserRole.Tenant ? '/tenant/dashboard'
        : authStore.role === UserRole.Owner ? '/owner/dashboard'
        : authStore.role === UserRole.Staff ? '/staff/dashboard'
        : authStore.role === UserRole.Admin ? '/admin/dashboard'
        : '/'
      return navigateTo(defaultRoute)
    }
  }
})
