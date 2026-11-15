using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IGenreService
{
    Task<List<GenreDto>> GetAllAsync();
    Task<GenreDto> GetAsync(int id);
    Task<GenreDto> CreateAsync(GenreUpsertDto dto);
    Task<GenreDto> UpdateAsync(int id, GenreUpsertDto dto);
    Task DeleteAsync(int id);
}

public class GenreService : IGenreService
{
    private readonly IGenreRepository _repo;

    public GenreService(IGenreRepository repo) => _repo = repo;

    public async Task<List<GenreDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).Select(ToDto).ToList();

    public async Task<GenreDto> GetAsync(int id) =>
        ToDto(await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy thể loại"));

    public async Task<GenreDto> CreateAsync(GenreUpsertDto dto)
    {
        var name = dto.GenreName.Trim();
        if (await _repo.NameExistsAsync(name))
            throw new ConflictException($"Thể loại \"{name}\" đã tồn tại");

        var genre = new Genre { GenreName = name };
        await _repo.AddAsync(genre);
        return ToDto(genre);
    }

    public async Task<GenreDto> UpdateAsync(int id, GenreUpsertDto dto)
    {
        var genre = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy thể loại");
        var name = dto.GenreName.Trim();
        if (await _repo.NameExistsAsync(name, id))
            throw new ConflictException($"Thể loại \"{name}\" đã tồn tại");

        genre.GenreName = name;
        await _repo.SaveAsync();
        return ToDto(genre);
    }

    public async Task DeleteAsync(int id)
    {
        var genre = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy thể loại");
        if (await _repo.HasMoviesAsync(id))
            throw new ConflictException("Thể loại đang được gán cho phim nên không thể xóa");
        await _repo.RemoveAsync(genre);
    }

    private static GenreDto ToDto(Genre g) => new() { GenreID = g.GenreID, GenreName = g.GenreName };
}
