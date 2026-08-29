import crypto from 'crypto'

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

export function createVnPayIpnQuery(
  orderId: string,
  amount: number,
  transactionNo = '12345678',
  tmnCode = 'Z00D1YY7',
  hashSecret = 'OTSTBBUGYTMOKQGEAEXNUBHZZLALRIWC'
): string {
  const fields: Record<string, string> = {
    vnp_Amount: (Math.round(amount) * 100).toString(),
    vnp_BankCode: 'NCB',
    vnp_CardType: 'ATM',
    vnp_OrderInfo: 'Thanh toan MotelLease',
    vnp_PayDate: '20260829120000',
    vnp_ResponseCode: '00',
    vnp_TmnCode: tmnCode,
    vnp_TransactionNo: transactionNo,
    vnp_TransactionStatus: '00',
    vnp_TxnRef: orderId,
  }
  const sortedKeys = Object.keys(fields).sort()
  const canonical = sortedKeys.map((k) => `${k}=${encodeURIComponent(fields[k])}`).join('&')
  const hash = crypto.createHmac('sha512', hashSecret).update(canonical).digest('hex')
  return `?${canonical}&vnp_SecureHash=${hash}`
}

export async function triggerLiveVnPayIpn(orderId: string, amount: number, transactionNo = '12345678'): Promise<any> {
  const query = createVnPayIpnQuery(orderId, amount, transactionNo)
  const res = await fetch(`http://localhost:5004/api/v1/payments/vnpay/ipn${query}`)
  return await res.json()
}
