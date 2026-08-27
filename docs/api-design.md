# MotelLease — Thiết kế API

> Thiết kế theo **resource**: một endpoint cho một tài nguyên, phân quyền bằng policy thay vì
> nhân bản đường dẫn theo role.

## Quy ước

- Base: `/api/v1`. OpenAPI ở `/swagger`, sinh client TS cho Nuxt từ đây.
- Auth: `Authorization: Bearer <access token>`, refresh qua `POST /auth/refresh`.
- Phân trang: `?page=1&pageSize=20` → `{ items, page, pageSize, total, totalPages }`.
- Lọc/sắp xếp bằng query param trên chính endpoint list, **không** có endpoint `/filter` riêng.
- Lỗi: RFC 7807 `application/problem+json`, message theo `Accept-Language` (`vi`/`en`).
- Cột `role` dưới đây: `–` công khai · `T` tenant · `S` staff · `O` owner · `A` admin.
  `S` luôn kèm điều kiện `StaffAssignment` đang hoạt động với nhà trọ liên quan.

## Auth & tài khoản (16)

| Method | Path | Role |
|---|---|---|
| POST | `/auth/register` | – |
| POST | `/auth/register/send-otp` | – |
| POST | `/auth/register/verify-otp` | – |
| POST | `/auth/login` | – |
| POST | `/auth/login/google` | – |
| POST | `/auth/refresh` | – |
| POST | `/auth/logout` | T S O A |
| POST | `/auth/password/forgot` | – |
| POST | `/auth/password/reset` | – |
| PUT | `/auth/password` | T S O A |
| GET | `/me` | T S O A |
| PUT | `/me` | T S O A |
| PUT | `/me/avatar` | T S O A |
| PUT | `/me/language` | T S O A |
| POST | `/me/email/send-otp` | T S O A |
| POST | `/me/email/verify-otp` | T S O A |
| GET | `/me/sessions` · DELETE `/me/sessions/{id}` | T S O A |

## Tìm kiếm & danh mục công khai (9)

| Method | Path | Ghi chú |
|---|---|---|
| GET | `/boarding-houses` | lọc: `q, province, district, minPrice, maxPrice, facilities[], type, minRating, sort` |
| GET | `/boarding-houses/nearby` | **PostGIS**: `lat, lon, radiusKm, sort=distance` |
| GET | `/boarding-houses/map` | bounding box: `swLat, swLon, neLat, neLon` → marker rút gọn |
| GET | `/boarding-houses/{id}` | chi tiết + loại phòng + tiện ích + ảnh + rating |
| GET | `/boarding-houses/{id}/rooms` | phòng còn trống |
| GET | `/boarding-houses/{id}/reviews` | phân trang, có nhãn đã xác minh |
| GET | `/facilities` | danh mục tiện ích |
| GET | `/provinces` · `/provinces/{code}/districts` | dữ liệu địa giới cho bộ lọc |

## Tin đã lưu (3)

| Method | Path | Role |
|---|---|---|
| GET | `/me/saved-listings` | T |
| POST | `/me/saved-listings` | T |
| DELETE | `/me/saved-listings/{boardingHouseId}` | T |

## Lịch xem phòng (6)

| Method | Path | Role |
|---|---|---|
| GET | `/appointments` | T S O — tenant thấy của mình, S/O thấy theo nhà trọ |
| POST | `/appointments` | T |
| GET | `/appointments/{id}` | T S O |
| PUT | `/appointments/{id}/approve` | S O |
| PUT | `/appointments/{id}/reject` | S O |
| PUT | `/appointments/{id}/cancel` | T |

## Đặt cọc (9)

| Method | Path | Role |
|---|---|---|
| GET | `/deposits` | T S O |
| POST | `/deposits` | T |
| GET | `/deposits/{id}` | T S O |
| PUT | `/deposits/{id}/approve` | S O — set `ExpiresAt` |
| PUT | `/deposits/{id}/reject` | S O |
| PUT | `/deposits/{id}/cancel` | T |
| POST | `/deposits/{id}/checkout` | T — tạo `PaymentTransaction`, trả URL cổng |
| GET | `/deposits/{id}/contract-preview` | T |
| POST | `/deposits/{id}/confirm-lease` | S O — cọc `Paid` → tạo `Lease` |

## Hợp đồng thuê (9) — mới

| Method | Path | Role |
|---|---|---|
| GET | `/leases` | T S O |
| GET | `/leases/{id}` | T S O |
| GET | `/leases/{id}/bills` | T S O |
| POST | `/leases/{id}/tenants` | S O — thêm người ở cùng |
| DELETE | `/leases/{id}/tenants/{tenantId}` | S O |
| POST | `/leases/{id}/terminate` | S O — chốt chỉ số cuối, đối trừ cọc |
| GET | `/leases/{id}/termination-preview` | T S O — xem trước số tiền đối trừ |
| GET | `/rooms/{roomId}/lease-history` | S O |
| GET | `/me/current-lease` | T |

## Gia hạn hợp đồng (5)

| Method | Path | Role |
|---|---|---|
| GET | `/extension-requests` | T S O |
| POST | `/extension-requests` | T |
| GET | `/extension-requests/{id}` | T S O |
| PUT | `/extension-requests/{id}/approve` | S O |
| PUT | `/extension-requests/{id}/reject` | S O |

## Hóa đơn (10)

| Method | Path | Role |
|---|---|---|
| GET | `/bills` | T S O — lọc `status, month, year, boardingHouseId, roomId` |
| GET | `/bills/{id}` | T S O |
| GET | `/bills/{id}/pdf` | T S O |
| POST | `/bills/preview` | S O — nhập chỉ số, xem trước tiền trước khi phát hành |
| POST | `/bills` | S O — phát hành (1 hóa đơn / phòng / tháng) |
| PUT | `/bills/{id}` | S O — chỉ khi `Draft` |
| PUT | `/bills/{id}/issue` | S O — `Draft` → `Issued`, đặt `DueDate`, bắn thông báo |
| PUT | `/bills/{id}/cancel` | S O |
| GET | `/rooms/{roomId}/additional-fees` | S O |
| POST/PUT/DELETE | `/rooms/{roomId}/additional-fees[/{id}]` | S O — lọc theo `month`,`year` |

## Thanh toán (8)

| Method | Path | Role |
|---|---|---|
| POST | `/payments/bills/{billId}/checkout` | T — chọn `provider`, trả URL cổng |
| GET | `/payments/vnpay/ipn` | – **nơi duy nhất** xác nhận tiền, verify HMAC |
| POST | `/payments/momo/ipn` | – idem, verify HMAC |
| GET | `/payments/vnpay/return` | – chỉ redirect về UI, không ghi DB |
| GET | `/payments/momo/return` | – idem |
| GET | `/payments` | T S O A — lịch sử giao dịch |
| GET | `/payments/{id}` | T S O A |
| GET | `/me/payments` | T |

## Hoàn cọc & rút tiền (10)

| Method | Path | Role |
|---|---|---|
| GET | `/refund-requests` | T S O A |
| POST | `/refund-requests` | T |
| GET | `/refund-requests/{id}` | T S O A |
| PUT | `/refund-requests/{id}/approve` | O A |
| PUT | `/refund-requests/{id}/reject` | O A |
| GET | `/withdraw-requests` | O A |
| POST | `/withdraw-requests` | O |
| GET | `/withdraw-requests/{id}` | O A |
| PUT | `/withdraw-requests/{id}/approve` | A |
| PUT | `/withdraw-requests/{id}/reject` | A |

## Nhà trọ / loại phòng / phòng (O, S) (19)

| Method | Path | Role |
|---|---|---|
| GET | `/my/boarding-houses` | O S — O thấy của mình, S thấy nhà được gán |
| POST · GET · PUT · DELETE | `/my/boarding-houses[/{id}]` | O (S chỉ `GET`/`PUT`) |
| PUT | `/my/boarding-houses/{id}/submit-review` | O — `Draft` → `PendingReview` |
| POST · DELETE | `/my/boarding-houses/{id}/images[/{imageId}]` | O S |
| PUT | `/my/boarding-houses/{id}/images/{imageId}/primary` | O S |
| PUT | `/my/boarding-houses/{id}/utility-prices` | O |
| GET · POST · PUT · DELETE | `/my/boarding-houses/{id}/room-types[/{typeId}]` | O S |
| GET · POST · PUT · DELETE | `/my/boarding-houses/{id}/rooms[/{roomId}]` | O S |
| PUT | `/my/rooms/{roomId}/status` | O S — chuyển `Maintenance` ⇄ `Available` |
| PUT | `/my/rooms/{roomId}/meter-readings` | O S — chốt chỉ số |

## Nhân viên & công việc (11)

| Method | Path | Role |
|---|---|---|
| GET · POST · PUT · DELETE | `/my/staff[/{id}]` | O — tạo/sửa/khóa tài khoản staff |
| GET | `/my/boarding-houses/{id}/staff` | O |
| POST | `/my/boarding-houses/{id}/staff` | O — gán staff (`StaffAssignment`) |
| DELETE | `/my/boarding-houses/{id}/staff/{staffId}` | O — bỏ gán |
| GET | `/tasks` | O S — lọc `boardingHouseId, assignedTo, status, priority` |
| POST · GET · PUT | `/tasks[/{id}]` | O (S được `PUT` trạng thái việc của mình) |
| PUT | `/tasks/{id}/status` | O S |

## Báo sự cố (6) — mới

| Method | Path | Role |
|---|---|---|
| GET | `/maintenance-requests` | T S O |
| POST | `/maintenance-requests` | T — kèm ảnh |
| GET | `/maintenance-requests/{id}` | T S O |
| PUT | `/maintenance-requests/{id}/accept` | S O — sinh `Task` cho staff phụ trách |
| PUT | `/maintenance-requests/{id}/resolve` | S O |
| PUT | `/maintenance-requests/{id}/reject` | S O |

## Đánh giá & báo cáo vi phạm (12)

| Method | Path | Role |
|---|---|---|
| POST | `/reviews` | T — chỉ khi có `Lease` với nhà trọ đó |
| PUT · DELETE | `/reviews/{id}` | T (chủ sở hữu) |
| POST | `/reviews/{id}/reply` | O S |
| PUT · DELETE | `/reviews/{id}/reply/{replyId}` | O S |
| GET | `/me/reviews` | T |
| GET | `/my/reviews` | O S — đánh giá về nhà trọ của mình |
| POST | `/reports` | T — báo cáo nhà trọ hoặc đánh giá |
| GET | `/me/reports` | T |
| GET | `/reports` | A — lọc `targetType, status` |
| GET | `/reports/{id}` | A |
| PUT | `/reports/{id}/resolve` · `/reports/{id}/dismiss` | A |

## Chi phí & thống kê chủ trọ (9)

| Method | Path | Role |
|---|---|---|
| GET · POST · PUT · DELETE | `/my/boarding-houses/{id}/expenses[/{expenseId}]` | O |
| GET | `/my/stats/revenue` | O — từ `vw_monthly_revenue`, lọc `year, boardingHouseId` |
| GET | `/my/stats/revenue/years` | O |
| GET | `/my/stats/occupancy` | O — từ `vw_room_occupancy` |
| GET | `/my/stats/profit` | O — doanh thu − chi phí cùng kỳ |
| GET | `/my/stats/summary` | O — thẻ tổng quan dashboard |

## Thông báo (5) — mới

| Method | Path | Role |
|---|---|---|
| GET | `/notifications` | T S O A |
| GET | `/notifications/unread-count` | T S O A |
| PUT | `/notifications/{id}/read` | T S O A |
| PUT | `/notifications/read-all` | T S O A |
| WS | `/hubs/notifications` | T S O A — SignalR |

## Admin (16)

| Method | Path |
|---|---|
| GET · POST · PUT · DELETE | `/admin/accounts[/{id}]` |
| PUT | `/admin/accounts/{id}/lock` · `/admin/accounts/{id}/unlock` |
| POST | `/admin/accounts/{id}/restore` |
| GET | `/admin/boarding-houses` — lọc `listingStatus` |
| PUT | `/admin/boarding-houses/{id}/approve` · `/reject` |
| DELETE · POST | `/admin/boarding-houses/{id}` · `/{id}/restore` |
| GET · POST · PUT · DELETE | `/admin/facilities[/{id}]` |
| GET | `/admin/reviews` · DELETE `/admin/reviews/{id}` · POST `/admin/reviews/{id}/restore` |
| GET | `/admin/audit-logs` — lọc `actor, entityType, entityId, from, to` |
| GET | `/admin/stats/summary` |

## Upload ảnh (2)

| Method | Path | Role |
|---|---|---|
| POST | `/images` | T S O A — upload Cloudinary, trả `url` + `publicId` |
| DELETE | `/images/{id}` | T S O A — xóa cả trên Cloudinary trong cùng luồng |

---

**Tổng: ~150 endpoint.** Con số này giữ được thấp nhờ hai quy ước ở trên: lọc là query param
trên endpoint list, và staff/owner/admin dùng chung một endpoint với authorization theo tài
nguyên thay vì ba bản sao.



