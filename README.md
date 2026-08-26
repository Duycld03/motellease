# MotelLease

Nền tảng tìm và quản lý nhà trọ. Viết lại từ đồ án tốt nghiệp
[`Boarding-house-booking`](../Boarding-house-booking) (MERN) sang stack mới.

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core (.NET 10), EF Core + Npgsql, SignalR |
| Frontend | Nuxt 4 (1 app cho cả 4 role), TypeScript, Tailwind, `@nuxtjs/i18n` (vi/en) |
| Database | PostgreSQL 17 + PostGIS |
| Ảnh | Cloudinary |
| Thanh toán | MoMo, VNPay (sandbox) |

## Trạng thái

**Bước 1 — đặc tả: xong.** Chưa có code.

| Tài liệu | Nội dung |
|---|---|
| [docs/features.md](docs/features.md) | Tính năng bản tối ưu: bỏ gì, thêm gì, vì sao |
| [docs/erd.md](docs/erd.md) | 29 bảng, index, view, ghi chú migration |
| [docs/domain-rules.md](docs/domain-rules.md) | Quy tắc nghiệp vụ + 4 lỗi tính tiền của bản cũ + 12 bất biến |
| [docs/api-design.md](docs/api-design.md) | ~150 endpoint nhóm theo resource |
| [docs/api-inventory.csv](docs/api-inventory.csv) | 221 route của bản cũ (trích tự động), để đối chiếu |
| [docs/seed-plan.md](docs/seed-plan.md) | Dữ liệu demo: điểm neo toạ độ, khối lượng, nhất quán |

## Các bước tiếp theo

2. Chốt schema → viết migration đầu + `docker-compose.yml` (PostGIS)
3. Dựng khung solution + Nuxt app + CI
4. Làm theo vertical slice theo thứ tự phụ thuộc (Auth → Nhà trọ/Phòng → Tìm kiếm →
   Lịch xem → Cọc → Hợp đồng → Hóa đơn/Thanh toán → Gia hạn/Hoàn cọc → Doanh thu →
   Nhân viên/Công việc → Đánh giá/Báo cáo → Admin)
5. Tích hợp Cloudinary / MoMo / VNPay / email / background job
6. Test (xUnit + Testcontainers PostGIS, Vitest) + seed + deploy

## Lưu ý môi trường

Máy dev hiện tại chưa vào được Docker (user không thuộc group `docker`). Cần:

```bash
sudo usermod -aG docker $USER   # rồi logout/login
```
