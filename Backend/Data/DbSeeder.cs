using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<AppDbContext>>();

        try
        {
            // 1. Kiểm tra và seed dữ liệu gốc (Accounts, Genres, Movies, Cinemas, Rooms, Seats) nếu chưa có
            if (!await context.Accounts.AnyAsync() || !await context.Movies.AnyAsync())
            {
                logger?.LogInformation("Seeding initial master data (Accounts, Movies, Cinemas, Rooms, Seats)...");
                await SeedMasterDataAsync(context);
            }
            else
            {
                logger?.LogInformation("Master data already exists. Checking showtimes schedule...");
            }

            // 2. Luôn đảm bảo có lịch chiếu hoạt động cho tất cả các phim trong 7 ngày tới
            await EnsureDailyShowtimesAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding or updating the database.");
            throw;
        }
    }

    private static async Task SeedMasterDataAsync(AppDbContext context)
    {
        // 1. Seed Accounts (1 ADMIN, 1 CUSTOMER)
        var adminAccount = new Account
        {
            FullName = "Administrator",
            Email = "admin@example.com",
            Phone = "0901000001",
            Dob = new DateOnly(1990, 1, 1),
            Gender = "Male",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "ADMIN"
        };

        var customerAccount = new Account
        {
            FullName = "Nguyen Van A",
            Email = "a@example.com",
            Phone = "0901234567",
            Dob = new DateOnly(2003, 5, 20),
            Gender = "Male",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Example123!"),
            Role = "CUSTOMER"
        };

        context.Accounts.AddRange(adminAccount, customerAccount);
        await context.SaveChangesAsync();

        var customer = new Customer
        {
            UserId = customerAccount.UserId
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // 2. Seed Genres (4 thể loại)
        var gAction = new Genre { GenreName = "Hành động" };
        var gHorror = new Genre { GenreName = "Kinh dị" };
        var gRomance = new Genre { GenreName = "Tình cảm" };
        var gSciFi = new Genre { GenreName = "Khoa học viễn tưởng" };

        context.Genres.AddRange(gAction, gHorror, gRomance, gSciFi);
        await context.SaveChangesAsync();

        // 3. Seed Movies (4 phim: 2 NOW_SHOWING, 2 COMING_SOON)
        var m1 = new Movie
        {
            Title = "Avengers: Hồi Kết",
            Description = "Sau những sự kiện tàn khốc của Avengers: Cuộc chiến vô cực, vũ trụ bị tàn phá. Với sự trợ giúp của các đồng minh còn lại, các Avengers tập hợp một lần nữa để đảo ngược hành động của Thanos và khôi phục lại sự cân bằng cho vũ trụ.",
            Duration = 181,
            PosterUrl = "https://image.tmdb.org/t/p/w500/or06FN3Dka5tukK1e9sl16pB3iy.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=TcMBFSGVi1c",
            AgeRating = "C13",
            ReleaseDate = new DateOnly(2026, 4, 26),
            Status = "NOW_SHOWING"
        };

        var m2 = new Movie
        {
            Title = "Ám Ảnh Kinh Hoàng",
            Description = "Được dựa trên câu chuyện có thật, hai nhà điều tra hiện tượng siêu nhiên Ed và Lorraine Warren đến giúp đỡ một gia đình đang bị khủng bố bởi một thế lực bóng tối trong trang trại hẻo lánh của họ.",
            Duration = 112,
            PosterUrl = "https://image.tmdb.org/t/p/w500/wVYREutTvI2tmxr6ujrHT704wGF.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=k10ETZ41q5o",
            AgeRating = "C18",
            ReleaseDate = new DateOnly(2026, 6, 15),
            Status = "NOW_SHOWING"
        };

        var m3 = new Movie
        {
            Title = "Những Kẻ Khờ Mộng Mơ",
            Description = "Câu chuyện tình lãng mạn giữa một nghệ sĩ dương cầm nhạc jazz đầy tham vọng và một nữ diễn viên trẻ đang chật vật tìm kiếm cơ hội ở Los Angeles hoa lệ.",
            Duration = 128,
            PosterUrl = "https://image.tmdb.org/t/p/w500/uDO8zWDhfWwoFdKS4fzkVJt0Rf0.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=0pdqf4P9MB8",
            AgeRating = "P",
            ReleaseDate = new DateOnly(2026, 11, 20),
            Status = "COMING_SOON"
        };

        var m4 = new Movie
        {
            Title = "Hố Đen Tử Thần",
            Description = "Khi Trái Đất dần trở nên không còn thích hợp cho sự sống, một nhóm các nhà thám hiểm dũng cảm thực hiện sứ mệnh quan trọng nhất lịch sử nhân loại: du hành xuyên qua một lỗ sâu vũ trụ để tìm kiếm một ngôi nhà mới cho con người.",
            Duration = 169,
            PosterUrl = "https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=zSWdZVtXT7E",
            AgeRating = "C13",
            ReleaseDate = new DateOnly(2026, 12, 1),
            Status = "COMING_SOON"
        };

        context.Movies.AddRange(m1, m2, m3, m4);
        await context.SaveChangesAsync();

        context.MovieGenres.AddRange(
            new MovieGenre { MovieId = m1.MovieId, GenreId = gAction.GenreId },
            new MovieGenre { MovieId = m1.MovieId, GenreId = gSciFi.GenreId },
            new MovieGenre { MovieId = m2.MovieId, GenreId = gHorror.GenreId },
            new MovieGenre { MovieId = m3.MovieId, GenreId = gRomance.GenreId },
            new MovieGenre { MovieId = m4.MovieId, GenreId = gSciFi.GenreId }
        );
        await context.SaveChangesAsync();

        // 4. Seed Cinemas, Rooms, Seats
        var cinema1 = new Cinema
        {
            CinemaName = "CGV Vincom Center Đồng Khởi",
            Address = "72 Lê Thánh Tôn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
            Status = "ACTIVE"
        };

        var cinema2 = new Cinema
        {
            CinemaName = "BHD Star Cineplex 3 Tháng 2",
            Address = "3C Đường 3 Tháng 2, Phường 11, Quận 10, TP. Hồ Chí Minh",
            Status = "ACTIVE"
        };

        context.Cinemas.AddRange(cinema1, cinema2);
        await context.SaveChangesAsync();

        var room1C1 = new Room { RoomName = "Phòng chiếu 1", Status = "ACTIVE", CinemaId = cinema1.CinemaId };
        var room2C1 = new Room { RoomName = "Phòng chiếu 2", Status = "ACTIVE", CinemaId = cinema1.CinemaId };
        var room1C2 = new Room { RoomName = "Phòng chiếu 1", Status = "ACTIVE", CinemaId = cinema2.CinemaId };
        var room2C2 = new Room { RoomName = "Phòng chiếu 2", Status = "ACTIVE", CinemaId = cinema2.CinemaId };

        context.Rooms.AddRange(room1C1, room2C1, room1C2, room2C2);
        await context.SaveChangesAsync();

        var allRooms = new[] { room1C1, room2C1, room1C2, room2C2 };
        var rows = new[] { "A", "B", "C", "D", "E" };
        var cols = new[] { "1", "2", "3", "4", "5", "6", "7", "8" };

        var seats = new List<Seat>();
        foreach (var r in allRooms)
        {
            foreach (var row in rows)
            {
                foreach (var col in cols)
                {
                    seats.Add(new Seat
                    {
                        RoomId = r.RoomId,
                        Row = row,
                        Column = col
                    });
                }
            }
        }
        context.Seats.AddRange(seats);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Đảm bảo luôn có đầy đủ lịch chiếu cho hôm nay và 7 ngày tiếp theo cho tất cả các phim
    /// </summary>
    public static async Task EnsureDailyShowtimesAsync(AppDbContext context, ILogger? logger = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rooms = await context.Rooms.ToListAsync();
        var movies = await context.Movies.ToListAsync();

        if (rooms.Count < 4 || movies.Count < 2)
        {
            return;
        }

        var room1C1 = rooms[0]; // Cinema 1 - Phòng 1
        var room2C1 = rooms[1]; // Cinema 1 - Phòng 2
        var room1C2 = rooms[2]; // Cinema 2 - Phòng 1
        var room2C2 = rooms[3]; // Cinema 2 - Phòng 2

        var m1 = movies[0]; // Avengers: Hồi Kết
        var m2 = movies[1]; // Ám Ảnh Kinh Hoàng
        var m3 = movies.Count > 2 ? movies[2] : null; // Những Kẻ Khờ Mộng Mơ
        var m4 = movies.Count > 3 ? movies[3] : null; // Hố Đen Tử Thần

        var allSeats = await context.Seats.ToListAsync();
        var roomSeatsLookup = allSeats.GroupBy(s => s.RoomId ?? 0).ToDictionary(g => g.Key, g => g.ToList());

        var newShowtimes = new List<Showtime>();

        // Duyệt 7 ngày tính từ hôm nay (day 0 đến day 6)
        for (int i = 0; i < 7; i++)
        {
            var targetDate = today.AddDays(i);

            // 1. Phim 1: Avengers (nếu ngày đó chưa có suất chiếu)
            if (!await context.Showtimes.AnyAsync(s => s.Date == targetDate && s.MovieId == m1.MovieId))
            {
                newShowtimes.Add(new Showtime { MovieId = m1.MovieId, RoomId = room1C1.RoomId, Date = targetDate, StartHour = new TimeOnly(9, 30), BasePrice = 85000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m1.MovieId, RoomId = room1C1.RoomId, Date = targetDate, StartHour = new TimeOnly(14, 0), BasePrice = 90000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m1.MovieId, RoomId = room1C1.RoomId, Date = targetDate, StartHour = new TimeOnly(19, 30), BasePrice = 95000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m1.MovieId, RoomId = room1C2.RoomId, Date = targetDate, StartHour = new TimeOnly(13, 0), BasePrice = 85000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m1.MovieId, RoomId = room1C2.RoomId, Date = targetDate, StartHour = new TimeOnly(18, 30), BasePrice = 90000, Status = "OPEN" });
            }

            // 2. Phim 2: Ám Ảnh Kinh Hoàng (nếu ngày đó chưa có suất chiếu)
            if (!await context.Showtimes.AnyAsync(s => s.Date == targetDate && s.MovieId == m2.MovieId))
            {
                newShowtimes.Add(new Showtime { MovieId = m2.MovieId, RoomId = room2C1.RoomId, Date = targetDate, StartHour = new TimeOnly(10, 0), BasePrice = 80000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m2.MovieId, RoomId = room2C1.RoomId, Date = targetDate, StartHour = new TimeOnly(15, 30), BasePrice = 85000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m2.MovieId, RoomId = room2C1.RoomId, Date = targetDate, StartHour = new TimeOnly(20, 30), BasePrice = 90000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m2.MovieId, RoomId = room2C2.RoomId, Date = targetDate, StartHour = new TimeOnly(11, 0), BasePrice = 80000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m2.MovieId, RoomId = room2C2.RoomId, Date = targetDate, StartHour = new TimeOnly(16, 30), BasePrice = 85000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m2.MovieId, RoomId = room2C2.RoomId, Date = targetDate, StartHour = new TimeOnly(21, 0), BasePrice = 90000, Status = "OPEN" });
            }

            // 3. Phim 3: Những Kẻ Khờ Mộng Mơ (nếu ngày đó chưa có suất chiếu)
            if (m3 != null && !await context.Showtimes.AnyAsync(s => s.Date == targetDate && s.MovieId == m3.MovieId))
            {
                newShowtimes.Add(new Showtime { MovieId = m3.MovieId, RoomId = room1C1.RoomId, Date = targetDate, StartHour = new TimeOnly(11, 45), BasePrice = 85000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m3.MovieId, RoomId = room1C1.RoomId, Date = targetDate, StartHour = new TimeOnly(16, 45), BasePrice = 90000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m3.MovieId, RoomId = room1C2.RoomId, Date = targetDate, StartHour = new TimeOnly(10, 15), BasePrice = 80000, Status = "OPEN" });
            }

            // 4. Phim 4: Hố Đen Tử Thần (nếu ngày đó chưa có suất chiếu)
            if (m4 != null && !await context.Showtimes.AnyAsync(s => s.Date == targetDate && s.MovieId == m4.MovieId))
            {
                newShowtimes.Add(new Showtime { MovieId = m4.MovieId, RoomId = room2C1.RoomId, Date = targetDate, StartHour = new TimeOnly(12, 45), BasePrice = 85000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m4.MovieId, RoomId = room2C1.RoomId, Date = targetDate, StartHour = new TimeOnly(18, 0), BasePrice = 90000, Status = "OPEN" });
                newShowtimes.Add(new Showtime { MovieId = m4.MovieId, RoomId = room2C2.RoomId, Date = targetDate, StartHour = new TimeOnly(13, 45), BasePrice = 85000, Status = "OPEN" });
            }
        }

        if (newShowtimes.Count > 0)
        {
            context.Showtimes.AddRange(newShowtimes);
            await context.SaveChangesAsync();

            var newShowtimeSeats = new List<ShowtimeSeat>();
            foreach (var st in newShowtimes)
            {
                if (st.RoomId.HasValue && roomSeatsLookup.TryGetValue(st.RoomId.Value, out var roomSeats))
                {
                    foreach (var s in roomSeats)
                    {
                        newShowtimeSeats.Add(new ShowtimeSeat
                        {
                            ShowtimeId = st.ShowtimeId,
                            SeatId = s.SeatId,
                            Status = "AVAILABLE",
                            Price = st.BasePrice
                        });
                    }
                }
            }

            if (newShowtimeSeats.Count > 0)
            {
                context.ShowtimeSeats.AddRange(newShowtimeSeats);
                await context.SaveChangesAsync();
            }

            logger?.LogInformation("Successfully ensured daily showtimes: added {ShowtimesCount} showtimes and {SeatsCount} seats across all movies.", newShowtimes.Count, newShowtimeSeats.Count);
        }
    }
}
