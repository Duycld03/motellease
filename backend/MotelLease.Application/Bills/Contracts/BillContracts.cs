using MotelLease.Domain.Enums;

namespace MotelLease.Application.Bills.Contracts;

public sealed record RoomAdditionalFeeResponse(
    Guid Id,
    Guid RoomId,
    Guid? PaymentBillId,
    string FeeName,
    decimal FeeAmount,
    int Month,
    int Year,
    DateTimeOffset CreatedAt);

public sealed record CreateRoomAdditionalFeeRequest(
    string FeeName,
    decimal FeeAmount,
    int Month,
    int Year);

public sealed record UpdateRoomAdditionalFeeRequest(
    string FeeName,
    decimal FeeAmount);

public sealed record TenantBillSplitResponse(
    Guid TenantId,
    Guid? UserId,
    string FullName,
    bool IsPrimary,
    decimal Amount);

public sealed record BillResponse(
    Guid Id,
    Guid LeaseId,
    Guid RoomId,
    string RoomNumber,
    Guid BoardingHouseId,
    string BoardingHouseName,
    int Month,
    int Year,
    decimal RentAmount,
    decimal ElectricityOld,
    decimal ElectricityNew,
    decimal ElectricityQty,
    decimal ElectricityUnitPrice,
    decimal ElectricityAmount,
    decimal WaterOld,
    decimal WaterNew,
    decimal WaterQty,
    decimal WaterUnitPrice,
    decimal WaterAmount,
    decimal AdditionalFeeTotal,
    decimal TotalAmount,
    BillStatus Status,
    DateTimeOffset? IssuedAt,
    DateOnly? DueDate,
    DateTimeOffset? PaidAt,
    IReadOnlyList<RoomAdditionalFeeResponse> AdditionalFees,
    IReadOnlyList<TenantBillSplitResponse> TenantSplits,
    DateTimeOffset CreatedAt);

public sealed record PreviewBillRequest(
    Guid RoomId,
    int Month,
    int Year,
    decimal ElectricityNew,
    decimal WaterNew);

public sealed record CreateBillRequest(
    Guid RoomId,
    int Month,
    int Year,
    decimal ElectricityNew,
    decimal WaterNew,
    DateOnly? DueDate = null,
    BillStatus Status = BillStatus.Issued);

public sealed record UpdateDraftBillRequest(
    decimal ElectricityNew,
    decimal WaterNew,
    DateOnly? DueDate = null);

public sealed record IssueDraftBillRequest(
    DateOnly DueDate);

public sealed record BillPdfResponse(
    byte[] Content,
    string FileName,
    string ContentType);
