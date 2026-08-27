# MotelLease — Lược đồ dữ liệu (PostgreSQL 17 + PostGIS)

> 29 bảng. Quy ước: PK là `Id uuid` (`gen_random_uuid()`), tiền là `decimal(18,2)`,
> thời điểm là `timestamptz`, xóa mềm là `IsDeleted boolean` + EF global query filter.
> Mọi bảng có `CreatedAt`/`UpdatedAt` trừ bảng nối và `AuditLogs`.

## Sơ đồ quan hệ chính

```
User ─1:1─ OwnerProfile / StaffProfile
User ─1:n─ RefreshToken, SavedListing, Notification, Report, Review

OwnerProfile ─1:n─ BoardingHouse ─1:n─ RoomType ─n:m─ Facility
                        │                  └─1:n─ Room
                        ├─1:n─ StaffAssignment ─n:1─ User(staff)
                        ├─1:n─ BoardingHouseExpense
                        └─1:n─ Task

Room ─1:n─ Appointment
Room ─1:n─ Deposit ──1:1─ Lease ─1:n─ LeaseTenant
Room ─1:n─ Lease ─1:n─ PaymentBill ─1:n─ RoomAdditionalFee
                  ├─1:n─ ExtensionRequest
                  ├─1:n─ MaintenanceRequest ─1:1─ Task
                  └─1:1─ Review (verified)

PaymentTransaction ─n:1─ Deposit | PaymentBill | RefundRequest
Image (polymorphic) → BoardingHouse | RoomType | Room | Review | Report | MaintenanceRequest
```

## 1. Người dùng & phiên

**Users**
`Id` · `Username` unique · `Email` unique · `PasswordHash` · `FullName` · `PhoneNumber` ·
`Gender` enum(Male,Female,Other) · `Role` enum(Tenant,Staff,Owner,Admin) · `SocialId` (Google, nullable, unique) ·
`AvatarUrl` · `AvatarPublicId` · `PreferredLanguage` char(2) default `'vi'` ·
`EmailConfirmed` · `IsLocked` · `LockedReason` · `IsDeleted`
Index: `Email`, `Username`, `SocialId`, `(Role, IsDeleted)`

**OwnerProfiles** — `UserId` PK/FK · `BusinessType` enum(Individual,Company) · `BusinessName` ·
`BankName` · `BankAccountNumber` · `BankAccountHolder` · `AvailableBalance` decimal(18,2)

**StaffProfiles** — `UserId` PK/FK · `HireDate` · `CreatedByUserId` FK→Users(Owner)

**RefreshTokens** — `Id` · `UserId` · `TokenHash` · `ExpiresAt` · `RevokedAt` ·
`ReplacedByTokenId` · `UserAgent` · `IpAddress`
Index: `TokenHash` unique, `(UserId, RevokedAt)`

## 2. Nhà trọ, phòng, tiện ích

**BoardingHouses**
`Id` · `OwnerUserId` FK · `Name` · `Description` ·
`Type` enum(Traditional, MiniHouse, DormStyle) ·
`AddressLine` · `Ward` · `District` · `Province` ·
`Latitude` decimal(9,6) · `Longitude` decimal(9,6) ·
`Location geography(Point,4326)` **computed STORED** từ Longitude/Latitude ·
`ElectricityUnitPrice` · `WaterUnitPrice` ·
`ListingStatus` enum(Draft, PendingReview, Published, Rejected) · `RejectionReason` ·
`Rating` decimal(2,1) cache · `ReviewCount` int cache · `IsDeleted`
Index: GiST trên `Location`, `(ListingStatus, IsDeleted)`, `(Province, District)`, `OwnerUserId`

Không có cột cache: `priceRange` tính từ `RoomTypes`, `totalRooms`/`availableRooms` tính từ
`vw_room_occupancy`, số lượt lưu tin đếm từ `SavedListings`. Staff phụ trách nằm ở
`StaffAssignments` chứ không phải một cột trên bảng này, vì một nhà trọ có nhiều staff.

**StaffAssignments** — `Id` · `BoardingHouseId` · `StaffUserId` · `AssignedByUserId` ·
`AssignedAt` · `UnassignedAt`
Index: unique partial `(BoardingHouseId, StaffUserId) WHERE UnassignedAt IS NULL`

**RoomTypes** — `Id` · `BoardingHouseId` · `TypeName` · `Price` · `RoomSizeM2` ·
`MaxOccupants` int · `Description` · `IsDeleted`

**Facilities** — `Id` · `Name` unique · `CodeName` unique · `Description` · `IconKey` · `IsDeleted`

**RoomTypeFacilities** — `RoomTypeId` + `FacilityId` composite PK

**Rooms** — `Id` · `BoardingHouseId` · `RoomTypeId` · `RoomNumber` ·
`Status` enum(Available, Reserved, Occupied, Maintenance) ·
`Description` · `CurrentElectricityReading` · `CurrentWaterReading` · `IsDeleted`
Index: unique `(BoardingHouseId, RoomNumber) WHERE IsDeleted = false`, `(BoardingHouseId, Status)`

Phòng chỉ giữ chỉ số hiện tại (đồng hồ đang chạy). Chỉ số đầu kỳ của một hóa đơn nằm ở
`PaymentBills.ElectricityOld`/`WaterOld`, không lặp lại trên `Rooms`.

**Images** (polymorphic) — `Id` · `OwnerType` enum(BoardingHouse, RoomType, Room, Review, Report, MaintenanceRequest) ·
`OwnerId` uuid · `Url` · `PublicId` (Cloudinary) · `IsPrimary` · `SortOrder`
Index: `(OwnerType, OwnerId)`, unique partial `(OwnerType, OwnerId) WHERE IsPrimary`

**SavedListings** — `Id` · `UserId` · `BoardingHouseId`
Index: unique `(UserId, BoardingHouseId)`

## 3. Xem phòng → cọc → hợp đồng

**Appointments** — `Id` · `UserId` · `RoomId` · `AppointmentDate` timestamptz ·
`Status` enum RequestStatus · `Note` · `ReasonForCancel` · `HandledByUserId`
Index: `(RoomId, AppointmentDate)`, `(UserId, Status)`

**Deposits** — `Id` · `UserId` · `RoomId` · `Amount` ·
`Status` enum(Pending, Accepted, Paid, Completed, Rejected, Expired, Refunding, Refunded) ·
`RequestedStartDate` · `RequestedTermMonths` int · `ExpiresAt` (hạn phải thanh toán sau khi được duyệt) ·
`ReasonForCancel` · `HandledByUserId`
Index: `(RoomId, Status)`, `(UserId, Status)`, partial `(RoomId) WHERE Status IN ('Accepted','Paid')`

Kỳ hạn, ngày bắt đầu và ngày kết thúc thuộc `Leases`; ở đây chỉ là *yêu cầu* giữ chỗ.

**Leases** — `Id` · `RoomId` · `DepositId` (nullable, unique) · `PrimaryTenantUserId` ·
`StartDate` date · `EndDate` date · `TermMonths` int ·
`MonthlyRent` (giá chốt tại thời điểm ký, không đọc từ `RoomTypes`) ·
`DepositHeld` · `Status` enum(Active, Expiring, Ended, Terminated) ·
`EndedAt` · `EndReason` · `FinalElectricityReading` · `FinalWaterReading` ·
`DepositDeducted` · `DepositRefunded` · `CreatedByUserId`
Index: partial unique `(RoomId) WHERE Status = 'Active'`, `(PrimaryTenantUserId, Status)`, `(EndDate, Status)`

**LeaseTenants** — `Id` · `LeaseId` · `UserId` (nullable — người ở cùng không cần có tài khoản) ·
`FullName` · `PhoneNumber` · `IdCardNumber` · `IsPrimary` · `MovedInAt` · `MovedOutAt`
Index: `(LeaseId)`, partial `(LeaseId) WHERE MovedOutAt IS NULL`

**ExtensionRequests** — `Id` · `LeaseId` · `RequestedByUserId` · `CurrentEndDate` ·
`RequestedEndDate` · `Status` enum RequestStatus · `TenantNote` · `OwnerNote` · `HandledByUserId`

## 4. Hóa đơn & tiền

**PaymentBills** — `Id` · `LeaseId` · `RoomId` · `Month` int · `Year` int ·
`RentAmount` ·
`ElectricityOld` · `ElectricityNew` · `ElectricityQty` · `ElectricityUnitPrice` · `ElectricityAmount` ·
`WaterOld` · `WaterNew` · `WaterQty` · `WaterUnitPrice` · `WaterAmount` ·
`AdditionalFeeTotal` · `TotalAmount` ·
`Status` enum(Draft, Issued, Overdue, Paid, Cancelled) · `IssuedAt` · `DueDate` · `PaidAt`
Index: unique `(RoomId, Month, Year)`, `(Status, DueDate)`, `(LeaseId, Year, Month)`

`Qty` và các `Amount` là cột lưu (chốt tại thời điểm phát hành), không computed —
đơn giá có thể đổi về sau, hóa đơn cũ không được đổi theo.

**RoomAdditionalFees** — `Id` · `RoomId` · `PaymentBillId` (nullable đến khi hóa đơn phát hành) ·
`FeeName` · `FeeAmount` · `Month` · `Year`

**PaymentTransactions** — `Id` · `UserId` ·
`Purpose` enum(Deposit, Rent, Refund) ·
`DepositId` / `PaymentBillId` / `RefundRequestId` (đúng một cái not null — CHECK constraint) ·
`Provider` enum(MoMo, VNPay) ·
`ProviderOrderId` **unique** (mã đơn ta sinh ra) ·
`ProviderTxnId` **unique nullable** (mã giao dịch phía cổng trả về — chống ghi trùng khi IPN gọi lại) ·
`Amount` · `Status` enum(Initiated, Pending, Succeeded, Failed, Refunded) ·
`RawCallbackPayload jsonb` · `SignatureVerified` bool · `InitiatedAt` · `CompletedAt`
Index: unique `ProviderOrderId`, unique partial `ProviderTxnId WHERE ProviderTxnId IS NOT NULL`, `(Status, InitiatedAt)`

Đây là bảng duy nhất được phép chuyển trạng thái tiền, và chỉ từ endpoint IPN.

**RefundRequests** — `Id` · `DepositId` · `LeaseId` (nullable) · `UserId` · `Amount` ·
`Status` enum RequestStatus · `Reason` · `ProcessedByUserId` · `ProcessedAt` · `RejectReason`
Index: `(UserId, Status)`, `(DepositId)`, `CreatedAt DESC`

**WithdrawRequests** — `Id` · `OwnerUserId` · `Amount` ·
`BankName` · `BankAccountNumber` · `BankAccountHolder` ·
`Status` enum RequestStatus · `ProcessedByUserId` · `ProcessedAt` · `RejectReason`
Index: `(OwnerUserId, Status)`, `(Status, CreatedAt)`

**BoardingHouseExpenses** — `Id` · `BoardingHouseId` · `Month` · `Year` ·
`ElectricityOld/New/Qty/Amount` · `WaterOld/New/Qty/Amount` ·
`OtherExpenses jsonb` (`[{feeName, feeAmount}]`) · `OtherExpensesTotal` · `TotalExpense`
Index: unique `(BoardingHouseId, Month, Year)`

`OtherExpenses` giữ dạng `jsonb` thay vì bảng con: chỉ dùng để hiển thị và cộng tổng,
không bao giờ query theo `feeName`.

## 5. Đánh giá, báo cáo, vận hành

**Reviews** — `Id` · `UserId` · `BoardingHouseId` ·
`LeaseId` (nullable — not null nghĩa là **đánh giá đã xác minh**) ·
`ParentReviewId` (nullable — trả lời của chủ trọ) ·
`Content` · `Rating` smallint CHECK 1..5 (null khi là reply) · `IsDeleted`
Index: `(BoardingHouseId, IsDeleted)`, `ParentReviewId`, unique partial `(UserId, LeaseId) WHERE ParentReviewId IS NULL`

**Reports** — `Id` · `ReporterUserId` · `TargetType` enum(Review, BoardingHouse) · `TargetId` uuid ·
`Reason` · `Details` · `Status` enum(Pending, Resolved, Dismissed) ·
`ProcessedByUserId` · `ProcessedAt` · `Resolution` · `IsDeleted`
Index: `(TargetType, TargetId)`, `(Status, CreatedAt)`

**MaintenanceRequests** — `Id` · `LeaseId` · `RoomId` · `ReportedByUserId` ·
`Category` enum(Electricity, Water, Door, Furniture, Internet, Other) · `Description` ·
`Status` enum(Open, InProgress, Resolved, Rejected) · `TaskId` (nullable)
Index: `(RoomId, Status)`, `(LeaseId)`

**Tasks** — `Id` · `BoardingHouseId` (để owner xem được việc theo từng nhà trọ) ·
`CreatedByUserId` · `AssignedToUserId` · `MaintenanceRequestId` (nullable) ·
`Title` · `Details` · `Priority` enum(Low, Medium, High) ·
`Status` enum(InProgress, Completed, Cancelled) · `DueDate` · `CompletedAt`
Index: `(BoardingHouseId, Status)`, `(AssignedToUserId, Status)`, `(DueDate, Status)`

## 6. Thông báo & nhật ký

**Notifications** — `Id` · `UserId` · `Type` enum (xem `domain-rules.md` §7) ·
`TitleKey` · `BodyKey` (khóa i18n, **không lưu câu tiếng Việt sẵn**) ·
`PayloadJson jsonb` (tham số điền vào câu: tên phòng, số tiền…) ·
`LinkUrl` · `IsRead` · `ReadAt`
Index: `(UserId, IsRead, CreatedAt DESC)`

Lưu khóa i18n thay vì câu hoàn chỉnh để người dùng đổi ngôn ngữ thì thông báo cũ
cũng đổi theo — nếu lưu sẵn tiếng Việt thì không dịch lại được.

**AuditLogs** — `Id` · `ActorUserId` · `Action` (vd `Account.Lock`, `Review.Delete`, `Withdraw.Approve`) ·
`EntityType` · `EntityId` · `BeforeJson jsonb` · `AfterJson jsonb` · `IpAddress` · `CreatedAt`
Index: `(EntityType, EntityId, CreatedAt DESC)`, `(ActorUserId, CreatedAt DESC)`
Không có `UpdatedAt`, không sửa, không xóa (append-only).

## 7. View

**vw_monthly_revenue** (materialized) — doanh thu suy ra từ hóa đơn, không lưu bảng tổng hợp
`BoardingHouseId, Year, Month, TotalRevenue, TransactionCount, PaidBillCount`
Nguồn: `PaymentBills` `Status='Paid'` join `Leases` join `Rooms`.
Refresh: background job sau mỗi lần hóa đơn chuyển sang `Paid`, và hằng đêm.
Index: unique `(BoardingHouseId, Year, Month)` (cần cho `REFRESH ... CONCURRENTLY`).

**vw_room_occupancy** (materialized) — số phòng theo trạng thái, suy ra từ `Rooms`
`BoardingHouseId, TotalRooms, AvailableRooms, ReservedRooms, OccupiedRooms, MaintenanceRooms, MinPrice, MaxPrice`
`MinPrice`/`MaxPrice` là khoảng giá hiển thị trên trang danh sách.

## 8. Ghi chú migration

> Toàn bộ mục này đã **kiểm chứng thật** trên `postgis/postgis:17-3.5`
> (PostgreSQL 17.5, PostGIS 3.5.2) — xem `docs/verification/erd-check.sql`.

1. Migration đầu tiên bật extension trước mọi bảng: `modelBuilder.HasPostgresExtension("postgis")`.
   **Không cần `pgcrypto`**: `gen_random_uuid()` đã nằm trong core PostgreSQL từ bản 13,
   đã test chạy được khi chưa cài extension nào.
2. `BoardingHouses.Location` khai bằng
   `.HasComputedColumnSql("ST_SetSRID(ST_MakePoint(\"Longitude\", \"Latitude\"), 4326)::geography", stored: true)`
   → seed chỉ cần nhập `Latitude`/`Longitude`, xem `seed-plan.md`. Đã kiểm:
   - `ST_MakePoint` nhận trực tiếp cột `decimal(9,6)`, **không cần cast** sang `double precision`
   - Sửa `Latitude`/`Longitude` thì `Location` tự cập nhật theo
   - Ghi thẳng vào `Location` bị PostgreSQL từ chối
     (`column "Location" can only be updated to DEFAULT`) → property này phải map
     `.ValueGeneratedOnAddOrUpdate()` và không bao giờ nằm trong `INSERT`/`UPDATE` của EF
   - `ST_DWithin` trên 5.000 bản ghi dùng **Bitmap Index Scan** trên index GiST
     (`Location && _st_expand(...)`), không seq scan
3. Enum: lưu dạng `text` + `HasConversion<string>()` thay vì PG enum type — đổi/thêm giá trị
   không cần migration `ALTER TYPE`.
4. Xóa mềm: `IsDeleted` + `HasQueryFilter(e => !e.IsDeleted)`. Mọi unique index trên bảng
   có xóa mềm phải là **partial index** `WHERE "IsDeleted" = false`. Đã kiểm: trùng số phòng
   khi chưa xóa thì bị chặn, xóa mềm rồi tạo lại cùng số phòng thì được.
5. Partial unique `WHERE "Status" = 'Active'` chặn được lease Active thứ hai trên cùng phòng,
   trong khi vẫn cho phép nhiều lease `Ended` — đã kiểm.
6. `REFRESH MATERIALIZED VIEW CONCURRENTLY` yêu cầu view có **unique index**; thiếu là
   lệnh refresh fail. Đã kiểm với `vw_test`.
7. Tiền: `decimal(18,2)`. Không dùng `float`/`double` ở bất kỳ cột tiền nào.





