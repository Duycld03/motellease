# MotelLease — Quy tắc nghiệp vụ trích từ repo cũ

> Mỗi quy tắc ghi kèm nơi nó đang nằm trong repo cũ, để đối chiếu khi implement.
> Phần "Đổi lại" là quyết định cho bản mới — đọc kỹ, có vài chỗ là **sửa lỗi**, không phải port.

## 1. Phòng còn trống

Đang chôn trong Mongoose `pre("save")` của `backend/src/models/room.js` (lặp lại 3 lần ở
`updateRoomAvailability`, `updateAllRoomsAvailability`, `checkAvailabilityRules`):

| Loại nhà trọ (`codeName` cũ) | Enum mới | Quy tắc |
|---|---|---|
| `nha_tro_truyen_thong` | `Traditional` | Tối đa **1** người thuê / phòng |
| `mini_house` | `MiniHouse` | Tối đa **1** người thuê / phòng |
| `nha_tro_kien_truc_xa` | `DormStyle` | Tối đa `RoomType.MaxOccupants` người |

**Đổi lại:** `Room.Status` là enum 4 giá trị thay cho `boolean isAvailable`. Số người ở
tính từ `LeaseTenants` đang hoạt động, không từ array `room.rentBy`. Quy tắc đặt trong
`Domain/Rooms/RoomOccupancyPolicy.cs`, gọi từ Application layer — **không** đặt trong
EF interceptor, để test được mà không cần DB.

Hệ quả: bỏ được cron `0 2 * * *` (`models/room.js:419`) vốn tồn tại chỉ để vá lại
`isAvailable` bị lệch.

## 2. Tiền cọc

`controllers/depositController.js:53` — `amount: price`, tức **tiền cọc = giá thuê 1 tháng**
của loại phòng. Kỳ hạn: `rentalTime * (timeType === "month" ? 1 : 12)` → luôn quy về số tháng.

Ràng buộc đã có: một người không được cọc 2 lần cùng 1 phòng (`existDeposit` check).

**Đổi lại:** giữ quy tắc "cọc = 1 tháng tiền thuê" nhưng chốt `Deposits.Amount` tại thời
điểm tạo. Thêm `ExpiresAt`: sau khi chủ trọ duyệt, khách có N giờ để thanh toán, hết hạn
thì `Status = Expired` và phòng trả về `Available` (bản cũ không có, phòng bị giữ vô thời hạn).

## 3. Tính hóa đơn tháng

`controllers/paymentBillController.js:140–205`:

```
electricalQty  = ElectricityNew - ElectricityOld
waterQty       = WaterNew - WaterOld
electricalAmt  = electricalQty * boardingHouse.electricityPrice
waterAmt       = waterQty * boardingHouse.waterPrice
additionalTotal= Σ RoomAdditionalFees(roomId)
paymentAmount  = roomType.price + electricalAmt + waterAmt + additionalTotal
```

Sau đó chia đều cho số người ở: `splitAmount = paymentAmount / room.rentBy.length`,
tạo 1 `UserPayment` cho mỗi người.

**4 lỗi trong công thức trên phải sửa, không port:**

1. `RoomAdditionalFees.find({ roomId })` (dòng 151) **không lọc `month`/`year`**, dù bảng có 2
   cột đó. Hóa đơn tháng 6 đang cộng cả phí phát sinh của tháng 1–5. → Bản mới lọc theo
   `(RoomId, Month, Year)` và gán `PaymentBillId` cho phí đã dùng để không cộng lại.
2. `roomType.price` đọc **trực tiếp** lúc tạo hóa đơn. Chủ trọ tăng giá phòng thì mọi hóa
   đơn tạo sau đó đổi theo, kể cả người đang thuê hợp đồng giá cũ. → Bản mới lấy
   `Leases.MonthlyRent` (giá chốt khi ký).
3. `splitAmount = paymentAmount / totalPeople` chia số thực. 3 người → mỗi người
   333.333,33đ, tổng ≠ hóa đơn. Và `totalPeople = 0` cho ra `Infinity`. → Bản mới chia
   theo đồng, phần dư dồn cho người đại diện (`LeaseTenants.IsPrimary`), và chặn phát hành
   hóa đơn khi hợp đồng không có người ở.
4. `room.save()` (cập nhật chỉ số) và `PaymentBill.create()` là 2 lệnh rời. Fail ở giữa là
   chỉ số đã nhảy mà hóa đơn không có. → Bản mới nằm trong 1 EF transaction.

Ngoài ra: bản cũ ghi `room.previousElectricityReading = newNumber` để làm chỉ số cũ cho
tháng sau. Bản mới chỉ giữ `Rooms.CurrentElectricityReading`; chỉ số cũ của hóa đơn tháng
sau lấy từ `PaymentBills.ElectricityNew` của tháng trước — một nguồn sự thật, không đồng bộ tay.

## 4. Lịch xem phòng

`models/appointment.js:76` — cron `0 * * * *` mỗi giờ đánh dấu lịch đã qua giờ thành hết hạn.

**Đổi lại:** giữ nguyên logic, chuyển thành `AppointmentExpiryJob : BackgroundService` khai
báo tường minh ở `Program.cs`. Không đặt cron trong file entity như bản cũ (đang chạy mỗi
lần model được import, kể cả trong test).

## 5. Doanh thu

Bản cũ ghi vào bảng `Revenue` (`totalRevenue`, `transactionCount`, `transactions[]`) —
đồng bộ tay, lệch là không phát hiện được.

**Đổi lại:** `vw_monthly_revenue` materialized view từ `PaymentBills` có `Status = 'Paid'`.
Doanh thu tháng = Σ `TotalAmount` các hóa đơn đã thanh toán, nhóm theo nhà trọ.
Lợi nhuận = doanh thu − `BoardingHouseExpenses.TotalExpense` cùng kỳ (bản cũ chưa trừ bao giờ).

## 6. Phân quyền

`middlewares/authMiddleware.js` — 4 middleware theo role:
`authMiddleware` (đã đăng nhập) · `staffMiddleware` (`staff` **hoặc** `owner`) ·
`ownerMiddleware` (`owner`) · `adminMiddleware` (`admin`).

Điểm yếu: chỉ kiểm tra role, **không kiểm tra quyền trên tài nguyên cụ thể**. Staff A gọi
API sửa nhà trọ của owner B vẫn qua middleware.

**Đổi lại:** 2 tầng —
- Policy theo role: `RequireOwner`, `RequireStaffOrOwner`, `RequireAdmin`
- `IAuthorizationHandler` theo tài nguyên: `BoardingHouseAccessHandler` kiểm tra
  `OwnerUserId == currentUser` **hoặc** tồn tại `StaffAssignment` đang hoạt động cho
  `(BoardingHouseId, currentUser)`. Mọi endpoint nhận `boardingHouseId` đều đi qua handler này.

## 7. Thông báo (mới — bản cũ không có)

| Sự kiện | Người nhận | `Type` |
|---|---|---|
| Lịch xem phòng được xác nhận / từ chối | Khách | `AppointmentHandled` |
| Có yêu cầu cọc mới | Chủ trọ + staff phụ trách | `DepositRequested` |
| Cọc được duyệt (kèm hạn thanh toán) | Khách | `DepositAccepted` |
| Cọc bị từ chối / hết hạn | Khách | `DepositRejected` / `DepositExpired` |
| Thanh toán thành công | Khách + chủ trọ | `PaymentSucceeded` |
| Hóa đơn tháng mới được phát hành | Khách đang thuê | `BillIssued` |
| Hóa đơn sắp đến hạn (trước 3 ngày) | Khách | `BillDueSoon` |
| Hóa đơn quá hạn | Khách + chủ trọ | `BillOverdue` |
| Yêu cầu gia hạn được trả lời | Khách | `ExtensionHandled` |
| Hoàn cọc đã xử lý | Khách | `RefundProcessed` |
| Yêu cầu rút tiền được duyệt / từ chối | Chủ trọ | `WithdrawHandled` |
| Hợp đồng sắp hết hạn (trước 30 ngày) | Khách + chủ trọ | `LeaseExpiring` |
| Có báo sự cố mới | Staff phụ trách | `MaintenanceReported` |
| Nhà trọ được duyệt / bị từ chối hiển thị | Chủ trọ | `ListingReviewed` |

Mỗi thông báo lưu `TitleKey`/`BodyKey` + `PayloadJson`, không lưu câu hoàn chỉnh.

## 8. Background job (thay 2 cron rải rác trong model)

| Job | Chu kỳ | Việc |
|---|---|---|
| `AppointmentExpiryJob` | 1 giờ | Lịch xem phòng đã qua giờ → `Expired` |
| `DepositExpiryJob` | 15 phút | Cọc đã duyệt nhưng quá `ExpiresAt` → `Expired`, phòng về `Available` |
| `BillReminderJob` | 1 ngày | `BillDueSoon` trước 3 ngày, `Issued` → `Overdue` khi quá hạn |
| `LeaseExpiryJob` | 1 ngày | `Active` → `Expiring` khi còn ≤30 ngày; quá `EndDate` → `Ended` |
| `RevenueViewRefreshJob` | 1 giờ + sau khi có hóa đơn `Paid` | `REFRESH MATERIALIZED VIEW CONCURRENTLY` |

## 9. Bất biến phải giữ (viết test cho từng dòng)

1. Một phòng có tối đa **1** `Lease` ở trạng thái `Active` (partial unique index).
2. Số `LeaseTenants` đang ở ≤ `RoomType.MaxOccupants`, và ≤ 1 nếu nhà trọ là
   `Traditional`/`MiniHouse`.
3. `Rooms.Status` phải nhất quán: có `Lease` `Active` ⇒ `Occupied`; có `Deposit`
   `Accepted`/`Paid` mà chưa có lease ⇒ `Reserved`; không có gì ⇒ `Available`.
4. Một `(RoomId, Month, Year)` chỉ có **1** `PaymentBill` (unique index).
5. `PaymentBills.TotalAmount` = `RentAmount + ElectricityAmount + WaterAmount + AdditionalFeeTotal`.
6. Σ số tiền chia cho các `LeaseTenants` = `PaymentBills.TotalAmount` (không lệch 1 đồng).
7. Một `ProviderTxnId` chỉ được ghi nhận thành công **1 lần** (unique index + kiểm tra ở IPN).
8. `PaymentBills` chỉ sang `Paid` khi có `PaymentTransaction` `Succeeded` với
   `SignatureVerified = true`.
9. `ElectricityNew ≥ ElectricityOld` và `WaterNew ≥ WaterOld` (CHECK constraint).
10. Chỉ tạo được `Review` gốc khi user có `Lease` với nhà trọ đó; mỗi `(UserId, LeaseId)`
    một đánh giá.
11. Owner không rút quá `OwnerProfile.AvailableBalance`.
12. Staff chỉ đọc/ghi được dữ liệu của nhà trọ có `StaffAssignment` đang hoạt động.


