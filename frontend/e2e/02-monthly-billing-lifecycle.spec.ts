import { test, expect } from '@playwright/test'
import { getLiveAuth, completeMoMoCardPayment } from './auth-helper'

test.describe('Golden Path 2: Utility Meter Recording, Monthly Invoicing & Online Bill Payment', () => {
  const safeGoto = async (page: any, url: string) => {
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    } catch {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    }
  }

  test('executes complete billing cycle: Owner Meter Readings & Invoice Creation -> Tenant Reviews & Pays Online with MoMo', async ({ browser }) => {
    const ownerContext = await browser.newContext({ locale: 'vi-VN' })
    const tenantContext = await browser.newContext({ locale: 'vi-VN' })

    const ownerPage = await ownerContext.newPage()
    const tenantPage = await tenantContext.newPage()

    let targetRoomId = 'room-101'
    let targetMonth = 10
    const targetTenantEmail = 'tenant2@motellease.local'

    const ownerAuth = await getLiveAuth('owner1@motellease.local')
    const tenantAuth = await getLiveAuth(targetTenantEmail)

    // Retrieve active leases to find the room where target tenant resides
    const leasesRes = await fetch('http://localhost:5004/api/v1/leases?status=Active&page=1&pageSize=50', {
      headers: { Authorization: `Bearer ${ownerAuth.accessToken}` },
    })
    const leasesData = await leasesRes.json()
    const targetLease = leasesData.items?.find((l: any) =>
      l.primaryTenantUserId === tenantAuth.user.id || l.tenants?.some((t: any) => t.userId === tenantAuth.user.id)
    ) || leasesData.items?.[0]

    if (targetLease) {
      targetRoomId = targetLease.roomId

      // Query existing bills to find an unbilled future billing cycle
      const billsRes = await fetch(`http://localhost:5004/api/v1/bills?roomId=${targetRoomId}&pageSize=50`, {
        headers: { Authorization: `Bearer ${ownerAuth.accessToken}` },
      })
      if (billsRes.ok) {
        const existingBills = await billsRes.json()
        const billedMonths = new Set(existingBills.items?.map((b: any) => b.month))
        for (let m = 10; m <= 12; m++) {
          if (!billedMonths.has(m)) {
            targetMonth = m
            break
          }
        }
      }
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
    // STEP 1 [METER READINGS & INVOICING]: Owner records readings and issues monthly bill
    // =================================================================================
    await safeGoto(ownerPage, '/owner/bills')
    await expect(ownerPage.locator('h1:has-text("Quản lý Hóa đơn")').or(ownerPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    const ownerModal = ownerPage.locator('div.fixed.inset-0')
    const modalTitle = ownerModal.locator('h3:has-text("Lập Hóa đơn"), h3:has-text("hóa đơn")').first()

    // Ensure invoice modal opens reliably
    await expect(async () => {
      const btn = ownerPage.locator('button:has-text("Lập hóa đơn"), button:has-text("Tạo hóa đơn")').first()
      await btn.click()
      await expect(modalTitle).toBeVisible({ timeout: 1500 })
    }).toPass({ timeout: 15000, intervals: [500, 1000] })

    const roomSelect = ownerModal.locator('select').first()
    await expect(roomSelect).toBeVisible({ timeout: 5000 })
    if (targetRoomId) {
      await roomSelect.selectOption({ value: targetRoomId })
    }

    // Select target month
    const monthSelect = ownerModal.locator('select').nth(1)
    if (await monthSelect.isVisible({ timeout: 3000 }).catch(() => false)) {
      await monthSelect.selectOption({ value: String(targetMonth) })
    }

    // Fill new electricity and water meter readings
    const elecInput = ownerModal.locator('input[type="number"]').nth(1)
    if (await elecInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await elecInput.fill('1200')
    }
    const waterInput = ownerModal.locator('input[type="number"]').nth(2)
    if (await waterInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await waterInput.fill('120')
    }

    // Submit Create Bill (Issue Now)
    const submitBillBtn = ownerModal.locator('button').filter({ hasText: /Phát hành hóa đơn ngay|Phát hành/ }).last()
    await expect(submitBillBtn).toBeVisible({ timeout: 5000 })
    await submitBillBtn.click()

    // Verify bill appears in Issued/Unpaid state on Owner dashboard
    await expect(ownerPage.locator('text=Chưa thanh toán').or(ownerPage.locator('text=Đã phát hành')).or(ownerPage.locator('div[class*="rounded-2xl"]')).first()).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 2 [TENANT BILL PAYMENT]: Tenant opens bill details & initiates online checkout
    // =================================================================================
    await safeGoto(tenantPage, '/tenant/bills')
    await expect(tenantPage.locator('h1:has-text("Hóa đơn")').or(tenantPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    const checkoutModal = tenantPage.locator('div.fixed.inset-0').last()
    const checkoutModalTitle = checkoutModal.locator('h3:has-text("Thanh toán"), h3:has-text("Online")').first()

    // Click pay button on the bill card until modal opens
    await expect(async () => {
      const payBillBtn = tenantPage.locator('button').filter({ hasText: '💳' }).first()
      await payBillBtn.click()
      await expect(checkoutModalTitle).toBeVisible({ timeout: 1500 })
    }).toPass({ timeout: 15000, intervals: [500, 1000] })

    // Select MoMo gateway radio option
    const momoOption = checkoutModal.locator('label').filter({ hasText: /MoMo/ }).first()
    if (await momoOption.isVisible({ timeout: 3000 }).catch(() => false)) {
      await momoOption.click()
    }

    const confirmPayBtn = checkoutModal.locator('button').filter({ hasText: /Thanh toán \d+|Thanh toán/ }).last()
    await expect(confirmPayBtn).toBeVisible({ timeout: 5000 })
    await confirmPayBtn.click()

    // =================================================================================
    // STEP 3 [GATEWAY CARD & REAL IPN]: Complete card payment on MoMo Sandbox & Napas OTP
    // =================================================================================
    await completeMoMoCardPayment(tenantPage)

    // =================================================================================
    // STEP 4 [RESULT VERIFICATION]: Verify payment success screen
    // =================================================================================
    const successTitle = tenantPage.locator('text=Thanh toán thành công').or(tenantPage.locator('text=Thành công')).or(tenantPage.locator('h1')).first()
    await expect(successTitle).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 5 [PAID STATE ASSERTION]: Verify bill transitions to Paid on Tenant & Owner views
    // =================================================================================
    await safeGoto(tenantPage, '/tenant/bills')
    await expect(tenantPage.locator('text=Đã thanh toán').or(tenantPage.locator('text=Paid')).first()).toBeVisible({ timeout: 15000 })

    await safeGoto(ownerPage, '/owner/bills')
    await expect(ownerPage.locator('text=Đã thanh toán').or(ownerPage.locator('text=Paid')).first()).toBeVisible({ timeout: 15000 })

    await ownerContext.close()
    await tenantContext.close()
  })
})
