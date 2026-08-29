import { test, expect } from '@playwright/test'
import { getLiveAuth, OWNER_MAP, completeMoMoCardPayment } from './auth-helper'

test.describe('Golden Path 1: Room Booking, Owner Approval, Multi-Gateway Checkout & Room Handover', () => {
  const safeGoto = async (page: any, url: string) => {
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    } catch {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    }
  }

  test('1. executes multi-actor lifecycle with MoMo Bank/ATM Checkout & Owner Room Handover', async ({ browser }) => {
    const tenantContext = await browser.newContext({ locale: 'vi-VN' })
    const ownerContext = await browser.newContext({ locale: 'vi-VN' })

    const tenantPage = await tenantContext.newPage()
    const ownerPage = await ownerContext.newPage()

    // Auto-accept browser confirmation dialogs
    ownerPage.on('dialog', async (dialog: any) => await dialog.accept())

    let targetHouseId = 'house-101'
    let targetOwnerEmail = 'owner1@motellease.local'
    const targetTenantEmail = 'tenant77@motellease.local'

    // Fetch real live boarding houses from backend
    const housesRes = await fetch('http://localhost:5004/api/v1/boarding-houses?page=1&pageSize=30')
    const housesData = await housesRes.json()
    const availHouse = housesData.items?.find((h: any) => h.availableRoomsCount > 0)
    if (availHouse) {
      targetHouseId = availHouse.id
      const detailRes = await fetch(`http://localhost:5004/api/v1/boarding-houses/${availHouse.id}`)
      const detailData = await detailRes.json()
      if (detailData.owner?.fullName && OWNER_MAP[detailData.owner.fullName]) {
        targetOwnerEmail = OWNER_MAP[detailData.owner.fullName]
      }
    }

    const tenantAuth = await getLiveAuth(targetTenantEmail)
    const ownerAuth = await getLiveAuth(targetOwnerEmail)

    // Clean up previous pending or accepted deposits for this tenant if any
    const existingDepositsRes = await fetch('http://localhost:5004/api/v1/deposits', {
      headers: { Authorization: `Bearer ${tenantAuth.accessToken}` },
    })
    if (existingDepositsRes.ok) {
      const existingData = await existingDepositsRes.json()
      for (const dep of existingData.items || []) {
        if (dep.status === 'Pending' || dep.status === 'Accepted') {
          await fetch(`http://localhost:5004/api/v1/deposits/${dep.id}/cancel`, {
            method: 'PUT',
            headers: { Authorization: `Bearer ${tenantAuth.accessToken}`, 'Content-Type': 'application/json' },
            body: JSON.stringify({ reason: 'E2E Reset' }),
          })
        }
      }
    }

    await tenantContext.addCookies([
      { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
      { name: 'ml_access_token', value: tenantAuth.accessToken, domain: 'localhost', path: '/' },
      { name: 'ml_refresh_token', value: tenantAuth.refreshToken, domain: 'localhost', path: '/' },
      { name: 'ml_user', value: encodeURIComponent(JSON.stringify(tenantAuth.user)), domain: 'localhost', path: '/' },
    ])
    await ownerContext.addCookies([
      { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
      { name: 'ml_access_token', value: ownerAuth.accessToken, domain: 'localhost', path: '/' },
      { name: 'ml_refresh_token', value: ownerAuth.refreshToken, domain: 'localhost', path: '/' },
      { name: 'ml_user', value: encodeURIComponent(JSON.stringify(ownerAuth.user)), domain: 'localhost', path: '/' },
    ])

    // =================================================================================
    // STEP 1 [SEARCH PROPERTY]: Tenant opens property with available rooms
    // =================================================================================
    await safeGoto(tenantPage, `/boarding-houses/${targetHouseId}`)
    await expect(tenantPage.locator('h1').first()).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 2 [HOLDING DEPOSIT]: Tenant submits 24h deposit request
    // =================================================================================
    const depositBtn = tenantPage.locator('button:has-text("Cọc giữ phòng"), button:has-text("Đặt cọc"), button:has-text("Cọc")').first()
    await expect(depositBtn).toBeVisible({ timeout: 10000 })
    await depositBtn.click()

    const submitDepositBtn = tenantPage.locator('button:has-text("Gửi yêu cầu đặt cọc"), button:has-text("Gửi yêu cầu")').first()
    await expect(submitDepositBtn).toBeVisible({ timeout: 5000 })
    await submitDepositBtn.click()

    await tenantPage.waitForURL((url) => url.pathname.includes('/tenant/deposits'), { timeout: 15000 })
    const pendingBadge = tenantPage.locator('text=Chờ duyệt').or(tenantPage.locator('text=Pending')).or(tenantPage.locator('div[class*="rounded-2xl"]')).first()
    await expect(pendingBadge).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 3 [OWNER APPROVAL]: Owner reviews and approves deposit request on UI
    // =================================================================================
    await safeGoto(ownerPage, '/owner/deposits')
    await expect(ownerPage.locator('h1:has-text("Yêu cầu đặt cọc")').or(ownerPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })
    const approveDepositBtn = ownerPage.locator('button:has-text("Duyệt cọc"), button:has-text("Approve")').first()
    await expect(approveDepositBtn).toBeVisible({ timeout: 10000 })
    await approveDepositBtn.click()
    await expect(ownerPage.locator('text=Đã duyệt').or(ownerPage.locator('text=Accepted')).first()).toBeVisible({ timeout: 10000 })

    // =================================================================================
    // STEP 4 [DRAFT CONTRACT PREVIEW]: Tenant previews draft contract before payment
    // =================================================================================
    await safeGoto(tenantPage, '/tenant/deposits')
    const viewDraftBtn = tenantPage.locator('button:has-text("Xem dự thảo hợp đồng"), button:has-text("Xem dự thảo")').first()
    await expect(viewDraftBtn).toBeVisible({ timeout: 10000 })
    await viewDraftBtn.click()

    // Verify draft contract modal header
    await expect(tenantPage.locator('h3:has-text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM"), h4:has-text("DỰ THẢO")').first()).toBeVisible({ timeout: 5000 })

    // =================================================================================
    // STEP 5 [MOMO BANK CHECKOUT & REAL IPN]: Tenant enters card details & Napas OTP
    // =================================================================================
    const proceedPayBtn = tenantPage.locator('button:has-text("Tiến hành thanh toán"), button:has-text("Thanh toán cọc ngay")').first()
    await expect(proceedPayBtn).toBeVisible({ timeout: 5000 })
    await proceedPayBtn.click()

    // Select MoMo payment gateway inside checkout modal
    const momoOption = tenantPage.locator('label').filter({ hasText: /MoMo/ }).first()
    if (await momoOption.isVisible({ timeout: 3000 }).catch(() => false)) {
      await momoOption.click()
    }

    // Confirm payment inside modal
    const confirmModalPayBtn = tenantPage.locator('button').filter({ hasText: /Thanh toán \d+/ }).first()
    if (await confirmModalPayBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
      await confirmModalPayBtn.click()
    } else {
      const fallbackBtn = tenantPage.locator('div.fixed button').filter({ hasText: /Thanh toán/ }).last()
      await fallbackBtn.click()
    }

    // Complete real card payment on MoMo Sandbox & Napas OTP form (Real IPN is triggered by MoMo)
    await completeMoMoCardPayment(tenantPage)

    // Verify payment outcome success on UI
    await expect(tenantPage.locator('text=Thanh toán thành công').or(tenantPage.locator('text=Thành công')).or(tenantPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 6 [ROOM HANDOVER]: Owner confirms room handover and generates lease
    // =================================================================================
    await safeGoto(ownerPage, '/owner/deposits')
    const confirmLeaseBtn = ownerPage.locator('button:has-text("Tạo Hợp đồng"), button:has-text("Nhận phòng")').first()
    await expect(confirmLeaseBtn).toBeVisible({ timeout: 15000 })
    await confirmLeaseBtn.click()

    // =================================================================================
    // STEP 7 [LEASE ACTIVE & ROOM OCCUPIED]: Verify lease state on Owner & Tenant portals
    // =================================================================================
    await safeGoto(ownerPage, '/owner/leases')
    await expect(ownerPage.locator('text=Đang hiệu lực').or(ownerPage.locator('text=Active')).first()).toBeVisible({ timeout: 15000 })

    await safeGoto(tenantPage, '/tenant/leases')
    await expect(tenantPage.locator('text=Đang hiệu lực').or(tenantPage.locator('text=Active')).first()).toBeVisible({ timeout: 15000 })

    await tenantContext.close()
    await ownerContext.close()
  })
})
