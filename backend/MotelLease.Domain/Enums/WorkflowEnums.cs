namespace MotelLease.Domain.Enums;

/// <summary>
/// Shared lifecycle for anything a tenant asks for and an owner answers: appointments,
/// extension requests, refunds, withdrawals. Deposits need more states and have their own
/// enum (see <see cref="DepositStatus"/>).
/// </summary>
public enum RequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled,
    Expired,
    Completed
}

/// <summary>
/// A deposit outlives a simple approval: it is approved, then paid, then either consumed by
/// a lease or refunded. <c>Accepted</c> means "approved, waiting for payment before
/// <see cref="Entities.Deposit.ExpiresAt"/>" (docs/domain-rules.md §2).
/// </summary>
public enum DepositStatus
{
    Pending,
    Accepted,
    Paid,
    Completed,
    Rejected,
    Expired,
    Refunding,
    Refunded
}

public enum LeaseStatus
{
    Active,

    /// <summary>Still active, but within 30 days of EndDate (LeaseExpiryJob sets this).</summary>
    Expiring,

    Ended,

    /// <summary>Ended before EndDate.</summary>
    Terminated
}

public enum BillStatus
{
    Draft,
    Issued,
    Overdue,
    Paid,
    Cancelled
}

public enum PaymentPurpose
{
    Deposit,
    Rent,
    Refund
}

public enum PaymentProvider
{
    MoMo,
    VNPay
}

/// <summary>
/// Only the IPN endpoint may advance this past <c>Pending</c>
/// (docs/domain-rules.md §9.7, §9.8).
/// </summary>
public enum PaymentStatus
{
    Initiated,
    Pending,
    Succeeded,
    Failed,
    Refunded
}

public enum ImageOwnerType
{
    BoardingHouse,
    RoomType,
    Room,
    Review,
    Report,
    MaintenanceRequest
}

public enum ReportTargetType
{
    Review,
    BoardingHouse
}

public enum ReportStatus
{
    Pending,
    Resolved,
    Dismissed
}

public enum MaintenanceCategory
{
    Electricity,
    Water,
    Door,
    Furniture,
    Internet,
    Other
}

public enum MaintenanceStatus
{
    Open,
    InProgress,
    Resolved,
    Rejected
}

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public enum WorkTaskStatus
{
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// One value per row of docs/domain-rules.md §7. The value is also the i18n key prefix:
/// <c>notification.{Type}.title</c> / <c>.body</c>.
/// </summary>
public enum NotificationType
{
    AppointmentHandled,
    DepositRequested,
    DepositAccepted,
    DepositRejected,
    DepositExpired,
    PaymentSucceeded,
    BillIssued,
    BillDueSoon,
    BillOverdue,
    ExtensionHandled,
    RefundProcessed,
    WithdrawHandled,
    LeaseExpiring,
    MaintenanceReported,
    ListingReviewed
}
