using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Backend.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _webRootPath;
    private readonly string _posterDirectory;
    private readonly string _trailerDirectory;

    private static readonly HashSet<string> AllowedPosterExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedTrailerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov"
    };

    public const long MaxPosterSizeBytes = 5 * 1024 * 1024;     // 5 MB
    public const long MaxTrailerSizeBytes = 100 * 1024 * 1024;  // 100 MB

    public FileStorageService(IWebHostEnvironment environment)
    {
        _webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(_webRootPath))
        {
            _webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        _posterDirectory = Path.Combine(_webRootPath, "uploads", "postersURL");
        _trailerDirectory = Path.Combine(_webRootPath, "uploads", "trailersURL");

        if (!Directory.Exists(_posterDirectory))
        {
            Directory.CreateDirectory(_posterDirectory);
        }

        if (!Directory.Exists(_trailerDirectory))
        {
            Directory.CreateDirectory(_trailerDirectory);
        }
    }

    public async Task<string> SavePosterAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File ảnh poster không hợp lệ hoặc rỗng.");
        }

        if (file.Length > MaxPosterSizeBytes)
        {
            throw new ArgumentException($"Dung lượng ảnh poster vượt quá giới hạn 5MB (Thực tế: {file.Length / (1024.0 * 1024.0):F2}MB).");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedPosterExtensions.Contains(extension))
        {
            throw new ArgumentException($"Định dạng ảnh '{extension}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedPosterExtensions)}.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(_posterDirectory, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/postersURL/{fileName}";
    }

    public async Task<string> SaveTrailerAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File video trailer không hợp lệ hoặc rỗng.");
        }

        if (file.Length > MaxTrailerSizeBytes)
        {
            throw new ArgumentException($"Dung lượng video trailer vượt quá giới hạn 100MB (Thực tế: {file.Length / (1024.0 * 1024.0):F2}MB).");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedTrailerExtensions.Contains(extension))
        {
            throw new ArgumentException($"Định dạng video '{extension}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedTrailerExtensions)}.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(_trailerDirectory, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/trailersURL/{fileName}";
    }

    public bool DeleteFile(string? relativeOrAbsoluteUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
        {
            return false;
        }

        // Bỏ qua nếu là link ngoài (YouTube, TMDB, v.v.)
        if (relativeOrAbsoluteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeOrAbsoluteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Chỉ xóa file nếu thuộc thư mục /uploads/
        var normalized = relativeOrAbsoluteUrl.TrimStart('/');
        if (!normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var physicalPath = Path.Combine(_webRootPath, normalized.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                return true;
            }
        }
        catch
        {
            // Nuốt lỗi IO khi xóa file để không làm gián đoạn nghiệp vụ DB
        }

        return false;
    }
}
