using Microsoft.AspNetCore.Http;

namespace Backend.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Lưu ảnh poster vào thư mục wwwroot/uploads/postersURL
    /// </summary>
    /// <param name="file">File ảnh tải lên</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Đường dẫn tương đối: /uploads/postersURL/{fileName}</returns>
    Task<string> SavePosterAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu video trailer vào thư mục wwwroot/uploads/trailersURL
    /// </summary>
    /// <param name="file">File video tải lên</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Đường dẫn tương đối: /uploads/trailersURL/{fileName}</returns>
    Task<string> SaveTrailerAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa file vật lý khỏi server nếu là file cục bộ (/uploads/...)
    /// Bỏ qua nếu là đường dẫn ngoài (http/https).
    /// </summary>
    /// <param name="relativeOrAbsoluteUrl">Đường dẫn file</param>
    /// <returns>True nếu xóa thành công, False nếu bỏ qua hoặc không tìm thấy</returns>
    bool DeleteFile(string? relativeOrAbsoluteUrl);
}
