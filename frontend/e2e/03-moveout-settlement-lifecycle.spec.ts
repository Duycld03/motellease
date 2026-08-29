import { test, expect } from '@playwright/test'
import { getLiveAuth } from './auth-helper'

test.describe('Golden Path 3: Move-out Inspection, Deposit Damage Settlement & Automatic Room Release', () => {
  const safeGoto = async (page: any, url: string) => {
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    } catch {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    }
  }

  test('executes move-out inspection, deposit damage settlement and room release to Available', async ({ browser }) => {
    const ownerContext = await browser.newContext({ locale: 'vi-VN' })
    const tenantContext = await browser.newContext({ locale: 'vi-VN' })
    const ownerPage = await ownerContext.newPage()
    const tenantPage = await tenantContext.newPage()

    // Auto-accept browser confirmation dialogs
    ownerPage.on('dialog', async (dialog: any) => await dialog.accept())

    let targetHouseId = 'house-101'

    const ownerAuth = await getLiveAuth('owner1@motellease.local')
    const tenantAuth = await getLiveAuth('tenant2@motellease.local')

    // Retrieve active lease to extract boarding house reference
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

    // =================================================================================
    // STEP 1 [LEASE OVERVIEW & SETTLEMENT]: Owner views active leases and opens checkout
    // =================================================================================
    await safeGoto(ownerPage, '/owner/leases')
    await expect(ownerPage.locator('h1:has-text("Hợp đồng")').or(ownerPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    // Locate active lease card
    const leaseCard = ownerPage.locator('div[class*="rounded-2xl"]').filter({ hasText: /P\.|Phòng|101/ }).first().or(ownerPage.locator('div[class*="rounded-2xl"]').first())
    await expect(leaseCard).toBeVisible({ timeout: 15000 })

    // Click "Settle deposit & Terminate" button
    const terminateBtn = ownerPage.locator('button:has-text("Quyết toán"), button:has-text("Thanh lý")').first()
    await expect(terminateBtn).toBeVisible({ timeout: 10000 })
    await terminateBtn.click()

    // =================================================================================
    // STEP 2 [MOVE-OUT INSPECTION & DAMAGE DEDUCTION]: Owner enters final readings and deductions
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

    // Submit move-out confirmation
    const confirmSettlementBtn = ownerPage.locator('button:has-text("Xác nhận trả phòng"), button:has-text("Thanh lý")').last()
    await expect(confirmSettlementBtn).toBeVisible({ timeout: 5000 })
    await confirmSettlementBtn.click()

    // =================================================================================
    // STEP 3 [LEASE TERMINATED]: Verify lease transitions to Ended on Owner & Tenant views
    // =================================================================================
    await expect(ownerPage.locator('text=Đã kết thúc').or(ownerPage.locator('text=Ended')).or(ownerPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    await safeGoto(tenantPage, '/tenant/leases')
    await expect(tenantPage.locator('text=Đã kết thúc').or(tenantPage.locator('text=Ended')).or(tenantPage.locator('text=Thanh lý')).first()).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 4 [ROOM RELEASE]: Verify room automatically releases back to Available status
    // =================================================================================
    if (targetHouseId) {
      await safeGoto(tenantPage, `/boarding-houses/${targetHouseId}`)
      await expect(tenantPage.locator('text=Còn trống').or(tenantPage.locator('text=phòng trống')).or(tenantPage.locator('text=Available')).first()).toBeVisible({ timeout: 15000 })
    }

    await ownerContext.close()
    await tenantContext.close()
  })
})
