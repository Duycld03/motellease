\set ON_ERROR_STOP off
\echo '=== 1) does gen_random_uuid() need pgcrypto? ==='
SELECT gen_random_uuid() AS uuid_without_pgcrypto;

\echo '=== 2) CREATE EXTENSION postgis ==='
CREATE EXTENSION IF NOT EXISTS postgis;
SELECT PostGIS_Lib_Version() AS postgis_version;

\echo '=== 3) STORED geography computed column from Latitude/Longitude ==='
CREATE TABLE "BoardingHouses" (
  "Id"        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  "Name"      text NOT NULL,
  "Latitude"  decimal(9,6) NOT NULL,
  "Longitude" decimal(9,6) NOT NULL,
  "Location"  geography(Point,4326)
              GENERATED ALWAYS AS
              (ST_SetSRID(ST_MakePoint("Longitude"::double precision,
                                       "Latitude"::double precision), 4326)::geography)
              STORED,
  "IsDeleted" boolean NOT NULL DEFAULT false
);

\echo '=== 4) GiST index on Location ==='
CREATE INDEX "IX_BoardingHouses_Location" ON "BoardingHouses" USING GIST ("Location");

\echo '=== 5) seed: 5000 points around 2 anchors (HUST Hanoi, HCMUT) ==='
INSERT INTO "BoardingHouses" ("Name", "Latitude", "Longitude")
SELECT
  'BH-' || g,
  ROUND((anchor_lat + (dist * cos(bearing)) / 111320.0)::numeric, 6),
  ROUND((anchor_lon + (dist * sin(bearing)) / (111320.0 * cos(radians(anchor_lat))))::numeric, 6)
FROM (
  SELECT g,
         CASE WHEN g % 2 = 0 THEN 21.0045 ELSE 10.7720 END AS anchor_lat,
         CASE WHEN g % 2 = 0 THEN 105.8435 ELSE 106.6580 END AS anchor_lon,
         random() * 2 * pi() AS bearing,
         300 + random() * 2700 AS dist
  FROM generate_series(1, 5000) g
) s;
ANALYZE "BoardingHouses";

\echo '=== 6) is Location generated automatically? (never inserted) ==='
SELECT "Name", "Latitude", "Longitude", ST_AsText("Location") AS wkt
FROM "BoardingHouses" ORDER BY "Name" LIMIT 2;

\echo '=== 7) ST_DWithin 3km around HUST: is the GiST index used? ==='
EXPLAIN (ANALYZE, BUFFERS, COSTS OFF)
SELECT "Id" FROM "BoardingHouses"
WHERE ST_DWithin("Location",
      ST_SetSRID(ST_MakePoint(105.8435, 21.0045), 4326)::geography, 3000);

\echo '=== 8) hit count + min/max distance (must be <= 3000m) ==='
SELECT COUNT(*) AS hits,
       ROUND(MIN(ST_Distance("Location", ST_SetSRID(ST_MakePoint(105.8435,21.0045),4326)::geography))) AS min_m,
       ROUND(MAX(ST_Distance("Location", ST_SetSRID(ST_MakePoint(105.8435,21.0045),4326)::geography))) AS max_m
FROM "BoardingHouses"
WHERE ST_DWithin("Location", ST_SetSRID(ST_MakePoint(105.8435,21.0045),4326)::geography, 3000);

\echo '=== 9) UPDATE lat/lon -> does Location follow? ==='
UPDATE "BoardingHouses" SET "Latitude" = 21.0045, "Longitude" = 105.8435
WHERE "Name" = 'BH-2';
SELECT "Name", ST_AsText("Location") AS wkt FROM "BoardingHouses" WHERE "Name" = 'BH-2';

\echo '=== 10) writing straight to the computed column -> must fail ==='
UPDATE "BoardingHouses" SET "Location" = ST_SetSRID(ST_MakePoint(0,0),4326)::geography
WHERE "Name" = 'BH-2';

\echo '=== 11) partial unique index on a soft-deletable table ==='
CREATE TABLE "Rooms" (
  "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  "BoardingHouseId" uuid NOT NULL,
  "RoomNumber" text NOT NULL,
  "Status" text NOT NULL,
  "IsDeleted" boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX "UX_Rooms_BH_Number"
  ON "Rooms" ("BoardingHouseId", "RoomNumber") WHERE "IsDeleted" = false;

INSERT INTO "Rooms" ("BoardingHouseId","RoomNumber","Status")
VALUES ('11111111-1111-1111-1111-111111111111','101','Available');
\echo '--- duplicate room number while the original is live -> must fail ---'
INSERT INTO "Rooms" ("BoardingHouseId","RoomNumber","Status")
VALUES ('11111111-1111-1111-1111-111111111111','101','Available');
\echo '--- soft-delete, then reuse the same room number -> must succeed ---'
UPDATE "Rooms" SET "IsDeleted" = true WHERE "RoomNumber" = '101';
INSERT INTO "Rooms" ("BoardingHouseId","RoomNumber","Status")
VALUES ('11111111-1111-1111-1111-111111111111','101','Available');
SELECT COUNT(*) AS rooms_101 FROM "Rooms" WHERE "RoomNumber" = '101';

\echo '=== 12) partial unique: one Active Lease per room ==='
CREATE TABLE "Leases" (
  "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  "RoomId" uuid NOT NULL,
  "Status" text NOT NULL
);
CREATE UNIQUE INDEX "UX_Leases_ActivePerRoom"
  ON "Leases" ("RoomId") WHERE "Status" = 'Active';
INSERT INTO "Leases" ("RoomId","Status") VALUES ('22222222-2222-2222-2222-222222222222','Active');
INSERT INTO "Leases" ("RoomId","Status") VALUES ('22222222-2222-2222-2222-222222222222','Ended');
\echo '--- a second Active Lease on the same room -> must fail ---'
INSERT INTO "Leases" ("RoomId","Status") VALUES ('22222222-2222-2222-2222-222222222222','Active');

\echo '=== 13) materialized view + REFRESH CONCURRENTLY needs a unique index ==='
CREATE MATERIALIZED VIEW vw_test AS
SELECT "BoardingHouseId", COUNT(*) AS total FROM "Rooms" GROUP BY "BoardingHouseId";
CREATE UNIQUE INDEX ON vw_test ("BoardingHouseId");
REFRESH MATERIALIZED VIEW CONCURRENTLY vw_test;
SELECT * FROM vw_test;
