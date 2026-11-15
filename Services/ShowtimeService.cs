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

public interface ICinemaService
{
    Task<List<CinemaDto>> GetAllAsync();
}

public class CinemaService : ICinemaService
{
    private readonly ICinemaRepository _repo;
    private readonly ICurrentUser _current;

    public CinemaService(ICinemaRepository repo, ICurrentUser current)
    {
        _repo = repo;
        _current = current;
    }

    public async Task<List<CinemaDto>> GetAllAsync()
    {
        // Admin thấy tất cả; Quản lý/Nhân viên chỉ thấy rạp của mình
        int? filter = _current.IsAdmin ? null : (_current.CinemaId ?? -1);
        var list = await _repo.GetAllAsync(filter);
        return list.Select(c => new CinemaDto
        {
            CinemaID = c.CinemaID,
            CinemaName = c.CinemaName,
            Address = c.Address,
            Status = c.Status
        }).ToList();
    }
}

public interface IRoomService
{
    Task<List<RoomResponseDto>> SearchAsync(int? cinemaId, string? keyword);
    Task<RoomResponseDto> GetAsync(int id);
    Task<RoomResponseDto> CreateAsync(RoomUpsertDto dto);
    Task<RoomResponseDto> UpdateAsync(int id, RoomUpsertDto dto);
    Task DeleteAsync(int id);
}

public class RoomService : IRoomService
{
    private readonly IRoomRepository _rooms;
    private readonly ISeatRepository _seats;
    private readonly ICinemaRepository _cinemas;
    private readonly ICurrentUser _current;

    public RoomService(IRoomRepository rooms, ISeatRepository seats, ICinemaRepository cinemas, ICurrentUser current)
    {
        _rooms = rooms;
        _seats = seats;
        _cinemas = cinemas;
        _current = current;
    }

    public async Task<List<RoomResponseDto>> SearchAsync(int? cinemaId, string? keyword)
    {
        var scope = _current.ScopeCinemaId();
        var rooms = await _rooms.SearchAsync(scope ?? cinemaId, keyword);
        var counts = await _rooms.GetSeatCountsAsync(rooms.Select(r => r.RoomID).ToList());
        return rooms.Select(r => ToDto(r, counts.GetValueOrDefault(r.RoomID))).ToList();
    }

    public async Task<RoomResponseDto> GetAsync(int id)
    {
        var room = await LoadAsync(id);
        return ToDto(room, await _seats.CountByRoomAsync(id));
    }

    public async Task<RoomResponseDto> CreateAsync(RoomUpsertDto dto)
    {
        var scope = _current.ScopeCinemaId();
        if (scope != null && dto.CinemaId != null && dto.CinemaId != scope)
            throw new ForbiddenException("Quản lý chỉ được thêm phòng chiếu vào rạp của mình");

        var cinemaId = scope ?? dto.CinemaId ?? throw new BadRequestException("Vui lòng chọn rạp");
        if (!await _cinemas.ExistsAsync(cinemaId))
            throw new BadRequestException("Rạp được chọn không tồn tại");

        var name = dto.RoomName.Trim();
        if (await _rooms.NameExistsAsync(cinemaId, name, null))
            throw new ConflictException($"Rạp đã có phòng tên \"{name}\"");

        var room = new Room
        {
            RoomName = name,
            CinemaID = cinemaId,
            Capacity = dto.Capacity,
            Description = dto.Description?.Trim(),
            Status = NormalizeStatus(dto.Status)
        };
        await _rooms.AddAsync(room);
        return await GetAsync(room.RoomID);
    }

    public async Task<RoomResponseDto> UpdateAsync(int id, RoomUpsertDto dto)
    {
        var room = await LoadAsync(id);
        var name = dto.RoomName.Trim();

        if (await _rooms.NameExistsAsync(room.CinemaID, name, id))
            throw new ConflictException($"Rạp đã có phòng tên \"{name}\"");

        var seatCount = await _seats.CountByRoomAsync(id);
        if (dto.Capacity.HasValue && dto.Capacity < seatCount)
            throw new ConflictException($"Sức chứa ({dto.Capacity}) nhỏ hơn số ghế hiện có ({seatCount})");

        room.RoomName = name;
        room.Capacity = dto.Capacity;
        room.Description = dto.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Status)) room.Status = NormalizeStatus(dto.Status);

        await _rooms.SaveAsync();
        return ToDto(room, seatCount);
    }

    /// <summary>Xóa vĩnh viễn khỏi database (kèm toàn bộ ghế của phòng). Bị chặn nếu phòng đã có suất chiếu.</summary>
    public async Task DeleteAsync(int id)
    {
        var room = await LoadAsync(id);
        if (await _rooms.HasShowtimesAsync(id))
            throw new ConflictException("Phòng chiếu đã có suất chiếu nên không thể xóa. Hãy xử lý các suất chiếu trước.");
        await _rooms.DeleteAsync(room);
    }

    // ---------- helpers ----------
    private async Task<Room> LoadAsync(int id)
    {
        var room = await _rooms.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy phòng chiếu");
        _current.EnsureCinemaAccess(room.CinemaID);
        return room;
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return RoomStatus.Active;
        if (string.Equals(status, RoomStatus.Active, StringComparison.OrdinalIgnoreCase)) return RoomStatus.Active;
        if (string.Equals(status, RoomStatus.Inactive, StringComparison.OrdinalIgnoreCase)) return RoomStatus.Inactive;
        throw new BadRequestException("Trạng thái phòng phải là Active hoặc Inactive");
    }

    private static RoomResponseDto ToDto(Room r, int seatCount) => new()
    {
        RoomID = r.RoomID,
        RoomName = r.RoomName,
        Status = r.Status,
        CinemaID = r.CinemaID,
        CinemaName = r.Cinema?.CinemaName,
        Capacity = r.Capacity,
        Description = r.Description,
        SeatCount = seatCount
    };
}

public interface ISeatService
{
    Task<List<SeatResponseDto>> ListByRoomAsync(int roomId);
    Task<SeatMapDto> GetRoomSeatMapAsync(int roomId);
    Task<SeatResponseDto> CreateAsync(int roomId, SeatCreateDto dto);
    Task<int> BulkCreateAsync(int roomId, SeatBulkCreateDto dto);
    Task<SeatResponseDto> UpdateAsync(int seatId, SeatCreateDto dto);
    Task DeleteAsync(int seatId);
}

public class SeatService : ISeatService
{
    private readonly ISeatRepository _seats;
    private readonly IRoomRepository _rooms;
    private readonly ICurrentUser _current;

    public SeatService(ISeatRepository seats, IRoomRepository rooms, ICurrentUser current)
    {
        _seats = seats;
        _rooms = rooms;
        _current = current;
    }

    public async Task<List<SeatResponseDto>> ListByRoomAsync(int roomId)
    {
        await LoadRoomAsync(roomId);
        var seats = await _seats.GetByRoomAsync(roomId);
        return SortSeats(seats).Select(ToDto).ToList();
    }

    public async Task<SeatMapDto> GetRoomSeatMapAsync(int roomId)
    {
        var room = await LoadRoomAsync(roomId);
        var seats = await _seats.GetByRoomAsync(roomId);
        if (seats.Count == 0)
            throw new NotFoundException("Phòng chiếu chưa được thiết lập sơ đồ ghế");

        var items = seats.Select(s => new SeatMapItemDto
        {
            SeatId = s.SeatID,
            Row = s.Row,
            Column = s.Column,
            SeatCode = (s.Row ?? "") + (s.Column ?? ""),
            SeatType = s.SeatType
        }).ToList();

        return SeatMapBuilder.Build(roomId, room.RoomName, null, items);
    }

    public async Task<SeatResponseDto> CreateAsync(int roomId, SeatCreateDto dto)
    {
        var room = await LoadRoomAsync(roomId);
        var row = dto.Row.Trim().ToUpperInvariant();
        var col = dto.Column.Trim();

        if (await _seats.ExistsAsync(roomId, row, col, null))
            throw new ConflictException($"Ghế {row}{col} đã tồn tại trong phòng");

        if (room.Capacity.HasValue && await _seats.CountByRoomAsync(roomId) >= room.Capacity)
            throw new ConflictException($"Phòng đã đủ {room.Capacity} ghế theo sức chứa");

        var seat = new Seat { RoomID = roomId, Row = row, Column = col, SeatType = dto.SeatType?.Trim() };
        await _seats.AddRangeAsync(new[] { seat });
        return ToDto(seat);
    }

    public async Task<int> BulkCreateAsync(int roomId, SeatBulkCreateDto dto)
    {
        var room = await LoadRoomAsync(roomId);
        var existing = await _seats.GetByRoomAsync(roomId);
        var taken = existing.Select(s => (s.Row ?? "") + "|" + (s.Column ?? "")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<Seat>();
        for (var r = 0; r < dto.RowCount; r++)
        {
            var row = ((char)('A' + r)).ToString();
            for (var c = 1; c <= dto.SeatsPerRow; c++)
            {
                var col = c.ToString();
                if (taken.Contains(row + "|" + col)) continue;   // bỏ qua ghế đã có
                toAdd.Add(new Seat { RoomID = roomId, Row = row, Column = col, SeatType = dto.SeatType?.Trim() });
            }
        }

        if (toAdd.Count == 0) return 0;

        if (room.Capacity.HasValue && existing.Count + toAdd.Count > room.Capacity)
            throw new ConflictException(
                $"Thêm {toAdd.Count} ghế sẽ vượt sức chứa {room.Capacity} (hiện có {existing.Count} ghế)");

        await _seats.AddRangeAsync(toAdd);
        return toAdd.Count;
    }

    public async Task<SeatResponseDto> UpdateAsync(int seatId, SeatCreateDto dto)
    {
        var seat = await _seats.GetByIdAsync(seatId) ?? throw new NotFoundException("Không tìm thấy ghế");
        _current.EnsureCinemaAccess(seat.Room?.CinemaID);

        var row = dto.Row.Trim().ToUpperInvariant();
        var col = dto.Column.Trim();
        if (seat.RoomID.HasValue && await _seats.ExistsAsync(seat.RoomID.Value, row, col, seatId))
            throw new ConflictException($"Ghế {row}{col} đã tồn tại trong phòng");

        seat.Row = row;
        seat.Column = col;
        seat.SeatType = dto.SeatType?.Trim();
        await _seats.SaveAsync();
        return ToDto(seat);
    }

    public async Task DeleteAsync(int seatId)
    {
        var seat = await _seats.GetByIdAsync(seatId) ?? throw new NotFoundException("Không tìm thấy ghế");
        _current.EnsureCinemaAccess(seat.Room?.CinemaID);

        if (await _seats.HasShowtimeSeatsAsync(seatId))
            throw new ConflictException("Ghế đã gắn với suất chiếu nên không thể xóa");

        await _seats.RemoveAsync(seat);
    }

    // ---------- helpers ----------
    private async Task<Room> LoadRoomAsync(int roomId)
    {
        var room = await _rooms.GetByIdAsync(roomId) ?? throw new NotFoundException("Không tìm thấy phòng chiếu");
        _current.EnsureCinemaAccess(room.CinemaID);
        return room;
    }

    private static IEnumerable<Seat> SortSeats(IEnumerable<Seat> seats) =>
        seats.OrderBy(s => s.Row, StringComparer.OrdinalIgnoreCase)
             .ThenBy(s => int.TryParse(s.Column, out var n) ? n : int.MaxValue)
             .ThenBy(s => s.Column);

    private static SeatResponseDto ToDto(Seat s) => new()
    {
        SeatID = s.SeatID,
        Row = s.Row,
        Column = s.Column,
        SeatCode = (s.Row ?? "") + (s.Column ?? ""),
        SeatType = s.SeatType,
        RoomID = s.RoomID
    };
}

/// <summary>Gom danh sách ghế thành sơ đồ theo hàng, dùng chung cho sơ đồ phòng và sơ đồ suất chiếu.</summary>
public static class SeatMapBuilder
{
    public static SeatMapDto Build(int roomId, string? roomName, int? showtimeId, List<SeatMapItemDto> items)
    {
        var rows = items
            .GroupBy(i => i.Row ?? "")
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SeatMapRowDto
            {
                Row = g.Key,
                Seats = g.OrderBy(i => int.TryParse(i.Column, out var n) ? n : int.MaxValue)
                         .ThenBy(i => i.Column)
                         .ToList()
            })
            .ToList();

        return new SeatMapDto { RoomId = roomId, RoomName = roomName, ShowtimeId = showtimeId, Rows = rows };
    }
}

public interface IStaffService
{
    Task<List<AccountResponseDto>> SearchAsync(string? keyword, string? status);
    Task<AccountResponseDto> GetAsync(int userId);
    Task<AccountResponseDto> CreateAsync(CreateAccountDto dto);
    Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto);
    Task DeleteAsync(int userId);
}

/// <summary>Admin: toàn chuỗi. Quản lý: chỉ nhân viên thuộc rạp của mình.</summary>
public class StaffService : IStaffService
{
    private readonly IAccountService _accounts;
    private readonly ICurrentUser _current;

    public StaffService(IAccountService accounts, ICurrentUser current)
    {
        _accounts = accounts;
        _current = current;
    }

    public Task<List<AccountResponseDto>> SearchAsync(string? keyword, string? status) =>
        _accounts.SearchAsync(keyword, AppRoles.Staff, status, _current.ScopeCinemaId());

    public Task<AccountResponseDto> GetAsync(int userId) => LoadStaffAsync(userId);

    public Task<AccountResponseDto> CreateAsync(CreateAccountDto dto)
    {
        var scope = _current.ScopeCinemaId();
        if (scope != null && dto.CinemaId != null && dto.CinemaId != scope)
            throw new ForbiddenException("Quản lý chỉ được thêm nhân viên vào rạp của mình");

        dto.Role = AppRoles.Staff;
        dto.CinemaId = scope ?? dto.CinemaId;
        return _accounts.CreateAsync(dto, requirePassword: true);
    }

    public async Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto)
    {
        await LoadStaffAsync(userId);
        if (!_current.IsAdmin) dto.CinemaId = null;   // chỉ Admin được chuyển rạp
        return await _accounts.UpdateAsync(userId, dto);
    }

    public async Task DeleteAsync(int userId)
    {
        await LoadStaffAsync(userId);
        await _accounts.SoftDeleteAsync(userId);
    }

    private async Task<AccountResponseDto> LoadStaffAsync(int userId)
    {
        var dto = await _accounts.GetAsync(userId);
        if (dto.Role != AppRoles.Staff) throw new NotFoundException("Không tìm thấy nhân viên");
        _current.EnsureCinemaAccess(dto.CinemaID);
        return dto;
    }
}
