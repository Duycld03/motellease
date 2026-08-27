# MotelLease — Feature specification

> Stack: ASP.NET Core (.NET 10) + Nuxt 4 (one app, four roles) + PostgreSQL 17 + PostGIS.
> Settled decisions: Cloudinary for images, i18n vi/en, PostGIS for location search, no mobile app.

## 0. Design principles

1. One business concept = one entity. No two tables meaning the same thing.
2. Never store a figure that can be recomputed — use a view or a query.
3. States are named enums, not free-form strings compared with regexes.
4. Endpoints are grouped by resource and authorized by policy, not duplicated per role.
5. Every change to money state is idempotent and runs inside a transaction.

---

## 1. Scope

A web system for four roles in a single Nuxt app (layouts and route middleware per role, not four
separate SPAs). Roughly 150 endpoints over 29 tables.

Out of scope for the first version:

| Not doing | Reason |
|---|---|
| Mobile app | A responsive web app is enough at this scope |
| Tenant ↔ owner/staff chat | See 3.11 — the SignalR hub already exists, so adding it later needs no architectural change |
| Saved searches + new-match alerts | Low value for the cost |
| Side-by-side comparison of 2–3 listings | Low value for the cost |
| Excel report export | PDF bills already cover this (3.4) |

Two data-model decisions worth stating up front, because they shape much of the schema:

- **The boarding-house type is a C# enum, not an admin-editable table.** Occupancy rules branch on
  this value (`domain-rules.md` §1), so adding or renaming one at runtime would create a state no
  rule applies to.
- **Admins do not self-register.** Admin accounts are only seeded or created by another admin.

---

## 2. Features by role

### Tenant
- Register / sign in (email + Google OAuth), email OTP verification, forgot/reset password, change password, change email with OTP confirmation
- Browse listings and details: boarding houses, room types, facilities, images, map location
- Search, filter (price, area, occupants, facilities, type, region), sort (newest, top rated, price)
- Save listings (`SavedListing`)
- Book a viewing (`Appointment`), cancel with a reason
- Pay a deposit online (MoMo / VNPay), view own deposits
- Request a refund (`RefundRequest`)
- Pay the monthly rent (metered electricity/water + additional fees)
- Request a lease extension (`ExtensionRequest`)
- Write and reply to reviews (with images), report a listing or a review
- Manage profile, change avatar

### Owner
- Manage boarding houses: create/edit/delete, images (with a primary one), location, utility prices
- Manage room types (price, area, occupants, facilities) and rooms (number, meter readings)
- Approve / reject viewings, deposits, refunds and extensions
- Record meter readings → issue monthly bills, add per-room additional fees
- Operating expenses (`BoardingHouseExpense`: electricity, water, other)
- Revenue reports by month / year / property
- Request a withdrawal (`WithdrawRequest`) with bank details
- Manage staff: create staff accounts, assign properties, hand out tasks
- Reply to tenant reviews

### Staff
- Act only on properties the owner has assigned (resource-based authorization)
- Manage rooms, record readings, issue bills, handle deposits and viewings
- Receive and update assigned work (`Task`)

### Admin
- Manage accounts (lock/unlock, soft delete, create)
- Manage all boarding houses and the facility catalogue
- Handle reports against listings and reviews, hide or delete reviews
- Approve owner withdrawal requests

---

## 3. Key features

### P0 — required; without these the domain does not hold together

**3.1. `Lease` separate from `Deposit`**
A deposit receipt and a rental contract are two different concepts and need two tables. Merged,
there is nowhere to keep the rent frozen at signing, the list of co-tenants, or the rental history
of a room, and no move-out flow exists at all.
- `Deposit`: holding the room, the deposit amount, the confirmation deadline
- `Lease`: the contract (term, frozen rent, deposit held, status) → the single source for who is renting which room
- `LeaseTenant`: the people living under one contract
- Move-out is recorded on `Lease` itself (`EndedAt`, `EndReason`, final meter readings, deposit settlement) — a contract ends once, so no separate table is needed

**3.2. Notifications (`Notification`)**
The deposit / approval / billing flows have to tell users what just happened rather than leaving
them to refresh:
- A `Notification` table (in-app, with `IsRead`) plus a SignalR hub for realtime delivery
- Email for the significant moments (deposit accepted/rejected, viewing confirmed, new bill, payment due soon, extension answered, refund processed, withdrawal approved)
- Templates in `vi`/`en`, chosen by the recipient's language

**3.3. Idempotent payments with IPN**
- `PaymentTransaction` with a UNIQUE `ProviderTxnId` → no replay, no double-write
- The IPN endpoint (server-to-server) is the **only** place money state changes; the browser return
  URL is display-only. The user controls the URL they are redirected to, so it cannot serve as
  proof of payment.
- HMAC verification is mandatory for both MoMo and VNPay, and only each gateway's actual success
  code is accepted
- Transaction history for tenants and owners

**3.4. Bill lifecycle with a due date**
`IssuedAt`, `DueDate`, a reminder job N days ahead, overdue marking, and PDF export.

**3.5. Verified reviews**
A review requires a `Lease` (or a completed `Deposit`) for that property, and carries a
"previously rented" label. If anyone can review any listing, the rating carries no information.

**3.6. Refresh tokens and session management**
Short-lived access tokens plus refresh-token rotation stored in the database (hash only). Individual
sessions are revocable, all sessions drop when an admin locks the account, and a "signed-in devices"
page is possible because every live refresh token is one device.

**3.7. Rate limiting and abuse prevention**
The ASP.NET Core rate limiter, partitioned by IP, on `/login`, `/register` and `/password/forgot`,
plus a separate per-email limit on the OTP endpoints (cooldown between sends + a cap on wrong
attempts) so nobody else's mailbox can be used as a target.

### P1

**3.8. Location search with PostGIS**
- A `geography(Point, 4326)` column with a GiST index
- Radius search (`ST_DWithin`), ordering by distance (`ST_Distance`)
- Map with a bounding box as the user pans/zooms, marker clustering
- "Near me" and "near a school/workplace" (address → geocode)

Filtering lat/lon by hand in the application layer cannot use an index and is wrong over larger
distances.

**3.9. Maintenance requests (`MaintenanceRequest`)**
A tenant reports broken electricity, water or a door lock (with photos) → a `Task` is generated
automatically for the staff assigned to that property. `Task` carries `BoardingHouseId` so an owner
can list work per property.

**3.10. Audit log (`AuditLog`)**
Records admin/owner actions that affect other people: locking an account, deleting a review,
rejecting a refund, approving a withdrawal.

**3.11. (Deferred) Tenant ↔ owner/staff chat**
Since `Notification` already brings up a SignalR hub, adding chat later needs no architectural
change. Not in the first version.

**3.12. Server-side i18n**
`@nuxtjs/i18n` on the frontend. On the server: validation messages, email bodies and notifications
are all localized, selected from `Accept-Language` and `User.PreferredLanguage`.

**3.13. Owner dashboard**
Occupancy rate, revenue vs expenses, vacant rooms over time, deposit cancellation rate — all
derived from `Lease` + `PaymentBill` + `BoardingHouseExpense`.

### P2 — accepted

- OpenAPI → generated TypeScript client for Nuxt (`openapi-typescript`), keeping the frontend in step with the backend
- Admin review before a listing goes public (`ListingStatus`: Draft → PendingReview → Published / Rejected)
- Reviewing and restoring soft-deleted records (accounts, reviews)

---

## 4. Entities (29 tables)

**Core domain (18):**
`User` · `BoardingHouse` · `Room` · `RoomType` · `Facility` · `SavedListing` · `Appointment` ·
`Deposit` · `Lease` · `RefundRequest` · `ExtensionRequest` · `PaymentBill` ·
`PaymentTransaction` · `RoomAdditionalFee` · `BoardingHouseExpense` · `WithdrawRequest` ·
`Review` · `Report`

**Relationships and decomposition (6):**
`OwnerProfile`, `StaffProfile` (role-specific fields, so a `User` row carries no unused columns) ·
`Image` (`PublicId` + `Url` + `IsPrimary` + `OwnerType`/`OwnerId`) · `RoomTypeFacility` ·
`LeaseTenant` · `StaffAssignment` (several staff per property, with a validity period)

**Operations (5):**
`Notification` · `MaintenanceRequest` · `AuditLog` · `RefreshToken` · `Task` (with `BoardingHouseId`)

**Not tables**: `vw_monthly_revenue`, `vw_room_occupancy` (materialized views)

## 5. State machines

```
RoomStatus:     Available → Reserved → Occupied → Maintenance → Available
DepositStatus:  Pending → Accepted → Paid → Completed
                       ↘ Rejected   ↘ Expired  ↘ Refunding → Refunded
LeaseStatus:    Active → Expiring → Ended
                      ↘ Terminated
BillStatus:     Draft → Issued → Overdue → Paid → Cancelled
PaymentStatus:  Initiated → Pending → Succeeded / Failed / Refunded
RequestStatus:  Pending → Approved / Rejected / Cancelled
                (shared by Appointment, Extension, Refund, Withdraw)
```

`Reserved` is why `RoomStatus` has to be an enum: a room with a paid deposit but nobody moved in yet
is neither free nor occupied. A boolean flag cannot express that state, and the consequence is a
periodic job that exists only to repair the flag.

## 6. Settled technical notes

- **Cloudinary**: `CloudinaryDotNet`. The `Image` table stores `PublicId` + `Url` + `IsPrimary` +
  `OwnerType`/`OwnerId`. Deleting from Cloudinary happens in the same flow that deletes the record,
  so no unreferenced file is left behind.
- **i18n**: `@nuxtjs/i18n`, `vi` default, `en` secondary, prefix strategy `prefix_except_default`.
  The server picks the message language from `Accept-Language`.
- **PostGIS**: `NetTopologySuite` + `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`,
  image `postgis/postgis:17-3.5` in `docker-compose.yml`.
- **Money**: `decimal(18,2)`, never `double`.
