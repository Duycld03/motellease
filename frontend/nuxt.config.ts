// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  future: {
    compatibilityVersion: 4,
  },
  telemetry: false,
  devtools: { enabled: false },

  modules: [
    '@nuxtjs/tailwindcss',
    '@nuxtjs/i18n',
    '@pinia/nuxt',
    '@vueuse/nuxt',
  ],

  css: [
    'leaflet/dist/leaflet.css',
    '~/assets/css/main.css',
  ],

  runtimeConfig: {
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5004/api/v1',
      hubUrl: process.env.NUXT_PUBLIC_HUB_URL || 'http://localhost:5004/hubs/notifications',
    },
  },

  i18n: {
    defaultLocale: 'vi',
    locales: [
      { code: 'vi', name: 'Tiếng Việt', file: 'vi.json' },
      { code: 'en', name: 'English', file: 'en.json' },
    ],
    strategy: 'prefix_except_default',
    lazy: true,
  },

  app: {
    head: {
      title: 'MotelLease - Nền tảng quản lý và thuê trọ tiện lợi',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        {
          name: 'description',
          content: 'MotelLease - Tìm kiếm phòng trọ, quản lý thuê phòng, hợp đồng và hóa đơn điện nước trực tuyến',
        },
      ],
      link: [
        { rel: 'icon', type: 'image/x-icon', href: '/favicon.ico' },
      ],
    },
  },
})
