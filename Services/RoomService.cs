using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

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
