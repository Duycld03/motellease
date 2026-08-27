export enum BoardingHouseType {
  Traditional = 'Traditional',
  MiniHouse = 'MiniHouse',
  DormStyle = 'DormStyle',
}

export enum ListingStatus {
  Draft = 'Draft',
  PendingReview = 'PendingReview',
  Published = 'Published',
  Rejected = 'Rejected',
}

export enum RoomStatus {
  Available = 'Available',
  Reserved = 'Reserved',
  Occupied = 'Occupied',
  Maintenance = 'Maintenance',
}

export enum UserRole {
  Tenant = 'Tenant',
  Staff = 'Staff',
  Owner = 'Owner',
  Admin = 'Admin',
}

export enum Gender {
  Male = 'Male',
  Female = 'Female',
  Other = 'Other',
}

export enum BusinessType {
  Individual = 'Individual',
  Company = 'Company',
}

export enum RequestStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
  Cancelled = 'Cancelled',
  Expired = 'Expired',
  Completed = 'Completed',
}

export enum DepositStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Paid = 'Paid',
  Completed = 'Completed',
  Rejected = 'Rejected',
  Expired = 'Expired',
  Refunding = 'Refunding',
  Refunded = 'Refunded',
}

export enum LeaseStatus {
  Active = 'Active',
  Expiring = 'Expiring',
  Ended = 'Ended',
  Terminated = 'Terminated',
}

export enum BillStatus {
  Draft = 'Draft',
  Issued = 'Issued',
  Overdue = 'Overdue',
  Paid = 'Paid',
  Cancelled = 'Cancelled',
}

export enum PaymentPurpose {
  Deposit = 'Deposit',
  Rent = 'Rent',
  Refund = 'Refund',
}

export enum PaymentProvider {
  MoMo = 'MoMo',
  VNPay = 'VNPay',
}

export enum PaymentStatus {
  Initiated = 'Initiated',
  Pending = 'Pending',
  Succeeded = 'Succeeded',
  Failed = 'Failed',
  Refunded = 'Refunded',
}

export enum ImageOwnerType {
  BoardingHouse = 'BoardingHouse',
  RoomType = 'RoomType',
  Room = 'Room',
  Review = 'Review',
  Report = 'Report',
  MaintenanceRequest = 'MaintenanceRequest',
}

export enum ReportTargetType {
  Review = 'Review',
  BoardingHouse = 'BoardingHouse',
}

export enum ReportStatus {
  Pending = 'Pending',
  Resolved = 'Resolved',
  Dismissed = 'Dismissed',
}

export enum MaintenanceCategory {
  Electricity = 'Electricity',
  Water = 'Water',
  Door = 'Door',
  Furniture = 'Furniture',
  Internet = 'Internet',
  Other = 'Other',
}

export enum MaintenanceStatus {
  Open = 'Open',
  InProgress = 'InProgress',
  Resolved = 'Resolved',
  Rejected = 'Rejected',
}

export enum TaskPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
}

export enum WorkTaskStatus {
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
}

export enum NotificationType {
  AppointmentHandled = 'AppointmentHandled',
  DepositRequested = 'DepositRequested',
  DepositAccepted = 'DepositAccepted',
  DepositRejected = 'DepositRejected',
  DepositExpired = 'DepositExpired',
  PaymentSucceeded = 'PaymentSucceeded',
  BillIssued = 'BillIssued',
  BillDueSoon = 'BillDueSoon',
  BillOverdue = 'BillOverdue',
  ExtensionHandled = 'ExtensionHandled',
  RefundProcessed = 'RefundProcessed',
  WithdrawHandled = 'WithdrawHandled',
  LeaseExpiring = 'LeaseExpiring',
  MaintenanceReported = 'MaintenanceReported',
  ListingReviewed = 'ListingReviewed',
}
