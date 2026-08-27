# MotelLease — Đặc tả tính năng

> Stack: ASP.NET Core (.NET 10) + Nuxt 4 (1 app, 4 role) + PostgreSQL 17 + PostGIS.
> Quyết định đã chốt: Cloudinary cho ảnh, i18n vi/en, PostGIS cho tìm kiếm vị trí, không làm mobile.

## 0. Nguyên tắc thiết kế

1. Một khái niệm nghiệp vụ = một entity. Không có 2 bảng cùng nghĩa.
2. Không lưu số liệu tổng hợp được tính lại được — dùng view/query.
3. Trạng thái là enum có tên, không phải string tự do so sánh bằng regex.
4. Endpoint nhóm theo resource, phân quyền bằng policy, không nhân bản theo role.
5. Mọi thay đổi trạng thái tiền tệ phải idempotent và nằm trong transaction.

---

## 1. Phạm vi

Hệ thống web cho 4 role trong một Nuxt app duy nhất (layout + route middleware theo role,
không phải 4 SPA rời). Khoảng 150 endpoint trên 29 bảng.

Nằm ngoài scope phiên bản đầu:

| Không làm | Lý do |
|---|---|
| App mobile | Web responsive đủ cho phạm vi này |
| Chat tenant ↔ owner/staff | Xem mục 3.11 — hạ tầng SignalR đã có nên thêm sau không phải sửa kiến trúc |
| Lưu bộ lọc + thông báo phòng mới khớp (price alert) | Giá trị thấp so với chi phí |
| So sánh 2–3 nhà trọ cạnh nhau | Giá trị thấp so với chi phí |
| Xuất Excel báo cáo | Đã có hóa đơn PDF ở mục 3.4 |

Hai quyết định về mô hình dữ liệu đáng nói rõ, vì chúng định hình phần lớn schema:

- **Loại nhà trọ là C# enum, không phải bảng admin CRUD được.** Quy tắc số người ở rẽ nhánh
  theo giá trị này (mục 1 của `domain-rules.md`), nên thêm/sửa một giá trị lúc runtime là tạo
  ra trạng thái không có quy tắc nào áp dụng được.
- **Admin không tự đăng ký.** Tài khoản admin chỉ tạo bằng seed hoặc do admin khác tạo.

---

## 2. Feature theo role

### Khách thuê (Tenant)
- Đăng ký / đăng nhập (email + Google OAuth), xác thực OTP qua email, quên/đặt lại mật khẩu, đổi mật khẩu, đổi email có OTP xác nhận
- Xem danh sách / chi tiết nhà trọ, loại phòng, tiện ích, ảnh, vị trí trên bản đồ
- Tìm kiếm, lọc (giá, diện tích, số người, tiện ích, loại nhà trọ, khu vực), sắp xếp (mới nhất, đánh giá cao, giá)
- Lưu tin (`SavedListing`)
- Đặt lịch xem phòng (`Appointment`), hủy lịch có lý do
- Đặt cọc phòng online (MoMo / VNPay), xem cọc của tôi
- Yêu cầu hoàn cọc (`RefundRequest`)
- Thanh toán tiền thuê hàng tháng (điện/nước theo chỉ số + phí phát sinh)
- Yêu cầu gia hạn hợp đồng (`ExtensionRequest`)
- Đánh giá + trả lời đánh giá (có ảnh), báo cáo nhà trọ / đánh giá vi phạm
- Quản lý profile, đổi avatar

### Chủ trọ (Owner)
- Quản lý nhà trọ: tạo/sửa/xóa, ảnh (ảnh chính), vị trí, giá điện/nước
- Quản lý loại phòng (giá, diện tích, số người, tiện ích) và phòng (số phòng, chỉ số điện/nước)
- Duyệt / từ chối đặt lịch xem phòng, duyệt cọc, duyệt hoàn cọc, duyệt gia hạn
- Chốt chỉ số điện nước → phát hành hóa đơn tháng, thêm phí phát sinh theo phòng
- Chi phí vận hành nhà trọ (`BoardingHouseExpense`: điện, nước, chi phí khác)
- Báo cáo doanh thu theo tháng/năm/nhà trọ
- Yêu cầu rút tiền (`WithdrawRequest`) kèm thông tin ngân hàng
- Quản lý nhân viên: tạo tài khoản staff, gán nhà trọ, giao việc
- Trả lời đánh giá của khách

### Nhân viên (Staff)
- Chỉ thao tác trên các nhà trọ được owner gán (resource-based authorization)
- Quản lý phòng, chốt chỉ số, phát hành hóa đơn, xử lý cọc/lịch xem phòng
- Nhận và cập nhật công việc được giao (`Task`)

### Admin
- Quản lý tài khoản (khóa/mở, xóa mềm, tạo tài khoản)
- Quản lý toàn bộ nhà trọ, danh mục tiện ích
- Xử lý báo cáo vi phạm (nhà trọ / đánh giá), ẩn/xóa đánh giá
- Duyệt yêu cầu rút tiền của chủ trọ

---

## 3. Feature trọng tâm

### P0 — bắt buộc, thiếu là vỡ nghiệp vụ

**3.1. Hợp đồng thuê (`Lease`) tách khỏi đặt cọc (`Deposit`)**
Phiếu cọc và hợp đồng là hai khái niệm khác nhau và phải là hai bảng: gộp lại thì không có nơi
lưu giá chốt tại thời điểm thuê, danh sách người ở cùng, lịch sử thuê của một phòng, và không có
luồng trả phòng.
- `Deposit`: giữ chỗ, tiền cọc, hạn xác nhận
- `Lease`: hợp đồng (kỳ hạn, giá chốt, tiền cọc đang giữ, trạng thái) → nguồn duy nhất để biết ai đang thuê phòng nào
- `LeaseTenant`: người ở cùng trên một hợp đồng
- Trả phòng: ghi trực tiếp trên `Lease` (`EndedAt`, `EndReason`, chỉ số điện/nước cuối, đối trừ cọc) — một hợp đồng chỉ kết thúc một lần nên không cần bảng riêng

**3.2. Hệ thống thông báo (`Notification`)**
Luồng cọc / duyệt / hóa đơn phải nói cho người dùng biết chuyện gì vừa xảy ra, không để họ tự F5:
- Bảng `Notification` (in-app, có `IsRead`) + SignalR hub để đẩy realtime
- Email cho các mốc quan trọng (cọc được duyệt/từ chối, lịch xem phòng được xác nhận, hóa đơn mới, sắp đến hạn thanh toán, gia hạn được trả lời, hoàn cọc đã xử lý, yêu cầu rút tiền được duyệt)
- Nội dung template theo `vi`/`en` theo ngôn ngữ của người nhận

**3.3. Giao dịch thanh toán idempotent + IPN**
- `PaymentTransaction` với `ProviderTxnId` UNIQUE → chống replay/ghi trùng
- Endpoint IPN (server-to-server) là **nơi duy nhất** được đổi trạng thái tiền; return URL của
  browser chỉ để hiển thị. Người dùng điều khiển được URL họ được redirect tới, nên không thể
  dùng nó làm bằng chứng đã trả tiền.
- Bắt buộc verify HMAC cả MoMo và VNPay, và chỉ chấp nhận đúng mã kết quả thành công của từng cổng
- Lịch sử giao dịch cho tenant và owner

**3.4. Vòng đời hóa đơn có hạn thanh toán**
`IssuedAt`, `DueDate`, job nhắc hạn trước N ngày, đánh dấu quá hạn, và xuất hóa đơn PDF.

**3.5. Đánh giá đã xác minh**
Chỉ cho review khi có `Lease` (hoặc `Deposit` đã hoàn tất) với nhà trọ đó, gắn nhãn "Đã từng thuê".
Nếu ai cũng review được nhà trọ bất kỳ thì điểm đánh giá không mang thông tin gì.

**3.6. Refresh token + quản lý phiên**
Access token ngắn hạn + refresh token rotation lưu DB (chỉ lưu hash). Thu hồi được từng phiên,
thu hồi toàn bộ khi admin khóa tài khoản, và có trang "thiết bị đang đăng nhập" — mỗi refresh
token còn hiệu lực là một thiết bị.

**3.7. Rate limiting + chống lạm dụng**
Rate limiter của ASP.NET Core theo IP cho `/login`, `/register`, `/password/forgot`, cộng với
giới hạn riêng theo địa chỉ email cho các endpoint gửi OTP (cooldown giữa 2 lần gửi + số lần
nhập sai tối đa), để hộp thư của người khác không bị dùng làm mục tiêu.

### P1

**3.8. Tìm kiếm theo vị trí bằng PostGIS**
- Cột `geography(Point, 4326)` + index GiST
- Tìm trong bán kính (`ST_DWithin`), sắp xếp theo khoảng cách (`ST_Distance`)
- Bản đồ kèm bounding box khi user pan/zoom, cluster marker
- "Gần tôi", "gần trường/công ty" (nhập địa chỉ → geocode)

Lọc lat/lon bằng tay trong tầng ứng dụng không dùng được index và sai ở khoảng cách lớn.

**3.9. Báo sự cố / yêu cầu sửa chữa (`MaintenanceRequest`)**
Tenant báo hỏng điện, nước, khóa cửa (kèm ảnh) → tự động sinh `Task` cho staff phụ trách nhà trọ
đó. `Task` mang `BoardingHouseId` để owner xem được công việc theo từng nhà trọ.

**3.10. Nhật ký hành động (`AuditLog`)**
Ghi lại hành động của admin/owner có ảnh hưởng tới người khác: khóa tài khoản, xóa đánh giá,
từ chối hoàn cọc, duyệt rút tiền.

**3.11. (HOÃN) Chat tenant ↔ owner/staff**
Vì `Notification` đã dựng SignalR hub, chat thêm sau không phải sửa kiến trúc. Không nằm trong
scope phiên bản đầu.

**3.12. i18n phía server**
`@nuxtjs/i18n` cho frontend. Phía server: message validation, nội dung email và thông báo cũng
đa ngữ, chọn theo `Accept-Language` và `User.PreferredLanguage`.

**3.13. Dashboard thống kê cho owner**
Tỉ lệ lấp phòng (occupancy rate), doanh thu vs chi phí, số phòng trống theo thời gian, tỉ lệ hủy
cọc — tất cả tính từ `Lease` + `PaymentBill` + `BoardingHouseExpense`.

### P2 — đã chốt lấy

- OpenAPI → sinh client TypeScript cho Nuxt (`openapi-typescript`), giữ frontend luôn khớp backend
- Admin duyệt nhà trọ trước khi hiển thị công khai (`ListingStatus`: Draft → PendingReview → Published / Rejected)
- Xem lại và phục hồi bản ghi đã xóa mềm (account, review)

---

## 4. Danh sách entity (29 bảng)

**Nghiệp vụ chính (18):**
`User` · `BoardingHouse` · `Room` · `RoomType` · `Facility` · `SavedListing` · `Appointment` ·
`Deposit` · `Lease` · `RefundRequest` · `ExtensionRequest` · `PaymentBill` ·
`PaymentTransaction` · `RoomAdditionalFee` · `BoardingHouseExpense` · `WithdrawRequest` ·
`Review` · `Report`

**Quan hệ và phân rã (6):**
`OwnerProfile`, `StaffProfile` (trường riêng theo role, để hàng `User` không mang cột không dùng) ·
`Image` (`PublicId` + `Url` + `IsPrimary` + `OwnerType`/`OwnerId`) · `RoomTypeFacility` ·
`LeaseTenant` · `StaffAssignment` (nhiều staff trên một nhà trọ, có thời hạn hiệu lực)

**Vận hành (5):**
`Notification` · `MaintenanceRequest` · `AuditLog` · `RefreshToken` · `Task` (có `BoardingHouseId`)

**Không phải bảng**: `vw_monthly_revenue`, `vw_room_occupancy` (materialized view)

## 5. State machine

```
RoomStatus:     Available → Reserved → Occupied → Maintenance → Available
DepositStatus:  Pending → Accepted → Paid → Completed
                       ↘ Rejected   ↘ Expired  ↘ Refunding → Refunded
LeaseStatus:    Active → Expiring → Ended
                      ↘ Terminated
BillStatus:     Draft → Issued → Overdue → Paid → Cancelled
PaymentStatus:  Initiated → Pending → Succeeded / Failed / Refunded
RequestStatus:  Pending → Approved / Rejected / Cancelled
                (dùng chung cho Appointment, Extension, Refund, Withdraw)
```

`Reserved` là lý do `RoomStatus` phải là enum: phòng đã có người cọc nhưng chưa nhận phòng thì
không trống mà cũng chưa có ai ở. Một cờ boolean không biểu diễn được trạng thái này, và hệ quả
là phải có job định kỳ đi vá lại cờ đó.

## 6. Ghi chú kỹ thuật đã chốt

- **Cloudinary**: `CloudinaryDotNet`. Bảng `Image` lưu `PublicId` + `Url` + `IsPrimary` +
  `OwnerType`/`OwnerId`. Xóa ảnh trên Cloudinary nằm trong cùng luồng xóa bản ghi, để không để
  lại file rác không ai tham chiếu.
- **i18n**: `@nuxtjs/i18n`, `vi` mặc định, `en` phụ, prefix strategy `prefix_except_default`.
  Server trả message theo `Accept-Language`.
- **PostGIS**: `NetTopologySuite` + `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`,
  image `postgis/postgis:17-3.5` trong `docker-compose.yml`.
- **Tiền tệ**: `decimal(18,2)`, không dùng `double`.
