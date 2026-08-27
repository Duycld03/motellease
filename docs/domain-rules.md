# MotelLease — Quy tắc nghiệp vụ

> Đây là nguồn sự thật cho tầng Domain và Application. Mỗi mục nói rõ quy tắc và **lý do**;
> phần bất biến ở mục 9 là yêu cầu bắt buộc có test, không phải khuyến nghị.

## 1. Phòng còn trống

Số người được ở trong một phòng phụ thuộc loại nhà trọ:

| Loại nhà trọ | Quy tắc |
|---|---|
| `Traditional` | Tối đa **1** người thuê / phòng |
| `MiniHouse` | Tối đa **1** người thuê / phòng |
| `DormStyle` | Tối đa `RoomType.MaxOccupants` người |

`Room.Status` là enum 4 giá trị (`Available`, `Reserved`, `Occupied`, `Maintenance`) chứ không
phải cờ boolean, vì "đã cọc, chờ nhận phòng" phải phân biệt được với "còn trống" và "đang ở".

Số người đang ở tính từ `LeaseTenants` đang hoạt động — trạng thái suy ra từ dữ liệu, không có
biến đếm song song để lệch. Quy tắc đặt trong `Domain/Rooms/RoomOccupancyPolicy.cs`, gọi từ
Application layer. **Không** đặt trong EF interceptor hay lifecycle hook: nếu một quy tắc cần
database mới test được thì nó đang nằm sai tầng.

## 2. Tiền cọc

Tiền cọc = giá thuê 1 tháng của loại phòng, **chốt vào `Deposits.Amount`** tại thời điểm tạo
yêu cầu. Đọc `RoomType.Price` lúc phát hành chứng từ sẽ làm số tiền đã cam kết thay đổi về sau.

Kỳ hạn luôn quy về số tháng (`RequestedTermMonths`).

Một người không được cọc 2 lần cùng một phòng khi yêu cầu trước còn hiệu lực.

Sau khi chủ trọ duyệt, `ExpiresAt` cho khách N giờ để thanh toán. Quá hạn thì `Status = Expired`
và phòng trả về `Available`, để một yêu cầu không thanh toán không giữ phòng vô thời hạn.

## 3. Tính hóa đơn tháng

```
electricityQty = ElectricityNew - ElectricityOld
waterQty       = WaterNew - WaterOld
electricityAmt = electricityQty * BoardingHouse.ElectricityPrice
waterAmt       = waterQty * BoardingHouse.WaterPrice
additionalTotal= Σ RoomAdditionalFees(RoomId, Month, Year)
TotalAmount    = Leases.MonthlyRent + electricityAmt + waterAmt + additionalTotal
```

Bốn điểm dễ sai, đã chốt cách xử lý:

1. **Phí phát sinh phải lọc theo kỳ.** `RoomAdditionalFees` lọc theo `(RoomId, Month, Year)`,
   và gán `PaymentBillId` cho phí đã dùng để không bị cộng lại ở hóa đơn sau.
2. **Tiền thuê lấy từ `Leases.MonthlyRent`**, là giá chốt khi ký, không đọc `RoomType.Price`
   hiện tại. Chủ trọ tăng giá phòng không được làm đổi hóa đơn của người đang thuê giá cũ.
   Nguyên tắc chung: chứng từ lịch sử (hóa đơn, hợp đồng) không bao giờ đọc giá hiện hành.
3. **Chia tiền theo đồng, không theo số thực.** Chia đều cho các `LeaseTenants` đang ở, phần dư
   dồn cho người đại diện (`IsPrimary`), để Σ phần chia luôn đúng bằng `TotalAmount`. Hợp đồng
   không có người ở thì chặn phát hành hóa đơn — không để phép chia cho 0 xảy ra.
4. **Cập nhật chỉ số và tạo hóa đơn nằm trong 1 EF transaction.** Nếu tách rời, lỗi ở giữa để
   lại chỉ số đã nhảy mà không có hóa đơn tương ứng.

Chỉ số cũ của hóa đơn tháng sau lấy từ `PaymentBills.ElectricityNew` của tháng trước;
`Rooms.CurrentElectricityReading` là con số hiện tại duy nhất. Một nguồn sự thật, không đồng bộ tay.

## 4. Lịch xem phòng

Lịch đã qua giờ chuyển sang `Expired` bằng `AppointmentExpiryJob : BackgroundService`, khai báo
tường minh ở `Program.cs`. Job không được khai báo trong file entity: ở đó nó chạy mỗi lần
entity được nạp, kể cả trong test, và không ai kiểm soát được vòng đời của nó.

## 5. Doanh thu

`vw_monthly_revenue` là materialized view dựng từ `PaymentBills` có `Status = 'Paid'`.
Doanh thu tháng = Σ `TotalAmount` các hóa đơn đã thanh toán, nhóm theo nhà trọ.
Lợi nhuận = doanh thu − `BoardingHouseExpenses.TotalExpense` cùng kỳ.

Không lưu số tổng hợp vào bảng riêng: mọi con số ở đây tính lại được, và một bộ đếm đồng bộ tay
lệch đi thì không có cách nào phát hiện.

## 6. Phân quyền

Hai tầng, vì role một mình không đủ: kiểm tra "người này là Staff" không trả lời được câu hỏi
"Staff này có phụ trách nhà trọ đó không".

- **Policy theo role:** `RequireTenant`, `RequireOwner`, `RequireStaffOrOwner`, `RequireAdmin`.
  Fallback policy yêu cầu đã đăng nhập, nên endpoint mặc định đóng; endpoint công khai tự mở
  bằng `[AllowAnonymous]`.
- **`IAuthorizationHandler` theo tài nguyên:** `BoardingHouseAccessHandler` kiểm tra
  `OwnerUserId == currentUser` **hoặc** tồn tại `StaffAssignment` đang hoạt động cho
  `(BoardingHouseId, currentUser)`. Mọi endpoint nhận `boardingHouseId` đều đi qua handler này.

## 7. Thông báo

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

Mỗi thông báo lưu `TitleKey`/`BodyKey` + `PayloadJson`, không lưu câu hoàn chỉnh — người nhận
đọc bằng ngôn ngữ họ chọn tại lúc xem, không phải lúc gửi.

## 8. Background job

Tất cả khai báo tường minh ở `Program.cs`, không nằm trong file entity.

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
   `SignatureVerified = true`. Trạng thái tiền không bao giờ xác nhận từ URL redirect của
   browser — chỉ từ callback server-to-server đã kiểm chữ ký.
9. `ElectricityNew ≥ ElectricityOld` và `WaterNew ≥ WaterOld` (CHECK constraint).
10. Chỉ tạo được `Review` gốc khi user có `Lease` với nhà trọ đó; mỗi `(UserId, LeaseId)`
    một đánh giá.
11. Owner không rút quá `OwnerProfile.AvailableBalance`.
12. Staff chỉ đọc/ghi được dữ liệu của nhà trọ có `StaffAssignment` đang hoạt động.
