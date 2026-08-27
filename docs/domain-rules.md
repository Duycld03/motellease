# MotelLease — Business rules

> The source of truth for the Domain and Application layers. Each section states the rule and
> **why** it holds; the invariants in section 9 are requirements that must have tests, not advice.

## 1. Room availability

How many people may live in a room depends on the type of boarding house:

| Type | Rule |
|---|---|
| `Traditional` | At most **1** tenant per room |
| `MiniHouse` | At most **1** tenant per room |
| `DormStyle` | At most `RoomType.MaxOccupants` tenants |

`Room.Status` is a four-value enum (`Available`, `Reserved`, `Occupied`, `Maintenance`) rather than
a boolean flag, because "deposited, not moved in yet" has to be distinguishable from both "free"
and "occupied".

Current occupancy is counted from live `LeaseTenants` — derived from the rows themselves, with no
parallel counter that could drift. The rule lives in `Domain/Rooms/RoomOccupancyPolicy.cs` and is
called from the Application layer. It must **not** live in an EF interceptor or a lifecycle hook:
a rule that needs a database to be tested is in the wrong layer.

## 2. Deposits

The deposit equals one month of the room type's rent, **frozen into `Deposits.Amount`** when the
request is created. Reading `RoomType.Price` at issue time would let an already-agreed amount
change after the fact.

Terms are always normalised to a number of months (`RequestedTermMonths`).

The same person cannot hold two deposit requests on the same room while the first is still live.

Once the owner accepts, `ExpiresAt` gives the tenant N hours to pay. Past the deadline the status
becomes `Expired` and the room returns to `Available`, so an unpaid request cannot hold a room
indefinitely.

## 3. Monthly bill calculation

```
electricityQty = ElectricityNew - ElectricityOld
waterQty       = WaterNew - WaterOld
electricityAmt = electricityQty * BoardingHouse.ElectricityPrice
waterAmt       = waterQty * BoardingHouse.WaterPrice
additionalTotal= Σ RoomAdditionalFees(RoomId, Month, Year)
TotalAmount    = Leases.MonthlyRent + electricityAmt + waterAmt + additionalTotal
```

Four things that are easy to get wrong, and how each is settled:

1. **Additional fees must be filtered by period.** `RoomAdditionalFees` is filtered on
   `(RoomId, Month, Year)`, and a fee that has been billed gets its `PaymentBillId` set so a later
   bill cannot pick it up again.
2. **Rent comes from `Leases.MonthlyRent`**, the price frozen at signing, never from the current
   `RoomType.Price`. An owner raising the room price must not change the bill of a tenant who
   signed at the old one. The general principle: a historical document (bill, contract) never
   reads a current price.
3. **Split the money in whole VND, not in floating point.** Divide evenly among the live
   `LeaseTenants` and give the remainder to the primary tenant (`IsPrimary`), so the sum of the
   shares is exactly `TotalAmount`. A lease with no live tenant blocks bill issuance — a division
   by zero must never be reachable.
4. **The meter update and the bill insert share one EF transaction.** Split apart, a failure in
   between leaves the reading advanced with no bill to account for it.

The opening reading of next month's bill comes from this month's `PaymentBills.ElectricityNew`;
`Rooms.CurrentElectricityReading` is the single current figure. One source of truth, no manual
synchronisation.

## 4. Viewing appointments

Appointments whose time has passed move to `Expired` via
`AppointmentExpiryJob : BackgroundService`, registered explicitly in `Program.cs`. A job must not
be declared inside an entity file: there it runs every time the entity is loaded, including during
tests, and nobody controls its lifetime.

## 5. Revenue

`vw_monthly_revenue` is a materialized view over `PaymentBills` with `Status = 'Paid'`.
Monthly revenue = Σ `TotalAmount` of paid bills, grouped by boarding house.
Profit = revenue − `BoardingHouseExpenses.TotalExpense` for the same period.

No aggregate is stored in a table of its own: every figure here is recomputable, and a manually
maintained counter that drifts gives no signal that it has.

## 6. Authorization

Two layers, because a role alone is not enough: knowing "this user is Staff" does not answer
"is this staff member responsible for that boarding house".

- **Role policies:** `RequireTenant`, `RequireOwner`, `RequireStaffOrOwner`, `RequireAdmin`. The
  fallback policy requires an authenticated user, so endpoints are closed by default; a public
  endpoint opens itself with `[AllowAnonymous]`.
- **Resource handlers (`IAuthorizationHandler`):** `BoardingHouseAccessHandler` checks
  `OwnerUserId == currentUser` **or** the existence of a live `StaffAssignment` for
  `(BoardingHouseId, currentUser)`. Every endpoint that takes a `boardingHouseId` goes through it.

## 7. Notifications

| Event | Recipient | `Type` |
|---|---|---|
| Viewing appointment approved / rejected | Tenant | `AppointmentHandled` |
| New deposit request | Owner + assigned staff | `DepositRequested` |
| Deposit accepted (with payment deadline) | Tenant | `DepositAccepted` |
| Deposit rejected / expired | Tenant | `DepositRejected` / `DepositExpired` |
| Payment succeeded | Tenant + owner | `PaymentSucceeded` |
| New monthly bill issued | Current tenant | `BillIssued` |
| Bill due soon (3 days ahead) | Tenant | `BillDueSoon` |
| Bill overdue | Tenant + owner | `BillOverdue` |
| Extension request answered | Tenant | `ExtensionHandled` |
| Refund processed | Tenant | `RefundProcessed` |
| Withdrawal approved / rejected | Owner | `WithdrawHandled` |
| Lease expiring (30 days ahead) | Tenant + owner | `LeaseExpiring` |
| New maintenance report | Assigned staff | `MaintenanceReported` |
| Listing approved / rejected | Owner | `ListingReviewed` |

Each notification stores `TitleKey`/`BodyKey` + `PayloadJson`, never a finished sentence — the
recipient reads it in the language they have selected when they open it, not when it was sent.

## 8. Background jobs

All registered explicitly in `Program.cs`, none inside an entity file.

| Job | Period | Work |
|---|---|---|
| `AppointmentExpiryJob` | 1 hour | Appointments past their time → `Expired` |
| `DepositExpiryJob` | 15 minutes | Accepted deposits past `ExpiresAt` → `Expired`, room back to `Available` |
| `BillReminderJob` | 1 day | `BillDueSoon` 3 days ahead, `Issued` → `Overdue` once past due |
| `LeaseExpiryJob` | 1 day | `Active` → `Expiring` at ≤30 days left; past `EndDate` → `Ended` |
| `RevenueViewRefreshJob` | 1 hour + after any bill turns `Paid` | `REFRESH MATERIALIZED VIEW CONCURRENTLY` |

## 9. Invariants that must hold (one test each)

1. A room has at most **one** `Lease` in status `Active` (partial unique index).
2. The number of live `LeaseTenants` ≤ `RoomType.MaxOccupants`, and ≤ 1 when the boarding house is
   `Traditional`/`MiniHouse`.
3. `Rooms.Status` stays consistent: an `Active` lease ⇒ `Occupied`; an `Accepted`/`Paid` deposit
   with no lease yet ⇒ `Reserved`; neither ⇒ `Available`.
4. One `(RoomId, Month, Year)` has exactly **one** `PaymentBill` (unique index).
5. `PaymentBills.TotalAmount` = `RentAmount + ElectricityAmount + WaterAmount + AdditionalFeeTotal`.
6. Σ of the amounts split across `LeaseTenants` = `PaymentBills.TotalAmount`, exact to the VND.
7. A `ProviderTxnId` is recorded as successful exactly **once** (unique index + a check in the IPN
   handler).
8. `PaymentBills` reaches `Paid` only with a `Succeeded` `PaymentTransaction` whose
   `SignatureVerified = true`. Money state is never confirmed from a browser redirect URL — only
   from a server-to-server callback with a verified signature.
9. `ElectricityNew ≥ ElectricityOld` and `WaterNew ≥ WaterOld` (CHECK constraint).
10. A top-level `Review` requires the user to have a `Lease` for that boarding house; one review
    per `(UserId, LeaseId)`.
11. An owner cannot withdraw more than `OwnerProfile.AvailableBalance`.
12. Staff can read and write only data of boarding houses they hold a live `StaffAssignment` for.
