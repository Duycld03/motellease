import { defineStore } from 'pinia'
import type { User, AuthTokenResponse } from '~/types/api'
import { UserRole } from '~/types/enums'

export const useAuthStore = defineStore('auth', () => {
  const accessTokenCookie = useCookie<string | null>('ml_access_token', {
    maxAge: 60 * 60 * 24 * 7, // 7 days
    sameSite: 'lax',
  })
  const refreshTokenCookie = useCookie<string | null>('ml_refresh_token', {
    maxAge: 60 * 60 * 24 * 30, // 30 days
    sameSite: 'lax',
  })
  const userCookie = useCookie<User | null>('ml_user', {
    maxAge: 60 * 60 * 24 * 7,
    sameSite: 'lax',
  })

  const user = ref<User | null>(userCookie.value || null)
  const accessToken = ref<string | null>(accessTokenCookie.value || null)
  const refreshToken = ref<string | null>(refreshTokenCookie.value || null)
  const isInitializing = ref(false)

  const isAuthenticated = computed(() => !!accessToken.value && !!user.value)
  const role = computed<UserRole | null>(() => user.value?.role || null)
  const isTenant = computed(() => role.value === UserRole.Tenant)
  const isOwner = computed(() => role.value === UserRole.Owner)
  const isStaff = computed(() => role.value === UserRole.Staff)
  const isAdmin = computed(() => role.value === UserRole.Admin)

  const setAuth = (authData: AuthTokenResponse) => {
    user.value = authData.user
    accessToken.value = authData.accessToken
    refreshToken.value = authData.refreshToken

    userCookie.value = authData.user
    accessTokenCookie.value = authData.accessToken
    refreshTokenCookie.value = authData.refreshToken
  }

  const setTokens = (newAccessToken: string, newRefreshToken: string) => {
    accessToken.value = newAccessToken
    refreshToken.value = newRefreshToken
    accessTokenCookie.value = newAccessToken
    refreshTokenCookie.value = newRefreshToken
  }

  const setUser = (newUser: User) => {
    user.value = newUser
    userCookie.value = newUser
  }

  const clearAuth = () => {
    user.value = null
    accessToken.value = null
    refreshToken.value = null

    userCookie.value = null
    accessTokenCookie.value = null
    refreshTokenCookie.value = null
  }

  const logout = async () => {
    try {
      if (accessToken.value) {
        const config = useRuntimeConfig()
        await $fetch(`${config.public.apiBase}/auth/logout`, {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${accessToken.value}`,
          },
        }).catch(() => {})
      }
    } finally {
      clearAuth()
      navigateTo('/auth/login')
    }
  }

  return {
    user,
    accessToken,
    refreshToken,
    isInitializing,
    isAuthenticated,
    role,
    isTenant,
    isOwner,
    isStaff,
    isAdmin,
    setAuth,
    setTokens,
    setUser,
    clearAuth,
    logout,
  }
})
