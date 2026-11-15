using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

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
