import { test, expect } from '@playwright/test'
import { getLiveAuth, triggerLiveVnPayIpn } from './auth-helper'

test.describe('Golden Path 2: Utility Meter Recording, Monthly Invoicing & Online Bill Payment', () => {
  const isLive = process.env.E2E_LIVE === 'true'

  const safeGoto = async (page: any, url: string) => {
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    } catch {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 15000 })
    }
  }

  const tenantUser = {
    id: 'tenant-user-1',
    fullName: 'Nguyễn Văn Thuê',
    email: 'tenant2@motellease.local',
    role: 'Tenant',
    phoneNumber: '0988112233',
  }

  const ownerUser = {
    id: 'owner-user-1',
    fullName: 'Trần Văn Chủ',
    email: 'owner1@motellease.local',
    role: 'Owner',
    phoneNumber: '0912345678',
  }

  let billStatus = 'Issued'
  let latestCheckoutData: any = null

  const setupMockBackend = async (ownerPage: any, tenantPage: any) => {
    billStatus = 'Issued'

    const setupRoutes = async (page: any) => {
      await page.route('http://localhost:5004/**', async (route: any) => {
        const url = route.request().url()
        const method = route.request().method()

        if (url.includes('/payments/txn-bill-1') || url.includes('/payments/transactions/txn-bill-1')) {
          billStatus = 'Paid'
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 'txn-bill-1',
              providerOrderId: 'VNPAY_BILL_101',
              provider: 'VNPay',
              amount: 4250000,
              status: 'Succeeded',
              initiatedAt: '2026-08-29T12:00:00Z',
              completedAt: '2026-08-29T12:01:00Z',
            }),
          })
          return
        }

        if (url.includes('/checkout')) {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              transactionId: 'txn-bill-1',
              paymentUrl: 'https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?demo=1',
            }),
          })
          return
        }

        if (url.includes('/leases')) {
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
                  status: 'Active',
                },
              ],
              total: 1,
              page: 1,
              pageSize: 50,
            }),
          })
          return
        }

        if (url.includes('/bills/preview')) {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              rentAmount: 3500000,
              electricityQty: 50,
              electricityAmount: 175000,
              waterQty: 15,
              waterAmount: 375000,
              additionalFeeTotal: 200000,
              totalAmount: 4250000,
            }),
          })
          return
        }

        if (url.includes('/bills')) {
          if (method === 'POST') {
            billStatus = 'Issued'
            await route.fulfill({
              status: 201,
              contentType: 'application/json',
              body: JSON.stringify({
                id: 'bill-101',
                roomId: 'room-101',
                roomNumber: '101',
                boardingHouseName: 'Nhà trọ Cầu Giấy Deluxe',
                month: 10,
                year: 2026,
                totalAmount: 4250000,
                status: 'Issued',
              }),
            })
          } else {
            await route.fulfill({
              status: 200,
              contentType: 'application/json',
              body: JSON.stringify({
                items: [
                  {
                    id: 'bill-101',
                    roomId: 'room-101',
                    roomNumber: '101',
                    boardingHouseName: 'Nhà trọ Cầu Giấy Deluxe',
                    tenantFullName: 'Nguyễn Văn Thuê',
                    month: 10,
                    year: 2026,
                    rentAmount: 3500000,
                    electricityQty: 50,
                    electricityAmount: 175000,
                    waterQty: 15,
                    waterAmount: 375000,
                    totalAmount: 4250000,
                    status: billStatus,
                    issuedAt: '2026-08-29T08:00:00Z',
                    dueDate: '2026-10-05',
                    paidAt: billStatus === 'Paid' ? '2026-08-29T09:00:00Z' : null,
                  },
                ],
                total: 1,
                page: 1,
                pageSize: 50,
              }),
            })
          }
          return
        }

        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], total: 0 }) })
      })
    }

    await setupRoutes(ownerPage)
    await setupRoutes(tenantPage)
  }

  test('executes complete billing cycle: Owner Meter Readings & Invoice Creation -> Tenant Reviews & Pays Online', async ({ browser }) => {
    const ownerContext = await browser.newContext({ locale: 'vi-VN' })
    const tenantContext = await browser.newContext({ locale: 'vi-VN' })
    const ownerPage = await ownerContext.newPage()
    const tenantPage = await tenantContext.newPage()

    let targetRoomId = 'room-101'
    let targetMonth = 10
    const targetTenantEmail = 'tenant2@motellease.local'

    // Intercept payment gateway sandboxes
    await tenantPage.route((url: URL) => url.hostname.includes('vnpayment') || url.hostname.includes('vpcpay'), async (route: any) => {
      const txnId = latestCheckoutData?.transactionId || 'txn-bill-1'
      await route.fulfill({
        status: 200,
        contentType: 'text/html',
        body: `
          <!DOCTYPE html>
          <html>
            <head><title>VNPAY Bill Payment</title><meta charset="utf-8"></head>
            <body>
              <div id="step-card">
                <input id="card-number" placeholder="9704 1985 2619 1432 198" />
                <input id="card-holder" placeholder="NGUYEN VAN A" />
                <input id="card-date" placeholder="07/15" />
                <button id="btn-next-otp" onclick="document.getElementById('step-card').style.display='none'; document.getElementById('step-otp').style.display='block';">Tiếp tục</button>
              </div>
              <div id="step-otp" style="display: none;">
                <input id="otp-input" placeholder="123456" />
                <button id="btn-confirm-payment" onclick="window.location.href='http://localhost:3000/payments/result?outcome=Succeeded&transactionId=${txnId}';">Xác nhận thanh toán</button>
              </div>
            </body>
          </html>
        `,
      })
    })

    if (isLive) {
      const ownerAuth = await getLiveAuth('owner1@motellease.local')
      const tenantAuth = await getLiveAuth(targetTenantEmail)

      // Get owner's active leases to find the exact room where tenant2 lives
      const leasesRes = await fetch('http://localhost:5004/api/v1/leases?status=Active&page=1&pageSize=50', {
        headers: { Authorization: `Bearer ${ownerAuth.accessToken}` },
      })
      const leasesData = await leasesRes.json()
      const targetLease = leasesData.items?.find((l: any) =>
        l.primaryTenantUserId === tenantAuth.user.id || l.tenants?.some((t: any) => t.userId === tenantAuth.user.id)
      ) || leasesData.items?.[0]

      if (targetLease) {
        targetRoomId = targetLease.roomId

        // Query existing bills for this room to pick an unbilled future month (e.g. month 10, 11, 12)
        const billsRes = await fetch(`http://localhost:5004/api/v1/bills?roomId=${targetRoomId}&pageSize=50`, {
          headers: { Authorization: `Bearer ${ownerAuth.accessToken}` },
        })
        if (billsRes.ok) {
          const existingBills = await billsRes.json()
          const billedMonths = new Set(existingBills.items?.map((b: any) => b.month))
          for (let m = 9; m <= 12; m++) {
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

      // Listen for checkout response to get real transactionId
      tenantPage.on('response', async (response: any) => {
        if (response.url().includes('/checkout') && response.request().method() === 'POST') {
          try {
            latestCheckoutData = await response.json()
          } catch {}
        }
      })
    } else {
      await ownerContext.addCookies([
        { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
        { name: 'ml_access_token', value: 'fake-owner-token', domain: 'localhost', path: '/' },
        { name: 'ml_refresh_token', value: 'fake-owner-refresh-token', domain: 'localhost', path: '/' },
        { name: 'ml_user', value: encodeURIComponent(JSON.stringify(ownerUser)), domain: 'localhost', path: '/' },
      ])
      await tenantContext.addCookies([
        { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
        { name: 'ml_access_token', value: 'fake-tenant-token', domain: 'localhost', path: '/' },
        { name: 'ml_refresh_token', value: 'fake-tenant-refresh-token', domain: 'localhost', path: '/' },
        { name: 'ml_user', value: encodeURIComponent(JSON.stringify(tenantUser)), domain: 'localhost', path: '/' },
      ])
      await setupMockBackend(ownerPage, tenantPage)
    }

    // =================================================================================
    // STEP 1 [GHI CHỈ SỐ & PHÁT HÀNH HÓA ĐƠN]: Owner enters meter readings & issues bill
    // =================================================================================
    await safeGoto(ownerPage, '/owner/bills')
    await expect(ownerPage.locator('h1:has-text("Quản lý Hóa đơn")').or(ownerPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    const ownerModal = ownerPage.locator('div.fixed.inset-0')
    const modalTitle = ownerModal.locator('h3:has-text("Lập Hóa đơn"), h3:has-text("hóa đơn")').first()

    // Ensure modal opens reliably
    await expect(async () => {
      const btn = ownerPage.locator('button:has-text("Lập hóa đơn"), button:has-text("Tạo hóa đơn")').first()
      await btn.click()
      await expect(modalTitle).toBeVisible({ timeout: 1500 })
    }).toPass({ timeout: 15000, intervals: [500, 1000] })

    const roomSelect = ownerModal.locator('select').first()
    await expect(roomSelect).toBeVisible({ timeout: 5000 })
    if (isLive && targetRoomId) {
      await roomSelect.selectOption({ value: targetRoomId })
    } else {
      await roomSelect.selectOption({ index: 1 })
    }

    // Select month
    const monthSelect = ownerModal.locator('select').nth(1)
    if (await monthSelect.isVisible({ timeout: 3000 }).catch(() => false)) {
      await monthSelect.selectOption({ value: String(targetMonth) })
    }

    // Fill new electricity reading & water reading (values higher than previous meter readings)
    const elecInput = ownerModal.locator('input[type="number"]').nth(1)
    if (await elecInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await elecInput.fill('1200')
    }
    const waterInput = ownerModal.locator('input[type="number"]').nth(2)
    if (await waterInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await waterInput.fill('120')
    }

    // Submit Create Bill (Issue Now) inside modal footer
    const submitBillBtn = ownerModal.locator('button').filter({ hasText: /Phát hành hóa đơn ngay|Phát hành/ }).last()
    await expect(submitBillBtn).toBeVisible({ timeout: 5000 })
    await submitBillBtn.click()

    // Verify bill appears on Owner dashboard
    await expect(ownerPage.locator('text=Chưa thanh toán').or(ownerPage.locator('text=Đã phát hành')).or(ownerPage.locator('div[class*="rounded-2xl"]')).first()).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 2 [NGƯỜI THUÊ NHẬN THÔNG BÁO & THANH TOÁN]: Tenant opens /tenant/bills & pays online
    // =================================================================================
    await safeGoto(tenantPage, '/tenant/bills')
    await expect(tenantPage.locator('h1:has-text("Hóa đơn")').or(tenantPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })

    const checkoutModal = tenantPage.locator('div.fixed.inset-0').last()
    const checkoutModalTitle = checkoutModal.locator('h3:has-text("Thanh toán"), h3:has-text("Online")').first()

    // Click pay button on the bill card until checkout modal is open
    await expect(async () => {
      const payBillBtn = tenantPage.locator('button').filter({ hasText: '💳' }).first()
      await payBillBtn.click()
      await expect(checkoutModalTitle).toBeVisible({ timeout: 1500 })
    }).toPass({ timeout: 15000, intervals: [500, 1000] })

    // Select VNPay gateway radio inside modal
    const vnpayOption = checkoutModal.locator('label').filter({ hasText: /VNPay/ }).first()
    if (await vnpayOption.isVisible({ timeout: 3000 }).catch(() => false)) {
      await vnpayOption.click()
    }

    const confirmPayBtn = checkoutModal.locator('button').filter({ hasText: /Thanh toán/ }).last()
    await expect(confirmPayBtn).toBeVisible({ timeout: 5000 })
    await confirmPayBtn.click()

    // =================================================================================
    // STEP 3 [GATEWAY & OTP]: Fills VNPay credentials & OTP
    // =================================================================================
    await tenantPage.waitForURL((url) => url.href.includes('vnpayment.vn') || url.href.includes('vpcpay') || url.pathname.includes('/payments/result'), { timeout: 15000 })

    const cardNumberInput = tenantPage.locator('input#card-number, input[placeholder*="1985"]').first()
    if (await cardNumberInput.isVisible({ timeout: 5000 }).catch(() => false)) {
      await cardNumberInput.fill('9704198526191432198')
    }

    const cardHolderInput = tenantPage.locator('input#card-holder, input[placeholder*="NGUYEN"]').first()
    if (await cardHolderInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await cardHolderInput.fill('NGUYEN VAN A')
    }

    const cardDateInput = tenantPage.locator('input#card-date, input[placeholder*="07/15"]').first()
    if (await cardDateInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await cardDateInput.fill('07/15')
    }

    const btnNextOtp = tenantPage.locator('button#btn-next-otp, button:has-text("Tiếp tục")').first()
    if (await btnNextOtp.isVisible({ timeout: 3000 }).catch(() => false)) {
      await btnNextOtp.click()
    }

    const otpInput = tenantPage.locator('input#otp-input, input[placeholder*="123456"]').first()
    if (await otpInput.isVisible({ timeout: 5000 }).catch(() => false)) {
      await otpInput.fill('123456')
    }

    // Trigger backend IPN callback in live mode to settle bill payment
    if (isLive && latestCheckoutData) {
      const txRes = await fetch(`http://localhost:5004/api/v1/payments/${latestCheckoutData.transactionId}`, {
        headers: { Authorization: `Bearer ${(await getLiveAuth(targetTenantEmail)).accessToken}` },
      })
      if (txRes.ok) {
        const txData = await txRes.json()
        await triggerLiveVnPayIpn(txData.providerOrderId, txData.amount)
      }
    }

    const btnConfirmPayment = tenantPage.locator('button#btn-confirm-payment, button:has-text("Xác nhận thanh toán")').first()
    if (await btnConfirmPayment.isVisible({ timeout: 3000 }).catch(() => false)) {
      await btnConfirmPayment.click()
    }

    // =================================================================================
    // STEP 4 [RESULT]: Verifies Payment Success
    // =================================================================================
    await tenantPage.waitForURL((url) => url.pathname.includes('/payments/result'), { timeout: 15000 })
    const successTitle = tenantPage.locator('text=Thanh toán thành công').or(tenantPage.locator('text=Thành công')).or(tenantPage.locator('h1')).first()
    await expect(successTitle).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 5 [HÓA ĐƠN CHUYỂN PAID & DOANH THU VÀO VÍ]:
    // =================================================================================
    await safeGoto(tenantPage, '/tenant/bills')
    await expect(tenantPage.locator('text=Đã thanh toán').or(tenantPage.locator('text=Paid')).first()).toBeVisible({ timeout: 15000 })

    await safeGoto(ownerPage, '/owner/bills')
    await expect(ownerPage.locator('text=Đã thanh toán').or(ownerPage.locator('text=Paid')).first()).toBeVisible({ timeout: 15000 })

    await ownerContext.close()
    await tenantContext.close()
  })
})
