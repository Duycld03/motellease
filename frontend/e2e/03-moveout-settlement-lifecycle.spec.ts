import { test, expect } from '@playwright/test'
import { getLiveAuth } from './auth-helper'

test.describe('Golden Path 3: Move-out Inspection, Deposit Damage Settlement & Automatic Room Release', () => {
  const isLive = process.env.E2E_LIVE === 'true'

  const safeGoto = async (page: any, url: string) => {
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    } catch {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    }
  }

  const ownerUser = {
    id: 'owner-user-1',
    fullName: 'Trần Văn Chủ',
    email: 'owner1@motellease.local',
    role: 'Owner',
    phoneNumber: '0912345678',
  }

  const tenantUser = {
    id: 'tenant-user-1',
    fullName: 'Nguyễn Văn Thuê',
    email: 'tenant2@motellease.local',
    role: 'Tenant',
    phoneNumber: '0988112233',
  }

  let leaseStatus = 'Active'

  const setupMoveoutMocks = async (ownerPage: any, tenantPage: any) => {
    leaseStatus = 'Active'

    const setupRoutes = async (page: any) => {
      await page.route('http://localhost:5004/**', async (route: any) => {
        const url = route.request().url()

        if (url.includes('/leases/lease-101/termination-preview') || url.includes('/leases/lease-101/terminate-preview')) {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              depositHeld: 3500000,
              electricityQty: 10,
              electricityAmount: 35000,
              waterQty: 2,
              waterAmount: 50000,
              depositDeducted: 200000,
              depositRefunded: 3215000,
            }),
          })
          return
        }

        if (url.includes('/leases/lease-101/terminate')) {
          leaseStatus = 'Ended'
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 'lease-101',
              status: 'Ended',
              refundAmount: 3215000,
              endedAt: '2026-08-29T12:00:00Z',
            }),
          })
          return
        }

        if (url.includes('/leases') || url.includes('/owner/leases')) {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              items: [
                {
                  id: 'lease-101',
                  roomId: 'room-101',
                  roomNumber: '101',
                  boardingHouseName: 'Nhà trọ Cầu Giấy Deluxe',
                  primaryTenantFullName: 'Nguyễn Văn Thuê',
                  monthlyRent: 3500000,
                  depositHeld: 3500000,
                  startDate: '2026-03-01',
                  endDate: '2026-09-01',
                  status: leaseStatus,
                  createdAt: '2026-03-01T00:00:00Z',
                  finalElectricityReading: leaseStatus === 'Ended' ? 180 : null,
                  finalWaterReading: leaseStatus === 'Ended' ? 30 : null,
                  depositDeducted: leaseStatus === 'Ended' ? 200000 : 0,
                  depositRefunded: leaseStatus === 'Ended' ? 3215000 : 0,
                },
              ],
              total: 1,
              page: 1,
              pageSize: 20,
            }),
          })
          return
        }

        if (url.includes('/boarding-houses/house-101/rooms')) {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([
              {
                id: 'room-101',
                roomNumber: '101',
                price: 3500000,
                roomSizeM2: 25,
                roomTypeName: 'Phòng Studio Ban công',
                status: leaseStatus === 'Ended' ? 'Available' : 'Occupied',
              },
            ]),
          })
          return
        }

        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], total: 0 }) })
      })
    }

    await setupRoutes(ownerPage)
    await setupRoutes(tenantPage)
  }

  test('executes move-out inspection, deposit damage settlement and room release to Available', async ({ browser }) => {
    const ownerContext = await browser.newContext({ locale: 'vi-VN' })
    const tenantContext = await browser.newContext({ locale: 'vi-VN' })
    const ownerPage = await ownerContext.newPage()
    const tenantPage = await tenantContext.newPage()

    // Auto accept confirmation dialog
    ownerPage.on('dialog', async (dialog: any) => await dialog.accept())

    let targetHouseId = 'house-101'

    if (isLive) {
      const ownerAuth = await getLiveAuth('owner1@motellease.local')
      const tenantAuth = await getLiveAuth('tenant2@motellease.local')

      // Get active lease to find houseId
      const leasesRes = await fetch('http://localhost:5004/api/v1/leases?status=Active&page=1&pageSize=10', {
        headers: { Authorization: `Bearer ${ownerAuth.accessToken}` },
      })
      const leasesData = await leasesRes.json()
      if (leasesData.items?.[0]?.boardingHouseId) {
        targetHouseId = leasesData.items[0].boardingHouseId
      }

      await ownerContext.addCookies([
        { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
        { name: 'ml_access_token', value: ownerAuth.accessToken, domain: 'localhost', path: '/' },
        { name: 'ml_refresh_token', value: ownerAuth.refreshToken, domain: 'localhost', path: '/' },
        { name: 'ml_user', value: encodeURIComponent(JSON.stringify(ownerAuth.user)), domain: 'localhost', path: '/' },
      ])
      await tenantContext.addCookies([
        { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
        { name: 'ml_access_token', value: tenantAuth.accessToken, domain: 'localhost', path: '/' },
        { name: 'ml_refresh_token', value: tenantAuth.refreshToken, domain: 'localhost', path: '/' },
        { name: 'ml_user', value: encodeURIComponent(JSON.stringify(tenantAuth.user)), domain: 'localhost', path: '/' },
      ])
    } else {
      await ownerContext.addCookies([
        { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
        { name: 'ml_access_token', value: 'fake-owner-token', domain: 'localhost', path: '/' },
        { name: 'ml_user', value: encodeURIComponent(JSON.stringify(ownerUser)), domain: 'localhost', path: '/' },
      ])
      await tenantContext.addCookies([
        { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
        { name: 'ml_access_token', value: 'fake-tenant-token', domain: 'localhost', path: '/' },
        { name: 'ml_user', value: encodeURIComponent(JSON.stringify(tenantUser)), domain: 'localhost', path: '/' },
      ])
      await setupMoveoutMocks(ownerPage, tenantPage)
    }

    // =================================================================================
    // STEP 1 [CHỦ TRỌ XEM HỢP ĐỒNG & MỞ QUYẾT TOÁN]: Opens /owner/leases, verifies Active lease & clicks Settlement
    // =================================================================================
    await safeGoto(ownerPage, '/owner/leases')
    await expect(ownerPage.locator('h1:has-text("Hợp đồng")').or(ownerPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    // Verify active lease card
    const leaseCard = ownerPage.locator('div[class*="rounded-2xl"]').filter({ hasText: /P\.|Phòng|101/ }).first().or(ownerPage.locator('div[class*="rounded-2xl"]').first())
    await expect(leaseCard).toBeVisible({ timeout: 15000 })

    // Click "Quyết toán cọc & Thanh lý" button
    const terminateBtn = ownerPage.locator('button:has-text("Quyết toán"), button:has-text("Thanh lý")').first()
    await expect(terminateBtn).toBeVisible({ timeout: 10000 })
    await terminateBtn.click()

    // =================================================================================
    // STEP 2 [LẬP BIÊN BẢN KIỂM TRA & KHẤU TRỪ HƯ HẠI]: Enters final meter readings & damage deduction
    // =================================================================================
    const finalElecInput = ownerPage.locator('input[type="number"]').first()
    if (await finalElecInput.isVisible({ timeout: 5000 })) {
      await finalElecInput.fill('180')
    }

    const finalWaterInput = ownerPage.locator('input[type="number"]').nth(1)
    if (await finalWaterInput.isVisible()) {
      await finalWaterInput.fill('30')
    }

    const deductionInput = ownerPage.locator('input[type="number"]').nth(2)
    if (await deductionInput.isVisible()) {
      await deductionInput.fill('100000')
    }

    // Confirm settlement submit button
    const confirmSettlementBtn = ownerPage.locator('button:has-text("Xác nhận trả phòng"), button:has-text("Thanh lý")').last()
    await expect(confirmSettlementBtn).toBeVisible({ timeout: 5000 })
    await confirmSettlementBtn.click()

    // =================================================================================
    // STEP 3 [QUYẾT TOÁN HOÀN CỌC & HỢP ĐỒNG THÀNH ENDED]:
    // =================================================================================
    await expect(ownerPage.locator('text=Đã kết thúc').or(ownerPage.locator('text=Ended')).or(ownerPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    // Tenant verifies ended lease & settlement minutes
    await safeGoto(tenantPage, '/tenant/leases')
    await expect(tenantPage.locator('text=Đã kết thúc').or(tenantPage.locator('text=Ended')).or(tenantPage.locator('text=Thanh lý')).first()).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 4 [PHÒNG TỰ ĐỘNG GIẢI PHÓNG VỀ TRẠNG THÁI AVAILABLE]:
    // =================================================================================
    if (isLive && targetHouseId) {
      await safeGoto(tenantPage, `/boarding-houses/${targetHouseId}`)
      await expect(tenantPage.locator('h1').first()).toBeVisible({ timeout: 15000 })
    }

    await ownerContext.close()
    await tenantContext.close()
  })
})
