using Backend.Common;
using Backend.DTOs;
using Backend.Repositories;

namespace Backend.Services;

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
