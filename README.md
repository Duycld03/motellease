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

**Auth feature group: done** — registration with emailed OTP, login (password and Google),
refresh rotation, session management, password reset, profile and email change. Covered by
integration tests that run against a real PostGIS container.

Next: listings and rooms, then search.

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
