# MotelLease — conventions for AI agents

Read `docs/` before writing code. The specs there are the source of truth.

| Question | Answer |
|---|---|
| What are we building? | `docs/features.md` |
| What does the schema look like? | `docs/erd.md` (assumptions verified — see `docs/verification/`) |
| What are the business rules? | `docs/domain-rules.md` |
| What endpoints exist? | `docs/api-design.md` |

## Language

- Code, identifiers, comments, commit messages, PR descriptions, README: **English**.
- Files under `docs/`: **Vietnamese**. Do not translate them.
- User-facing strings: never hardcoded. Resource keys resolved through i18n (`vi` default, `en`).

## Layering

```
Api            → controllers, DI wiring, middleware, auth policies
Application    → use-case handlers, DTOs, FluentValidation validators
Domain         → entities, enums, business rules. No EF, no ASP.NET, no I/O.
Infrastructure → DbContext, EF configurations, Cloudinary, payment gateways, email, jobs
```

Dependencies point inward only: `Api → Application → Domain`, `Infrastructure → Application → Domain`.
`Domain` references nothing. If a rule needs a database to be tested, it is in the wrong layer.

No MediatR — handlers are plain classes registered in DI. One handler per use case,
named after the use case (`ApproveDepositHandler`, not `DepositService`).

## Database rules

These are not stylistic. Violating them breaks invariants listed in `docs/domain-rules.md` §9.

- Money is `decimal(18,2)`. Never `float`/`double`.
- Timestamps are `timestamptz` (`DateTimeOffset` in C#).
- Enums are stored as text via `HasConversion<string>()`, never as PostgreSQL enum types.
- Soft delete is `IsDeleted` + `HasQueryFilter`. Every unique index on a soft-deletable
  table must be a **partial** index with `WHERE "IsDeleted" = false`.
- `BoardingHouse.Location` is a STORED generated column. EF must never write to it —
  PostgreSQL rejects the write. Seed and update `Latitude`/`Longitude` only.
- `ST_MakePoint` takes **longitude first**. Getting this backwards puts Hanoi in the
  Indian Ocean and every proximity query returns nothing.
- Anything that changes money state goes through one EF transaction and is idempotent.

## Testing

- Integration tests use Testcontainers with `postgis/postgis:17-3.5`. The plain
  `postgres` image will fail — the schema needs the extension.
- Every invariant in `docs/domain-rules.md` §9 gets a test. They are requirements, not extras.
- Payment gateway callbacks are tested with a replayed request: the second call must not
  change any balance.

## Hard prohibitions

Each of these has bitten real systems and is cheap to avoid. They are not preferences:

- No business logic inside ORM lifecycle hooks, and no cron jobs declared inside entity files.
  Both run at times nobody controls, including during tests.
- Never confirm a payment from a browser return URL. Only a server-to-server IPN callback with
  a verified signature may change money state — the user controls the URL they land on.
- Never read a current price when issuing a historical document (bill, contract). Freeze the
  amount at creation time.
- No secrets in committed files. They belong in user-secrets locally and GitHub Actions
  secrets in CI, referenced as `${{ secrets.NAME }}`.
