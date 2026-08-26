# MotelLease — Kế hoạch dữ liệu demo

> Mục tiêu: demo tìm kiếm theo vị trí, luồng cọc → hợp đồng → hóa đơn, và báo cáo doanh
> thu **nhìn ra được là thật**. Dữ liệu random đều sẽ làm phần PostGIS trông vô nghĩa.

## 1. Khối lượng

| Bảng | Số bản ghi | Ghi chú |
|---|---|---|
| Users | 120 | 1 admin, 8 owner, 12 staff, ~99 tenant |
| BoardingHouses | 60 | 35 Hà Nội, 25 TP.HCM; 54 `Published`, 4 `PendingReview`, 2 `Rejected` |
| RoomTypes | 150 | 2–3 loại / nhà trọ |
| Rooms | 600 | 8–15 phòng / nhà trọ |
| Facilities | 20 | wifi, điều hòa, nóng lạnh, gác lửng, chỗ để xe, tự do giờ… |
| Leases | 260 | ~43% phòng đang có người thuê |
| PaymentBills | 1.800 | 6 tháng gần nhất cho các hợp đồng đang hoạt động |
| Reviews | 320 | chỉ từ user có `Lease` (đúng quy tắc đánh giá đã xác minh) |
| Deposits | 90 | rải đủ 8 trạng thái |
| Appointments | 140 | quá khứ + tương lai |

Đủ để bảng có phân trang thật, biểu đồ doanh thu có 6 điểm, và `EXPLAIN` cho thấy index GiST
được dùng thay vì seq scan.

## 2. Toạ độ: rải quanh điểm neo, không random đều

Nhà trọ thật mọc quanh trường đại học và khu công nghiệp. Chọn 7 điểm neo, mỗi neo 6–10
nhà trọ trong bán kính 300m–3km:

| Điểm neo | Lat, Lon (xấp xỉ) | Quận/Huyện | Số nhà trọ |
|---|---|---|---|
| ĐH Bách khoa Hà Nội | 21.0045, 105.8435 | Hai Bà Trưng, HN | 8 |
| ĐH Quốc gia HN (Xuân Thủy) | 21.0378, 105.7825 | Cầu Giấy, HN | 9 |
| ĐH Thương mại / Hồ Tùng Mậu | 21.0410, 105.7690 | Cầu Giấy, HN | 7 |
| KCN Thăng Long | 21.1160, 105.7770 | Đông Anh, HN | 6 |
| ĐH Bách khoa TP.HCM (Q10) | 10.7720, 106.6580 | Quận 10, HCM | 8 |
| ĐH Quốc gia TP.HCM (Linh Trung) | 10.8700, 106.8000 | Thủ Đức, HCM | 9 |
| KCN Tân Bình | 10.8100, 106.6200 | Tân Bình, HCM | 8 |

Toạ độ trên là **xấp xỉ, phải kiểm tra lại trên bản đồ trước khi seed** — sai một chữ số
thập phân là lệch ~11km.

Sinh điểm quanh neo (bán kính `r` mét, hướng ngẫu nhiên):

```csharp
// 1 độ vĩ ≈ 111_320 m; 1 độ kinh ≈ 111_320 * cos(lat) m
var bearing  = rnd.NextDouble() * 2 * Math.PI;
var distance = 300 + rnd.NextDouble() * 2700;          // 300m .. 3km
var dLat = distance * Math.Cos(bearing) / 111_320.0;
var dLon = distance * Math.Sin(bearing) / (111_320.0 * Math.Cos(anchor.Lat * Math.PI / 180));
var lat  = Math.Round(anchor.Lat + dLat, 6);
var lon  = Math.Round(anchor.Lon + dLon, 6);
```

## 3. Seeder chỉ ghi Latitude/Longitude

`BoardingHouses.Location` là computed column STORED (xem `erd.md` §8), nên seeder **không**
được set `Location` — Postgres tự sinh. Điều này cũng có nghĩa: đổi lat/lon là geography tự
cập nhật, không có nguy cơ hai cột lệch nhau.

Nhắc lại cái bẫy: `ST_MakePoint(Longitude, Latitude)` — **kinh độ trước**. Test kiểm chứng:

```sql
-- Bách khoa HN → phải trả về đúng các nhà trọ neo ở Bách khoa, không phải toàn bộ 60 cái
SELECT "Name", ROUND(ST_Distance("Location",
         ST_SetSRID(ST_MakePoint(105.8435, 21.0045), 4326)::geography)) AS m
FROM "BoardingHouses"
WHERE ST_DWithin("Location",
        ST_SetSRID(ST_MakePoint(105.8435, 21.0045), 4326)::geography, 3000)
ORDER BY m;
```

Nếu query này trả về 0 dòng hoặc trả về cả 60 nhà trọ → seed sai thứ tự toạ độ.

## 4. Địa chỉ phải khớp toạ độ

`AddressLine` sinh theo quận của điểm neo, không random toàn quốc — nếu toạ độ ở Cầu Giấy
mà địa chỉ ghi "Quận 1, TP.HCM" thì người xem demo phát hiện ngay.

Mỗi neo giữ sẵn 4–6 tên phố thật của quận đó, ghép với số nhà random:
`"{số} {phố}, {phường}, {quận}, {tỉnh}"`.

## 5. Dữ liệu phải nhất quán nghiệp vụ

Seeder không được tạo dữ liệu vi phạm bất biến ở `domain-rules.md` §9 — nếu vi phạm thì
constraint sẽ chặn và đó là dấu hiệu tốt, nhưng đừng tắt constraint để seed cho xong:

- Phòng có `Lease` `Active` ⇒ `Rooms.Status = Occupied`; có `Deposit` `Accepted`/`Paid`
  chưa lên hợp đồng ⇒ `Reserved`.
- `PaymentBills` của một phòng phải liên tiếp theo tháng, và `ElectricityOld` tháng N =
  `ElectricityNew` tháng N−1. Sinh chỉ số tăng dần 30–120 kWh/tháng, nước 3–15 m³.
- Hóa đơn 6 tháng gần nhất: tháng cũ `Paid`, tháng gần nhất trộn `Issued`/`Overdue`/`Paid`
  để dashboard có cả 3 màu.
- Mỗi hóa đơn `Paid` phải có `PaymentTransaction` `Succeeded` với `ProviderTxnId` khác nhau
  (test luôn được unique index).
- `Reviews` chỉ tạo cho user có `Lease` với nhà trọ đó, rating lệch nhau để `Rating` trung
  bình không phải toàn 4.5.
- `Rating`/`ReviewCount` cache trên `BoardingHouses` tính lại ở cuối seeder, không gán bừa.

## 6. Cách chạy

Seeder là `dotnet run --project backend/MotelLease.Api -- seed` (không tự chạy khi khởi
động app), idempotent theo `Guid` cố định — chạy 2 lần không nhân đôi dữ liệu.

Tài khoản demo (mật khẩu chung `Demo@1234`, chỉ dùng ở môi trường Development):

| Role | Email |
|---|---|
| Admin | `admin@motellease.local` |
| Owner | `owner1@motellease.local` … `owner8@…` |
| Staff | `staff1@motellease.local` … |
| Tenant | `tenant1@motellease.local` … |

Cuối seeder in ra bảng tổng kết số bản ghi từng loại + chạy sẵn query PostGIS ở §3 để
khẳng định index hoạt động.

