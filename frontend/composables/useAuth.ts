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
    const localePath = useLocalePath()
    await navigateTo(localePath(target))
    return data
  }

  const loginWithGoogle = async (idToken: string, role?: UserRole) => {
    const payload: { idToken: string; role?: UserRole } = { idToken }
    if (role) {
      payload.role = role
    }

    const data = await post<{
      accessToken: string
      refreshToken: string
      expiresIn: number
      user: User
    }>('/auth/login/google', payload)

    authStore.setAuth(data)
    toast.success(t('auth.loginSuccess'))

    const target = getDefaultRouteForRole(data.user.role)
    const localePath = useLocalePath()
    await navigateTo(localePath(target))
    return data
  }

  const sendRegistrationOtp = async (email: string) => {
    return await post<{ message: string; expiresInMinutes: number }>('/auth/register/send-otp', { email })
  }

  const verifyRegistrationOtp = async (email: string, code: string) => {
    return await post<{ email: string; isVerified: boolean }>('/auth/register/verify-otp', { email, code })
  }

  const register = async (payload: {
    username?: string
    email: string
    password: string
    fullName: string
    phoneNumber?: string
    gender?: string
    role: UserRole
    preferredLanguage?: string
  }) => {
    const rawUsername = payload.username || payload.email.split('@')[0].replace(/[^a-zA-Z0-9._-]/g, '')
    const username = rawUsername.length >= 3 ? rawUsername : `user_${Date.now().toString().slice(-6)}`

    const body = {
      username,
      email: payload.email,
      password: payload.password,
      fullName: payload.fullName,
      phoneNumber: payload.phoneNumber || null,
      gender: payload.gender || 'Other',
      role: payload.role,
      preferredLanguage: payload.preferredLanguage || 'vi',
    }

    const data = await post<{
      accessToken: string
      refreshToken: string
      expiresIn: number
      user: User
    }>('/auth/register', body)

    authStore.setAuth(data)
    toast.success(t('auth.registerSuccess'))

    const target = getDefaultRouteForRole(data.user.role)
    const localePath = useLocalePath()
    await navigateTo(localePath(target))
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
    sendRegistrationOtp,
    verifyRegistrationOtp,
    register,
    fetchProfile,
    logout,
  }
}
