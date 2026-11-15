using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IShowtimeService
{
    Task<List<ShowtimeResponseDto>> SearchAsync(int? cinemaId, int? movieId, int? roomId, DateOnly? date, string? status, string? keyword);
    Task<ShowtimeResponseDto> GetAsync(int id);
    Task<ShowtimeResponseDto> CreateAsync(CreateShowtimeDto dto);
    Task<ShowtimeResponseDto> UpdateStartTimeAsync(int id, UpdateShowtimeTimeDto dto);
    Task<ShowtimeResponseDto> CancelAsync(int id, CancelShowtimeDto dto);
    Task<SeatMapDto> GetSeatMapAsync(int id);
    Task<SeatMapItemDto> SetSeatStatusAsync(int showtimeId, int seatId, string status);
}

public class ShowtimeService : IShowtimeService
{
    private readonly IShowtimeRepository _repo;
    private readonly IRoomRepository _rooms;
    private readonly ISeatRepository _seats;
    private readonly IMovieRepository _movies;
    private readonly ICurrentUser _current;

    public ShowtimeService(IShowtimeRepository repo, IRoomRepository rooms, ISeatRepository seats,
                           IMovieRepository movies, ICurrentUser current)
    {
        _repo = repo;
        _rooms = rooms;
        _seats = seats;
        _movies = movies;
        _current = current;
    }

    public async Task<List<ShowtimeResponseDto>> SearchAsync(int? cinemaId, int? movieId, int? roomId,
                                                             DateOnly? date, string? status, string? keyword)
    {
        var scope = _current.ScopeCinemaId();
        var list = await _repo.SearchAsync(scope ?? cinemaId, movieId, roomId, date, status, keyword);
        return list.Select(s => ToDto(s)).ToList();
    }

    public async Task<ShowtimeResponseDto> GetAsync(int id)
    {
        var st = await LoadAsync(id);
        return ToDto(st, await _repo.CountActiveTicketsAsync(id));
    }

    // ===== UC-5.1 Thêm suất chiếu =====
    public async Task<ShowtimeResponseDto> CreateAsync(CreateShowtimeDto dto)
    {
        var room = await _rooms.GetByIdAsync(dto.RoomId) ?? throw new NotFoundException("Phòng chiếu không tồn tại");
        _current.EnsureCinemaAccess(room.CinemaID);

        if (room.Status == RoomStatus.Inactive)
            throw new ConflictException("Phòng chiếu đang ngừng hoạt động");

        var movie = await _movies.GetByIdAsync(dto.MovieId) ?? throw new NotFoundException("Phim không tồn tại");
        if (movie.Duration is null or <= 0)
            throw new ConflictException("Phim chưa có thời lượng nên không thể xếp lịch");
        if (movie.Status is MovieStatus.Ended or MovieStatus.Inactive)
            throw new ConflictException("Phim đã kết thúc hoặc ngừng hiển thị nên không thể xếp lịch");

        var start = dto.Date!.Value.ToDateTime(dto.StartHour!.Value);
        if (start <= DateTime.Now)
            throw new BadRequestException("Thời gian bắt đầu phải ở tương lai");

        await EnsureNoOverlapAsync(room.RoomID, start, movie.Duration.Value, null);

        var seats = await _seats.GetByRoomAsync(room.RoomID);
        if (seats.Count == 0)
            throw new ConflictException("Phòng chiếu chưa có ghế. Hãy tạo sơ đồ ghế trước khi thêm suất chiếu.");

        var showtime = new Showtime
        {
            MovieID = dto.MovieId,
            RoomID = dto.RoomId,
            Date = dto.Date,
            StartHour = dto.StartHour,
            BasePrice = dto.BasePrice,
            Status = ShowtimeStatus.Active,
            // Tạo luôn trạng thái ghế cho suất chiếu (mặc định Available, giá = giá cơ bản)
            ShowtimeSeats = seats.Select(s => new ShowtimeSeat
            {
                SeatID = s.SeatID,
                Status = SeatStatus.Available,
                Price = dto.BasePrice
            }).ToList()
        };

        await _repo.AddAsync(showtime);
        return await GetAsync(showtime.ShowtimeID);
    }

    // ===== UC-5.3 Sửa thời gian bắt đầu (chỉ khi còn ít nhất 2 tiếng) =====
    public async Task<ShowtimeResponseDto> UpdateStartTimeAsync(int id, UpdateShowtimeTimeDto dto)
    {
        var st = await LoadAsync(id);

        if (st.Status != ShowtimeStatus.Active)
            throw new ConflictException("Chỉ sửa được suất chiếu đang hoạt động");

        var oldStart = StartOf(st) ?? throw new ConflictException("Suất chiếu thiếu thông tin thời gian");
        if (oldStart - DateTime.Now < TimeSpan.FromHours(2))
            throw new ConflictException("Chỉ được sửa thời gian bắt đầu trước giờ chiếu ít nhất 2 tiếng");

        var newStart = dto.Date!.Value.ToDateTime(dto.StartHour!.Value);
        if (newStart <= DateTime.Now)
            throw new BadRequestException("Thời gian bắt đầu mới phải ở tương lai");
        if (newStart == oldStart)
            throw new BadRequestException("Thời gian mới trùng với thời gian hiện tại");

        await EnsureNoOverlapAsync(st.RoomID!.Value, newStart, st.Movie?.Duration ?? 0, st.ShowtimeID);

        st.Date = dto.Date;
        st.StartHour = dto.StartHour;
        await _repo.SaveAsync();

        // TODO: gửi thông báo cho khách hàng đã đặt vé (cần module thông báo/email của nhóm)
        return ToDto(st, await _repo.CountActiveTicketsAsync(id));
    }

    // ===== UC-5.4 Hủy suất chiếu =====
    public async Task<ShowtimeResponseDto> CancelAsync(int id, CancelShowtimeDto dto)
    {
        var st = await LoadAsync(id);
        if (st.Status == ShowtimeStatus.Cancelled)
            throw new ConflictException("Suất chiếu này đã bị hủy");

        var booked = await _repo.CountActiveTicketsAsync(id);

        if (booked > 0)
        {
            var start = StartOf(st);
            var minutesLeft = start.HasValue ? (start.Value - DateTime.Now).TotalMinutes : 0;

            // Đã có vé + còn dưới 60 phút: chỉ Admin được hủy khi có sự cố, kèm lý do
            if (minutesLeft < 60 && !(dto.IsIncident && _current.IsAdmin && !string.IsNullOrWhiteSpace(dto.Reason)))
                throw new ConflictException(
                    "Suất chiếu đã có vé và còn dưới 60 phút nên không thể hủy. " +
                    "Chỉ Admin được hủy khi có sự cố không thể khắc phục (IsIncident = true và ghi rõ Reason).");
        }

        st.Status = ShowtimeStatus.Cancelled;
        await _repo.SaveAsync();

        // TODO: xử lý vé / hoàn tiền cho các đơn đã đặt (cần module thanh toán của nhóm)
        return ToDto(st, booked);
    }

    // ===== UC-4.7 Sơ đồ ghế theo suất chiếu =====
    public async Task<SeatMapDto> GetSeatMapAsync(int id)
    {
        var st = await LoadAsync(id);
        var seats = await _repo.GetShowtimeSeatsAsync(id);
        if (seats.Count == 0)
            throw new NotFoundException("Suất chiếu chưa có sơ đồ ghế");

        var items = seats.Select(x => ToSeatItem(x)).ToList();
        return SeatMapBuilder.Build(st.RoomID ?? 0, st.Room?.RoomName, id, items);
    }

    // ===== UC-4.10 Cập nhật trạng thái ghế theo suất chiếu =====
    public async Task<SeatMapItemDto> SetSeatStatusAsync(int showtimeId, int seatId, string status)
    {
        await LoadAsync(showtimeId);

        var newStatus = status.Trim();
        if (!string.Equals(newStatus, SeatStatus.Available, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(newStatus, SeatStatus.Disabled, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException($"Chỉ được đổi ghế sang {SeatStatus.Available} hoặc {SeatStatus.Disabled}");
        newStatus = string.Equals(newStatus, SeatStatus.Disabled, StringComparison.OrdinalIgnoreCase)
            ? SeatStatus.Disabled : SeatStatus.Available;

        var ss = await _repo.GetShowtimeSeatAsync(showtimeId, seatId)
                 ?? throw new NotFoundException("Ghế không thuộc suất chiếu này");

        if (ss.Status is SeatStatus.Held or SeatStatus.Booked
            || await _repo.ShowtimeSeatHasTicketAsync(ss.ShowtimeSeatID))
            throw new ConflictException("Ghế đang được giữ hoặc đã gắn với vé nên không thể đổi trạng thái");

        ss.Status = newStatus;
        await _repo.SaveAsync();

        var fresh = (await _repo.GetShowtimeSeatsAsync(showtimeId)).First(x => x.SeatID == seatId);
        return ToSeatItem(fresh);
    }

    // ---------- helpers ----------
    private async Task<Showtime> LoadAsync(int id)
    {
        var st = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy suất chiếu");
        _current.EnsureCinemaAccess(st.Room?.CinemaID);
        return st;
    }

    private static DateTime? StartOf(Showtime s) =>
        s.Date.HasValue && s.StartHour.HasValue ? s.Date.Value.ToDateTime(s.StartHour.Value) : null;

    /// <summary>Không cho 2 suất chiếu trong cùng phòng chồng lên nhau (tính theo giờ bắt đầu + thời lượng phim).</summary>
    private async Task EnsureNoOverlapAsync(int roomId, DateTime start, int durationMinutes, int? exceptShowtimeId)
    {
        var end = start.AddMinutes(durationMinutes);
        var from = DateOnly.FromDateTime(start.AddDays(-1));
        var to = DateOnly.FromDateTime(start.AddDays(1));

        var others = await _repo.GetRoomShowtimesAsync(roomId, from, to, exceptShowtimeId);
        foreach (var o in others)
        {
            var oStart = StartOf(o);
            if (oStart == null) continue;
            var oEnd = oStart.Value.AddMinutes(o.Movie?.Duration ?? 0);

            if (start < oEnd && oStart.Value < end)
                throw new ConflictException(
                    $"Trùng lịch với suất chiếu #{o.ShowtimeID} (phim \"{o.Movie?.Title}\", " +
                    $"{oStart.Value:HH:mm}–{oEnd:HH:mm} ngày {oStart.Value:dd/MM/yyyy}) trong cùng phòng");
        }
    }

    private static SeatMapItemDto ToSeatItem(ShowtimeSeat x) => new()
    {
        SeatId = x.SeatID ?? 0,
        Row = x.Seat?.Row,
        Column = x.Seat?.Column,
        SeatCode = (x.Seat?.Row ?? "") + (x.Seat?.Column ?? ""),
        SeatType = x.Seat?.SeatType,
        ShowtimeSeatId = x.ShowtimeSeatID,
        Status = x.Status,
        Price = x.Price
    };

    private static ShowtimeResponseDto ToDto(Showtime s, int? bookedTickets = null)
    {
        var start = StartOf(s);
        DateTime? end = start?.AddMinutes(s.Movie?.Duration ?? 0);

        return new ShowtimeResponseDto
        {
            ShowtimeID = s.ShowtimeID,
            CinemaID = s.Room?.CinemaID,
            CinemaName = s.Room?.Cinema?.CinemaName,
            RoomID = s.RoomID,
            RoomName = s.Room?.RoomName,
            MovieID = s.MovieID,
            MovieTitle = s.Movie?.Title,
            Date = s.Date,
            StartTime = s.StartHour,
            StartAt = start,
            EndAt = end,
            BasePrice = s.BasePrice,
            Status = s.Status,
            BookedTickets = bookedTickets
        };
    }
}
