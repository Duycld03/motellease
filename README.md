# MotelLease

Boarding house search and management platform: tenants find and book rooms, owners and their
staff run the properties, and the monthly billing and payment cycle is handled end to end.

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10), EF Core + Npgsql, SignalR |
| Frontend | Nuxt 4 (single app serving all 4 roles), TypeScript, Tailwind, `@nuxtjs/i18n` (vi/en) |
| Database | PostgreSQL 17 + PostGIS |
| Media | Cloudinary |
| Payments | MoMo, VNPay (sandbox) |

Four roles — tenant, staff, owner, admin — served by one Nuxt app with layouts and route
middleware rather than separate SPAs.

## Design decisions worth knowing

- **Clean layering, enforced by project references.** `Api → Application → Domain` and
  `Infrastructure → Application → Domain`; `Domain` references nothing. A business rule that
  needs a database to be tested is in the wrong layer.
- **No MediatR.** One plain handler class per use case, registered explicitly in DI. The
  indirection of a mediator buys nothing at this size and hides the call graph.
- **Money state changes are transactional and idempotent.** Payment confirmation happens only in
  a server-to-server IPN callback with a verified HMAC signature; `PaymentTransaction.ProviderTxnId`
  is unique, so a replayed callback cannot move a balance twice.
- **Historical documents freeze their amounts.** A bill reads `Leases.MonthlyRent`, not the
  current room price, so a price change never rewrites what an existing tenant owes.
- **Sessions are revocable.** Access tokens are short-lived; refresh tokens rotate and only their
  SHA-256 hash is stored. Presenting a rotated token proves the value leaked and drops every live
  session for that user.
- **Location search is done in the database.** `geography(Point, 4326)` as a stored generated
  column plus a GiST index, queried with `ST_DWithin`/`ST_Distance`.
- **No hardcoded user-facing strings.** Every message is a resource key resolved through i18n,
  and that includes validation errors, emails and notifications — not just the frontend.

## Status

- **Auth feature group: done** — registration with emailed OTP, login (password and Google),
  refresh rotation, session management, password reset, profile and email change.
- **Property management & payments group: done** — boarding houses, room types, rooms, images,
  utility prices; viewing appointments with auto-expiry background sweep; in-app and SignalR
  realtime notifications; room deposits; end-to-end deposit and monthly bill payments via VNPay &
  MoMo with idempotent IPN confirmation; deposit-to-lease transition.
- **Lease lifecycle & billing group: done** — full rental contract lifecycle, co-tenant management
  with occupancy enforcement, early termination and settlement, tenant extension requests with owner
  approval/rejection; room additional fees, bill preview and creation with meter readings advance,
  tenant bill splitting exact to 1 VND, draft bill lifecycle, PDF bill generation (QuestPDF), and
  automated background sweeps for lease expiry and bill reminders.
- **Public search, catalogue & saved listings group: done** — public catalogue search with rich
  filters (keyword, province/district, price range, facilities, house type, min rating, sorting);
  PostGIS spatial nearby searches (`ST_DWithin`, `ST_Distance`) with correct longitude-first
  geometries; bounding box map markers; detailed property views and vacant room lists; verified review
  queries; facility and province/district lookups; tenant saved listings management.
- **Reviews, review replies & reports moderation group: done** — verified review submissions
  enforcing Invariant §9.10 (lease requirement, 1 review per lease); automatic property rating and
  review count recomputation; owner and assigned staff review replies; tenant violation reports
  against listings or reviews; admin report inspection, resolution and dismissal.
- **Staff management, work tasks & maintenance requests group: done** — owner staff account creation,
  boarding house staff assignments with resource-based authorization enforcement (§6, §9.12), staff
  account locking; owner/staff work tasks lifecycle with priority, due date, status and completion
  timestamp; tenant room maintenance requests with category, photos, and auto-dispatching
  `MaintenanceReported` notifications; maintenance request acceptance with automatic linked `WorkTask`
  generation, resolution and rejection.
- **Deposit refunds & owner withdrawals group: done** — tenant deposit refund requests with deposit
  state machine integration (`Paid` -> `Refunding` -> `Refunded`/`Paid`); owner withdrawal requests
  strictly enforcing Invariant §9.11 (available balance guard) with balance reservation, frozen bank
  details snapshot, admin approval, and admin rejection restoring available balance.
  Covered by 136 integration tests that run against a real PostGIS container.

Next: expenses & statistics/analytics, admin platform management & audit logs.

| Document | Contents |
|---|---|
| [docs/features.md](docs/features.md) | Feature scope by role, priorities, entity list, state machines |
| [docs/erd.md](docs/erd.md) | 29 tables, indexes, materialized views, PostGIS migration notes |
| [docs/domain-rules.md](docs/domain-rules.md) | Business rules and the 12 invariants that must hold |
| [docs/api-design.md](docs/api-design.md) | ~150 resource-oriented endpoints with policy-based authorization |
| [docs/seed-plan.md](docs/seed-plan.md) | Demo data: coordinate anchors, volumes, consistency rules |

Specs, code, comments and commit messages are all in English. Vietnamese appears only in the i18n
resource files, where `vi` is the default user-facing language and `en` the alternative.

## Local development

Requires .NET 10 SDK, Node 24, and Docker. PostgreSQL runs in a container, so no local `psql`
installation is needed.

```bash
docker compose up -d                     # PostgreSQL 17 + PostGIS
cd backend/MotelLease.Api
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 bytes>"
dotnet run
```

Swagger UI is at `/swagger` in Development. `dotnet test` starts its own throwaway PostGIS
container, so the integration suite needs Docker but not a running local database.

### Receiving a payment callback locally

A gateway confirms a payment by calling this API from its own servers, and it cannot reach
`localhost`. Nothing else in the flow needs a tunnel: creating the payment URL, the redirect out to
the sandbox and the browser's return all work without one — but the deposit or bill only reaches
`Paid` when the IPN callback arrives.

Configure payment gateway credentials via `dotnet user-secrets` in `backend/MotelLease.Api`:

```bash
# VNPay sandbox credentials
dotnet user-secrets set "VnPay:TmnCode" "<your-tmn-code>"
dotnet user-secrets set "VnPay:HashSecret" "<your-hash-secret>"

# MoMo sandbox credentials
dotnet user-secrets set "MoMo:PartnerCode" "MOMO"
dotnet user-secrets set "MoMo:AccessKey" "<your-access-key>"
dotnet user-secrets set "MoMo:SecretKey" "<your-secret-key>"
```

Copy `.env.example` to `.env`, fill in the two ngrok values, then:

```bash
docker compose --profile tunnel up -d    # publishes the API at your ngrok dev domain
cd backend/MotelLease.Api
ASPNETCORE_URLS=http://0.0.0.0:5004 \
  App__ApiBaseUrl=https://<your-domain>.ngrok-free.app \
  dotnet run
```

`ASPNETCORE_URLS` matters: the default binding is `localhost` only, which the tunnel container
cannot reach. `App__ApiBaseUrl` is what the callback URLs are built from — MoMo is told where to
call back in each request, while VNPay's IPN URL (`https://<your-domain>.ngrok-free.app/api/v1/payments/vnpay/ipn`)
is registered once in the VNPay merchant portal (`https://sandbox.vnpayment.vn/merchantv2/`).
Inspect what a gateway actually sent at `http://localhost:4040`.
