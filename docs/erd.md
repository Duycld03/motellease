# MotelLease — Data model (PostgreSQL 17 + PostGIS)

> 29 tables. Conventions: primary key is `Id uuid` (`gen_random_uuid()`), money is
> `decimal(18,2)`, instants are `timestamptz`, soft delete is `IsDeleted boolean` plus an EF
> global query filter. Every table has `CreatedAt`/`UpdatedAt` except join tables and `AuditLogs`.

## Main relationships

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

## 1. Users and sessions

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

## 2. Boarding houses, rooms, facilities

**BoardingHouses**
`Id` · `OwnerUserId` FK · `Name` · `Description` ·
`Type` enum(Traditional, MiniHouse, DormStyle) ·
`AddressLine` · `Ward` · `District` · `Province` ·
`Latitude` decimal(9,6) · `Longitude` decimal(9,6) ·
`Location geography(Point,4326)` **computed STORED** from Longitude/Latitude ·
`ElectricityUnitPrice` · `WaterUnitPrice` ·
`ListingStatus` enum(Draft, PendingReview, Published, Rejected) · `RejectionReason` ·
`Rating` decimal(2,1) cache · `ReviewCount` int cache · `IsDeleted`
Index: GiST on `Location`, `(ListingStatus, IsDeleted)`, `(Province, District)`, `OwnerUserId`

No cached aggregates beyond rating: the price range is derived from `RoomTypes`, room counts from
`vw_room_occupancy`, and save counts from `SavedListings`. Staff coverage lives in
`StaffAssignments` rather than a column here, because one boarding house has several staff.

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

A room keeps only the current meter reading. The opening reading of a bill lives in
`PaymentBills.ElectricityOld`/`WaterOld` and is not duplicated on `Rooms`.

**Images** (polymorphic) — `Id` · `OwnerType` enum(BoardingHouse, RoomType, Room, Review, Report, MaintenanceRequest) ·
`OwnerId` uuid · `Url` · `PublicId` (Cloudinary) · `IsPrimary` · `SortOrder`
Index: `(OwnerType, OwnerId)`, unique partial `(OwnerType, OwnerId) WHERE IsPrimary`

**SavedListings** — `Id` · `UserId` · `BoardingHouseId`
Index: unique `(UserId, BoardingHouseId)`

## 3. Viewing → deposit → lease

**Appointments** — `Id` · `UserId` · `RoomId` · `AppointmentDate` timestamptz ·
`Status` enum RequestStatus · `Note` · `ReasonForCancel` · `HandledByUserId`
Index: `(RoomId, AppointmentDate)`, `(UserId, Status)`

**Deposits** — `Id` · `UserId` · `RoomId` · `Amount` ·
`Status` enum(Pending, Accepted, Paid, Completed, Rejected, Expired, Refunding, Refunded) ·
`RequestedStartDate` · `RequestedTermMonths` int · `ExpiresAt` (payment deadline once accepted) ·
`ReasonForCancel` · `HandledByUserId`
Index: `(RoomId, Status)`, `(UserId, Status)`, partial `(RoomId) WHERE Status IN ('Accepted','Paid')`

Term, start date and end date belong to `Leases`; this table is only the *request* to hold a room.

**Leases** — `Id` · `RoomId` · `DepositId` (nullable, unique) · `PrimaryTenantUserId` ·
`StartDate` date · `EndDate` date · `TermMonths` int ·
`MonthlyRent` (frozen at signing, never read from `RoomTypes` afterwards) ·
`DepositHeld` · `Status` enum(Active, Expiring, Ended, Terminated) ·
`EndedAt` · `EndReason` · `FinalElectricityReading` · `FinalWaterReading` ·
`DepositDeducted` · `DepositRefunded` · `CreatedByUserId`
Index: partial unique `(RoomId) WHERE Status = 'Active'`, `(PrimaryTenantUserId, Status)`, `(EndDate, Status)`

**LeaseTenants** — `Id` · `LeaseId` · `UserId` (nullable — a co-tenant needs no account) ·
`FullName` · `PhoneNumber` · `IdCardNumber` · `IsPrimary` · `MovedInAt` · `MovedOutAt`
Index: `(LeaseId)`, partial `(LeaseId) WHERE MovedOutAt IS NULL`

**ExtensionRequests** — `Id` · `LeaseId` · `RequestedByUserId` · `CurrentEndDate` ·
`RequestedEndDate` · `Status` enum RequestStatus · `TenantNote` · `OwnerNote` · `HandledByUserId`

## 4. Bills and money

**PaymentBills** — `Id` · `LeaseId` · `RoomId` · `Month` int · `Year` int ·
`RentAmount` ·
`ElectricityOld` · `ElectricityNew` · `ElectricityQty` · `ElectricityUnitPrice` · `ElectricityAmount` ·
`WaterOld` · `WaterNew` · `WaterQty` · `WaterUnitPrice` · `WaterAmount` ·
`AdditionalFeeTotal` · `TotalAmount` ·
`Status` enum(Draft, Issued, Overdue, Paid, Cancelled) · `IssuedAt` · `DueDate` · `PaidAt`
Index: unique `(RoomId, Month, Year)`, `(Status, DueDate)`, `(LeaseId, Year, Month)`

The `Qty` and `Amount` columns are stored, not computed: they are frozen when the bill is issued.
Unit prices can change later, and an already-issued bill must not change with them.

**RoomAdditionalFees** — `Id` · `RoomId` · `PaymentBillId` (null until the bill is issued) ·
`FeeName` · `FeeAmount` · `Month` · `Year`

**PaymentTransactions** — `Id` · `UserId` ·
`Purpose` enum(Deposit, Rent, Refund) ·
`DepositId` / `PaymentBillId` / `RefundRequestId` (exactly one is not null — CHECK constraint) ·
`Provider` enum(MoMo, VNPay) ·
`ProviderOrderId` **unique** (the order id we generate) ·
`ProviderTxnId` **unique nullable** (the gateway's transaction id — what stops a repeated IPN
call from being recorded twice) ·
`Amount` · `Status` enum(Initiated, Pending, Succeeded, Failed, Refunded) ·
`RawCallbackPayload jsonb` · `SignatureVerified` bool · `InitiatedAt` · `CompletedAt`
Index: unique `ProviderOrderId`, unique partial `ProviderTxnId WHERE ProviderTxnId IS NOT NULL`, `(Status, InitiatedAt)`

This is the only table allowed to move money state, and only from an IPN endpoint.

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

`OtherExpenses` stays `jsonb` rather than becoming a child table: it is only displayed and summed,
never queried by `feeName`.

## 5. Reviews, reports, operations

**Reviews** — `Id` · `UserId` · `BoardingHouseId` ·
`LeaseId` (nullable — not null means a **verified** review) ·
`ParentReviewId` (nullable — the owner's reply) ·
`Content` · `Rating` smallint CHECK 1..5 (null on a reply) · `IsDeleted`
Index: `(BoardingHouseId, IsDeleted)`, `ParentReviewId`, unique partial `(UserId, LeaseId) WHERE ParentReviewId IS NULL`

**Reports** — `Id` · `ReporterUserId` · `TargetType` enum(Review, BoardingHouse) · `TargetId` uuid ·
`Reason` · `Details` · `Status` enum(Pending, Resolved, Dismissed) ·
`ProcessedByUserId` · `ProcessedAt` · `Resolution` · `IsDeleted`
Index: `(TargetType, TargetId)`, `(Status, CreatedAt)`

**MaintenanceRequests** — `Id` · `LeaseId` · `RoomId` · `ReportedByUserId` ·
`Category` enum(Electricity, Water, Door, Furniture, Internet, Other) · `Description` ·
`Status` enum(Open, InProgress, Resolved, Rejected) · `TaskId` (nullable)
Index: `(RoomId, Status)`, `(LeaseId)`

**Tasks** — `Id` · `BoardingHouseId` (so an owner can list work per property) ·
`CreatedByUserId` · `AssignedToUserId` · `MaintenanceRequestId` (nullable) ·
`Title` · `Details` · `Priority` enum(Low, Medium, High) ·
`Status` enum(InProgress, Completed, Cancelled) · `DueDate` · `CompletedAt`
Index: `(BoardingHouseId, Status)`, `(AssignedToUserId, Status)`, `(DueDate, Status)`

## 6. Notifications and audit log

**Notifications** — `Id` · `UserId` · `Type` enum (see `domain-rules.md` §7) ·
`TitleKey` · `BodyKey` (i18n keys, **never a pre-rendered sentence**) ·
`PayloadJson jsonb` (values to interpolate: room number, amount, …) ·
`LinkUrl` · `IsRead` · `ReadAt`
Index: `(UserId, IsRead, CreatedAt DESC)`

Keys rather than sentences, so that switching language also re-renders notifications already on
file. A stored sentence cannot be translated after the fact.

**AuditLogs** — `Id` · `ActorUserId` · `Action` (e.g. `Account.Lock`, `Review.Delete`, `Withdraw.Approve`) ·
`EntityType` · `EntityId` · `BeforeJson jsonb` · `AfterJson jsonb` · `IpAddress` · `CreatedAt`
Index: `(EntityType, EntityId, CreatedAt DESC)`, `(ActorUserId, CreatedAt DESC)`
No `UpdatedAt`; append-only, never edited or deleted.

## 7. Views

**vw_monthly_revenue** (materialized) — revenue derived from bills, not stored in a summary table
`BoardingHouseId, Year, Month, TotalRevenue, TransactionCount, PaidBillCount`
Source: `PaymentBills` with `Status='Paid'` joined to `Leases` and `Rooms`.
Refresh: a background job after every bill that turns `Paid`, plus nightly.
Index: unique `(BoardingHouseId, Year, Month)` (required by `REFRESH ... CONCURRENTLY`).

**vw_room_occupancy** (materialized) — room counts by status, derived from `Rooms`
`BoardingHouseId, TotalRooms, AvailableRooms, ReservedRooms, OccupiedRooms, MaintenanceRooms, MinPrice, MaxPrice`
`MinPrice`/`MaxPrice` is the price range shown on the listing page.

## 8. Migration notes

> Everything in this section was **verified against a live database** on
> `postgis/postgis:17-3.5` (PostgreSQL 17.5, PostGIS 3.5.2) — see `docs/verification/erd-check.sql`.

1. The first migration enables the extension before any table:
   `modelBuilder.HasPostgresExtension("postgis")`. **`pgcrypto` is not needed**:
   `gen_random_uuid()` has been in core PostgreSQL since 13, and was confirmed to work with no
   extension installed.
2. `BoardingHouses.Location` is declared with
   `.HasComputedColumnSql("ST_SetSRID(ST_MakePoint(\"Longitude\", \"Latitude\"), 4326)::geography", stored: true)`
   so seeding only needs `Latitude`/`Longitude` — see `seed-plan.md`. Verified:
   - `ST_MakePoint` accepts the `decimal(9,6)` columns directly, with **no cast** to `double precision`
   - Updating `Latitude`/`Longitude` updates `Location` automatically
   - Writing to `Location` is rejected by PostgreSQL
     (`column "Location" can only be updated to DEFAULT`), so the property must be mapped
     `.ValueGeneratedOnAddOrUpdate()` and must never appear in an EF `INSERT`/`UPDATE`
   - `ST_DWithin` over 5,000 rows uses a **Bitmap Index Scan** on the GiST index
     (`Location && _st_expand(...)`), not a sequential scan
3. Enums are stored as `text` with `HasConversion<string>()` rather than PostgreSQL enum types, so
   adding or renaming a value needs no `ALTER TYPE` migration.
4. Soft delete is `IsDeleted` + `HasQueryFilter(e => !e.IsDeleted)`. Every unique index on a
   soft-deletable table must be a **partial index** `WHERE "IsDeleted" = false`. Verified: a
   duplicate room number is rejected while the original is live, and accepted once the original
   is soft-deleted.
5. Partial unique `WHERE "Status" = 'Active'` blocks a second active lease on the same room while
   still allowing many `Ended` ones — verified.
6. `REFRESH MATERIALIZED VIEW CONCURRENTLY` requires the view to have a **unique index**; without
   one the refresh fails. Verified with `vw_test`.
7. Money is `decimal(18,2)`. No `float`/`double` on any monetary column.
