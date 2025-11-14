using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;

namespace BitNow_Backend.Services
{
    public interface IFileUploadService
    {
        Task<string> SaveImageAsync(IFormFile file, string? productName = null, int index = 0);
        Task<List<string>> SaveImagesAsync(List<IFormFile> files, string? productName = null);
        bool DeleteImage(string imagePath);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly string _rootPath;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long _maxFileSize = 10 * 1024 * 1024; // 10MB

        public FileUploadService(IWebHostEnvironment environment)
        {
            // Lưu ảnh vào wwwroot/uploads
            // Tạo path: wwwroot/uploads từ ContentRootPath
            var wwwrootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
            _rootPath = Path.Combine(wwwrootPath, "uploads");
            
            // Đảm bảo thư mục wwwroot và uploads tồn tại
            if (!Directory.Exists(wwwrootPath))
            {
                Directory.CreateDirectory(wwwrootPath);
            }
            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
            }
        }

        /// <summary>
        /// Sanitize tên sản phẩm để làm tên file hợp lệ
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            // Loại bỏ ký tự đặc biệt, chỉ giữ chữ, số, space, dash, underscore
            var sanitized = Regex.Replace(fileName, @"[^a-zA-Z0-9\s\-_]", "");
            // Thay space bằng dash
            sanitized = Regex.Replace(sanitized, @"\s+", "-");
            // Loại bỏ dash ở đầu và cuối
            sanitized = sanitized.Trim('-');
            // Giới hạn độ dài tối đa 50 ký tự
            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);
            // Chuyển về lowercase
            return sanitized.ToLowerInvariant();
        }

        public async Task<string> SaveImageAsync(IFormFile file, string? productName = null, int index = 0)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new ArgumentException($"File extension {extension} is not allowed. Allowed: {string.Join(", ", _allowedExtensions)}");

            // Validate file size
            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

            // Generate filename dựa trên tên sản phẩm
            string fileName;
            if (!string.IsNullOrWhiteSpace(productName))
            {
                var sanitizedProductName = SanitizeFileName(productName);
                // Thêm index nếu có nhiều ảnh
                var indexSuffix = index > 0 ? $"-{index}" : "";
                // Thêm GUID ngắn để đảm bảo unique
                var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
                fileName = $"{sanitizedProductName}{indexSuffix}-{uniqueId}{extension}";
            }
            else
            {
                // Fallback: dùng GUID nếu không có tên sản phẩm
                fileName = $"{Guid.NewGuid()}{extension}";
            }
            
            // Lưu vào wwwroot/uploads
            var filePath = Path.Combine(_rootPath, fileName);

            // Kiểm tra nếu file đã tồn tại, thêm timestamp
            if (File.Exists(filePath))
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                fileName = $"{nameWithoutExt}-{timestamp}{extension}";
                filePath = Path.Combine(_rootPath, fileName);
            }

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path từ wwwroot (uploads/filename)
            // Frontend sẽ truy cập qua /images/uploads/filename
            return Path.Combine("uploads", fileName).Replace("\\", "/");
        }

        public async Task<List<string>> SaveImagesAsync(List<IFormFile> files, string? productName = null)
        {
            var savedPaths = new List<string>();

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                try
                {
                    // Truyền index để đánh số ảnh nếu có nhiều ảnh
                    var path = await SaveImageAsync(file, productName, i);
                    savedPaths.Add(path);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other files
                    // In production, you might want to log this properly
                    throw new Exception($"Error saving file {file.FileName}: {ex.Message}", ex);
                }
            }

            return savedPaths;
        }

        public bool DeleteImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return false;

            try
            {
                // imagePath có thể là "uploads/filename" hoặc chỉ "filename"
                string fullPath;
                if (imagePath.StartsWith("uploads/") || imagePath.StartsWith("uploads\\"))
                {
                    // Nếu đã có "uploads/" trong path, chỉ cần combine với wwwroot
                    var wwwrootPath = Path.GetDirectoryName(_rootPath);
                    fullPath = Path.Combine(wwwrootPath!, imagePath.Replace("/", "\\"));
                }
                else
                {
                    // Nếu chỉ có filename, combine với uploads path
                    fullPath = Path.Combine(_rootPath, imagePath);
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

