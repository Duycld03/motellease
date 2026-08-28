using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Infrastructure.Persistence;

public static class DbSeeder
{
    private const string SharedPassword = "Demo@1234";

    private sealed record GeoAnchor(
        string Name,
        double Lat,
        double Lon,
        string District,
        string Province,
        string[] Wards,
        string[] Streets);

    private static readonly GeoAnchor[] Anchors =
    [
        new("Hanoi University of Science and Technology", 21.0045, 105.8435, "Hai Bà Trưng", "Hà Nội",
            ["Bách Khoa", "Đồng Tâm", "Trương Định", "Bạch Mai"],
            ["Tạ Quang Bửu", "Trần Đại Nghĩa", "Lê Thanh Nghị", "Đại La", "Bạch Mai"]),

        new("Vietnam National University Hanoi (Xuân Thủy)", 21.0378, 105.7825, "Cầu Giấy", "Hà Nội",
            ["Dịch Vọng Hậu", "Dịch Vọng", "Quan Hoa", "Nghĩa Tân"],
            ["Xuân Thủy", "Duy Tân", "Trần Thái Tông", "Cầu Giấy", "Phạm Văn Đồng"]),

        new("Thuongmai University / Hồ Tùng Mậu", 21.0410, 105.7690, "Cầu Giấy", "Hà Nội",
            ["Mai Dịch", "Dịch Vọng Hậu", "Phú Diễn"],
            ["Hồ Tùng Mậu", "Doãn Kế Thiện", "Trần Vỹ", "Lê Đức Thọ", "Nguyễn Đổng Chi"]),

        new("Thăng Long Industrial Park", 21.1160, 105.7770, "Đông Anh", "Hà Nội",
            ["Kim Chung", "Võ Văn Kiệt", "Hải Bối", "Đại Mạch"],
            ["Kim Chung", "Võ Văn Kiệt", "Hoàng Sa", "Đại Mạch", "Đường số 6"]),

        new("HCMC University of Technology (District 10)", 10.7720, 106.6580, "Quận 10", "Hồ Chí Minh",
            ["Phường 14", "Phường 15", "Phường 12", "Phường 10"],
            ["Lý Thường Kiệt", "Tô Hiến Thành", "Thành Thái", "3 Tháng 2", "Nguyễn Tri Phương"]),

        new("Vietnam National University HCMC (Linh Trung)", 10.8700, 106.8000, "Thành phố Thủ Đức", "Hồ Chí Minh",
            ["Linh Trung", "Linh Chiểu", "Hiệp Phú", "Tăng Nhơn Phú A"],
            ["Võ Văn Ngân", "Hoàng Diệu 2", "Đặng Văn Bi", "Lê Văn Việt", "Quốc Lộ 1K"]),

        new("Tân Bình Industrial Park", 10.8100, 106.6200, "Tân Bình", "Hồ Chí Minh",
            ["Tây Thạnh", "Sơn Kỳ", "Phường 15", "Phường 13"],
            ["Trường Chinh", "Tây Thạnh", "Lê Trọng Tấn", "Cộng Hòa", "Chế Lan Viên"])
    ];

    private static readonly (string Name, string Code, string Icon)[] StandardFacilities =
    [
        ("Wi-Fi tốc độ cao", "wifi", "wifi"),
        ("Điều hòa nhiệt độ", "air_conditioner", "ac"),
        ("Bình nóng lạnh", "water_heater", "water_heater"),
        ("Gác lửng thông minh", "mezzanine", "loft"),
        ("Tủ lạnh Inverter", "refrigerator", "fridge"),
        ("Máy giặt riêng", "washing_machine", "washer"),
        ("Vệ sinh khép kín", "private_bathroom", "bathroom"),
        ("Ban công / Cửa sổ thoáng", "balcony", "balcony"),
        ("Kệ bếp & Chậu rửa", "kitchen_shelf", "kitchen"),
        ("Chỗ để xe máy an toàn", "parking", "parking"),
        ("Bảo vệ & Camera 24/7", "security_24_7", "security"),
        ("Khóa cửa vân tay / Thẻ từ", "fingerprint_lock", "lock"),
        ("Giờ giấc tự do, không chung chủ", "no_curfew", "no_curfew"),
        ("Cho phép nuôi thú cưng", "pet_friendly", "pet"),
        ("Giường & Nệm cao cấp", "bed", "bed"),
        ("Tủ quần áo gỗ", "wardrobe", "wardrobe"),
        ("Bàn ghế học tập / làm việc", "desk", "desk"),
        ("Thang máy", "elevator", "elevator"),
        ("Hệ thống PCCC đạt chuẩn", "fire_protection", "fire"),
        ("Dọn vệ sinh hàng tuần", "cleaning_service", "clean")
    ];

    private static readonly string[] SamplePhotos =
    [
        "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1484154218962-a197022b5858?w=800&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=800&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1505691938895-1758d7feb511?w=800&auto=format&fit=crop&q=80",
        "https://images.unsplash.com/photo-1598928506311-c55ded91a20c?w=800&auto=format&fit=crop&q=80"
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MotelLeaseDbContext>>();

        logger.LogInformation("🌱 Starting MotelLease Database Seeder...");

        // Ensure migrations
        await db.Database.MigrateAsync();

        // 1. Seed Facilities
        var facilities = await SeedFacilitiesAsync(db);

        // 2. Seed Users
        var (admin, owners, staffList, tenants) = await SeedUsersAsync(db, passwordHasher);

        // 3. Seed Boarding Houses, Room Types, Rooms & Staff Assignments
        var (houses, rooms, activeRooms) = await SeedBoardingHousesAsync(db, owners, staffList, facilities);

        // 4. Seed Leases & Co-tenants
        var leases = await SeedLeasesAsync(db, activeRooms, tenants);

        // 5. Seed Payment Bills (Last 6 months) & Succeeded PaymentTransactions
        await SeedBillsAndPaymentsAsync(db, leases);

        // 6. Seed Master Utility Expenses for Boarding Houses
        await SeedExpensesAsync(db, houses);

        // 7. Seed Deposits
        await SeedDepositsAsync(db, rooms, tenants);

        // 8. Seed Viewing Appointments
        await SeedAppointmentsAsync(db, rooms, tenants);

        // 9. Seed Verified Reviews & Recompute Ratings
        await SeedReviewsAsync(db, houses, leases, owners, staffList);

        // 10. Seed Maintenance Requests & WorkTasks
        await SeedMaintenanceAndTasksAsync(db, leases, staffList);

        // 11. Seed Withdraw Requests & Platform Reports
        await SeedWithdrawsAndReportsAsync(db, owners, tenants, houses);

        // Print Verification Summary
        await PrintVerificationSummaryAsync(db, logger);
    }

    private static async Task<List<Facility>> SeedFacilitiesAsync(MotelLeaseDbContext db)
    {
        var existing = await db.Facilities.ToListAsync();
        var existingCodes = existing.Select(f => f.CodeName).ToHashSet();

        var newFacilities = new List<Facility>();
        foreach (var (name, code, icon) in StandardFacilities)
        {
            if (!existingCodes.Contains(code))
            {
                var fac = new Facility
                {
                    Name = name,
                    CodeName = code,
                    IconKey = icon,
                    Description = $"Tiện ích {name} phục vụ sinh hoạt"
                };
                newFacilities.Add(fac);
            }
        }

        if (newFacilities.Count > 0)
        {
            db.Facilities.AddRange(newFacilities);
            await db.SaveChangesAsync();
        }

        return await db.Facilities.ToListAsync();
    }

    private static async Task<(User Admin, List<User> Owners, List<User> StaffList, List<User> Tenants)> SeedUsersAsync(
        MotelLeaseDbContext db,
        IPasswordHasher passwordHasher)
    {
        var passwordHash = passwordHasher.Hash(SharedPassword);
        var existingUsers = await db.Users.IgnoreQueryFilters()
            .Include(u => u.OwnerProfile)
            .Include(u => u.StaffProfile)
            .ToListAsync();

        // 1. Admin
        var admin = existingUsers.FirstOrDefault(u => 
            u.Email.Equals("admin@motellease.local", StringComparison.OrdinalIgnoreCase) ||
            u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase));
        if (admin == null)
        {
            admin = new User
            {
                Username = "admin",
                Email = "admin@motellease.local",
                PasswordHash = passwordHash,
                FullName = "Quản trị viên Hệ thống",
                Role = UserRole.Admin,
                Gender = Gender.Male,
                EmailConfirmed = true
            };
            db.Users.Add(admin);
        }

        // 2. Owners (8)
        var ownerNames = new[]
        {
            "Nguyễn Văn An", "Trần Thị Bình", "Lê Hoàng Cường", "Phạm Thu Dung",
            "Vũ Đức Giang", "Đặng Mai Hoa", "Hoàng Minh Khôi", "Bùi Lan Phương"
        };
        var banks = new[] { "MB Bank", "Vietcombank", "Techcombank", "VPBank", "BIDV", "ACB", "TPBank", "Sacombank" };
        var accounts = new[] { "0987654321", "19034567890123", "1029384756", "9876543210", "0123456789", "1122334455", "6677889900", "9988776655" };

        var owners = new List<User>();
        for (int i = 1; i <= 8; i++)
        {
            var email = $"owner{i}@motellease.local";
            var username = $"owner{i}";
            var owner = existingUsers.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (owner == null)
            {
                owner = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = passwordHash,
                    FullName = ownerNames[i - 1],
                    PhoneNumber = $"090{i:D2}123456",
                    Gender = (i % 2 == 0) ? Gender.Female : Gender.Male,
                    Role = UserRole.Owner,
                    EmailConfirmed = true
                };
                db.Users.Add(owner);

                var profile = new OwnerProfile
                {
                    UserId = owner.Id,
                    User = owner,
                    BusinessType = (i % 3 == 0) ? BusinessType.Company : BusinessType.Individual,
                    BusinessName = (i % 3 == 0) ? $"Công ty TNHH Bất động sản {ownerNames[i - 1]}" : null,
                    BankName = banks[i - 1],
                    BankAccountNumber = accounts[i - 1],
                    BankAccountHolder = ownerNames[i - 1].ToUpperInvariant(),
                    AvailableBalance = 15_000_000m + (i * 5_000_000m)
                };
                db.OwnerProfiles.Add(profile);
            }
            owners.Add(owner);
        }

        await db.SaveChangesAsync();

        // 3. Staff (12)
        var staffNames = new[]
        {
            "Nguyễn Tuấn Anh", "Lê Thị Bích", "Trần Quốc Cường", "Phạm Hải Đăng",
            "Đỗ Thu Trang", "Vũ Đình Huy", "Bùi Yến Nhi", "Ngô Văn Long",
            "Dương Thùy Linh", "Trương Quang Hưng", "Hồ Ngọc Mai", "Lý Thành Nam"
        };
        var staffList = new List<User>();
        for (int i = 1; i <= 12; i++)
        {
            var email = $"staff{i}@motellease.local";
            var username = $"staff{i}";
            var staff = existingUsers.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (staff == null)
            {
                var assignedOwner = owners[(i - 1) % owners.Count];
                staff = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = passwordHash,
                    FullName = staffNames[i - 1],
                    PhoneNumber = $"091{i:D2}654321",
                    Gender = (i % 2 == 0) ? Gender.Female : Gender.Male,
                    Role = UserRole.Staff,
                    EmailConfirmed = true
                };
                db.Users.Add(staff);

                var profile = new StaffProfile
                {
                    UserId = staff.Id,
                    User = staff,
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-i - 2)),
                    CreatedByUserId = assignedOwner.Id
                };
                db.StaffProfiles.Add(profile);
            }
            staffList.Add(staff);
        }

        // 4. Tenants (99)
        var tenantFirstNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý" };
        var tenantMiddleNames = new[] { "Văn", "Thị", "Minh", "Đức", "Thành", "Hải", "Tuấn", "Thanh", "Quốc", "Phương", "Thu", "Ngọc" };
        var tenantLastNames = new[] { "Anh", "Bình", "Châu", "Duy", "Dương", "Đạt", "Hà", "Hải", "Hằng", "Hiếu", "Hòa", "Huy", "Huyền", "Khánh", "Khoa", "Khôi", "Lan", "Linh", "Long", "Mai", "Minh", "Nam", "Nga", "Nghĩa", "Nhi", "Nhung", "Phong", "Phúc", "Quân", "Quang", "Quyên", "Sơn", "Tâm", "Thảo", "Thắng", "Thịnh", "Thu", "Trang", "Trung", "Trúc", "Tú", "Tùng", "Uyên", "Vân", "Việt", "Vinh", "Vy", "Yến" };

        var rnd = new Random(42);
        var tenants = new List<User>();
        for (int i = 1; i <= 99; i++)
        {
            var email = $"tenant{i}@motellease.local";
            var username = $"tenant{i}";
            var tenant = existingUsers.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (tenant == null)
            {
                var fn = tenantFirstNames[rnd.Next(tenantFirstNames.Length)];
                var mn = tenantMiddleNames[rnd.Next(tenantMiddleNames.Length)];
                var ln = tenantLastNames[rnd.Next(tenantLastNames.Length)];
                var fullName = $"{fn} {mn} {ln}";

                tenant = new User
                {
                    Username = $"tenant{i}",
                    Email = email,
                    PasswordHash = passwordHash,
                    FullName = fullName,
                    PhoneNumber = $"098{i:D3}{rnd.Next(1000, 9999)}",
                    Gender = (i % 2 == 0) ? Gender.Female : Gender.Male,
                    Role = UserRole.Tenant,
                    EmailConfirmed = true
                };
                db.Users.Add(tenant);
            }
            tenants.Add(tenant);
        }

        await db.SaveChangesAsync();
        return (admin, owners, staffList, tenants);
    }

    private static async Task<(List<BoardingHouse> Houses, List<Room> Rooms, List<Room> ActiveRooms)> SeedBoardingHousesAsync(
        MotelLeaseDbContext db,
        List<User> owners,
        List<User> staffList,
        List<Facility> facilities)
    {
        var existingHouses = await db.BoardingHouses
            .Include(b => b.RoomTypes)
            .Include(b => b.Rooms)
            .ToListAsync();

        if (existingHouses.Count >= 60)
        {
            var allRooms = existingHouses.SelectMany(b => b.Rooms).ToList();
            var occupied = allRooms.Where(r => r.Status == RoomStatus.Occupied).ToList();
            return (existingHouses, allRooms, occupied);
        }

        var rnd = new Random(101);
        var housesToCreate = new List<BoardingHouse>();
        var housePrefixes = new[]
        {
            "Nhà trọ cao cấp", "Ký túc xá hiện đại", "Chung cư mini", "Homestay sinh viên",
            "Nhà trọ sinh viên tiện nghi", "Căn hộ dịch vụ", "Khu nhà trọ an ninh", "Nhà trọ xanh"
        };

        int houseIndex = 0;
        foreach (var anchor in Anchors)
        {
            // 8-9 houses per anchor to reach 60 total
            int countForAnchor = (anchor.Name.Contains("Thuongmai") || anchor.Name.Contains("Thăng Long")) ? 7 :
                                 (anchor.Name.Contains("VNU") || anchor.Name.Contains("HUST")) ? 9 : 8;

            for (int i = 0; i < countForAnchor; i++)
            {
                houseIndex++;
                if (houseIndex > 60) break;

                var owner = owners[(houseIndex - 1) % owners.Count];
                var bearing = rnd.NextDouble() * 2 * Math.PI;
                var distance = 300 + rnd.NextDouble() * 2200; // 300m .. 2.5km
                var dLat = distance * Math.Cos(bearing) / 111_320.0;
                var dLon = distance * Math.Sin(bearing) / (111_320.0 * Math.Cos(anchor.Lat * Math.PI / 180));
                var lat = Math.Round((decimal)(anchor.Lat + dLat), 6);
                var lon = Math.Round((decimal)(anchor.Lon + dLon), 6);

                var street = anchor.Streets[rnd.Next(anchor.Streets.Length)];
                var ward = anchor.Wards[rnd.Next(anchor.Wards.Length)];
                var houseNumber = rnd.Next(1, 280);
                var addressLine = $"Số {houseNumber} {street}";

                var prefix = housePrefixes[rnd.Next(housePrefixes.Length)];
                var houseName = $"{prefix} {street} - Cơ sở {i + 1}";

                var listingStatus = (houseIndex <= 54) ? ListingStatus.Published :
                                    (houseIndex <= 58) ? ListingStatus.PendingReview : ListingStatus.Rejected;

                var house = new BoardingHouse
                {
                    OwnerUserId = owner.Id,
                    OwnerUser = owner,
                    Name = houseName,
                    Description = $"Khu trọ cao cấp, an ninh đảm bảo, giờ giấc tự do tại khu vực {anchor.District}, gần {anchor.Name}. Đầy đủ tiện ích, đường rộng thoáng xe ba gác vào tận cửa.",
                    Type = (i % 3 == 0) ? BoardingHouseType.MiniHouse :
                           (i % 3 == 1) ? BoardingHouseType.DormStyle : BoardingHouseType.Traditional,
                    AddressLine = addressLine,
                    Ward = ward,
                    District = anchor.District,
                    Province = anchor.Province,
                    Latitude = lat,
                    Longitude = lon,
                    ElectricityUnitPrice = 3500m + (rnd.Next(0, 4) * 100m),
                    WaterUnitPrice = (i % 2 == 0) ? 30000m : 90000m,
                    ListingStatus = listingStatus,
                    RejectionReason = (listingStatus == ListingStatus.Rejected) ? "Hình ảnh phòng chưa rõ ràng, thiếu giấy chứng nhận PCCC cơ sở." : null
                };

                // 2-3 RoomTypes per house
                var roomTypes = new List<RoomType>();
                var rt1 = new RoomType
                {
                    BoardingHouseId = house.Id,
                    BoardingHouse = house,
                    TypeName = "Phòng Studio Gác Lửng Full Đồ",
                    Price = 3_800_000m + (rnd.Next(0, 6) * 200_000m),
                    RoomSizeM2 = 25m,
                    MaxOccupants = 2,
                    Description = "Phòng có gác lửng cao không đụng đầu, ban công riêng đón gió, full điều hòa, nóng lạnh, tủ lạnh, kệ bếp.",
                    Facilities = facilities.OrderBy(_ => rnd.Next()).Take(8).ToList()
                };
                var rt2 = new RoomType
                {
                    BoardingHouseId = house.Id,
                    BoardingHouse = house,
                    TypeName = "Phòng Đơn Tiện Nghi Ban Công",
                    Price = 2_800_000m + (rnd.Next(0, 4) * 200_000m),
                    RoomSizeM2 = 18m,
                    MaxOccupants = 1,
                    Description = "Phòng đơn khép kín sạch sẽ, cửa sổ lớn, sẵn giường đệm tủ quần áo, điều hòa tiết kiệm điện.",
                    Facilities = facilities.OrderBy(_ => rnd.Next()).Take(6).ToList()
                };
                roomTypes.Add(rt1);
                roomTypes.Add(rt2);

                if (i % 2 == 0)
                {
                    var rt3 = new RoomType
                    {
                        BoardingHouseId = house.Id,
                        BoardingHouse = house,
                        TypeName = "Căn Hộ Mini 1 Phòng Ngủ",
                        Price = 5_200_000m + (rnd.Next(0, 5) * 300_000m),
                        RoomSizeM2 = 35m,
                        MaxOccupants = 3,
                        Description = "Căn hộ mini 1 phòng ngủ tách biệt phòng khách, ban công riêng máy giặt riêng, bếp từ âm hiện đại.",
                        Facilities = facilities.OrderBy(_ => rnd.Next()).Take(12).ToList()
                    };
                    roomTypes.Add(rt3);
                }

                house.RoomTypes = roomTypes;

                // 8-12 Rooms per house
                var rooms = new List<Room>();
                int roomNum = 100;
                for (int floor = 1; floor <= 4; floor++)
                {
                    for (int r = 1; r <= 3; r++)
                    {
                        roomNum = floor * 100 + r;
                        var selectedType = roomTypes[(r - 1) % roomTypes.Count];
                        var status = (rooms.Count < 5) ? RoomStatus.Occupied :
                                     (rooms.Count < 6) ? RoomStatus.Reserved :
                                     (rooms.Count == 9 && floor == 4) ? RoomStatus.Maintenance : RoomStatus.Available;

                        rooms.Add(new Room
                        {
                            BoardingHouseId = house.Id,
                            BoardingHouse = house,
                            RoomTypeId = selectedType.Id,
                            RoomType = selectedType,
                            RoomNumber = $"P.{roomNum}",
                            Status = status,
                            Description = $"Phòng tầng {floor}, view thoáng sáng sủa",
                            CurrentElectricityReading = rnd.Next(200, 800),
                            CurrentWaterReading = rnd.Next(30, 150)
                        });
                    }
                }
                house.Rooms = rooms;

                // Attach sample images
                var imgCount = rnd.Next(3, 6);
                for (int m = 0; m < imgCount; m++)
                {
                    db.Images.Add(new Image
                    {
                        OwnerType = ImageOwnerType.BoardingHouse,
                        OwnerId = house.Id,
                        Url = SamplePhotos[(houseIndex + m) % SamplePhotos.Length],
                        PublicId = $"motellease/seed/house_{houseIndex}_{m}",
                        IsPrimary = (m == 0),
                        SortOrder = m
                    });
                }

                // Staff assignment
                var houseStaff = staffList.Where(s => s.StaffProfile?.CreatedByUserId == owner.Id).ToList();
                if (houseStaff.Count > 0)
                {
                    var staffMember = houseStaff[houseIndex % houseStaff.Count];
                    db.StaffAssignments.Add(new StaffAssignment
                    {
                        BoardingHouseId = house.Id,
                        StaffUserId = staffMember.Id,
                        AssignedByUserId = owner.Id,
                        AssignedAt = DateTimeOffset.UtcNow.AddMonths(-3)
                    });
                }

                housesToCreate.Add(house);
            }
        }

        db.BoardingHouses.AddRange(housesToCreate);
        await db.SaveChangesAsync();

        var allCreatedHouses = await db.BoardingHouses
            .Include(b => b.Rooms)
            .ThenInclude(r => r.RoomType)
            .ToListAsync();

        var allRoomsList = allCreatedHouses.SelectMany(b => b.Rooms).ToList();
        var activeRoomsList = allRoomsList.Where(r => r.Status == RoomStatus.Occupied).ToList();
        return (allCreatedHouses, allRoomsList, activeRoomsList);
    }

    private static async Task<List<Lease>> SeedLeasesAsync(
        MotelLeaseDbContext db,
        List<Room> activeRooms,
        List<User> tenants)
    {
        var existingLeases = await db.Leases
            .Include(l => l.Tenants)
            .ToListAsync();

        if (existingLeases.Count >= 200)
        {
            return existingLeases;
        }

        var rnd = new Random(202);
        var leasesToCreate = new List<Lease>();

        for (int i = 0; i < activeRooms.Count; i++)
        {
            var room = activeRooms[i];
            var primaryTenant = tenants[i % tenants.Count];
            var startMonthsAgo = rnd.Next(3, 8);
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-startMonthsAgo));
            var termMonths = (rnd.Next(2) == 0) ? 6 : 12;
            var endDate = startDate.AddMonths(termMonths);

            var lease = new Lease
            {
                RoomId = room.Id,
                Room = room,
                PrimaryTenantUserId = primaryTenant.Id,
                PrimaryTenant = primaryTenant,
                StartDate = startDate,
                EndDate = endDate,
                TermMonths = termMonths,
                MonthlyRent = room.RoomType.Price,
                DepositHeld = room.RoomType.Price,
                Status = (endDate < DateOnly.FromDateTime(DateTime.UtcNow)) ? LeaseStatus.Ended :
                         (endDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))) ? LeaseStatus.Expiring :
                         LeaseStatus.Active,
                CreatedByUserId = room.BoardingHouse.OwnerUserId
            };

            // Primary leaseholder tenant
            var primaryLeaseTenant = new LeaseTenant
            {
                LeaseId = lease.Id,
                Lease = lease,
                UserId = primaryTenant.Id,
                User = primaryTenant,
                FullName = primaryTenant.FullName,
                PhoneNumber = primaryTenant.PhoneNumber,
                IdCardNumber = $"0012000{i:D5}",
                IsPrimary = true,
                MovedInAt = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            };
            lease.Tenants.Add(primaryLeaseTenant);

            // Add 1 co-tenant for larger rooms
            if (room.RoomType.MaxOccupants > 1 && i % 2 == 0)
            {
                var coTenantUser = tenants[(i + 15) % tenants.Count];
                var coTenant = new LeaseTenant
                {
                    LeaseId = lease.Id,
                    Lease = lease,
                    UserId = coTenantUser.Id,
                    User = coTenantUser,
                    FullName = coTenantUser.FullName,
                    PhoneNumber = coTenantUser.PhoneNumber,
                    IdCardNumber = $"0792000{i:D5}",
                    IsPrimary = false,
                    MovedInAt = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
                };
                lease.Tenants.Add(coTenant);
            }

            leasesToCreate.Add(lease);
        }

        db.Leases.AddRange(leasesToCreate);
        await db.SaveChangesAsync();

        return await db.Leases
            .Include(l => l.Room)
            .ThenInclude(r => r.BoardingHouse)
            .Include(l => l.PrimaryTenant)
            .ToListAsync();
    }

    private static async Task SeedBillsAndPaymentsAsync(
        MotelLeaseDbContext db,
        List<Lease> leases)
    {
        var existingBillsCount = await db.PaymentBills.CountAsync();
        if (existingBillsCount >= 1000) return;

        var now = DateTime.UtcNow;
        var rnd = new Random(303);
        var billsToCreate = new List<PaymentBill>();
        var txnsToCreate = new List<PaymentTransaction>();

        int txnCounter = 0;
        foreach (var lease in leases)
        {
            var eleUnitPrice = lease.Room.BoardingHouse.ElectricityUnitPrice > 0 ? lease.Room.BoardingHouse.ElectricityUnitPrice : 3800m;
            var watUnitPrice = lease.Room.BoardingHouse.WaterUnitPrice > 0 ? lease.Room.BoardingHouse.WaterUnitPrice : 30000m;

            decimal runningEle = rnd.Next(100, 300);
            decimal runningWat = rnd.Next(15, 40);

            // Generate consecutive bills for the last 6 months
            for (int m = 6; m >= 1; m--)
            {
                var billDate = now.AddMonths(-m);
                int month = billDate.Month;
                int year = billDate.Year;

                decimal eleQty = rnd.Next(45, 120);
                decimal watQty = rnd.Next(4, 12);

                decimal eleOld = runningEle;
                decimal eleNew = eleOld + eleQty;
                runningEle = eleNew;

                decimal watOld = runningWat;
                decimal watNew = watOld + watQty;
                runningWat = watNew;

                decimal eleAmount = eleQty * eleUnitPrice;
                decimal watAmount = watQty * watUnitPrice;
                decimal extraFee = 150_000m; // Internet + service
                decimal totalAmount = lease.MonthlyRent + eleAmount + watAmount + extraFee;

                var isLatestMonth = (m == 1);
                var status = !isLatestMonth ? BillStatus.Paid :
                             (txnCounter % 3 == 0) ? BillStatus.Issued :
                             (txnCounter % 3 == 1) ? BillStatus.Overdue : BillStatus.Paid;

                var bill = new PaymentBill
                {
                    LeaseId = lease.Id,
                    Lease = lease,
                    RoomId = lease.RoomId,
                    Room = lease.Room,
                    Month = month,
                    Year = year,
                    RentAmount = lease.MonthlyRent,
                    ElectricityOld = eleOld,
                    ElectricityNew = eleNew,
                    ElectricityQty = eleQty,
                    ElectricityUnitPrice = eleUnitPrice,
                    ElectricityAmount = eleAmount,
                    WaterOld = watOld,
                    WaterNew = watNew,
                    WaterQty = watQty,
                    WaterUnitPrice = watUnitPrice,
                    WaterAmount = watAmount,
                    AdditionalFeeTotal = extraFee,
                    TotalAmount = totalAmount,
                    Status = status,
                    IssuedAt = new DateTimeOffset(new DateTime(year, month, 1, 8, 0, 0, DateTimeKind.Utc)),
                    DueDate = DateOnly.FromDateTime(new DateTime(year, month, 10)),
                    PaidAt = (status == BillStatus.Paid) ? new DateTimeOffset(new DateTime(year, month, 5, 14, 30, 0, DateTimeKind.Utc)) : null
                };

                // Add Additional Fees
                bill.AdditionalFees.Add(new RoomAdditionalFee
                {
                    RoomId = lease.RoomId,
                    PaymentBillId = bill.Id,
                    FeeName = "Internet cáp quang & Wifi",
                    FeeAmount = 100_000m,
                    Month = month,
                    Year = year
                });
                bill.AdditionalFees.Add(new RoomAdditionalFee
                {
                    RoomId = lease.RoomId,
                    PaymentBillId = bill.Id,
                    FeeName = "Vệ sinh & Rác thải",
                    FeeAmount = 50_000m,
                    Month = month,
                    Year = year
                });

                billsToCreate.Add(bill);

                // Add Succeeded PaymentTransaction for Paid bills
                if (status == BillStatus.Paid)
                {
                    txnCounter++;
                    var provider = (txnCounter % 2 == 0) ? PaymentProvider.VNPay : PaymentProvider.MoMo;
                    var txn = new PaymentTransaction
                    {
                        UserId = lease.PrimaryTenantUserId,
                        Purpose = PaymentPurpose.Rent,
                        PaymentBillId = bill.Id,
                        PaymentBill = bill,
                        Provider = provider,
                        ProviderOrderId = $"BILL_{year}{month:D2}_{lease.Room.RoomNumber}_{txnCounter}",
                        ProviderTxnId = $"TXN_MOMOVNPAY_{year}{month:D2}_{txnCounter:D6}",
                        Amount = totalAmount,
                        Status = PaymentStatus.Succeeded,
                        SignatureVerified = true,
                        InitiatedAt = bill.PaidAt!.Value.AddMinutes(-5),
                        CompletedAt = bill.PaidAt
                    };
                    txnsToCreate.Add(txn);
                }
            }
        }

        db.PaymentBills.AddRange(billsToCreate);
        db.PaymentTransactions.AddRange(txnsToCreate);
        await db.SaveChangesAsync();
    }

    private static async Task SeedExpensesAsync(
        MotelLeaseDbContext db,
        List<BoardingHouse> houses)
    {
        var existingCount = await db.BoardingHouseExpenses.CountAsync();
        if (existingCount >= 200) return;

        var now = DateTime.UtcNow;
        var rnd = new Random(404);
        var expenses = new List<BoardingHouseExpense>();

        foreach (var house in houses)
        {
            decimal eleRunning = rnd.Next(1500, 3000);
            decimal watRunning = rnd.Next(100, 250);

            for (int m = 6; m >= 1; m--)
            {
                var dt = now.AddMonths(-m);
                int month = dt.Month;
                int year = dt.Year;

                decimal eleQty = rnd.Next(400, 900);
                decimal watQty = rnd.Next(30, 80);

                decimal eleOld = eleRunning;
                decimal eleNew = eleOld + eleQty;
                eleRunning = eleNew;

                decimal watOld = watRunning;
                decimal watNew = watOld + watQty;
                watRunning = watNew;

                decimal eleAmount = eleQty * 2500m; // Master wholesale rate
                decimal watAmount = watQty * 18000m;

                var otherItems = new[]
                {
                    new { feeName = "Bảo trì thang máy & máy bơm", feeAmount = 800_000m },
                    new { feeName = "Thu gom rác & vệ sinh tòa nhà", feeAmount = 450_000m }
                };
                var jsonOther = JsonSerializer.Serialize(otherItems);
                decimal otherTotal = 1_250_000m;

                expenses.Add(new BoardingHouseExpense
                {
                    BoardingHouseId = house.Id,
                    BoardingHouse = house,
                    Month = month,
                    Year = year,
                    ElectricityOld = eleOld,
                    ElectricityNew = eleNew,
                    ElectricityQty = eleQty,
                    ElectricityAmount = eleAmount,
                    WaterOld = watOld,
                    WaterNew = watNew,
                    WaterQty = watQty,
                    WaterAmount = watAmount,
                    OtherExpenses = jsonOther,
                    OtherExpensesTotal = otherTotal,
                    TotalExpense = eleAmount + watAmount + otherTotal
                });
            }
        }

        db.BoardingHouseExpenses.AddRange(expenses);
        await db.SaveChangesAsync();
    }

    private static async Task SeedDepositsAsync(
        MotelLeaseDbContext db,
        List<Room> rooms,
        List<User> tenants)
    {
        var count = await db.Deposits.CountAsync();
        if (count >= 50) return;

        var rnd = new Random(505);
        var deposits = new List<Deposit>();

        for (int i = 0; i < 60; i++)
        {
            var room = rooms[i % rooms.Count];
            var tenant = tenants[(i + 30) % tenants.Count];
            var status = (i % 6 == 0) ? DepositStatus.Paid :
                         (i % 6 == 1) ? DepositStatus.Pending :
                         (i % 6 == 2) ? DepositStatus.Accepted :
                         (i % 6 == 3) ? DepositStatus.Expired :
                         (i % 6 == 4) ? DepositStatus.Rejected : DepositStatus.Refunded;

            deposits.Add(new Deposit
            {
                UserId = tenant.Id,
                User = tenant,
                RoomId = room.Id,
                Room = room,
                Amount = room.RoomType?.Price ?? 3_500_000m,
                Status = status,
                RequestedStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(rnd.Next(3, 15))),
                RequestedTermMonths = 6,
                ExpiresAt = (status == DepositStatus.Accepted) ? DateTimeOffset.UtcNow.AddHours(18) : null
            });
        }

        db.Deposits.AddRange(deposits);
        await db.SaveChangesAsync();
    }

    private static async Task SeedAppointmentsAsync(
        MotelLeaseDbContext db,
        List<Room> rooms,
        List<User> tenants)
    {
        var count = await db.Appointments.CountAsync();
        if (count >= 80) return;

        var rnd = new Random(606);
        var appts = new List<Appointment>();

        for (int i = 0; i < 90; i++)
        {
            var room = rooms[(i * 3) % rooms.Count];
            var tenant = tenants[(i * 2) % tenants.Count];
            var isPast = (i % 2 == 0);
            var apptDate = isPast ? DateTimeOffset.UtcNow.AddDays(-rnd.Next(2, 20)) : DateTimeOffset.UtcNow.AddDays(rnd.Next(1, 10)).AddHours(rnd.Next(8, 17));

            var status = isPast ? (i % 3 == 0 ? RequestStatus.Completed : RequestStatus.Cancelled) :
                         (i % 3 == 0 ? RequestStatus.Pending : RequestStatus.Accepted);

            appts.Add(new Appointment
            {
                UserId = tenant.Id,
                User = tenant,
                RoomId = room.Id,
                Room = room,
                AppointmentDate = apptDate,
                Status = status,
                Note = "Khách muốn qua xem phòng trực tiếp sau giờ làm việc."
            });
        }

        db.Appointments.AddRange(appts);
        await db.SaveChangesAsync();
    }

    private static async Task SeedReviewsAsync(
        MotelLeaseDbContext db,
        List<BoardingHouse> houses,
        List<Lease> leases,
        List<User> owners,
        List<User> staffList)
    {
        var count = await db.Reviews.CountAsync();
        if (count >= 150) return;

        var reviewComments = new[]
        {
            ("Phòng sạch sẽ, thoáng mát, chủ nhà rất nhiệt tình hỗ trợ khi có sự cố.", (short)5),
            ("An ninh khu trọ rất tốt, camera và khóa vân tay tiện lợi. Gần trường đi bộ 5 phút.", (short)5),
            ("Phòng mới đẹp như hình chụp, giá điện nước rõ ràng minh bạch. Sẽ ở lâu dài!", (short)5),
            ("Vị trí thuận tiện, gần chợ và siêu thị. Wifi đôi lúc hơi chậm vào buổi tối.", (short)4),
            ("Chỗ để xe rộng rãi, không chung chủ thoải mái giờ giấc. Đánh giá 4 sao.", (short)4),
            ("Phòng cách âm ở mức khá, ban công phơi đồ đón nắng tốt.", (short)4),
            ("Mọi thứ ổn, giá hợp lý so với khu vực trung tâm.", (short)4),
            ("Phòng hơi ẩm mùa nồm, nhưng chủ nhà có hỗ trợ xử lý chống thấm nhiệt tình.", (short)3)
        };

        var rnd = new Random(707);
        var reviews = new List<Review>();

        foreach (var lease in leases.Take(120))
        {
            var (comment, rating) = reviewComments[rnd.Next(reviewComments.Length)];
            var rev = new Review
            {
                UserId = lease.PrimaryTenantUserId,
                User = lease.PrimaryTenant,
                BoardingHouseId = lease.Room.BoardingHouseId,
                BoardingHouse = lease.Room.BoardingHouse,
                LeaseId = lease.Id,
                Lease = lease,
                Rating = rating,
                Content = comment
            };

            // Owner reply for some reviews
            if (rnd.Next(2) == 0)
            {
                var reply = new Review
                {
                    UserId = lease.Room.BoardingHouse.OwnerUserId,
                    BoardingHouseId = lease.Room.BoardingHouseId,
                    ParentReview = rev,
                    Content = "Cảm ơn bạn đã tin tưởng và lựa chọn thuê phòng tại khu trọ! Ban quản lý sẽ luôn hỗ trợ bạn tốt nhất."
                };
                rev.Replies.Add(reply);
            }

            reviews.Add(rev);
        }

        db.Reviews.AddRange(reviews);
        await db.SaveChangesAsync();

        // Recompute BoardingHouses cached Rating & ReviewCount
        var allReviews = await db.Reviews
            .Where(r => r.Rating != null && !r.IsDeleted)
            .GroupBy(r => r.BoardingHouseId)
            .Select(g => new
            {
                HouseId = g.Key,
                Count = g.Count(),
                Avg = g.Average(r => (decimal)r.Rating!.Value)
            })
            .ToListAsync();

        foreach (var stat in allReviews)
        {
            var h = await db.BoardingHouses.FindAsync(stat.HouseId);
            if (h != null)
            {
                h.ReviewCount = stat.Count;
                h.Rating = Math.Round(stat.Avg, 1);
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedMaintenanceAndTasksAsync(
        MotelLeaseDbContext db,
        List<Lease> leases,
        List<User> staffList)
    {
        var count = await db.MaintenanceRequests.CountAsync();
        if (count >= 30) return;

        var rnd = new Random(808);
        var reqs = new List<MaintenanceRequest>();

        var categories = new[]
        {
            (MaintenanceCategory.Electricity, "Bóng đèn phòng tắm bị chập chờn, nhờ ban quản lý thay giúp."),
            (MaintenanceCategory.Water, "Vòi hoa sen bị rò rỉ nước ở khớp nối."),
            (MaintenanceCategory.Door, "Khóa cửa ra vào bị kẹt, cần bảo dưỡng tra dầu."),
            (MaintenanceCategory.Internet, "Wifi tầng 2 chập chờn không vào được mạng."),
            (MaintenanceCategory.Furniture, "Bản lề cánh cửa tủ quần áo bị lỏng.")
        };

        for (int i = 0; i < 40; i++)
        {
            var lease = leases[i % leases.Count];
            var (cat, desc) = categories[i % categories.Length];
            var status = (i % 3 == 0) ? MaintenanceStatus.Resolved :
                         (i % 3 == 1) ? MaintenanceStatus.InProgress : MaintenanceStatus.Open;

            var staff = staffList[i % staffList.Count];

            var req = new MaintenanceRequest
            {
                LeaseId = lease.Id,
                Lease = lease,
                RoomId = lease.RoomId,
                Room = lease.Room,
                ReportedByUserId = lease.PrimaryTenantUserId,
                ReportedByUser = lease.PrimaryTenant,
                Category = cat,
                Description = desc,
                Status = status
            };

            // Link WorkTask for in-progress or resolved
            if (status != MaintenanceStatus.Open)
            {
                req.Task = new WorkTask
                {
                    BoardingHouseId = lease.Room.BoardingHouseId,
                    BoardingHouse = lease.Room.BoardingHouse,
                    CreatedByUserId = lease.Room.BoardingHouse.OwnerUserId,
                    AssignedToUserId = staff.Id,
                    AssignedToUser = staff,
                    MaintenanceRequest = req,
                    Title = $"Sửa chữa {cat}: {lease.Room.RoomNumber}",
                    Details = desc,
                    Priority = TaskPriority.High,
                    Status = (status == MaintenanceStatus.Resolved) ? WorkTaskStatus.Completed : WorkTaskStatus.InProgress,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                    CompletedAt = (status == MaintenanceStatus.Resolved) ? DateTimeOffset.UtcNow.AddDays(-1) : null
                };
            }

            reqs.Add(req);
        }

        db.MaintenanceRequests.AddRange(reqs);
        await db.SaveChangesAsync();
    }

    private static async Task SeedWithdrawsAndReportsAsync(
        MotelLeaseDbContext db,
        List<User> owners,
        List<User> tenants,
        List<BoardingHouse> houses)
    {
        var count = await db.WithdrawRequests.CountAsync();
        if (count >= 10) return;

        var withdraws = new List<WithdrawRequest>();
        for (int i = 0; i < owners.Count; i++)
        {
            var owner = owners[i];
            var prof = owner.OwnerProfile;
            if (prof == null) continue;

            withdraws.Add(new WithdrawRequest
            {
                OwnerUserId = owner.Id,
                OwnerUser = owner,
                Amount = 5_000_000m + (i * 2_000_000m),
                BankName = prof.BankName ?? "Vietcombank",
                BankAccountNumber = prof.BankAccountNumber ?? "0987654321",
                BankAccountHolder = prof.BankAccountHolder ?? owner.FullName.ToUpperInvariant(),
                Status = (i % 3 == 0) ? RequestStatus.Accepted :
                         (i % 3 == 1) ? RequestStatus.Pending : RequestStatus.Rejected,
                ProcessedAt = (i % 3 != 1) ? DateTimeOffset.UtcNow.AddDays(-2) : null
            });
        }
        db.WithdrawRequests.AddRange(withdraws);

        // Reports
        var reports = new List<Report>();
        for (int i = 0; i < 8; i++)
        {
            var reporter = tenants[i % tenants.Count];
            var house = houses[i % houses.Count];
            reports.Add(new Report
            {
                ReporterUserId = reporter.Id,
                ReporterUser = reporter,
                TargetType = ReportTargetType.BoardingHouse,
                TargetId = house.Id,
                Reason = "Thông tin tiện ích không đúng thực tế",
                Details = "Tin đăng ghi có thang máy nhưng thực tế khu trọ đi thang bộ.",
                Status = (i % 2 == 0) ? ReportStatus.Resolved : ReportStatus.Pending,
                Resolution = (i % 2 == 0) ? "Đã yêu cầu chủ nhà cập nhật lại thông tin niêm yết." : null,
                ProcessedAt = (i % 2 == 0) ? DateTimeOffset.UtcNow.AddDays(-1) : null
            });
        }
        db.Reports.AddRange(reports);

        await db.SaveChangesAsync();
    }

    private static async Task PrintVerificationSummaryAsync(MotelLeaseDbContext db, ILogger logger)
    {
        var usersCount = await db.Users.CountAsync();
        var housesCount = await db.BoardingHouses.CountAsync();
        var roomTypesCount = await db.RoomTypes.CountAsync();
        var roomsCount = await db.Rooms.CountAsync();
        var facilitiesCount = await db.Facilities.CountAsync();
        var leasesCount = await db.Leases.CountAsync();
        var billsCount = await db.PaymentBills.CountAsync();
        var txnsCount = await db.PaymentTransactions.CountAsync();
        var reviewsCount = await db.Reviews.CountAsync();
        var depositsCount = await db.Deposits.CountAsync();
        var appointmentsCount = await db.Appointments.CountAsync();
        var maintenanceCount = await db.MaintenanceRequests.CountAsync();
        var tasksCount = await db.Tasks.CountAsync();
        var expensesCount = await db.BoardingHouseExpenses.CountAsync();
        var withdrawsCount = await db.WithdrawRequests.CountAsync();

        logger.LogInformation("==========================================================");
        logger.LogInformation("🎉 MotelLease Demo Database Seeded Successfully!");
        logger.LogInformation("==========================================================");
        logger.LogInformation("  👥 Users:                 {Count} (1 Admin, 8 Owners, 12 Staff, {Tenants} Tenants)", usersCount, usersCount - 21);
        logger.LogInformation("  🏢 Boarding Houses:       {Count} (Clustered around 7 Real University & Industrial Anchors)", housesCount);
        logger.LogInformation("  🛏️  Room Types:            {Count}", roomTypesCount);
        logger.LogInformation("  🚪 Rooms:                 {Count}", roomsCount);
        logger.LogInformation("  ✨ Facilities:            {Count}", facilitiesCount);
        logger.LogInformation("  📄 Leases:                {Count}", leasesCount);
        logger.LogInformation("  🧾 Monthly Payment Bills: {Count}", billsCount);
        logger.LogInformation("  💳 Payment Transactions:  {Count} (Succeeded MoMo/VNPay with unique ProviderTxnIds)", txnsCount);
        logger.LogInformation("  ⭐ Verified Reviews:      {Count}", reviewsCount);
        logger.LogInformation("  💰 Deposits:              {Count}", depositsCount);
        logger.LogInformation("  📅 Appointments:          {Count}", appointmentsCount);
        logger.LogInformation("  🛠️  Maintenance Requests:  {Count}", maintenanceCount);
        logger.LogInformation("  ⚡ Work Tasks:            {Count}", tasksCount);
        logger.LogInformation("  📊 House Expenses:        {Count}", expensesCount);
        logger.LogInformation("  🏧 Withdraw Requests:     {Count}", withdrawsCount);
        logger.LogInformation("==========================================================");
        logger.LogInformation("🔑 Default Shared Password for all accounts: {Password}", SharedPassword);
        logger.LogInformation("   - Admin:  admin@motellease.local");
        logger.LogInformation("   - Owner:  owner1@motellease.local ... owner8@motellease.local");
        logger.LogInformation("   - Staff:  staff1@motellease.local ... staff12@motellease.local");
        logger.LogInformation("   - Tenant: tenant1@motellease.local ... tenant99@motellease.local");
        logger.LogInformation("==========================================================");
    }
}
