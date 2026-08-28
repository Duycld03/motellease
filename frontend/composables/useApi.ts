import type { NitroFetchRequest, NitroFetchOptions } from 'nitropack'
import type { ProblemDetails } from '~/types/api'

let isRefreshing = false
let refreshPromise: Promise<boolean> | null = null

export const useApi = () => {
  const config = useRuntimeConfig()
  const authStore = useAuthStore()
  const { locale } = useI18n()

  const apiBase = config.public.apiBase

  const refreshAccessToken = async (): Promise<boolean> => {
    if (!authStore.refreshToken) return false

    if (isRefreshing && refreshPromise) {
      return refreshPromise
    }

    isRefreshing = true
    refreshPromise = (async () => {
      try {
        const response = await $fetch<{
          accessToken: string
          refreshToken: string
          expiresIn: number
        }>(`${apiBase}/auth/refresh`, {
          method: 'POST',
          body: {
            refreshToken: authStore.refreshToken,
          },
        })

        if (response?.accessToken && response?.refreshToken) {
          authStore.setTokens(response.accessToken, response.refreshToken)
          return true
        }
        return false
      } catch (err) {
        authStore.clearAuth()
        return false
      } finally {
        isRefreshing = false
        refreshPromise = null
      }
    })()

    return refreshPromise
  }

  const $api = async <T = any>(
    request: NitroFetchRequest,
    options?: NitroFetchOptions<NitroFetchRequest>
  ): Promise<T> => {
    const url = typeof request === 'string' && request.startsWith('http')
      ? request
      : `${apiBase}${request}`

    const headers: Record<string, string> = {
      'Accept-Language': locale.value || 'vi',
      ...(options?.headers as Record<string, string> || {}),
    }

    if (authStore.accessToken && !headers.Authorization) {
      headers.Authorization = `Bearer ${authStore.accessToken}`
    }

    try {
      return await $fetch<T>(url as any, {
        ...options,
        headers,
      })
    } catch (err: any) {
      // If unauthorized and we have a refresh token, attempt transparent refresh once
      if (err?.response?.status === 401 && authStore.refreshToken) {
        const refreshed = await refreshAccessToken()
        if (refreshed && authStore.accessToken) {
          headers.Authorization = `Bearer ${authStore.accessToken}`
          return await $fetch<T>(url as any, {
            ...options,
            headers,
          })
        }
      }

      // Parse RFC 7807 problem+json
      const problem = err?.data as ProblemDetails | undefined
      if (problem) {
        let message = problem.detail
        if (!message && problem.errors && Object.keys(problem.errors).length > 0) {
          const firstKey = Object.keys(problem.errors)[0]
          const firstList = problem.errors[firstKey]
          if (Array.isArray(firstList) && firstList.length > 0) {
            message = firstList[0]
          }
        }
        if (!message) {
          message = problem.title || err.message || 'Đã xảy ra lỗi'
        }
        const customError: any = new Error(message)
        customError.status = err?.response?.status || problem.status
        customError.problem = problem
        customError.errors = problem.errors
        throw customError
      }

      throw err
    }
  }

  return {
    $api,
    get: <T = any>(url: string, params?: Record<string, any>) =>
      $api<T>(url, { method: 'GET', query: params }),
    post: <T = any>(url: string, body?: any) =>
      $api<T>(url, { method: 'POST', body }),
    put: <T = any>(url: string, body?: any) =>
      $api<T>(url, { method: 'PUT', body }),
    delete: <T = any>(url: string, query?: Record<string, any>) =>
      $api<T>(url, { method: 'DELETE', query }),
    patch: <T = any>(url: string, body?: any) =>
      $api<T>(url, { method: 'PATCH', body }),
  }
}
