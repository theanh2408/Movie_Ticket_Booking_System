using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Tự động tạo cơ sở dữ liệu nếu chưa tồn tại
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Warning: EnsureCreated failed: {ex.Message}");
            return;
        }

        // Kiểm tra xem đã có dữ liệu chưa
        if (await context.Genres.AnyAsync())
        {
            return; // Đã nạp dữ liệu mẫu rồi
        }

        Console.WriteLine("[DbInitializer] Seeding initial database data...");

        // 1. Thể loại phim (Genres)
        var actionGenre = new Genre { GenreName = "Hành Động" };
        var comedyGenre = new Genre { GenreName = "Hài Hước" };
        var dramaGenre = new Genre { GenreName = "Tâm Lý - Tình Cảm" };
        var scifiGenre = new Genre { GenreName = "Khoa Học Viễn Tưởng" };
        var animGenre = new Genre { GenreName = "Hoạt Hình" };
        var horrorGenre = new Genre { GenreName = "Kinh Dị" };

        var genres = new List<Genre> { actionGenre, comedyGenre, dramaGenre, scifiGenre, animGenre, horrorGenre };
        context.Genres.AddRange(genres);
        await context.SaveChangesAsync();

        // 2. Tài khoản mẫu (Accounts & Customers)
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var adminAccount = new Account
        {
            FullName = "Quản Trị Viên",
            Email = "admin@cinema.com",
            Phone = "0901234567",
            Dob = new DateOnly(1995, 1, 1),
            Gender = "Nam",
            Role = "ADMIN",
            PasswordHash = adminPasswordHash
        };

        var customerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123");
        var customerAccount = new Account
        {
            FullName = "Nguyễn Văn Khách",
            Email = "customer@gmail.com",
            Phone = "0987654321",
            Dob = new DateOnly(2000, 5, 20),
            Gender = "Nam",
            Role = "CUSTOMER",
            PasswordHash = customerPasswordHash
        };

        context.Accounts.AddRange(adminAccount, customerAccount);
        await context.SaveChangesAsync();

        var customerRecord = new Customer
        {
            UserId = customerAccount.UserId
        };
        context.Customers.Add(customerRecord);
        await context.SaveChangesAsync();

        // 3. Rạp chiếu & Phòng chiếu (Cinemas & Rooms)
        var cgv = new Cinema
        {
            CinemaName = "CGV Vincom Center",
            Address = "72 Lê Thánh Tôn, Bến Nghé, Quận 1, TP.HCM",
            Status = "ACTIVE"
        };
        var bhd = new Cinema
        {
            CinemaName = "BHD Star Cineplex 3/2",
            Address = "3C Đường 3 Tháng 2, Phường 11, Quận 10, TP.HCM",
            Status = "ACTIVE"
        };
        context.Cinemas.AddRange(cgv, bhd);
        await context.SaveChangesAsync();

        var room1 = new Room { RoomName = "Phòng chiếu 01 (2D)", CinemaId = cgv.CinemaId, Status = "ACTIVE" };
        var room2 = new Room { RoomName = "Phòng chiếu 02 (IMAX)", CinemaId = cgv.CinemaId, Status = "ACTIVE" };
        var room3 = new Room { RoomName = "Phòng chiếu 01 (3D)", CinemaId = bhd.CinemaId, Status = "ACTIVE" };
        context.Rooms.AddRange(room1, room2, room3);
        await context.SaveChangesAsync();

        // 4. Khởi tạo ghế cho mỗi phòng chiếu (40 ghế: Hàng A-E, Cột 1-8)
        var rooms = new[] { room1, room2, room3 };
        var rowLetters = new[] { "A", "B", "C", "D", "E" };
        var allSeats = new List<Seat>();

        foreach (var r in rooms)
        {
            foreach (var row in rowLetters)
            {
                for (int col = 1; col <= 8; col++)
                {
                    allSeats.Add(new Seat
                    {
                        RoomId = r.RoomId,
                        Row = row,
                        Column = col.ToString()
                    });
                }
            }
        }
        context.Seats.AddRange(allSeats);
        await context.SaveChangesAsync();

        // 5. Phim mẫu (Movies)
        var movie1 = new Movie
        {
            Title = "Mai",
            Description = "Tác phẩm tâm lý, tình cảm đặc sắc của Trấn Thành xoay quanh số phận và tình yêu của một phụ nữ tên Mai đối diện với định kiến xã hội.",
            Duration = 131,
            PosterUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=800&auto=format&fit=crop&q=80",
            TrailerUrl = "https://www.youtube.com/watch?v=HKn_7s80Q4c",
            AgeRating = "T18",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
            Status = "NOW_SHOWING",
            Genres = new List<Genre> { dramaGenre, comedyGenre }
        };

        var movie2 = new Movie
        {
            Title = "Dune: Hành Tinh Cát 2",
            Description = "Hành trình báo thù và số phận vĩ đại của Paul Atreides giữa những âm mưu chính trị và cuộc chiến vũ trụ khốc liệt tại Arrakis.",
            Duration = 166,
            PosterUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&auto=format&fit=crop&q=80",
            TrailerUrl = "https://www.youtube.com/watch?v=Way9Dexny3w",
            AgeRating = "T16",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            Status = "NOW_SHOWING",
            Genres = new List<Genre> { scifiGenre, actionGenre }
        };

        var movie3 = new Movie
        {
            Title = "Lật Mặt 7: Một Điều Ước",
            Description = "Câu chuyện gia đình xúc động lấy nước mắt hàng triệu khán giả về tình yêu thương vô bờ bến của người mẹ dành cho các con.",
            Duration = 138,
            PosterUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=800&auto=format&fit=crop&q=80",
            TrailerUrl = "https://www.youtube.com/watch?v=z3m4rR9E2c8",
            AgeRating = "K",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
            Status = "NOW_SHOWING",
            Genres = new List<Genre> { dramaGenre, comedyGenre }
        };

        var movie4 = new Movie
        {
            Title = "Kung Fu Panda 4",
            Description = "Gấu Po tài ba bước vào nhiệm vụ mới: trở thành Thủ lĩnh Tinh thần và huấn luyện thế hệ Thần Long Đại Hiệp tiếp theo.",
            Duration = 94,
            PosterUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800&auto=format&fit=crop&q=80",
            TrailerUrl = "https://www.youtube.com/watch?v=_inKs4eeHiI",
            AgeRating = "P",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Today),
            Status = "NOW_SHOWING",
            Genres = new List<Genre> { animGenre, comedyGenre }
        };

        var movie5 = new Movie
        {
            Title = "Deadpool & Wolverine",
            Description = "Bộ đôi siêu anh hùng lầy lội tái xuất cứu vãn dòng thời gian trong cuộc hỗn chiến đa vũ trụ mãn nhãn và hài hước.",
            Duration = 128,
            PosterUrl = "https://images.unsplash.com/photo-1574375927938-d5a98e8ffe85?w=800&auto=format&fit=crop&q=80",
            TrailerUrl = "https://www.youtube.com/watch?v=73_1biulkYk",
            AgeRating = "T18",
            ReleaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
            Status = "UPCOMING",
            Genres = new List<Genre> { actionGenre, comedyGenre }
        };

        context.Movies.AddRange(movie1, movie2, movie3, movie4, movie5);
        await context.SaveChangesAsync();

        // 6. Suất chiếu & Ghế suất chiếu (Showtimes & ShowtimeSeats)
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);

        var showtimeList = new List<Showtime>
        {
            // Hôm nay
            new() { MovieId = movie1.MovieId, RoomId = room1.RoomId, Date = today, StartHour = new TimeOnly(10, 0), BasePrice = 90000m, Status = "ACTIVE" },
            new() { MovieId = movie1.MovieId, RoomId = room1.RoomId, Date = today, StartHour = new TimeOnly(14, 30), BasePrice = 90000m, Status = "ACTIVE" },
            new() { MovieId = movie2.MovieId, RoomId = room2.RoomId, Date = today, StartHour = new TimeOnly(13, 0), BasePrice = 120000m, Status = "ACTIVE" },
            new() { MovieId = movie2.MovieId, RoomId = room2.RoomId, Date = today, StartHour = new TimeOnly(19, 0), BasePrice = 130000m, Status = "ACTIVE" },
            new() { MovieId = movie3.MovieId, RoomId = room3.RoomId, Date = today, StartHour = new TimeOnly(15, 0), BasePrice = 85000m, Status = "ACTIVE" },
            new() { MovieId = movie4.MovieId, RoomId = room1.RoomId, Date = today, StartHour = new TimeOnly(18, 0), BasePrice = 85000m, Status = "ACTIVE" },

            // Ngày mai
            new() { MovieId = movie1.MovieId, RoomId = room1.RoomId, Date = tomorrow, StartHour = new TimeOnly(10, 0), BasePrice = 90000m, Status = "ACTIVE" },
            new() { MovieId = movie2.MovieId, RoomId = room2.RoomId, Date = tomorrow, StartHour = new TimeOnly(14, 0), BasePrice = 120000m, Status = "ACTIVE" },
            new() { MovieId = movie3.MovieId, RoomId = room3.RoomId, Date = tomorrow, StartHour = new TimeOnly(18, 30), BasePrice = 95000m, Status = "ACTIVE" }
        };

        context.Showtimes.AddRange(showtimeList);
        await context.SaveChangesAsync();

        // Tạo ShowtimeSeats cho mỗi suất chiếu dựa trên ghế của phòng
        var showtimeSeats = new List<ShowtimeSeat>();
        foreach (var st in showtimeList)
        {
            var roomSeats = allSeats.Where(s => s.RoomId == st.RoomId).ToList();
            foreach (var seat in roomSeats)
            {
                showtimeSeats.Add(new ShowtimeSeat
                {
                    ShowtimeId = st.ShowtimeId,
                    SeatId = seat.SeatId,
                    Price = st.BasePrice,
                    Status = "AVAILABLE"
                });
            }
        }
        context.ShowtimeSeats.AddRange(showtimeSeats);
        await context.SaveChangesAsync();

        // 7. Khuyến mãi mẫu (Promotions)
        var promo1 = new Promotion
        {
            Code = "CHAOBANMOI",
            DiscountType = "PERCENT",
            DiscountValue = 20,
            MaxDiscountAmount = 50000m,
            MinOrderAmount = 80000m,
            Status = "ACTIVE",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(2),
            UsageLimit = 1000,
            UsagePerCustomer = 1
        };

        var promo2 = new Promotion
        {
            Code = "GIAM30K",
            DiscountType = "FIXED",
            DiscountValue = 30000m,
            MinOrderAmount = 100000m,
            Status = "ACTIVE",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(2),
            UsageLimit = 500,
            UsagePerCustomer = 2
        };

        context.Promotions.AddRange(promo1, promo2);
        await context.SaveChangesAsync();

        Console.WriteLine("[DbInitializer] Seeding completed successfully!");
    }
}
