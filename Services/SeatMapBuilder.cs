using Backend.DTOs;

namespace Backend.Services;

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
