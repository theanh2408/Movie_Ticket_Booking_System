using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IShowtimeService
{
    Task<PagedResult<ShowtimeResponseDto>> SearchAsync(int? cinemaId, int? movieId, int? roomId, DateOnly? date, string? status, string? keyword, PageQuery paging, CancellationToken ct = default);
    Task<ShowtimeResponseDto> GetAsync(int id, CancellationToken ct = default);
    Task<ShowtimeResponseDto> CreateAsync(CreateShowtimeDto dto, CancellationToken ct = default);
    Task<ShowtimeResponseDto> UpdateStartTimeAsync(int id, UpdateShowtimeTimeDto dto, CancellationToken ct = default);
    Task<ShowtimeResponseDto> UpdateAsync(int id, UpdateShowtimeDto dto, CancellationToken ct = default);
    Task<ShowtimeResponseDto> CancelAsync(int id, CancelShowtimeDto? dto, CancellationToken ct = default);
    Task<SeatMapDto> GetSeatMapAsync(int id, CancellationToken ct = default);
    Task<SeatMapItemDto> SetSeatStatusAsync(int showtimeId, int seatId, string status, CancellationToken ct = default);
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

    public async Task<PagedResult<ShowtimeResponseDto>> SearchAsync(int? cinemaId, int? movieId, int? roomId,
                                                                    DateOnly? date, string? status, string? keyword, PageQuery paging, CancellationToken ct = default)
    {
        var scope = _current.ScopeCinemaId();
        var (items, total) = await _repo.SearchAsync(scope ?? cinemaId, movieId, roomId, date, status, keyword, paging, ct: ct);
        return PagedResult<ShowtimeResponseDto>.Create(items.Select(s => ToDto(s)).ToList(), total, paging);
    }

    public async Task<ShowtimeResponseDto> GetAsync(int id, CancellationToken ct = default)
    {
        var st = await LoadAsync(id, ct: ct);
        return ToDto(st, await _repo.CountActiveTicketsAsync(id, ct: ct));
    }

    public async Task<ShowtimeResponseDto> CreateAsync(CreateShowtimeDto dto, CancellationToken ct = default)
    {
        var room = await _rooms.GetByIdAsync(dto.RoomId, ct: ct) ?? throw new NotFoundException("Phòng chiếu không tồn tại");
        _current.EnsureCinemaAccess(room.CinemaID);

        if (room.Status == RoomStatus.Inactive)
            throw new ConflictException("Phòng chiếu đang ngừng hoạt động");

        // BUG-11: rạp phải đang mở cửa (Opened) thì mới được tạo suất chiếu mới
        if (room.Cinema?.Status != CinemaStatus.Opened)
            throw new ConflictException("Rạp đang đóng cửa (hoặc chưa mở cửa) nên không thể tạo suất chiếu mới");

        var movie = await _movies.GetByIdAsync(dto.MovieId, ct: ct) ?? throw new NotFoundException("Phim không tồn tại");
        if (movie.Duration is null or <= 0)
            throw new ConflictException("Phim chưa có thời lượng nên không thể xếp lịch");
        if (movie.Status is MovieStatus.Ended or MovieStatus.Inactive)
            throw new ConflictException("Phim đã kết thúc hoặc ngừng hiển thị nên không thể xếp lịch");

        var start = dto.Date!.Value.ToDateTime(dto.StartHour!.Value);
        if (start <= AppClock.LocalNow)
            throw new BadRequestException("Thời gian bắt đầu phải ở tương lai");

        var seats = await _seats.GetByRoomAsync(room.RoomID, ct: ct);
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
                Status = ShowtimeSeatStatus.Available,
                Price = dto.BasePrice
            }).ToList()
        };

        // BR08: "kiểm tra trùng lịch" rồi "ghi" phải là một khối không bị xen ngang. Nếu không, 2 request đồng thời
        // cùng thấy phòng trống rồi cùng tạo -> trùng lịch. Mức Serializable khóa cả khoảng dữ liệu vừa đọc,
        // nên request thứ hai phải chờ request thứ nhất xong rồi mới kiểm tra lại (và sẽ bị báo trùng).
        try
        {
            await _repo.ExecuteInTransactionAsync(async () =>
            {
                await EnsureNoOverlapAsync(room.RoomID, start, movie.Duration.Value, null, ct: ct);
                await _repo.AddAsync(showtime, ct: ct);
            }, System.Data.IsolationLevel.Serializable, ct: ct);
        }
        catch (Exception ex) when (ex.GetBaseException() is Microsoft.Data.SqlClient.SqlException { Number: 1205 })
        {
            // 1205 = SQL Server chọn request này làm "nạn nhân" của deadlock; an toàn để client thử lại
            throw new ConflictException("Đang có thao tác xếp lịch khác trong cùng phòng, vui lòng thử lại sau vài giây.");
        }

        return await GetAsync(showtime.ShowtimeID, ct: ct);
    }

    public async Task<ShowtimeResponseDto> UpdateStartTimeAsync(int id, UpdateShowtimeTimeDto dto, CancellationToken ct = default)
    {
        var st = await LoadAsync(id, ct: ct);

        if (st.Status != ShowtimeStatus.Active)
            throw new ConflictException("Chỉ sửa được suất chiếu đang hoạt động");

        var oldStart = StartOf(st) ?? throw new ConflictException("Suất chiếu thiếu thông tin thời gian");
        if (oldStart - AppClock.LocalNow < TimeSpan.FromHours(2))
            throw new ConflictException("Chỉ được sửa thời gian bắt đầu trước giờ chiếu ít nhất 2 tiếng");

        var newStart = dto.Date!.Value.ToDateTime(dto.StartHour!.Value);
        if (newStart <= AppClock.LocalNow)
            throw new BadRequestException("Thời gian bắt đầu mới phải ở tương lai");
        if (newStart == oldStart)
            throw new BadRequestException("Thời gian mới trùng với thời gian hiện tại");

        await EnsureNoOverlapAsync(st.RoomID!.Value, newStart, st.Movie?.Duration ?? 0, st.ShowtimeID, ct: ct);

        st.Date = dto.Date;
        st.StartHour = dto.StartHour;
        await _repo.SaveAsync(ct: ct);

        // TODO: gửi thông báo cho khách hàng đã đặt vé (cần module thông báo/email của nhóm)
        return ToDto(st, await _repo.CountActiveTicketsAsync(id, ct: ct));
    }

    public async Task<ShowtimeResponseDto> UpdateAsync(int id, UpdateShowtimeDto dto, CancellationToken ct = default)
    {
        if (dto.RoomId == null && dto.BasePrice == null && string.IsNullOrWhiteSpace(dto.Status))
            throw new BadRequestException("Không có thông tin nào để cập nhật (RoomId, BasePrice hoặc Status)");

        var st = await LoadAsync(id, ct: ct);
        if (st.Status == ShowtimeStatus.Cancelled)
            throw new ConflictException("Suất chiếu đã bị hủy nên không thể sửa");

        var start = StartOf(st) ?? throw new ConflictException("Suất chiếu thiếu thông tin thời gian");
        if (start <= AppClock.LocalNow)
            throw new ConflictException("Suất chiếu đã bắt đầu hoặc đã diễn ra nên không thể sửa");

        string? newStatus = null;
        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var statusText = dto.Status.Trim();
            if (string.Equals(statusText, ShowtimeStatus.Active, StringComparison.OrdinalIgnoreCase)) newStatus = ShowtimeStatus.Active;
            else if (string.Equals(statusText, ShowtimeStatus.Inactive, StringComparison.OrdinalIgnoreCase)) newStatus = ShowtimeStatus.Inactive;
            else throw new BadRequestException("Trạng thái chỉ nhận Active hoặc Inactive. Muốn hủy suất chiếu hãy dùng POST /api/showtimes/{id}/cancel");
        }

        // Serializable vì đổi phòng phải kiểm tra trùng lịch ở phòng mới rồi mới ghi
        await _repo.ExecuteInTransactionAsync(async () =>
        {
            var seatsNow = await _repo.GetShowtimeSeatsTrackedAsync(id, ct: ct);

            // Tạm ngưng (Inactive) khi đã có vé là không hợp lý: phải dùng chức năng hủy để xử lý vé / hoàn tiền
            if (newStatus == ShowtimeStatus.Inactive && st.Status != ShowtimeStatus.Inactive
                && await _repo.CountActiveTicketsAsync(id, ct: ct) > 0)
                throw new ConflictException("Suất chiếu đã có vé nên không thể tạm ngưng. Hãy dùng chức năng hủy suất chiếu.");

            if (dto.RoomId.HasValue && dto.RoomId != st.RoomID)
            {
                // Ghế của suất chiếu được tạo theo phòng, nên đổi phòng = tạo lại toàn bộ ghế: chỉ làm được khi chưa từng có vé
                if (await _repo.ShowtimeHasAnyTicketAsync(id, ct: ct))
                    throw new ConflictException("Suất chiếu đã (hoặc từng) có vé nên không thể đổi phòng");
                if (seatsNow.Any(x => x.Status == ShowtimeSeatStatus.Held || x.Status == ShowtimeSeatStatus.Booked))
                    throw new ConflictException("Suất chiếu đang có ghế được giữ hoặc đã đặt nên không thể đổi phòng");

                var newRoom = await _rooms.GetByIdAsync(dto.RoomId.Value, ct: ct) ?? throw new NotFoundException("Phòng chiếu không tồn tại");
                _current.EnsureCinemaAccess(newRoom.CinemaID);
                if (newRoom.CinemaID != st.Room?.CinemaID)
                    throw new ConflictException("Chỉ được đổi sang phòng khác trong cùng một rạp");
                if (newRoom.Status == RoomStatus.Inactive)
                    throw new ConflictException("Phòng chiếu mới đang ngừng hoạt động");

                await EnsureNoOverlapAsync(newRoom.RoomID, start, st.Movie?.Duration ?? 0, id, ct: ct);

                var newSeats = await _seats.GetByRoomAsync(newRoom.RoomID, ct: ct);
                if (newSeats.Count == 0)
                    throw new ConflictException("Phòng chiếu mới chưa có ghế");

                var price = dto.BasePrice ?? st.BasePrice;
                _repo.RemoveShowtimeSeats(seatsNow);
                _repo.AddShowtimeSeats(newSeats.Select(seat => new ShowtimeSeat
                {
                    ShowtimeID = id,
                    SeatID = seat.SeatID,
                    Status = ShowtimeSeatStatus.Available,
                    Price = price
                }));

                st.RoomID = newRoom.RoomID;
                st.Room = newRoom;
            }
            else if (dto.BasePrice.HasValue && dto.BasePrice != st.BasePrice)
            {
                // Đổi giá: chỉ áp cho ghế chưa bán; ghế đã giữ/đặt giữ nguyên giá cũ
                foreach (var seat in seatsNow.Where(x => x.Status == ShowtimeSeatStatus.Available || x.Status == ShowtimeSeatStatus.Disabled))
                    seat.Price = dto.BasePrice;
            }

            if (dto.BasePrice.HasValue) st.BasePrice = dto.BasePrice;
            if (newStatus != null) st.Status = newStatus;

            await _repo.SaveAsync(ct: ct);
        }, System.Data.IsolationLevel.Serializable, ct: ct);

        return ToDto(st, await _repo.CountActiveTicketsAsync(id, ct: ct));
    }

    public async Task<ShowtimeResponseDto> CancelAsync(int id, CancelShowtimeDto? dto, CancellationToken ct = default)
    {
        dto ??= new CancelShowtimeDto();   // không gửi body = không phải hủy do sự cố
        var st = await LoadAsync(id, ct: ct);
        if (st.Status == ShowtimeStatus.Cancelled)
            throw new ConflictException("Suất chiếu này đã bị hủy");

        var start = StartOf(st) ?? throw new ConflictException("Suất chiếu thiếu thông tin thời gian nên không thể hủy");

        // Suất đã diễn ra / đang chiếu thì không hủy được, kể cả khi Admin truyền IsIncident = true
        if (start <= AppClock.LocalNow)
            throw new ConflictException("Suất chiếu đã diễn ra hoặc đang chiếu nên không thể hủy (kể cả khi có sự cố).");

        var booked = 0;

        // Toàn bộ thay đổi nằm trong 1 transaction: lỗi ở bước nào thì rollback hết, không để dữ liệu nửa vời
        await _repo.ExecuteInTransactionAsync(async () =>
        {
            var tickets = await _repo.GetActiveTicketsAsync(id, ct: ct);
            booked = tickets.Count;

            // Đã có vé + còn dưới 60 phút: chỉ Admin được hủy khi có sự cố, kèm lý do
            if (booked > 0
                && (start - AppClock.LocalNow).TotalMinutes < 60
                && !(dto.IsIncident && _current.IsAdmin && !string.IsNullOrWhiteSpace(dto.Reason)))
                throw new ConflictException(
                    "Suất chiếu đã có vé và còn dưới 60 phút nên không thể hủy. " +
                    "Chỉ Admin được hủy khi có sự cố không thể khắc phục (IsIncident = true và ghi rõ Reason).");

            // 1. Vé -> Cancelled
            foreach (var t in tickets) t.Status = TicketStatus.Cancelled;

            // 2. Đơn đã thanh toán -> chờ hoàn tiền; đơn chưa thanh toán -> hủy
            foreach (var booking in tickets.Select(t => t.Booking).Where(b => b != null).DistinctBy(b => b.BookingID))
            {
                if (booking.Status == BookingStatus.Confirmed) booking.Status = BookingStatus.RefundPending;
                else if (booking.Status == BookingStatus.Pending) booking.Status = BookingStatus.Cancelled;
            }

            // 3. Giải phóng toàn bộ ghế của suất chiếu (ghế đang bị khóa Disabled thì giữ nguyên)
            foreach (var seat in await _repo.GetShowtimeSeatsTrackedAsync(id, ct: ct))
                if (seat.Status != ShowtimeSeatStatus.Disabled) seat.Status = ShowtimeSeatStatus.Available;

            // 4. Cuối cùng mới đổi trạng thái suất chiếu
            st.Status = ShowtimeStatus.Cancelled;
            await _repo.SaveAsync(ct: ct);
        }, ct: ct);

        // TODO: hoàn tiền thực tế cho các đơn RefundPending do module thanh toán xử lý (Payment không bị sửa ở đây)
        return ToDto(st, booked);
    }

    public async Task<SeatMapDto> GetSeatMapAsync(int id, CancellationToken ct = default)
    {
        var st = await LoadAsync(id, ct: ct);
        var seats = await _repo.GetShowtimeSeatsAsync(id, ct: ct);
        if (seats.Count == 0)
            throw new NotFoundException("Suất chiếu chưa có sơ đồ ghế");

        var items = seats.Select(x => ToSeatItem(x)).ToList();
        return SeatMapBuilder.Build(st.RoomID ?? 0, st.Room?.RoomName, id, items);
    }

    public async Task<SeatMapItemDto> SetSeatStatusAsync(int showtimeId, int seatId, string status, CancellationToken ct = default)
    {
        await LoadAsync(showtimeId, ct: ct);

        var newStatus = status.Trim();
        if (!string.Equals(newStatus, ShowtimeSeatStatus.Available, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(newStatus, ShowtimeSeatStatus.Disabled, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException($"Chỉ được đổi ghế sang {ShowtimeSeatStatus.Available} hoặc {ShowtimeSeatStatus.Disabled}");
        newStatus = string.Equals(newStatus, ShowtimeSeatStatus.Disabled, StringComparison.OrdinalIgnoreCase)
            ? ShowtimeSeatStatus.Disabled : ShowtimeSeatStatus.Available;

        var ss = await _repo.GetShowtimeSeatAsync(showtimeId, seatId, ct: ct)
                 ?? throw new NotFoundException("Ghế không thuộc suất chiếu này");

        if (ss.Status is ShowtimeSeatStatus.Held or ShowtimeSeatStatus.Booked
            || await _repo.ShowtimeSeatHasTicketAsync(ss.ShowtimeSeatID, ct: ct))
            throw new ConflictException("Ghế đang được giữ hoặc đã gắn với vé nên không thể đổi trạng thái");

        ss.Status = newStatus;
        await _repo.SaveAsync(ct: ct);

        var fresh = (await _repo.GetShowtimeSeatsAsync(showtimeId, ct: ct)).First(x => x.SeatID == seatId);
        return ToSeatItem(fresh);
    }

    // ---------- helpers ----------
    private async Task<Showtime> LoadAsync(int id, CancellationToken ct = default)
    {
        var st = await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy suất chiếu");
        _current.EnsureCinemaAccess(st.Room?.CinemaID);
        return st;
    }

    private static DateTime? StartOf(Showtime s) =>
        s.Date.HasValue && s.StartHour.HasValue ? s.Date.Value.ToDateTime(s.StartHour.Value) : null;

    /// <summary>Không cho 2 suất chiếu trong cùng phòng chồng lên nhau (tính theo giờ bắt đầu + thời lượng phim).</summary>
    private async Task EnsureNoOverlapAsync(int roomId, DateTime start, int durationMinutes, int? exceptShowtimeId, CancellationToken ct = default)
    {
        var end = start.AddMinutes(durationMinutes);
        var from = DateOnly.FromDateTime(start.AddDays(-1));
        var to = DateOnly.FromDateTime(start.AddDays(1));

        var others = await _repo.GetRoomShowtimesAsync(roomId, from, to, exceptShowtimeId, ct: ct);
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
