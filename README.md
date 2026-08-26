# MotelLease

Boarding house search and management platform. A ground-up rewrite of a MERN
graduation project, keeping the domain and dropping the old architecture.

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10), EF Core + Npgsql, SignalR |
| Frontend | Nuxt 4 (single app serving all 4 roles), TypeScript, Tailwind, `@nuxtjs/i18n` (vi/en) |
| Database | PostgreSQL 17 + PostGIS |
| Media | Cloudinary |
| Payments | MoMo, VNPay (sandbox) |

Four roles: tenant, staff, owner, admin — one Nuxt app with layouts and route
middleware, not separate SPAs.

## Status

**Step 1 — specification: done.** No application code yet.

Specs are written in Vietnamese to match the thesis report. Commit messages,
PR descriptions and this README are in English.

| Document | Contents |
|---|---|
| [docs/features.md](docs/features.md) | Agreed feature scope: what was dropped, what was added, and why |
| [docs/erd.md](docs/erd.md) | 29 tables, indexes, materialized views, PostGIS migration notes |
| [docs/domain-rules.md](docs/domain-rules.md) | Business rules, 12 invariants, and 4 billing bugs inherited from the old code |
| [docs/api-design.md](docs/api-design.md) | ~150 resource-oriented endpoints with policy-based authorization |
| [docs/seed-plan.md](docs/seed-plan.md) | Demo data: coordinate anchors, volumes, consistency rules |
| [docs/api-inventory.csv](docs/api-inventory.csv) | Old project's routes, used only as a cross-check list |

## What changed from the original project

- **`Lease` split from `Deposit`.** The old schema used the deposit record to
  store rental term and dates, so there was nowhere to keep the agreed rent, no
  rental history per room, and no move-out flow.
- **Payments are confirmed by IPN, not by a browser redirect.** The old MoMo
  return handler verified no signature and could be replayed to reserve a room
  for free. `PaymentTransaction.ProviderTxnId` is now unique.
- **Room status is a 4-value enum**, not a boolean plus a nightly cron job that
  repaired the boolean.
- **Notifications exist.** The old backend had none beyond registration OTP.
- **Location search uses PostGIS**, not manual latitude/longitude filtering.
- **Endpoints are grouped by resource**, not duplicated per role.

## Roadmap

2. Lock the schema, write the first migration and `docker-compose.yml` (PostGIS)
3. Scaffold the solution, the Nuxt app and CI
4. Build vertical slices in dependency order: auth → listings/rooms → search →
   viewings → deposits → leases → billing/payments → extensions/refunds →
   revenue → staff/tasks → reviews/reports → admin
5. Integrations: Cloudinary, MoMo, VNPay, email, background jobs
6. Tests (xUnit + Testcontainers with PostGIS, Vitest), seed data, deployment

## Local development

Requires .NET 10 SDK, Node 24, and Docker. PostgreSQL runs in a container — no
local `psql` installation needed.
