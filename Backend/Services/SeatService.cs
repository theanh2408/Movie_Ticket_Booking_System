using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface ISeatService
{
    Task<List<SeatResponseDto>> ListByRoomAsync(int roomId, CancellationToken ct = default);
    Task<SeatMapDto> GetRoomSeatMapAsync(int roomId, CancellationToken ct = default);
    Task<SeatResponseDto> CreateAsync(int roomId, SeatCreateDto dto, CancellationToken ct = default);
    Task<int> BulkCreateAsync(int roomId, SeatBulkCreateDto dto, CancellationToken ct = default);
    Task<SeatResponseDto> UpdateAsync(int seatId, SeatCreateDto dto, CancellationToken ct = default);
    Task DeleteAsync(int seatId, CancellationToken ct = default);
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

    public async Task<List<SeatResponseDto>> ListByRoomAsync(int roomId, CancellationToken ct = default)
    {
        await LoadRoomAsync(roomId, ct: ct);
        var seats = await _seats.GetByRoomAsync(roomId, ct: ct);
        return SortSeats(seats).Select(ToDto).ToList();
    }

    public async Task<SeatMapDto> GetRoomSeatMapAsync(int roomId, CancellationToken ct = default)
    {
        var room = await LoadRoomAsync(roomId, ct: ct);
        var seats = await _seats.GetByRoomAsync(roomId, ct: ct);
        if (seats.Count == 0)
            throw new NotFoundException("Phòng chiếu chưa được thiết lập sơ đồ ghế");

        var items = seats.Select(s => new SeatMapItemDto
        {
            SeatId = s.SeatID,
            Row = s.Row,
            Column = s.Column,
            SeatCode = (s.Row ?? "") + (s.Column ?? "")
        }).ToList();

        return SeatMapBuilder.Build(roomId, room.RoomName, null, items);
    }

    public async Task<SeatResponseDto> CreateAsync(int roomId, SeatCreateDto dto, CancellationToken ct = default)
    {
        await LoadRoomAsync(roomId, ct: ct);
        var row = dto.Row.Trim().ToUpperInvariant();
        var col = dto.Column.Trim();

        if (await _seats.ExistsAsync(roomId, row, col, null, ct: ct))
            throw new ConflictException($"Ghế {row}{col} đã tồn tại trong phòng");

        var seat = new Seat { RoomID = roomId, Row = row, Column = col };
        await _seats.AddRangeAsync(new[] { seat }, ct: ct);
        return ToDto(seat);
    }

    public async Task<int> BulkCreateAsync(int roomId, SeatBulkCreateDto dto, CancellationToken ct = default)
    {
        await LoadRoomAsync(roomId, ct: ct);
        var existing = await _seats.GetByRoomAsync(roomId, ct: ct);
        var taken = existing.Select(s => (s.Row ?? "") + "|" + (s.Column ?? "")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<Seat>();
        for (var r = 0; r < dto.RowCount; r++)
        {
            var row = ((char)('A' + r)).ToString();
            for (var c = 1; c <= dto.SeatsPerRow; c++)
            {
                var col = c.ToString();
                if (taken.Contains(row + "|" + col)) continue;   // bỏ qua ghế đã có
                toAdd.Add(new Seat { RoomID = roomId, Row = row, Column = col });
            }
        }

        if (toAdd.Count == 0) return 0;

        await _seats.AddRangeAsync(toAdd, ct: ct);
        return toAdd.Count;
    }

    public async Task<SeatResponseDto> UpdateAsync(int seatId, SeatCreateDto dto, CancellationToken ct = default)
    {
        var seat = await _seats.GetByIdAsync(seatId, ct: ct) ?? throw new NotFoundException("Không tìm thấy ghế");
        _current.EnsureCinemaAccess(seat.Room?.CinemaID);

        var row = dto.Row.Trim().ToUpperInvariant();
        var col = dto.Column.Trim();

        // BUG-07: ghế đã gắn với suất chiếu (và có thể đã có vé/hóa đơn) thì không được đổi mã ghế,
        // nếu không mã ghế trên vé/hóa đơn cũ sẽ bị sai lệch.
        var codeChanged = seat.Row != row || seat.Column != col;
        if (codeChanged && await _seats.HasShowtimeSeatsAsync(seatId, ct: ct))
            throw new ConflictException(
                "Ghế đã gắn với suất chiếu (có thể đã có vé/hóa đơn) nên không được đổi hàng hoặc số ghế.");

        if (seat.RoomID.HasValue && await _seats.ExistsAsync(seat.RoomID.Value, row, col, seatId, ct: ct))
            throw new ConflictException($"Ghế {row}{col} đã tồn tại trong phòng");

        seat.Row = row;
        seat.Column = col;
        await _seats.SaveAsync(ct: ct);
        return ToDto(seat);
    }

    public async Task DeleteAsync(int seatId, CancellationToken ct = default)
    {
        var seat = await _seats.GetByIdAsync(seatId, ct: ct) ?? throw new NotFoundException("Không tìm thấy ghế");
        _current.EnsureCinemaAccess(seat.Room?.CinemaID);

        if (await _seats.HasShowtimeSeatsAsync(seatId, ct: ct))
            throw new ConflictException("Ghế đã gắn với suất chiếu nên không thể xóa");

        await _seats.RemoveAsync(seat, ct: ct);
    }

    // ---------- helpers ----------
    private async Task<Room> LoadRoomAsync(int roomId, CancellationToken ct = default)
    {
        var room = await _rooms.GetByIdAsync(roomId, ct: ct) ?? throw new NotFoundException("Không tìm thấy phòng chiếu");
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
        RoomID = s.RoomID
    };
}
