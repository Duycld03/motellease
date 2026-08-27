import type {
  BoardingHouseType,
  ListingStatus,
  RoomStatus,
  UserRole,
  Gender,
  BusinessType,
  RequestStatus,
  DepositStatus,
  LeaseStatus,
  BillStatus,
  PaymentPurpose,
  PaymentProvider,
  PaymentStatus,
  ImageOwnerType,
  ReportTargetType,
  ReportStatus,
  MaintenanceCategory,
  MaintenanceStatus,
  TaskPriority,
  WorkTaskStatus,
  NotificationType,
} from './enums'

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
  [key: string]: unknown
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export interface User {
  id: string
  fullName: string
  email: string
  phoneNumber?: string
  role: UserRole
  avatarUrl?: string
  gender?: Gender
  dateOfBirth?: string
  idCardNumber?: string
  preferredLanguage?: string
  isEmailVerified: boolean
  isLocked: boolean
  createdAt: string
  updatedAt?: string
}

export interface AuthTokenResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  user: User
}

export interface SessionInfo {
  id: string
  deviceInfo?: string
  ipAddress?: string
  lastActiveAt: string
  createdAt: string
  isCurrent: boolean
}

export interface ProvinceResponse {
  code: string
  name: string
  fullName: string
}

export interface DistrictResponse {
  code: string
  name: string
  fullName: string
  provinceCode: string
}

export interface FacilityResponse {
  id: string
  name: string
  codeName?: string
  iconKey?: string
}

export interface ImageResponse {
  id: string
  url: string
  isPrimary: boolean
  sortOrder?: number
}

export interface UploadImageResponse {
  url: string
  publicId: string
}

export interface RoomCountsResponse {
  total: number
  available: number
  reserved: number
  occupied: number
  maintenance: number
}

export interface BoardingHouseSummaryResponse {
  id: string
  name: string
  type: BoardingHouseType
  addressLine: string
  ward: string
  district: string
  province: string
  listingStatus: ListingStatus
  rating: number
  reviewCount: number
  roomCount: number
  availableRoomCount: number
  minPrice?: number
  maxPrice?: number
  primaryImageUrl?: string
  createdAt: string
}

export interface BoardingHouseDetailResponse {
  id: string
  ownerUserId: string
  name: string
  description?: string
  type: BoardingHouseType
  addressLine: string
  ward: string
  district: string
  province: string
  latitude: number
  longitude: number
  electricityUnitPrice: number
  waterUnitPrice: number
  listingStatus: ListingStatus
  rejectionReason?: string
  rating: number
  reviewCount: number
  roomCounts: RoomCountsResponse
  images: ImageResponse[]
  createdAt: string
  updatedAt: string
}

export interface SaveBoardingHouseRequest {
  name: string
  description?: string
  type: BoardingHouseType
  addressLine: string
  ward: string
  district: string
  province: string
  latitude: number
  longitude: number
}

export interface UpdateUtilityPricesRequest {
  electricityUnitPrice: number
  waterUnitPrice: number
}

export interface RoomTypeResponse {
  id: string
  boardingHouseId: string
  typeName: string
  price: number
  roomSizeM2: number
  maxOccupants: number
  description?: string
  roomCount: number
  facilities: FacilityResponse[]
}

export interface SaveRoomTypeRequest {
  typeName: string
  price: number
  roomSizeM2: number
  maxOccupants: number
  description?: string
  facilityIds: string[]
}

export interface RoomResponse {
  id: string
  boardingHouseId: string
  roomTypeId: string
  roomTypeName: string
  price: number
  roomNumber: string
  status: RoomStatus
  description?: string
  currentElectricityReading: number
  currentWaterReading: number
  updatedAt: string
}

export interface SaveRoomRequest {
  roomTypeId: string
  roomNumber: string
  description?: string
}

export interface UpdateRoomStatusRequest {
  status: RoomStatus
}

export interface UpdateMeterReadingsRequest {
  electricityReading: number
  waterReading: number
}

// Aliases for compatibility
export type BoardingHouse = BoardingHouseDetailResponse
export type RoomType = RoomTypeResponse
export type Room = RoomResponse
export type Facility = FacilityResponse

export interface Appointment {
  id: string
  boardingHouseId: string
  boardingHouseName?: string
  tenantId: string
  tenantName?: string
  tenantPhone?: string
  tenantEmail?: string
  scheduledAt: string
  status: RequestStatus
  notes?: string
  rejectionReason?: string
  createdAt: string
}

export interface Deposit {
  id: string
  boardingHouseId: string
  boardingHouseName?: string
  roomId: string
  roomNumber?: string
  tenantId: string
  tenantName?: string
  amount: number
  status: DepositStatus
  expiresAt?: string
  paidAt?: string
  paymentTransactionId?: string
  createdAt: string
}

export interface LeaseTenant {
  id: string
  leaseId: string
  tenantId?: string
  fullName: string
  phoneNumber: string
  idCardNumber: string
  isPrimary: boolean
}

export interface Lease {
  id: string
  boardingHouseId: string
  boardingHouseName?: string
  roomId: string
  roomNumber?: string
  tenantId: string
  tenantName?: string
  startDate: string
  endDate: string
  monthlyRent: number
  depositHeld: number
  status: LeaseStatus
  endedAt?: string
  endReason?: string
  tenants: LeaseTenant[]
  createdAt: string
}

export interface PaymentBillItem {
  id: string
  billId: string
  title: string
  amount: number
  description?: string
}

export interface PaymentBill {
  id: string
  leaseId: string
  roomNumber?: string
  boardingHouseName?: string
  month: number
  year: number
  rentAmount: number
  electricityOldReading: number
  electricityNewReading: number
  electricityAmount: number
  waterOldReading: number
  waterNewReading: number
  waterAmount: number
  otherFeesAmount: number
  totalAmount: number
  status: BillStatus
  issuedAt: string
  dueDate: string
  paidAt?: string
  items?: PaymentBillItem[]
}

export interface PaymentTransaction {
  id: string
  purpose: PaymentPurpose
  provider: PaymentProvider
  providerTxnId?: string
  amount: number
  status: PaymentStatus
  paymentUrl?: string
  completedAt?: string
  createdAt: string
}

export interface MaintenanceRequest {
  id: string
  roomId: string
  roomNumber?: string
  boardingHouseId: string
  boardingHouseName?: string
  tenantId: string
  tenantName?: string
  category: MaintenanceCategory
  description: string
  status: MaintenanceStatus
  images: ImageResponse[]
  rejectionReason?: string
  resolvedAt?: string
  createdAt: string
}

export interface WorkTask {
  id: string
  boardingHouseId: string
  boardingHouseName?: string
  assignedStaffId: string
  assignedStaffName?: string
  maintenanceRequestId?: string
  title: string
  description?: string
  priority: TaskPriority
  status: WorkTaskStatus
  dueDate?: string
  completedAt?: string
  createdAt: string
}

export interface BoardingHouseExpense {
  id: string
  boardingHouseId: string
  month: number
  year: number
  electricityAmount: number
  waterAmount: number
  otherExpenses?: Record<string, number>
  totalAmount: number
  notes?: string
}

export interface WithdrawRequest {
  id: string
  ownerId: string
  ownerName?: string
  amount: number
  bankName: string
  accountNumber: string
  accountHolderName: string
  status: RequestStatus
  processedAt?: string
  rejectionReason?: string
  createdAt: string
}

export interface Review {
  id: string
  boardingHouseId: string
  tenantId: string
  tenantName: string
  tenantAvatarUrl?: string
  leaseId?: string
  isVerifiedTenant: boolean
  rating: number
  comment: string
  images: ImageResponse[]
  reply?: string
  repliedAt?: string
  createdAt: string
}

export interface Report {
  id: string
  reporterId: string
  reporterName?: string
  targetType: ReportTargetType
  targetId: string
  reason: string
  status: ReportStatus
  adminNotes?: string
  createdAt: string
}

export interface AuditLog {
  id: string
  actorId: string
  actorName?: string
  action: string
  targetResource: string
  targetId: string
  details?: string
  createdAt: string
}

export interface InAppNotification {
  id: string
  userId: string
  type: NotificationType
  title: string
  body: string
  targetUrl?: string
  isRead: boolean
  createdAt: string
}
