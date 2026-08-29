export interface LiveAuthResult {
  accessToken: string
  refreshToken: string
  user: {
    id: string
    username: string
    email: string
    fullName: string
    role: string
    avatarUrl?: string | null
    preferredLanguage?: string
    emailConfirmed?: boolean
  }
}

export const OWNER_MAP: Record<string, string> = {
  'Nguyễn Văn An': 'owner1@motellease.local',
  'Trần Thị Bình': 'owner2@motellease.local',
  'Lê Hoàng Cường': 'owner3@motellease.local',
  'Phạm Thu Dung': 'owner4@motellease.local',
  'Vũ Đức Giang': 'owner5@motellease.local',
  'Đặng Mai Hoa': 'owner6@motellease.local',
  'Hoàng Minh Khôi': 'owner7@motellease.local',
  'Bùi Lan Phương': 'owner8@motellease.local',
}

export async function getLiveAuth(email: string, password = 'Demo@1234'): Promise<LiveAuthResult> {
  const res = await fetch('http://localhost:5004/api/v1/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      login: email,
      password: password,
    }),
  })

  if (!res.ok) {
    const errorText = await res.text()
    throw new Error(`Failed to authenticate ${email}: ${res.status} ${errorText}`)
  }

  const data = await res.json()
  return {
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
    user: data.user,
  }
}

// =================================================================================
// MoMo Live Sandbox & Napas Bank Card Payment Automation Helper (Real MoMo IPN)
// =================================================================================

export async function completeMoMoCardPayment(page: any) {
  // Wait for redirect to MoMo payment portal or result page
  await page.waitForURL((url: URL) => url.hostname.includes('momo.vn') || url.hostname.includes('mservice') || url.pathname.includes('/payments/result'), { timeout: 30000 })

  if (page.url().includes('momo.vn')) {
    await page.waitForTimeout(2500)
    const cardInput = page.locator('#addNewCard #card-number, #card-number').filter({ visible: true }).first()
    await cardInput.waitFor({ state: 'visible', timeout: 30000 })
    await cardInput.click()
    await cardInput.pressSequentially('9704000000000018', { delay: 15 })

    const expireInput = page.locator('#addNewCard #card-expire, #card-expire').filter({ visible: true }).first()
    await expireInput.click()
    await expireInput.pressSequentially('0307', { delay: 15 })

    const nameInput = page.locator('#addNewCard #card-name, #card-name').filter({ visible: true }).first()
    await nameInput.click()
    await nameInput.pressSequentially('NGUYEN VAN A', { delay: 15 })

    const phoneInput = page.locator('#addNewCard #number-phone, #number-phone').filter({ visible: true }).first()
    await phoneInput.click()
    await phoneInput.pressSequentially('0987654321', { delay: 15 })

    const submitBtn = page.locator('#addNewCard #btn-pay-card, #btn-pay-card').filter({ visible: true }).first()
    await submitBtn.click()

    // Wait for Napas Bank OTP form on mservice
    await page.waitForURL((url: URL) => url.hostname.includes('mservice.com.vn') || url.hostname.includes('napas') || url.pathname.includes('/payments/result'), { timeout: 30000 })
    await page.waitForTimeout(2000)

    const otpInput = page.locator('#otpInput').filter({ visible: true }).first()
    if (await otpInput.isVisible({ timeout: 20000 }).catch(() => false)) {
      await otpInput.click()
      await otpInput.pressSequentially('123456', { delay: 15 })
      await page.locator('#btnSubmit').click()
    }
  }

  // Handle Ngrok interstitial visit page if shown on redirect
  const startTime = Date.now()
  while (Date.now() - startTime < 30000) {
    if (page.url().includes('/payments/result')) {
      break
    }
    if (page.url().includes('ngrok-free.dev')) {
      const visitBtn = page.locator('button:has-text("Visit Site")').first()
      if (await visitBtn.isVisible({ timeout: 1000 }).catch(() => false)) {
        await visitBtn.click()
        await page.waitForTimeout(1000)
      }
    }
    await page.waitForTimeout(500)
  }

  await page.waitForURL((url: URL) => url.pathname.includes('/payments/result'), { timeout: 30000 })
}
