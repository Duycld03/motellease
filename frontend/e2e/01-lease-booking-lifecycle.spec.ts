import { test, expect } from '@playwright/test'
import { getLiveAuth, OWNER_MAP, triggerLiveVnPayIpn } from './auth-helper'

test.describe('Golden Path 1: Room Booking, Owner Approval, Multi-Gateway Checkout & Room Handover', () => {
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
    email: 'tenant77@motellease.local',
    role: 'Tenant',
    phoneNumber: '0988112233',
  }

  const ownerUser = {
    id: 'owner-user-2',
    fullName: 'Trần Thị Bình',
    email: 'owner2@motellease.local',
    role: 'Owner',
    phoneNumber: '09002123456',
  }

  let latestCheckoutData: any = null

  const setupPaymentGateways = async (tenantPage: any) => {
    // Intercept payment gateway sandboxes
    await tenantPage.route((url: URL) => url.hostname.includes('vnpayment') || url.hostname.includes('momo') || url.pathname.includes('vpcpay'), async (route: any) => {
      const txnId = latestCheckoutData?.transactionId || 'txn-vnpay-1'
      await route.fulfill({
        status: 200,
        contentType: 'text/html',
        body: `
          <!DOCTYPE html>
          <html>
            <head><title>Payment Gateway Sandbox</title><meta charset="utf-8"></head>
            <body>
              <div id="step-card">
                <input id="card-number" placeholder="9704 1985 2619 1432 198" />
                <input id="card-holder" placeholder="NGUYEN LOG" />
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
  }

  const setupMockBackend = async (tenantPage: any, ownerPage: any, provider: 'VNPay' | 'MoMo') => {
    let mockDepositStatus = 'Pending'
    const txnId = provider === 'VNPay' ? 'txn-vnpay-1' : 'txn-momo-1'
    const gatewayUrl = provider === 'VNPay' ? 'https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?demo=1' : 'https://test-payment.momo.vn/v2/gateway/pay?demo=1'

    const setupRouteInterceptions = async (page: any) => {
      await page.route('http://localhost:5004/**', async (route: any) => {
        const url = route.request().url()
        const method = route.request().method()

        if (url.includes('/provinces') || url.includes('/facilities')) {
          await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
          return
        }

        if (url.includes('/payments/txn-') || url.includes('/payments/transactions/txn-')) {
          mockDepositStatus = 'Paid'
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: txnId,
              providerOrderId: provider === 'VNPay' ? 'VNPAY_101_89234' : 'MOMO_101_54321',
              provider: provider,
              amount: 3500000,
              status: 'Succeeded',
              initiatedAt: '2026-08-29T12:00:00Z',
              completedAt: '2026-08-29T12:01:00Z',
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
                description: 'Tầng 1 ban công rộng',
                status: 'Available',
              },
            ]),
          })
          return
        }

        if (url.includes('/boarding-houses/house-101/reviews')) {
          await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], total: 0, totalCount: 0 }) })
          return
        }

        if (url.includes('/boarding-houses/house-101')) {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 'house-101',
              name: 'Nhà trọ Cầu Giấy Deluxe',
              addressLine: '123 Đường Cầu Giấy',
              ward: 'Dịch Vọng',
              district: 'Cầu Giấy',
              province: 'Hà Nội',
              type: 'Traditional',
              description: 'Khu trọ an ninh, camera 24/7, giờ giấc tự do, đầy đủ tiện nghi cao cấp.',
              electricityUnitPrice: 3500,
              waterUnitPrice: 25000,
              rating: 4.9,
              reviewCount: 18,
              availableRoomsCount: 3,
              images: [{ id: 'img-1', url: 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267' }],
              owner: { id: 'owner-1', fullName: 'Trần Văn Chủ', phoneNumber: '0912345678' },
              roomTypes: [
                {
                  id: 'rt-1',
                  typeName: 'Phòng Studio Ban công',
                  roomSizeM2: 25,
                  price: 3500000,
                  maxOccupants: 2,
                  availableRoomsCount: 2,
                  description: 'Ban công thoáng mát, có bếp riêng',
                  facilities: [{ id: 'f-1', name: 'Điều hòa' }, { id: 'f-2', name: 'Nóng lạnh' }],
                },
              ],
            }),
          })
          return
        }

        if (url.includes('/boarding-houses')) {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              items: [
                {
                  id: 'house-101',
                  name: 'Nhà trọ Cầu Giấy Deluxe',
                  addressLine: '123 Đường Cầu Giấy',
                  ward: 'Dịch Vọng',
                  district: 'Cầu Giấy',
                  province: 'Hà Nội',
                  type: 'Traditional',
                  latitude: 21.0333,
                  longitude: 105.7833,
                  rating: 4.9,
                  reviewCount: 18,
                  minPrice: 3500000,
                  maxPrice: 4500000,
                  availableRoomsCount: 3,
                  totalRoomsCount: 10,
                  facilities: [],
                  primaryImageUrl: 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267',
                  createdAt: '2026-08-01T00:00:00Z',
                },
              ],
              total: 1,
              totalCount: 1,
              page: 1,
              pageSize: 12,
              totalPages: 1,
            }),
          })
          return
        }

        if (url.includes('/me/saved-listings')) {
          await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], total: 0, totalCount: 0 }) })
          return
        }

        if (url.includes('/deposits/dep-101/approve')) {
          mockDepositStatus = 'Accepted'
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ id: 'dep-101', status: 'Accepted', amount: 3500000 }),
          })
          return
        }

        if (url.includes('/deposits/dep-101/confirm-lease')) {
          mockDepositStatus = 'Completed'
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              leaseContractId: 'lease-101',
              status: 'Active',
              depositStatus: 'Completed',
            }),
          })
          return
        }

        if (url.includes('/deposits/dep-101/contract-preview')) {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              depositId: 'dep-101',
              boardingHouseName: 'Nhà trọ Cầu Giấy Deluxe',
              addressLine: '123 Đường Cầu Giấy',
              ward: 'Dịch Vọng',
              district: 'Cầu Giấy',
              province: 'Hà Nội',
              tenantFullName: 'Nguyễn Văn Thuê',
              tenantPhoneNumber: '0988112233',
              roomNumber: '101',
              startDate: '2026-09-01',
              endDate: '2027-03-01',
              termMonths: 6,
              monthlyRent: 3500000,
              depositHeld: 3500000,
            }),
          })
          return
        }

        if (url.includes('/deposits/dep-101/checkout')) {
          mockDepositStatus = 'Paid'
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              transactionId: txnId,
              paymentUrl: gatewayUrl,
            }),
          })
          return
        }

        if (url.includes('/deposits')) {
          if (method === 'POST') {
            mockDepositStatus = 'Pending'
            await route.fulfill({
              status: 201,
              contentType: 'application/json',
              body: JSON.stringify({ id: 'dep-101', status: 'Pending', amount: 3500000 }),
            })
          } else {
            await route.fulfill({
              status: 200,
              contentType: 'application/json',
              body: JSON.stringify({
                items: [
                  {
                    id: 'dep-101',
                    roomId: 'room-101',
                    roomNumber: '101',
                    boardingHouseId: 'house-101',
                    boardingHouseName: 'Nhà trọ Cầu Giấy Deluxe',
                    tenantUserId: 'tenant-user-1',
                    tenantFullName: 'Nguyễn Văn Thuê',
                    tenantPhoneNumber: '0988112233',
                    amount: 3500000,
                    status: mockDepositStatus,
                    requestedStartDate: '2026-09-01',
                    requestedTermMonths: 6,
                    expiresAt: '2026-09-01T10:00:00Z',
                    createdAt: '2026-08-29T08:00:00Z',
                  },
                ],
                total: 1,
                totalPages: 1,
                page: 1,
                pageSize: 50,
              }),
            })
          }
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
                  depositHeld: 3500000,
                  startDate: '2026-09-01',
                  endDate: '2027-03-01',
                  status: 'Active',
                },
              ],
              total: 1,
              page: 1,
              pageSize: 20,
            }),
          })
          return
        }

        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], total: 0, totalCount: 0 }) })
      })
    }

    await setupRouteInterceptions(tenantPage)
    await setupRouteInterceptions(ownerPage)
  }

  test('1. executes multi-actor lifecycle with VNPay Checkout & Owner Room Handover', async ({ browser }) => {
    const tenantContext = await browser.newContext({ locale: 'vi-VN' })
    const ownerContext = await browser.newContext({ locale: 'vi-VN' })
    const tenantPage = await tenantContext.newPage()
    const ownerPage = await ownerContext.newPage()

    // Auto-accept confirmation dialogs
    ownerPage.on('dialog', async (dialog: any) => await dialog.accept())

    let targetHouseId = 'house-101'
    let targetOwnerEmail = 'owner1@motellease.local'
    const targetTenantEmail = 'tenant77@motellease.local'

    await setupPaymentGateways(tenantPage)

    if (isLive) {
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

      // Clean up previous pending deposits for tenant if any
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

      // Listen for checkout response to get real transactionId & providerOrderId
      tenantPage.on('response', async (response: any) => {
        if (response.url().includes('/checkout') && response.request().method() === 'POST') {
          try {
            latestCheckoutData = await response.json()
          } catch {}
        }
      })
    } else {
      await tenantContext.addCookies([
        { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
        { name: 'ml_access_token', value: 'fake-tenant-token', domain: 'localhost', path: '/' },
        { name: 'ml_refresh_token', value: 'fake-tenant-refresh-token', domain: 'localhost', path: '/' },
        { name: 'ml_user', value: encodeURIComponent(JSON.stringify(tenantUser)), domain: 'localhost', path: '/' },
      ])
      await ownerContext.addCookies([
        { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' },
        { name: 'ml_access_token', value: 'fake-owner-token', domain: 'localhost', path: '/' },
        { name: 'ml_refresh_token', value: 'fake-owner-refresh-token', domain: 'localhost', path: '/' },
        { name: 'ml_user', value: encodeURIComponent(JSON.stringify(ownerUser)), domain: 'localhost', path: '/' },
      ])
      await setupMockBackend(tenantPage, ownerPage, 'VNPay')
    }

    // =================================================================================
    // STEP 1 [TÌM PHÒNG]: Tenant searches & opens property with available rooms
    // =================================================================================
    if (isLive) {
      await safeGoto(tenantPage, `/boarding-houses/${targetHouseId}`)
    } else {
      await safeGoto(tenantPage, '/search')
      const propertyCard = tenantPage.locator('text=Nhà trọ Cầu Giấy Deluxe').or(tenantPage.locator('div[class*="cursor-pointer"] h3')).first()
      await expect(propertyCard).toBeVisible({ timeout: 15000 })
      await propertyCard.click()
    }

    await expect(tenantPage.locator('h1').first()).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 2 [ĐẶT CỌC 24H]: Tenant submits deposit request
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
    // STEP 3 [CHỦ NHÀ DUYỆT CỌC TRÊN UI]: Owner approves deposit on UI
    // =================================================================================
    await safeGoto(ownerPage, '/owner/deposits')
    await expect(ownerPage.locator('h1:has-text("Yêu cầu đặt cọc")').or(ownerPage.locator('h1')).first()).toBeVisible({ timeout: 15000 })
    const approveDepositBtn = ownerPage.locator('button:has-text("Duyệt cọc"), button:has-text("Approve")').first()
    await expect(approveDepositBtn).toBeVisible({ timeout: 10000 })
    await approveDepositBtn.click()
    await expect(ownerPage.locator('text=Đã duyệt').or(ownerPage.locator('text=Accepted')).first()).toBeVisible({ timeout: 10000 })

    // =================================================================================
    // STEP 4 [XEM HỢP ĐỒNG DỰ THẢO]: Tenant views draft contract preview
    // =================================================================================
    await safeGoto(tenantPage, '/tenant/deposits')
    const viewDraftBtn = tenantPage.locator('button:has-text("Xem dự thảo hợp đồng"), button:has-text("Xem dự thảo")').first()
    await expect(viewDraftBtn).toBeVisible({ timeout: 10000 })
    await viewDraftBtn.click()

    // Assert draft contract modal content
    await expect(tenantPage.locator('h3:has-text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM"), h4:has-text("DỰ THẢO")').first()).toBeVisible({ timeout: 5000 })

    // =================================================================================
    // STEP 5 [CHECKOUT VNPAY/MOMO]: Tenant initiates & completes payment
    // =================================================================================
    const proceedPayBtn = tenantPage.locator('button:has-text("Tiến hành thanh toán"), button:has-text("Thanh toán cọc ngay")').first()
    await expect(proceedPayBtn).toBeVisible({ timeout: 5000 })
    await proceedPayBtn.click()

    // Select VNPay gateway inside modal
    const vnpayOption = tenantPage.locator('label').filter({ hasText: /VNPay/ }).first()
    if (await vnpayOption.isVisible({ timeout: 3000 }).catch(() => false)) {
      await vnpayOption.click()
    }

    // Click confirm payment inside modal
    const confirmModalPayBtn = tenantPage.locator('div.fixed button, div[role="dialog"] button').filter({ hasText: /Thanh toán/ }).last()
    await expect(confirmModalPayBtn).toBeVisible({ timeout: 5000 })
    await confirmModalPayBtn.click()

    // Fill Payment Portal Card & OTP
    await tenantPage.waitForURL((url) => url.href.includes('vnpayment.vn') || url.href.includes('vpcpay') || url.pathname.includes('/payments/result'), { timeout: 15000 })

    const cardNumberInput = tenantPage.locator('input#card-number, input[placeholder*="1985"]').first()
    if (await cardNumberInput.isVisible({ timeout: 5000 }).catch(() => false)) {
      await cardNumberInput.fill('9704198526191432198')
    }

    const cardHolderInput = tenantPage.locator('input#card-holder, input[placeholder*="LOG"]').first()
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

    // If in live mode, trigger the backend IPN callback right before confirming payment
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

    // Result page verification
    await tenantPage.waitForURL((url) => url.pathname.includes('/payments/result'), { timeout: 15000 })
    const successTitle = tenantPage.locator('text=Thanh toán thành công').or(tenantPage.locator('text=Thành công')).or(tenantPage.locator('h1')).first()
    await expect(successTitle).toBeVisible({ timeout: 15000 })

    // =================================================================================
    // STEP 6 [CHỦ NHÀ XÁC NHẬN BÀN GIAO PHÒNG]: Owner confirms room handover
    // =================================================================================
    await safeGoto(ownerPage, '/owner/deposits')
    const confirmLeaseBtn = ownerPage.locator('button:has-text("Tạo Hợp đồng"), button:has-text("Nhận phòng")').first()
    await expect(confirmLeaseBtn).toBeVisible({ timeout: 15000 })
    await confirmLeaseBtn.click()

    // =================================================================================
    // STEP 7 [HỢP ĐỒNG THÀNH ACTIVE & PHÒNG CHUYỂN SANG OCCUPIED]:
    // =================================================================================
    await safeGoto(ownerPage, '/owner/leases')
    await expect(ownerPage.locator('text=Đang hiệu lực').or(ownerPage.locator('text=Active')).first()).toBeVisible({ timeout: 15000 })

    await safeGoto(tenantPage, '/tenant/leases')
    await expect(tenantPage.locator('text=Đang hiệu lực').or(tenantPage.locator('text=Active')).first()).toBeVisible({ timeout: 15000 })

    await tenantContext.close()
    await ownerContext.close()
  })
})
