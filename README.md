# MotelLease — Boarding House Search & Management Platform

A comprehensive boarding house search and property management platform built with modern enterprise standards: tenants find and book rooms via spatial search, owners and their staff run properties, and the monthly billing, contract, and payment lifecycle is handled end to end.

| Layer | Technology |
|---|---|
| **Backend** | ASP.NET Core (.NET 10), EF Core 10 + Npgsql, SignalR Hubs, QuestPDF |
| **Frontend** | Nuxt 4 (Single app serving 4 role portals), Vue 3, Tailwind CSS, `@nuxtjs/i18n` (vi/en), Leaflet / OpenStreetMap |
| **Database** | PostgreSQL 17 + PostGIS 3.5 Extension |
| **Media** | Cloudinary (Signed Direct Uploads) |
| **Payments** | MoMo & VNPay (Sandbox / Server-to-Server IPN Confirmation) |
| **Containers** | Docker & Docker Compose (Multi-stage build) |

---

## 🏛️ Clean Architecture & Key Design Decisions

- **Clean Layering Enforced by Project References**:
  - `Api` → Controllers, DI wiring, middleware, SignalR hubs, auth policies.
  - `Application` → Use-case handlers, DTOs, FluentValidation validators.
  - `Domain` → Entities, enums, business rules. **Zero dependencies on EF, ASP.NET, or I/O**.
  - `Infrastructure` → DbContext, EF configurations, Cloudinary, payment gateways, email, background sweeps.
- **No MediatR**: One plain handler class per use case, registered explicitly in DI. Named after use cases (`CreateBillHandler`, `ApproveDepositHandler`).
- **Transactional & Idempotent Financial Operations**: Payment confirmation happens exclusively via server-to-server IPN callbacks with verified HMAC signatures. `PaymentTransaction.ProviderTxnId` is unique, guaranteeing replayed callbacks never move balance twice.
- **Frozen Historical Documents**: Invoices and contracts freeze historical room/utility rates at issuance time, ensuring subsequent property price updates never alter tenant dues.
- **PostGIS Spatial Database Queries**: Boarding house coordinates are stored as `geography(Point, 4326)` in STORED generated columns with GiST indexes, queried using longitude-first `ST_DWithin` and `ST_Distance`.
- **Single Nuxt 4 App for 4 Roles**: Role-based layouts (`default`, `tenant`, `owner`, `staff`, `admin`) with route middleware, responsive mobile-first UI, full bilingual support (`vi` default, `en`), and persistent Dark Mode.

---

## 🌟 Feature Roadmap & Functional Scope

The system is fully developed across 8 major phases:

### 1. Authentication, Profiles & Multi-role Portals (Phase 1)
- User registration with emailed 6-digit OTP verification, secure password login & Google OAuth2.
- Session token management with automatic SHA-256 refresh token rotation and revocation.
- Profile update, avatar upload via Cloudinary, and password reset flow.
- Full bilingual i18n support (Vietnamese default, English) and persistent Dark Mode.

### 2. PostGIS Discovery, Map View & Viewing Appointments (Phase 2)
- Fast spatial search ("Near Me", radius filter, price range, property types, standard facilities, rating).
- Interactive Leaflet map with bounding box auto-fetching and custom property pins.
- Viewing appointments system with auto-expiry background sweep (`Appointments`).
- Tenant saved listings bookmarks.

### 3. Holding Deposits & Payment Gateways (Phase 3)
- 24-hour room holding deposit requests with automatic expiry background worker.
- Draft lease contract preview before payment.
- Seamless payment integration with **MoMo** and **VNPay**.
- Tenant room check-in & deposit-to-lease transition (`Confirm-to-Lease`).

### 4. Leases Lifecycle, Co-tenants & Move-out Settlement (Phase 4)
- Comprehensive rental contracts management (`Active`, `Expiring`, `Ended`, `Terminated`).
- Co-tenants management with property occupancy limits enforcement.
- Tenant lease extension requests with owner review/approval/rejection.
- Real-time move-out settlement calculation preview (`electricity`, `water`, deposit deductions, deposit refund).

### 5. Monthly Meter Readings, Bills & Rent Checkout (Phase 5)
- Owner electric and water meter logging with automatic cost calculation.
- Dynamic room additional fees (Wifi, cleaning, trash, parking).
- Tenant bill split allocation exact to 1 VNĐ.
- Official PDF bill generation using QuestPDF (`GET /api/v1/bills/{id}/pdf`).
- Online monthly rent checkout via MoMo / VNPay.

### 6. Property Operating Expenses, Analytics & Withdrawals (Phase 6)
- Master utility meter logging (Electricity/Water) and miscellaneous operating expenses tracking.
- Owner Financial Analytics Suite: 12-month Revenue vs Expenses bar charts, occupancy doughnut chart, and house-level metrics.
- Real-time dashboard KPI summary.
- Owner bank account withdrawal requests with available balance validation.

### 7. Staff Management, Property Delegation & Work Tasks (Phase 7)
- Owner staff account provisioning, hire date tracking, and account locking.
- Boarding house staff assignments with resource-based authorization enforcement (`BoardingHouseAccess.Managed()`).
- Work Tasks delegation with priority (`Urgent`, `High`, `Medium`, `Low`), due dates, and quick status lifecycle (`Pending` ➔ `InProgress` ➔ `Completed`).
- Dedicated Staff portal and dashboard.

### 8. Maintenance Reports, Violations Moderation & Admin Portal (Phase 8)
- Tenant room maintenance incident reporting with category breakdown (`Electrical`, `Plumbing`, `Furniture`, `Appliances`, `Internet`, `Other`) and image attachments.
- Admin platform dashboard with system-wide user, property, room, and transaction volume KPIs.
- Admin account management (lock/unlock users, create admin accounts).
- Standard facilities catalogue management with room-type usage tracking.
- Owner withdrawal disbursement review and approval.
- Violation reports moderation stream (`Resolve` / `Dismiss`) and system audit log trail (`Audit Logs`).

---

## 🧪 Testing & Verification

- **Backend Integration Test Suite**:
  - Powered by **Testcontainers** running a real `postgis/postgis:17-3.5` PostgreSQL instance.
  - **144 / 144 tests passed (100%)**.
  - All 12 domain invariants from `docs/domain-rules.md` §9 and PDF document generation are verified by automated tests.
  - Run command: `dotnet test backend/MotelLease.slnx`
- **Frontend Automated Test Suite**:
  - Powered by **Vitest** + **@vue/test-utils** + **happy-dom**.
  - **67 / 67 tests passed across 29 test suites (100%)**.
  - Covers end-to-end form validation, DOM input events, modal workflows, API DTO payload assertions, and lifecycle state across all 8 stages.
  - Run command: `npm --prefix frontend test`
- **Frontend Production Build**:
  - `npm --prefix frontend run build` compiles cleanly with **0 errors / 0 warnings** (~1.1 MB client gzip payload).

---

## 🚀 Getting Started

### Option A: One-Command Run with Docker Compose (Recommended)

Start the complete application stack (PostGIS Database + .NET 10 API + Nuxt 4 Frontend) with one command:

```bash
docker compose up --build -d
```

- **Frontend App**: `http://localhost:3000`
- **Backend API & Swagger**: `http://localhost:5004/swagger`
- **PostGIS Database**: `localhost:5432` (`motellease` / `motellease`)

*(Optional)* Start management tools:
```bash
# Start pgAdmin web management UI on http://localhost:5050
docker compose --profile tools up -d

# Start ngrok tunnel for public IPN payment callbacks on http://localhost:4040
docker compose --profile tunnel up -d
```

---

### Option B: Local Manual Development

#### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for PostgreSQL / PostGIS)

#### 1. Database & Backend API Setup

```bash
# 1. Start PostgreSQL with PostGIS
docker compose up db -d

# 2. Configure JWT secret in user-secrets
cd backend/MotelLease.Api
dotnet user-secrets set "Jwt:SigningKey" "a-very-secret-and-secure-key-with-at-least-32-bytes"

# 3. (Optional) Configure payment gateways
dotnet user-secrets set "VnPay:TmnCode" "<your-tmn-code>"
dotnet user-secrets set "VnPay:HashSecret" "<your-hash-secret>"
dotnet user-secrets set "MoMo:PartnerCode" "MOMO"
dotnet user-secrets set "MoMo:AccessKey" "<your-access-key>"
dotnet user-secrets set "MoMo:SecretKey" "<your-secret-key>"

# 4. Run API (Database will auto-migrate & seed)
dotnet run
```

- Swagger UI available at `http://localhost:5004/swagger`.
- Run tests: `dotnet test backend/MotelLease.slnx`

#### 2. Frontend Setup

```bash
# Navigate to frontend
cd frontend

# Install dependencies
npm install

# Run frontend tests
npm test

# Start development server
npm run dev
```

- Web application accessible at `http://localhost:3000`.

---

## 📚 Documentation Reference

| Document | Contents |
|---|---|
| [docs/features.md](docs/features.md) | Full feature scope by role, priorities, state machines |
| [docs/erd.md](docs/erd.md) | Database ERD, 29 tables, indexes, PostGIS spatial geometries |
| [docs/domain-rules.md](docs/domain-rules.md) | Business logic rules and 12 core domain invariants |
| [docs/api-design.md](docs/api-design.md) | Complete REST API endpoint contracts (~150 endpoints) |
| [docs/seed-plan.md](docs/seed-plan.md) | Seed data specification and coordinate anchors |

