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
  fullName?: string
}

export interface DistrictResponse {
  code: string
  name: string
  fullName?: string
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

// Public Catalogue Responses
export interface PublicBoardingHouseCardResponse {
  id: string
  name: string
  type: BoardingHouseType
  addressLine: string
  ward: string
  district: string
  province: string
  latitude: number
  longitude: number
  rating: number
  reviewCount: number
  minPrice?: number
  maxPrice?: number
  primaryImageUrl?: string
  totalRoomsCount: number
  availableRoomsCount: number
  facilities: FacilityResponse[]
  createdAt: string
}

export interface BoardingHouseNearbyResponse extends PublicBoardingHouseCardResponse {
  distanceMeters: number
}

export interface BoardingHouseMapMarkerResponse {
  id: string
  name: string
  latitude: number
  longitude: number
  minPrice?: number
  maxPrice?: number
  primaryImageUrl?: string
  addressLine: string
  rating: number
  reviewCount: number
}

export interface PublicOwnerInfoResponse {
  id: string
  fullName: string
  phoneNumber?: string
  avatarUrl?: string
}

export interface PublicRoomTypeResponse {
  id: string
  typeName: string
  price: number
  roomSizeM2: number
  maxOccupants: number
  description?: string
  totalRoomsCount: number
  availableRoomsCount: number
  facilities: FacilityResponse[]
  images: ImageResponse[]
}

export interface PublicBoardingHouseDetailResponse {
  id: string
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
  rating: number
  reviewCount: number
  totalRoomsCount: number
  availableRoomsCount: number
  owner: PublicOwnerInfoResponse
  images: ImageResponse[]
  roomTypes: PublicRoomTypeResponse[]
  createdAt: string
}

export interface PublicVacantRoomResponse {
  id: string
  roomNumber: string
  roomTypeId: string
  roomTypeName: string
  price: number
  roomSizeM2: number
  maxOccupants: number
  description?: string
}

export interface PublicReviewReplyResponse {
  id: string
  userId: string
  userFullName: string
  userAvatarUrl?: string
  content: string
  createdAt: string
}

export interface PublicReviewResponse {
  id: string
  userId: string
  userFullName: string
  userAvatarUrl?: string
  rating: number
  content: string
  isVerified: boolean
  createdAt: string
  replies: PublicReviewReplyResponse[]
}

// Appointments Contracts
export interface AppointmentResponse {
  id: string
  roomId: string
  roomNumber: string
  boardingHouseId: string
  boardingHouseName: string
  tenantUserId: string
  tenantFullName: string
  tenantPhoneNumber?: string
  appointmentDate: string
  status: RequestStatus
  note?: string
  reasonForCancel?: string
  handledByUserId?: string
  createdAt: string
}

export interface BookAppointmentRequest {
  roomId: string
  appointmentDate: string
  note?: string
}

export interface RejectAppointmentRequest {
  reason: string
}

export interface CancelAppointmentRequest {
  reason?: string
}

// Saved Listings
export interface SaveListingRequest {
  boardingHouseId: string
}

export interface SavedListingResponse {
  id: string
  boardingHouseId: string
  boardingHouse: PublicBoardingHouseCardResponse
  savedAt: string
}

// Deposits Contracts
export interface DepositResponse {
  id: string
  roomId: string
  roomNumber: string
  boardingHouseId: string
  boardingHouseName: string
  tenantUserId: string
  tenantFullName: string
  tenantPhoneNumber?: string
  amount: number
  status: DepositStatus
  requestedStartDate: string
  requestedTermMonths: number
  expiresAt?: string
  reasonForCancel?: string
  handledByUserId?: string
  createdAt: string
}

export interface RequestDepositRequest {
  roomId: string
  requestedStartDate: string
  requestedTermMonths: number
}

export interface RejectDepositRequest {
  reason: string
}

export interface CancelDepositRequest {
  reason?: string
}

export interface DepositContractPreviewResponse {
  depositId: string
  boardingHouseName: string
  addressLine: string
  ward: string
  district: string
  province: string
  roomNumber: string
  tenantFullName: string
  tenantPhoneNumber?: string
  monthlyRent: number
  depositHeld: number
  termMonths: number
  startDate: string
  endDate: string
}

// Payment Contracts
export interface StartPaymentRequest {
  provider: PaymentProvider
}

export interface PaymentCheckoutResponse {
  transactionId: string
  providerOrderId: string
  provider: PaymentProvider
  amount: number
  expiresAt: string
  paymentUrl: string
}

export interface PaymentTransactionResponse {
  id: string
  userId: string
  purpose: PaymentPurpose
  provider: PaymentProvider
  providerOrderId: string
  providerTxnId?: string
  amount: number
  status: PaymentStatus
  signatureVerified: boolean
  depositId?: string
  paymentBillId?: string
  refundRequestId?: string
  initiatedAt: string
  completedAt?: string
}

// Lease Contracts
export interface LeaseTenantResponse {
  id: string
  userId?: string
  fullName: string
  phoneNumber?: string
  idCardNumber?: string
  isPrimary: boolean
  movedInAt?: string
  movedOutAt?: string
}

export interface LeaseResponse {
  id: string
  roomId: string
  roomNumber: string
  boardingHouseId: string
  boardingHouseName: string
  depositId?: string
  primaryTenantUserId?: string
  primaryTenantFullName?: string
  startDate: string
  endDate: string
  termMonths?: number
  monthlyRent: number
  depositHeld: number
  status: LeaseStatus
  tenants: LeaseTenantResponse[]
  createdAt: string
  endedAt?: string
  endReason?: string
  finalElectricityReading?: number
  finalWaterReading?: number
  depositDeducted?: number
  depositRefunded?: number
}

export interface AddLeaseTenantRequest {
  fullName: string
  phoneNumber?: string
  idCardNumber?: string
  userId?: string
}

export interface TerminateLeaseRequest {
  finalElectricityReading: number
  finalWaterReading: number
  depositDeducted?: number
  endReason?: string
}

export interface LeaseTerminationPreviewResponse {
  leaseId: string
  roomId: string
  depositHeld: number
  electricityOld: number
  finalElectricityReading: number
  electricityQty: number
  electricityUnitPrice: number
  electricityAmount: number
  waterOld: number
  finalWaterReading: number
  waterQty: number
  waterUnitPrice: number
  waterAmount: number
  depositDeducted: number
  depositRefunded: number
}

// Extension Requests
export interface CreateExtensionRequest {
  leaseId: string
  requestedEndDate: string
  tenantNote?: string
}

export interface RejectExtensionRequest {
  ownerNote?: string
}

export interface ExtensionRequestResponse {
  id: string
  leaseId: string
  roomId: string
  roomNumber: string
  boardingHouseId: string
  boardingHouseName: string
  requestedByUserId: string
  requesterFullName: string
  currentEndDate: string
  requestedEndDate: string
  status: RequestStatus
  tenantNote?: string
  ownerNote?: string
  handledByUserId?: string
  createdAt: string
}

// Aliases for compatibility
export type BoardingHouse = PublicBoardingHouseCardResponse
export type RoomType = RoomTypeResponse
export type Room = RoomResponse
export type Facility = FacilityResponse
export type Appointment = AppointmentResponse
export type Deposit = DepositResponse
export type Lease = LeaseResponse
export type LeaseTenant = LeaseTenantResponse
export type PaymentTransaction = PaymentTransactionResponse

// Bill Contracts
export interface RoomAdditionalFeeResponse {
  id: string
  roomId: string
  paymentBillId?: string
  feeName: string
  feeAmount: number
  month: number
  year: number
  createdAt: string
}

export interface CreateRoomAdditionalFeeRequest {
  feeName: string
  feeAmount: number
  month: number
  year: number
}

export interface UpdateRoomAdditionalFeeRequest {
  feeName: string
  feeAmount: number
}

export interface TenantBillSplitResponse {
  tenantId: string
  userId?: string
  fullName: string
  isPrimary: boolean
  amount: number
}

export interface BillResponse {
  id: string
  leaseId: string
  roomId: string
  roomNumber: string
  boardingHouseId: string
  boardingHouseName: string
  month: number
  year: number
  rentAmount: number
  electricityOld: number
  electricityNew: number
  electricityQty: number
  electricityUnitPrice: number
  electricityAmount: number
  waterOld: number
  waterNew: number
  waterQty: number
  waterUnitPrice: number
  waterAmount: number
  additionalFeeTotal: number
  totalAmount: number
  status: BillStatus
  issuedAt?: string
  dueDate?: string
  paidAt?: string
  additionalFees: RoomAdditionalFeeResponse[]
  tenantSplits: TenantBillSplitResponse[]
  createdAt: string
}

export interface PreviewBillRequest {
  roomId: string
  month: number
  year: number
  electricityNew: number
  waterNew: number
}

export interface CreateBillRequest {
  roomId: string
  month: number
  year: number
  electricityNew: number
  waterNew: number
  dueDate?: string
  status?: BillStatus
}

export interface UpdateDraftBillRequest {
  electricityNew: number
  waterNew: number
  dueDate?: string
}

export interface IssueDraftBillRequest {
  dueDate: string
}

export type PaymentBill = BillResponse

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

// Maintenance Contracts
export interface CreateMaintenanceRequest {
  leaseId: string
  category: MaintenanceCategory
  description: string
  imageUrls?: string[]
}

export interface AcceptMaintenanceRequest {
  assignToStaffUserId?: string
  taskTitle?: string
  dueDate?: string
}

export interface RejectMaintenanceRequest {
  reason?: string
}

export interface MaintenanceRequestResponse {
  id: string
  leaseId: string
  roomId: string
  roomNumber: string
  boardingHouseId: string
  boardingHouseName: string
  reportedByUserId: string
  reporterFullName: string
  category: MaintenanceCategory
  description: string
  status: MaintenanceStatus
  taskId?: string
  images: ImageResponse[]
  createdAt: string
}

export type MaintenanceRequest = MaintenanceRequestResponse

// Staff Contracts
export interface CreateStaffRequest {
  username: string
  email: string
  password: string
  fullName: string
  phoneNumber?: string
  gender: Gender
  hireDate: string
}

export interface UpdateStaffRequest {
  fullName: string
  phoneNumber?: string
  gender: Gender
  hireDate: string
}

export interface AssignStaffRequest {
  staffUserId: string
}

export interface StaffAssignmentResponse {
  id: string
  boardingHouseId: string
  boardingHouseName: string
  staffUserId: string
  staffFullName: string
  assignedAt: string
}

export interface StaffSummaryResponse {
  id: string
  username: string
  email: string
  fullName: string
  phoneNumber?: string
  gender: Gender
  isLocked: boolean
  hireDate: string
  activeAssignmentsCount: number
  createdAt: string
}

export interface StaffDetailResponse {
  id: string
  username: string
  email: string
  fullName: string
  phoneNumber?: string
  gender: Gender
  isLocked: boolean
  hireDate: string
  assignments: StaffAssignmentResponse[]
  createdAt: string
}

// Tasks Contracts
export interface CreateTaskRequest {
  boardingHouseId: string
  assignedToUserId: string
  title: string
  details?: string
  priority?: TaskPriority
  dueDate?: string
}

export interface UpdateTaskRequest {
  assignedToUserId: string
  title: string
  details?: string
  priority?: TaskPriority
  dueDate?: string
}

export interface UpdateTaskStatusRequest {
  status: WorkTaskStatus
}

export interface TaskResponse {
  id: string
  boardingHouseId: string
  boardingHouseName: string
  createdByUserId: string
  assignedToUserId: string
  assignedToFullName: string
  maintenanceRequestId?: string
  title: string
  details?: string
  priority: TaskPriority
  status: WorkTaskStatus
  dueDate?: string
  completedAt?: string
  createdAt: string
}

export type WorkTask = TaskResponse

// Expenses Contracts
export interface OtherExpenseItem {
  feeName: string
  feeAmount: number
}

export interface CreateExpenseRequest {
  month: number
  year: number
  electricityOld: number
  electricityNew: number
  electricityQty: number
  electricityAmount: number
  waterOld: number
  waterNew: number
  waterQty: number
  waterAmount: number
  otherExpenses?: OtherExpenseItem[]
}

export interface UpdateExpenseRequest {
  electricityOld: number
  electricityNew: number
  electricityQty: number
  electricityAmount: number
  waterOld: number
  waterNew: number
  waterQty: number
  waterAmount: number
  otherExpenses?: OtherExpenseItem[]
}

export interface ExpenseResponse {
  id: string
  boardingHouseId: string
  boardingHouseName: string
  month: number
  year: number
  electricityOld: number
  electricityNew: number
  electricityQty: number
  electricityAmount: number
  waterOld: number
  waterNew: number
  waterQty: number
  waterAmount: number
  otherExpenses: OtherExpenseItem[]
  otherExpensesTotal: number
  totalExpense: number
  createdAt: string
}

export type BoardingHouseExpense = ExpenseResponse

// Statistics Contracts
export interface MonthlyRevenueItem {
  month: number
  revenue: number
  rentRevenue: number
  utilityRevenue: number
  paidBillsCount: number
}

export interface RevenueStatsResponse {
  year: number
  boardingHouseId?: string
  totalRevenue: number
  totalRentRevenue: number
  totalUtilityRevenue: number
  totalPaidBills: number
  monthlyBreakdown: MonthlyRevenueItem[]
}

export interface RevenueYearsResponse {
  years: number[]
}

export interface HouseOccupancyItem {
  boardingHouseId: string
  boardingHouseName: string
  totalRooms: number
  rentedRooms: number
  reservedRooms: number
  vacantRooms: number
  occupancyRate: number
}

export interface OccupancyStatsResponse {
  totalRooms: number
  rentedRooms: number
  reservedRooms: number
  vacantRooms: number
  overallOccupancyRate: number
  houses: HouseOccupancyItem[]
}

export interface MonthlyProfitItem {
  month: number
  revenue: number
  expense: number
  netProfit: number
}

export interface ProfitStatsResponse {
  year: number
  boardingHouseId?: string
  totalRevenue: number
  totalExpense: number
  totalNetProfit: number
  monthlyBreakdown: MonthlyProfitItem[]
}

export interface DashboardSummaryResponse {
  totalBoardingHouses: number
  totalRooms: number
  occupiedRooms: number
  vacantRooms: number
  occupancyRate: number
  activeLeases: number
  pendingAppointments: number
  pendingMaintenanceRequests: number
  unpaidBillsCount: number
  unpaidBillsAmount: number
  revenueThisMonth: number
  expensesThisMonth: number
  profitThisMonth: number
  availableBalance: number
}

// Withdrawals Contracts
export interface CreateWithdrawRequest {
  amount: number
  bankName?: string
  bankAccountNumber?: string
  bankAccountHolder?: string
}

export interface RejectWithdrawRequest {
  reason?: string
}

export interface WithdrawRequestResponse {
  id: string
  ownerUserId: string
  ownerFullName: string
  amount: number
  bankName: string
  bankAccountNumber: string
  bankAccountHolder: string
  status: RequestStatus
  processedByUserId?: string
  processedByFullName?: string
  processedAt?: string
  rejectReason?: string
  createdAt: string
}

export type WithdrawRequest = WithdrawRequestResponse

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

// Reports Contracts
export interface CreateReportRequest {
  targetType: ReportTargetType
  targetId: string
  reason: string
  details?: string
}

export interface ResolveReportRequest {
  resolution?: string
}

export interface DismissReportRequest {
  resolution?: string
}

export interface ReportResponse {
  id: string
  reporterUserId: string
  reporterFullName: string
  targetType: ReportTargetType
  targetId: string
  reason: string
  details?: string
  status: ReportStatus
  processedByUserId?: string
  processedByFullName?: string
  processedAt?: string
  resolution?: string
  createdAt: string
}

export type Report = ReportResponse

// Admin Accounts Contracts
export interface AdminCreateAccountRequest {
  email: string
  username: string
  password: string
  fullName: string
  phoneNumber?: string
  gender: Gender
  role: UserRole
}

export interface AdminUpdateAccountRequest {
  fullName: string
  phoneNumber?: string
  gender: Gender
  role: UserRole
}

export interface AdminLockAccountRequest {
  reason?: string
}

export interface AdminAccountSummaryResponse {
  id: string
  email: string
  username: string
  fullName: string
  phoneNumber?: string
  gender: Gender
  avatarUrl?: string
  role: UserRole
  emailConfirmed: boolean
  isLocked: boolean
  lockedReason?: string
  isDeleted: boolean
  createdAt: string
}

export interface AdminAccountDetailResponse extends AdminAccountSummaryResponse {
  boardingHousesCount: number
  activeLeasesCount: number
  availableBalance?: number
}

// Admin Boarding Houses
export interface AdminBoardingHouseResponse {
  id: string
  name: string
  addressLine: string
  province: string
  district: string
  ward: string
  ownerUserId: string
  ownerFullName: string
  ownerEmail: string
  listingStatus: ListingStatus
  rejectionReason?: string
  isDeleted: boolean
  roomsCount: number
  rating: number
  reviewCount: number
  createdAt: string
}

// Facility Catalog
export interface CreateFacilityRequest {
  name: string
  codeName?: string
  iconKey?: string
  description?: string
}

export interface UpdateFacilityRequest {
  name: string
  codeName?: string
  iconKey?: string
  description?: string
}

export interface FacilityDetailResponse {
  id: string
  name: string
  codeName: string
  iconKey?: string
  description?: string
  inUseByRoomTypesCount: number
  createdAt: string
}

// Audit Logs & Platform Stats
export interface AuditLogResponse {
  id: string
  actorUserId: string
  actorFullName?: string
  action: string
  entityType: string
  entityId?: string
  beforeJson?: string
  afterJson?: string
  ipAddress?: string
  createdAt: string
}

export type AuditLog = AuditLogResponse

export interface AdminPlatformStatsResponse {
  totalUsers: number
  usersByRole: Record<string, number>
  totalBoardingHouses: number
  housesByStatus: Record<string, number>
  totalRooms: number
  roomsByStatus: Record<string, number>
  activeLeases: number
  totalTransactions: number
  totalTransactionVolume: number
  pendingReports: number
  pendingWithdrawals: number
  pendingListingReviews: number
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
