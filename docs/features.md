# MotelLease — Đặc tả tính năng (bản tối ưu)

> Nguồn: audit repo cũ `Boarding-house-booking` (MERN, 21 model, 221 route, 24 controller).
> Stack mới: ASP.NET Core (.NET 10) + Nuxt 4 (1 app, 4 role) + PostgreSQL 17 + PostGIS.
> Quyết định đã chốt: giữ Cloudinary, i18n vi/en, dùng PostGIS, bỏ mobile.

## 0. Nguyên tắc tối ưu

1. Một khái niệm nghiệp vụ = một entity. Không có 2 bảng cùng nghĩa.
2. Không lưu số liệu tổng hợp được tính lại được — dùng view/query.
3. Trạng thái là enum có tên, không phải string tự do so sánh bằng regex.
4. Endpoint nhóm theo resource, phân quyền bằng policy, không nhân bản theo role.
5. Mọi thay đổi trạng thái tiền tệ phải idempotent và nằm trong transaction.

---

## 1. Feature loại bỏ

| Feature cũ | Bằng chứng | Xử lý |
|---|---|---|
| `WatchLater` | `models/watchLater.js` và `models/favoriteBH.js` có schema **giống hệt nhau** (`accountId` + `boardingHouseId`), 2 controller, 6 endpoint, 2 trang, component `WatchLaterCard` riêng | Gộp thành 1 feature `SavedListing` |
| Bảng `Revenue` | `models/revenue.js` lưu `totalRevenue`, `transactionCount`, `transactions[]` — toàn bộ tính lại được từ `PaymentBill` | Materialized view `vw_monthly_revenue`, refresh bằng background job |
| `/reviews` + `/reviews/filter`, `/account` + `/account/filter`, `/facilities` + `/facilities/filter`, `/reports` + `/reports/filter` | 4 cặp endpoint trùng chức năng trong `adminRouter.js` | 1 endpoint list + query param (`?status=&q=&page=`) |
| 40 controller method mount ở 2 router | `ownerRouter` và `staffRouter` chia sẻ 40/175 method (revenue, tasks, review reply…) | 1 endpoint + resource-based authorization |
| `POST /login` khai báo 2 lần | `routes/commonRouter.js` | Bỏ 1 |
| Admin CRUD `BoardingHouseType` | `codeName` bị hardcode trong logic (`nha_tro_truyen_thong`, `mini_house`, `nha_tro_kien_truc_xa` ở `models/room.js`) — admin sửa/xóa là vỡ nghiệp vụ | Chuyển thành C# enum, bỏ trang `BoardingHouseTypeManagement` |
| Admin tự đăng ký | `dashboard` có `/register`, `/register-with-google` | Admin chỉ được seed hoặc do admin khác tạo |
| `backend/src/validators/` | 11 file của project shopping cart khác (`cartItemValidator`, `productValidator`, `orderValidator`…) | Không port |
| `boolean isAvailable` + cron sync 2:00 AM | `models/room.js` — boolean không biểu diễn được "đã cọc, chờ nhận phòng" | Thay bằng enum `RoomStatus` (mục 5) |
| Toàn bộ `mobile/` | Đã thống nhất bỏ | Không port |

Kết quả: 21 model → 29 bảng chuẩn hóa (bỏ 3, thêm 11 — xem mục 4), và 221 route → ~150 endpoint mà không mất tính năng nào.

---

## 2. Feature giữ nguyên (đã có, port sang stack mới)

### Khách thuê (Tenant)
- Đăng ký / đăng nhập (email + Google OAuth), xác thực OTP qua email, quên/đặt lại mật khẩu, đổi mật khẩu, đổi email có OTP xác nhận
- Xem danh sách / chi tiết nhà trọ, loại phòng, tiện ích, ảnh, vị trí trên bản đồ
- Tìm kiếm, lọc (giá, diện tích, số người, tiện ích, loại nhà trọ, khu vực), sắp xếp (mới nhất, đánh giá cao, giá)
- Lưu tin (`SavedListing` — gộp từ Favorite + WatchLater)
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
- Báo cáo doanh thu theo tháng/năm/nhà trọ (từ view thay vì bảng `Revenue`)
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

## 3. Feature thêm mới

### P0 — bắt buộc, hệ thống hiện tại thiếu và gây lỗi nghiệp vụ

**3.1. Hợp đồng thuê (`Lease`) tách khỏi đặt cọc (`Deposit`)**
Repo cũ dùng `depositRoom` làm cả phiếu cọc lẫn hợp đồng (`rentalTime`, `startDate`, `endDate` nằm trong bảng cọc), nên không có nơi lưu: giá chốt tại thời điểm thuê, danh sách người ở cùng, lịch sử thuê của một phòng, và không có luồng trả phòng. Tách:
- `Deposit`: giữ chỗ, tiền cọc, hạn xác nhận
- `Lease`: hợp đồng (kỳ hạn, giá chốt, tiền cọc đang giữ, trạng thái) → nguồn duy nhất để biết ai đang thuê phòng nào
- `LeaseTenant`: người ở cùng trên một hợp đồng (thay cho array `room.rentBy`)
- Trả phòng: ghi trực tiếp trên `Lease` (`EndedAt`, `EndReason`, chỉ số điện/nước cuối, đối trừ cọc) — một hợp đồng chỉ kết thúc một lần nên không cần bảng riêng

**3.2. Hệ thống thông báo (`Notification`)**
Backend cũ **không có gì** về thông báo — không model, không email transaction ngoài OTP. Toàn bộ luồng cọc / duyệt / hóa đơn đều im lặng, khách phải tự F5. Thêm:
- Bảng `Notification` (in-app, có `IsRead`) + SignalR hub để đẩy realtime
- Email cho các mốc quan trọng (cọc được duyệt/từ chối, lịch xem phòng được xác nhận, hóa đơn mới, sắp đến hạn thanh toán, gia hạn được trả lời, hoàn cọc đã xử lý, yêu cầu rút tiền được duyệt)
- Nội dung template theo `vi`/`en` theo ngôn ngữ của người nhận

**3.3. Giao dịch thanh toán idempotent + IPN**
Thay cho việc xác nhận tiền ngay trong return URL (`depositController.momoReturn` không verify signature, coi `resultCode=7002` là thành công, và replay được):
- `PaymentTransaction` với `ProviderTxnId` UNIQUE → chống replay/ghi trùng
- Endpoint IPN riêng là nơi duy nhất được đổi trạng thái tiền; return URL chỉ để hiển thị
- Bắt buộc verify HMAC cả MoMo và VNPay
- Lịch sử giao dịch cho tenant và owner (hiện chưa có trang nào xem được)

**3.4. Vòng đời hóa đơn có hạn thanh toán**
`paymentBill` hiện chỉ có `status`, không có `dueDate`. Thêm `IssuedAt`, `DueDate`, job nhắc hạn trước N ngày, đánh dấu quá hạn, và xuất hóa đơn PDF.

**3.5. Đánh giá đã xác minh**
Hiện bất kỳ account nào cũng review được nhà trọ bất kỳ → dễ thành rác/seeding. Chỉ cho review khi có `Lease` (hoặc `Deposit` đã hoàn tất) với nhà trọ đó, gắn nhãn "Đã từng thuê".

**3.6. Refresh token + quản lý phiên**
`utils/functions.js` chỉ có 1 JWT, không revoke được. Thêm refresh token rotation lưu DB, thu hồi khi admin khóa tài khoản, trang "thiết bị đang đăng nhập".

**3.7. Rate limiting + chống lạm dụng**
Chưa có gì trên `/login`, `/send-otp-register`, `/forgot-password`. Thêm rate limiter của ASP.NET Core + giới hạn số lần gửi OTP.

### P1 — đã chốt LẤY (trừ 3.11 chat, xem P2)

**3.8. Tìm kiếm theo vị trí bằng PostGIS**
Repo cũ lưu `lat`/`lon` rời và lọc bằng tay trong JS. Với PostGIS:
- Cột `geography(Point, 4326)` + index GiST
- Tìm trong bán kính (`ST_DWithin`), sắp xếp theo khoảng cách (`ST_Distance`)
- Bản đồ kèm bounding box khi user pan/zoom, cluster marker
- "Gần tôi", "gần trường/công ty" (nhập địa chỉ → geocode)

**3.9. Báo sự cố / yêu cầu sửa chữa (`MaintenanceRequest`)**
Tenant báo hỏng điện, nước, khóa cửa (kèm ảnh) → tự động sinh `Task` cho staff phụ trách nhà trọ đó. Đây cũng là thứ làm module `Task` có ý nghĩa: hiện `models/task.js` chỉ có `createdBy`/`responsibleBy`, **không có `boardingHouseId`**, nên owner không thể xem công việc theo nhà trọ.

**3.10. Nhật ký hành động (`AuditLog`)**
Ghi lại hành động của admin/owner có ảnh hưởng tới người khác: khóa tài khoản, xóa đánh giá, từ chối hoàn cọc, duyệt rút tiền. Hiện hoàn toàn không có, không truy được ai làm gì.

**3.11. (HOÃN — xem P2) Chat tenant ↔ owner/staff**
Vì `Notification` đã dựng SignalR hub, chat thêm sau không phải sửa kiến trúc. Không nằm trong scope phiên bản đầu.

**3.12. i18n phía server**
25 namespace `vi`/`en` trong `website/src/locales/` port sang `@nuxtjs/i18n` được gần như nguyên vẹn. Bổ sung phần repo cũ chưa làm: message validation, nội dung email và thông báo cũng phải đa ngữ (`Accept-Language` + `User.PreferredLanguage`).

**3.13. Dashboard thống kê cho owner**
Hiện chỉ có báo cáo doanh thu. Thêm: tỉ lệ lấp phòng (occupancy rate), doanh thu vs chi phí, số phòng trống theo thời gian, tỉ lệ hủy cọc — tất cả tính từ `Lease` + `PaymentBill` + `BoardingHouseExpense`.

### P2 — quyết định từng món

Đã chốt LẤY (chi phí gần bằng 0, giá trị cao):
- OpenAPI → sinh client TypeScript cho Nuxt (`openapi-typescript`) — giữ frontend luôn khớp backend
- Admin duyệt nhà trọ trước khi hiển thị công khai (`ListingStatus`: Draft → PendingReview → Published / Rejected)
- Xem lại và phục hồi bản ghi đã xóa mềm (account, review)

Đã chốt HOÃN (không nằm trong scope phiên bản đầu):
- Chat tenant ↔ owner/staff (3.11)
- Lưu bộ lọc tìm kiếm + thông báo khi có phòng mới khớp (price alert)
- So sánh 2–3 nhà trọ cạnh nhau
- Xuất Excel báo cáo (đã có hóa đơn PDF ở 3.4)

---

## 4. Danh sách entity (29 bảng)

**Giữ từ 21 model cũ, bỏ 3 → còn 18** (tên mới nếu có):
`User` (từ `account`) · `BoardingHouse` · `Room` · `RoomType` · `Facility` · `SavedListing` (từ `favoriteBH`, gộp `watchLater`) · `Appointment` · `Deposit` (từ `depositRoom`) · `RefundRequest` · `ExtensionRequest` · `PaymentBill` · `PaymentTransaction` (từ `userPayment`, thêm `ProviderTxnId` unique) · `RoomAdditionalFee` · `BoardingHouseExpense` · `WithdrawRequest` · `Review` · `Report` · `Task` (thêm `BoardingHouseId`)

**Bỏ 3**: `watchLater` (trùng `favoriteBH`) · `revenue` (thành materialized view) · `boardingHouseType` (thành C# enum)

**Thêm do chuẩn hóa quan hệ (6)**:
`OwnerProfile`, `StaffProfile` (thay Mongo discriminator) · `Image` (thay ảnh embedded rải rác) · `RoomTypeFacility` (thay array `facilities`) · `LeaseTenant` (thay array `room.rentBy`) · `StaffAssignment` (thay `boardingHouse.staffId` — hiện là 1 ObjectId đơn, mỗi nhà trọ chỉ gán được đúng 1 staff)

**Thêm do feature mới (5)**:
`Lease` · `Notification` · `MaintenanceRequest` · `AuditLog` · `RefreshToken`

**Không phải bảng**: `vw_monthly_revenue`, `vw_room_occupancy` (materialized view)


## 5. State machine (thay cho boolean và string tự do)

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

`Reserved` là trạng thái repo cũ không biểu diễn được: phòng đã có người cọc nhưng chưa nhận phòng. Đây là nguyên nhân phải có cron 2:00 AM sync lại `isAvailable`.

## 6. Ghi chú kỹ thuật đã chốt

- **Cloudinary**: giữ, dùng `CloudinaryDotNet`. Bảng `Image` lưu `PublicId` + `Url` + `IsPrimary` + `OwnerType`/`OwnerId`. Xóa ảnh trên Cloudinary phải nằm trong cùng luồng xóa bản ghi (repo cũ có chỗ xóa DB mà để rác trên Cloudinary).
- **i18n**: `@nuxtjs/i18n`, `vi` là mặc định, `en` phụ, prefix strategy `prefix_except_default`. Server trả message theo `Accept-Language`.
- **PostGIS**: `NetTopologySuite` + `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`, image `postgis/postgis:17-3.5` trong `docker-compose.yml`.
- **Tiền tệ**: `decimal(18,2)`, không dùng `double`.




