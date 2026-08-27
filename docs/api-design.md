# MotelLease — API design

> Resource-oriented: one endpoint per resource, authorized by policy rather than by duplicating
> paths per role.

## Conventions

- Base: `/api/v1`. OpenAPI at `/swagger`; the TypeScript client for Nuxt is generated from it.
- Auth: `Authorization: Bearer <access token>`, refreshed through `POST /auth/refresh`.
- Paging: `?page=1&pageSize=20` → `{ items, page, pageSize, total, totalPages }`.
- Filtering and sorting are query params on the list endpoint itself — there is **no** separate
  `/filter` endpoint.
- Errors: RFC 7807 `application/problem+json`, message language from `Accept-Language` (`vi`/`en`).
- The `role` column below: `–` public · `T` tenant · `S` staff · `O` owner · `A` admin.
  `S` always additionally requires a live `StaffAssignment` for the boarding house in question.

## Auth and accounts (16)

| Method | Path | Role |
|---|---|---|
| POST | `/auth/register` | – |
| POST | `/auth/register/send-otp` | – |
| POST | `/auth/register/verify-otp` | – |
| POST | `/auth/login` | – |
| POST | `/auth/login/google` | – |
| POST | `/auth/refresh` | – |
| POST | `/auth/logout` | T S O A |
| POST | `/auth/password/forgot` | – |
| POST | `/auth/password/reset` | – |
| PUT | `/auth/password` | T S O A |
| GET | `/me` | T S O A |
| PUT | `/me` | T S O A |
| PUT | `/me/avatar` | T S O A |
| PUT | `/me/language` | T S O A |
| POST | `/me/email/send-otp` | T S O A |
| POST | `/me/email/verify-otp` | T S O A |
| GET | `/me/sessions` · DELETE `/me/sessions/{id}` | T S O A |

## Public search and catalogue (9)

| Method | Path | Notes |
|---|---|---|
| GET | `/boarding-houses` | filters: `q, province, district, minPrice, maxPrice, facilities[], type, minRating, sort` |
| GET | `/boarding-houses/nearby` | **PostGIS**: `lat, lon, radiusKm, sort=distance` |
| GET | `/boarding-houses/map` | bounding box: `swLat, swLon, neLat, neLon` → trimmed markers |
| GET | `/boarding-houses/{id}` | detail + room types + facilities + images + rating |
| GET | `/boarding-houses/{id}/rooms` | vacant rooms |
| GET | `/boarding-houses/{id}/reviews` | paged, with the verified label |
| GET | `/facilities` | facility catalogue |
| GET | `/provinces` · `/provinces/{code}/districts` | administrative data for the filters |

## Saved listings (3)

| Method | Path | Role |
|---|---|---|
| GET | `/me/saved-listings` | T |
| POST | `/me/saved-listings` | T |
| DELETE | `/me/saved-listings/{boardingHouseId}` | T |

## Viewing appointments (6)

| Method | Path | Role |
|---|---|---|
| GET | `/appointments` | T S O — a tenant sees their own, S/O see theirs by property |
| POST | `/appointments` | T |
| GET | `/appointments/{id}` | T S O |
| PUT | `/appointments/{id}/approve` | S O |
| PUT | `/appointments/{id}/reject` | S O |
| PUT | `/appointments/{id}/cancel` | T |

## Deposits (9)

| Method | Path | Role |
|---|---|---|
| GET | `/deposits` | T S O |
| POST | `/deposits` | T |
| GET | `/deposits/{id}` | T S O |
| PUT | `/deposits/{id}/approve` | S O — sets `ExpiresAt` |
| PUT | `/deposits/{id}/reject` | S O |
| PUT | `/deposits/{id}/cancel` | T |
| POST | `/deposits/{id}/checkout` | T — creates a `PaymentTransaction`, returns the gateway URL |
| GET | `/deposits/{id}/contract-preview` | T |
| POST | `/deposits/{id}/confirm-lease` | S O — a `Paid` deposit becomes a `Lease` |

## Leases (9)

| Method | Path | Role |
|---|---|---|
| GET | `/leases` | T S O |
| GET | `/leases/{id}` | T S O |
| GET | `/leases/{id}/bills` | T S O |
| POST | `/leases/{id}/tenants` | S O — add a co-tenant |
| DELETE | `/leases/{id}/tenants/{tenantId}` | S O |
| POST | `/leases/{id}/terminate` | S O — final readings, deposit settlement |
| GET | `/leases/{id}/termination-preview` | T S O — preview the settlement amount |
| GET | `/rooms/{roomId}/lease-history` | S O |
| GET | `/me/current-lease` | T |

## Lease extensions (5)

| Method | Path | Role |
|---|---|---|
| GET | `/extension-requests` | T S O |
| POST | `/extension-requests` | T |
| GET | `/extension-requests/{id}` | T S O |
| PUT | `/extension-requests/{id}/approve` | S O |
| PUT | `/extension-requests/{id}/reject` | S O |

## Bills (10)

| Method | Path | Role |
|---|---|---|
| GET | `/bills` | T S O — filters `status, month, year, boardingHouseId, roomId` |
| GET | `/bills/{id}` | T S O |
| GET | `/bills/{id}/pdf` | T S O |
| POST | `/bills/preview` | S O — enter readings, preview the amount before issuing |
| POST | `/bills` | S O — issue (one bill per room per month) |
| PUT | `/bills/{id}` | S O — only while `Draft` |
| PUT | `/bills/{id}/issue` | S O — `Draft` → `Issued`, sets `DueDate`, sends notifications |
| PUT | `/bills/{id}/cancel` | S O |
| GET | `/rooms/{roomId}/additional-fees` | S O |
| POST/PUT/DELETE | `/rooms/{roomId}/additional-fees[/{id}]` | S O — filtered by `month`, `year` |

## Payments (8)

| Method | Path | Role |
|---|---|---|
| POST | `/payments/bills/{billId}/checkout` | T — pick a `provider`, returns the gateway URL |
| GET | `/payments/vnpay/ipn` | – **the only place** money is confirmed; verifies HMAC |
| POST | `/payments/momo/ipn` | – same |
| GET | `/payments/vnpay/return` | – redirects to the UI only, writes nothing |
| GET | `/payments/momo/return` | – same |
| GET | `/payments` | T S O A — transaction history |
| GET | `/payments/{id}` | T S O A |
| GET | `/me/payments` | T |

## Refunds and withdrawals (10)

| Method | Path | Role |
|---|---|---|
| GET | `/refund-requests` | T S O A |
| POST | `/refund-requests` | T |
| GET | `/refund-requests/{id}` | T S O A |
| PUT | `/refund-requests/{id}/approve` | O A |
| PUT | `/refund-requests/{id}/reject` | O A |
| GET | `/withdraw-requests` | O A |
| POST | `/withdraw-requests` | O |
| GET | `/withdraw-requests/{id}` | O A |
| PUT | `/withdraw-requests/{id}/approve` | A |
| PUT | `/withdraw-requests/{id}/reject` | A |

## Boarding houses / room types / rooms (O, S) (19)

| Method | Path | Role |
|---|---|---|
| GET | `/my/boarding-houses` | O S — O sees their own, S sees the ones assigned to them |
| POST · GET · PUT · DELETE | `/my/boarding-houses[/{id}]` | O (S may only `GET`/`PUT`) |
| PUT | `/my/boarding-houses/{id}/submit-review` | O — `Draft` → `PendingReview` |
| POST · DELETE | `/my/boarding-houses/{id}/images[/{imageId}]` | O S |
| PUT | `/my/boarding-houses/{id}/images/{imageId}/primary` | O S |
| PUT | `/my/boarding-houses/{id}/utility-prices` | O |
| GET · POST · PUT · DELETE | `/my/boarding-houses/{id}/room-types[/{typeId}]` | O S |
| GET · POST · PUT · DELETE | `/my/boarding-houses/{id}/rooms[/{roomId}]` | O S |
| PUT | `/my/rooms/{roomId}/status` | O S — `Maintenance` ⇄ `Available` |
| PUT | `/my/rooms/{roomId}/meter-readings` | O S — record readings |

## Staff and tasks (11)

| Method | Path | Role |
|---|---|---|
| GET · POST · PUT · DELETE | `/my/staff[/{id}]` | O — create/edit/lock staff accounts |
| GET | `/my/boarding-houses/{id}/staff` | O |
| POST | `/my/boarding-houses/{id}/staff` | O — assign staff (`StaffAssignment`) |
| DELETE | `/my/boarding-houses/{id}/staff/{staffId}` | O — unassign |
| GET | `/tasks` | O S — filters `boardingHouseId, assignedTo, status, priority` |
| POST · GET · PUT | `/tasks[/{id}]` | O (S may `PUT` the status of their own task) |
| PUT | `/tasks/{id}/status` | O S |

## Maintenance requests (6)

| Method | Path | Role |
|---|---|---|
| GET | `/maintenance-requests` | T S O |
| POST | `/maintenance-requests` | T — with photos |
| GET | `/maintenance-requests/{id}` | T S O |
| PUT | `/maintenance-requests/{id}/accept` | S O — generates a `Task` for the assigned staff |
| PUT | `/maintenance-requests/{id}/resolve` | S O |
| PUT | `/maintenance-requests/{id}/reject` | S O |

## Reviews and reports (12)

| Method | Path | Role |
|---|---|---|
| POST | `/reviews` | T — only with a `Lease` for that property |
| PUT · DELETE | `/reviews/{id}` | T (the author) |
| POST | `/reviews/{id}/reply` | O S |
| PUT · DELETE | `/reviews/{id}/reply/{replyId}` | O S |
| GET | `/me/reviews` | T |
| GET | `/my/reviews` | O S — reviews of their own properties |
| POST | `/reports` | T — report a listing or a review |
| GET | `/me/reports` | T |
| GET | `/reports` | A — filters `targetType, status` |
| GET | `/reports/{id}` | A |
| PUT | `/reports/{id}/resolve` · `/reports/{id}/dismiss` | A |

## Owner expenses and statistics (9)

| Method | Path | Role |
|---|---|---|
| GET · POST · PUT · DELETE | `/my/boarding-houses/{id}/expenses[/{expenseId}]` | O |
| GET | `/my/stats/revenue` | O — from `vw_monthly_revenue`, filters `year, boardingHouseId` |
| GET | `/my/stats/revenue/years` | O |
| GET | `/my/stats/occupancy` | O — from `vw_room_occupancy` |
| GET | `/my/stats/profit` | O — revenue − expenses for the same period |
| GET | `/my/stats/summary` | O — dashboard summary cards |

## Notifications (5)

| Method | Path | Role |
|---|---|---|
| GET | `/notifications` | T S O A |
| GET | `/notifications/unread-count` | T S O A |
| PUT | `/notifications/{id}/read` | T S O A |
| PUT | `/notifications/read-all` | T S O A |
| WS | `/hubs/notifications` | T S O A — SignalR |

## Admin (16)

| Method | Path |
|---|---|
| GET · POST · PUT · DELETE | `/admin/accounts[/{id}]` |
| PUT | `/admin/accounts/{id}/lock` · `/admin/accounts/{id}/unlock` |
| POST | `/admin/accounts/{id}/restore` |
| GET | `/admin/boarding-houses` — filter `listingStatus` |
| PUT | `/admin/boarding-houses/{id}/approve` · `/reject` |
| DELETE · POST | `/admin/boarding-houses/{id}` · `/{id}/restore` |
| GET · POST · PUT · DELETE | `/admin/facilities[/{id}]` |
| GET | `/admin/reviews` · DELETE `/admin/reviews/{id}` · POST `/admin/reviews/{id}/restore` |
| GET | `/admin/audit-logs` — filters `actor, entityType, entityId, from, to` |
| GET | `/admin/stats/summary` |

## Image upload (2)

| Method | Path | Role |
|---|---|---|
| POST | `/images` | T S O A — uploads to Cloudinary, returns `url` + `publicId` |
| DELETE | `/images/{id}` | T S O A — deletes from Cloudinary in the same flow |

---

**Total: ~150 endpoints.** The count stays low because of two conventions above: filtering is a
query param on the list endpoint, and staff/owner/admin share one endpoint with resource-level
authorization instead of three copies.
