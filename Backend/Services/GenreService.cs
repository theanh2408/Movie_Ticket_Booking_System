using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IGenreService
{
    Task<List<GenreDto>> GetAllAsync(CancellationToken ct = default);
    Task<GenreDto> GetAsync(int id, CancellationToken ct = default);
    Task<GenreDto> CreateAsync(GenreUpsertDto dto, CancellationToken ct = default);
    Task<GenreDto> UpdateAsync(int id, GenreUpsertDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class GenreService : IGenreService
{
    private readonly IGenreRepository _repo;

    public GenreService(IGenreRepository repo) => _repo = repo;

    public async Task<List<GenreDto>> GetAllAsync(CancellationToken ct = default) =>
        (await _repo.GetAllAsync(ct: ct)).Select(ToDto).ToList();

    public async Task<GenreDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDto(await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy thể loại"));

    public async Task<GenreDto> CreateAsync(GenreUpsertDto dto, CancellationToken ct = default)
    {
        var name = dto.GenreName.Trim();
        if (await _repo.NameExistsAsync(name, ct: ct))
            throw new ConflictException($"Thể loại \"{name}\" đã tồn tại");

        var genre = new Genre { GenreName = name };
        await _repo.AddAsync(genre, ct: ct);
        return ToDto(genre);
    }

    public async Task<GenreDto> UpdateAsync(int id, GenreUpsertDto dto, CancellationToken ct = default)
    {
        var genre = await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy thể loại");
        var name = dto.GenreName.Trim();
        if (await _repo.NameExistsAsync(name, id, ct: ct))
            throw new ConflictException($"Thể loại \"{name}\" đã tồn tại");

        genre.GenreName = name;
        await _repo.SaveAsync(ct: ct);
        return ToDto(genre);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var genre = await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy thể loại");
        if (await _repo.HasMoviesAsync(id, ct: ct))
            throw new ConflictException("Thể loại đang được gán cho phim nên không thể xóa");
        await _repo.RemoveAsync(genre, ct: ct);
    }

    private static GenreDto ToDto(Genre g) => new() { GenreID = g.GenreID, GenreName = g.GenreName };
}
