# MotelLease — Demo data plan

> Goal: demo location search, the deposit → lease → bill flow, and revenue reporting in a way that
> **looks real**. Uniformly random data makes the PostGIS work look pointless.

## 1. Volumes

| Table | Rows | Notes |
|---|---|---|
| Users | 120 | 1 admin, 8 owners, 12 staff, ~99 tenants |
| BoardingHouses | 60 | 35 in Hanoi, 25 in Ho Chi Minh City; 54 `Published`, 4 `PendingReview`, 2 `Rejected` |
| RoomTypes | 150 | 2–3 types per property |
| Rooms | 600 | 8–15 rooms per property |
| Facilities | 20 | wifi, air conditioning, water heater, loft, parking, no curfew, … |
| Leases | 260 | ~43% of rooms currently rented |
| PaymentBills | 1,800 | the last 6 months for every active lease |
| Reviews | 320 | only from users with a `Lease`, per the verified-review rule |
| Deposits | 90 | spread across all 8 statuses |
| Appointments | 140 | past and future |

Enough that lists page for real, the revenue chart has 6 points, and `EXPLAIN` shows the GiST index
being used instead of a sequential scan.

## 2. Coordinates: clustered around anchors, not uniformly random

Real boarding houses grow around universities and industrial parks. Pick 7 anchors, each with 6–10
properties within 300 m–3 km:

| Anchor | Lat, Lon (approx.) | District | Properties |
|---|---|---|---|
| Hanoi University of Science and Technology | 21.0045, 105.8435 | Hai Bà Trưng, HN | 8 |
| Vietnam National University Hanoi (Xuân Thủy) | 21.0378, 105.7825 | Cầu Giấy, HN | 9 |
| Thuongmai University / Hồ Tùng Mậu | 21.0410, 105.7690 | Cầu Giấy, HN | 7 |
| Thăng Long Industrial Park | 21.1160, 105.7770 | Đông Anh, HN | 6 |
| HCMC University of Technology (District 10) | 10.7720, 106.6580 | District 10, HCM | 8 |
| Vietnam National University HCMC (Linh Trung) | 10.8700, 106.8000 | Thủ Đức, HCM | 9 |
| Tân Bình Industrial Park | 10.8100, 106.6200 | Tân Bình, HCM | 8 |

These coordinates are **approximate and must be checked on a map before seeding** — one wrong
decimal digit is roughly 11 km off.

Generating a point around an anchor (radius `r` metres, random bearing):

```csharp
// 1° of latitude ≈ 111_320 m; 1° of longitude ≈ 111_320 * cos(lat) m
var bearing  = rnd.NextDouble() * 2 * Math.PI;
var distance = 300 + rnd.NextDouble() * 2700;          // 300 m .. 3 km
var dLat = distance * Math.Cos(bearing) / 111_320.0;
var dLon = distance * Math.Sin(bearing) / (111_320.0 * Math.Cos(anchor.Lat * Math.PI / 180));
var lat  = Math.Round(anchor.Lat + dLat, 6);
var lon  = Math.Round(anchor.Lon + dLon, 6);
```

## 3. The seeder writes only Latitude/Longitude

`BoardingHouses.Location` is a STORED computed column (see `erd.md` §8), so the seeder must **not**
set `Location` — PostgreSQL derives it. This also means changing lat/lon updates the geography
automatically, with no risk of the two drifting apart.

The trap worth repeating: `ST_MakePoint(Longitude, Latitude)` — **longitude first**. A query to
verify it:

```sql
-- HUST → must return the properties anchored at HUST, not all 60
SELECT "Name", ROUND(ST_Distance("Location",
         ST_SetSRID(ST_MakePoint(105.8435, 21.0045), 4326)::geography)) AS m
FROM "BoardingHouses"
WHERE ST_DWithin("Location",
        ST_SetSRID(ST_MakePoint(105.8435, 21.0045), 4326)::geography, 3000)
ORDER BY m;
```

If this returns 0 rows, or all 60 properties, the coordinate order in the seed is wrong.

## 4. Addresses must match the coordinates

`AddressLine` is generated from the anchor's district rather than randomly nationwide — coordinates
in Cầu Giấy with an address reading "District 1, HCMC" is something a demo viewer spots immediately.

Each anchor keeps 4–6 real street names from its district, combined with a random house number:
`"{number} {street}, {ward}, {district}, {province}"`.

## 5. Data must satisfy the business invariants

The seeder must not produce data that violates the invariants in `domain-rules.md` §9. If it does,
a constraint will reject it — that is the constraint doing its job, so do not disable constraints
just to get the seed through:

- A room with an `Active` `Lease` ⇒ `Rooms.Status = Occupied`; with an `Accepted`/`Paid` `Deposit`
  and no lease yet ⇒ `Reserved`.
- A room's `PaymentBills` must be consecutive by month, and `ElectricityOld` in month N must equal
  `ElectricityNew` in month N−1. Grow readings by 30–120 kWh and 3–15 m³ per month.
- Of the last 6 months of bills: older months `Paid`, the most recent month a mix of
  `Issued`/`Overdue`/`Paid` so the dashboard shows all three states.
- Every `Paid` bill needs a `Succeeded` `PaymentTransaction` with a distinct `ProviderTxnId`, which
  also exercises the unique index.
- `Reviews` only for users with a `Lease` for that property, with varied ratings so the average
  `Rating` is not 4.5 everywhere.
- The `Rating`/`ReviewCount` caches on `BoardingHouses` are recomputed at the end of the seeder,
  never assigned arbitrarily.

## 6. Running it

The seeder is `dotnet run --project backend/MotelLease.Api -- seed` — it does not run on app
startup. It is idempotent through fixed `Guid`s, so running it twice does not duplicate data.

Demo accounts (shared password `Demo@1234`, Development environment only):

| Role | Email |
|---|---|
| Admin | `admin@motellease.local` |
| Owner | `owner1@motellease.local` … `owner8@…` |
| Staff | `staff1@motellease.local` … |
| Tenant | `tenant1@motellease.local` … |

The seeder finishes by printing a per-table row count and running the PostGIS query from §3 to
confirm the index is working.
