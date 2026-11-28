using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IRoomService
{
    Task<PagedResult<RoomResponseDto>> SearchAsync(int? cinemaId, string? keyword, PageQuery paging, CancellationToken ct = default);
    Task<RoomResponseDto> GetAsync(int id, CancellationToken ct = default);
    Task<RoomResponseDto> CreateAsync(RoomUpsertDto dto, CancellationToken ct = default);
    Task<RoomResponseDto> UpdateAsync(int id, RoomUpsertDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
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

    public async Task<PagedResult<RoomResponseDto>> SearchAsync(int? cinemaId, string? keyword, PageQuery paging, CancellationToken ct = default)
    {
        var scope = _current.ScopeCinemaId();
        var (rooms, total) = await _rooms.SearchAsync(scope ?? cinemaId, keyword, paging, ct: ct);
        var counts = await _rooms.GetSeatCountsAsync(rooms.Select(r => r.RoomID).ToList(), ct: ct);
        var dtos = rooms.Select(r => ToDto(r, counts.GetValueOrDefault(r.RoomID))).ToList();
        return PagedResult<RoomResponseDto>.Create(dtos, total, paging);
    }

    public async Task<RoomResponseDto> GetAsync(int id, CancellationToken ct = default)
    {
        var room = await LoadAsync(id, ct: ct);
        return ToDto(room, await _seats.CountByRoomAsync(id, ct: ct));
    }

    public async Task<RoomResponseDto> CreateAsync(RoomUpsertDto dto, CancellationToken ct = default)
    {
        var scope = _current.ScopeCinemaId();
        if (scope != null && dto.CinemaId != null && dto.CinemaId != scope)
            throw new ForbiddenException("Quản lý chỉ được thêm phòng chiếu vào rạp của mình");

        var cinemaId = scope ?? dto.CinemaId ?? throw new BadRequestException("Vui lòng chọn rạp");
        if (!await _cinemas.ExistsAsync(cinemaId, ct: ct))
            throw new BadRequestException("Rạp được chọn không tồn tại");

        var name = dto.RoomName.Trim();
        if (await _rooms.NameExistsAsync(cinemaId, name, null, ct: ct))
            throw new ConflictException($"Rạp đã có phòng tên \"{name}\"");

        var room = new Room
        {
            RoomName = name,
            CinemaID = cinemaId,
            Status = NormalizeStatus(dto.Status)
        };
        await _rooms.AddAsync(room, ct: ct);
        return await GetAsync(room.RoomID, ct: ct);
    }

    public async Task<RoomResponseDto> UpdateAsync(int id, RoomUpsertDto dto, CancellationToken ct = default)
    {
        var room = await LoadAsync(id, ct: ct);
        var name = dto.RoomName.Trim();

        // BUG-14: không cho chuyển phòng sang rạp khác (ghế, suất chiếu, vé đều gắn theo rạp hiện tại)
        if (dto.CinemaId.HasValue && dto.CinemaId != room.CinemaID)
            throw new ConflictException("Không thể chuyển phòng chiếu sang rạp khác. Hãy tạo phòng mới ở rạp đó.");

        if (await _rooms.NameExistsAsync(room.CinemaID, name, id, ct: ct))
            throw new ConflictException($"Rạp đã có phòng tên \"{name}\"");

        // BUG-14: không cho ngừng hoạt động phòng khi vẫn còn suất chiếu sắp tới
        var newStatus = string.IsNullOrWhiteSpace(dto.Status) ? room.Status : NormalizeStatus(dto.Status);
        if (newStatus == RoomStatus.Inactive && room.Status != RoomStatus.Inactive
            && await _rooms.HasUpcomingShowtimesAsync(id, ct: ct))
            throw new ConflictException(
                "Phòng chiếu còn suất chiếu sắp tới nên không thể chuyển sang Inactive. Hãy hủy hoặc chuyển các suất chiếu đó trước.");

        var seatCount = await _seats.CountByRoomAsync(id, ct: ct);

        room.RoomName = name;
        room.Status = newStatus;

        await _rooms.SaveAsync(ct: ct);
        return ToDto(room, seatCount);
    }

    /// <summary>Xóa vĩnh viễn khỏi database (kèm toàn bộ ghế của phòng). Bị chặn nếu phòng đã có suất chiếu.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var room = await LoadAsync(id, ct: ct);
        if (await _rooms.HasShowtimesAsync(id, ct: ct))
            throw new ConflictException("Phòng chiếu đã có suất chiếu nên không thể xóa. Hãy xử lý các suất chiếu trước.");
        await _rooms.DeleteAsync(room, ct: ct);
    }

    // ---------- helpers ----------
    private async Task<Room> LoadAsync(int id, CancellationToken ct = default)
    {
        var room = await _rooms.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy phòng chiếu");
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
        SeatCount = seatCount
    };
}
