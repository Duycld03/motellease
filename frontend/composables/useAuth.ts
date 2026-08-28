import { UserRole } from '~/types/enums'
import type { User } from '~/types/api'

export const useAuth = () => {
  const authStore = useAuthStore()
  const { post, get } = useApi()
  const toast = useToast()
  const { t } = useI18n()

  const user = computed(() => authStore.user)
  const role = computed(() => authStore.role)
  const isAuthenticated = computed(() => authStore.isAuthenticated)
  const isTenant = computed(() => authStore.isTenant)
  const isOwner = computed(() => authStore.isOwner)
  const isStaff = computed(() => authStore.isStaff)
  const isAdmin = computed(() => authStore.isAdmin)

  const hasRole = (allowedRoles: UserRole | UserRole[]): boolean => {
    if (!authStore.role) return false
    if (Array.isArray(allowedRoles)) {
      return allowedRoles.includes(authStore.role)
    }
    return authStore.role === allowedRoles
  }

  const getDefaultRouteForRole = (userRole?: UserRole | null): string => {
    switch (userRole) {
      case UserRole.Tenant:
        return '/tenant/dashboard'
      case UserRole.Owner:
        return '/owner/dashboard'
      case UserRole.Staff:
        return '/staff/dashboard'
      case UserRole.Admin:
        return '/admin/dashboard'
      default:
        return '/'
    }
  }

  const login = async (email: string, password: string) => {
    const data = await post<{
      accessToken: string
      refreshToken: string
      expiresIn: number
      user: User
    }>('/auth/login', { login: email, password })

    authStore.setAuth(data)
    toast.success(t('auth.loginSuccess'))

    const target = getDefaultRouteForRole(data.user.role)
    await navigateTo(target)
    return data
  }

  const loginWithGoogle = async (idToken: string) => {
    const data = await post<{
      accessToken: string
      refreshToken: string
      expiresIn: number
      user: User
    }>('/auth/login/google', { idToken })

    authStore.setAuth(data)
    toast.success(t('auth.loginSuccess'))

    const target = getDefaultRouteForRole(data.user.role)
    await navigateTo(target)
    return data
  }

  const register = async (payload: {
    email: string
    password: string
    fullName: string
    phoneNumber?: string
    role: UserRole
  }) => {
    const data = await post('/auth/register', payload)
    toast.success(t('auth.registerSuccess'))
    return data
  }

  const fetchProfile = async () => {
    if (!authStore.accessToken) return null
    try {
      const userData = await get<User>('/me')
      authStore.setUser(userData)
      return userData
    } catch {
      return null
    }
  }

  const logout = async () => {
    await authStore.logout()
  }

  return {
    user,
    role,
    isAuthenticated,
    isTenant,
    isOwner,
    isStaff,
    isAdmin,
    hasRole,
    getDefaultRouteForRole,
    login,
    loginWithGoogle,
    register,
    fetchProfile,
    logout,
  }
}
