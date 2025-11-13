using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace BitNow_Backend.Services
{
    public interface IFileUploadService
    {
        Task<string> SaveImageAsync(IFormFile file, string subfolder = "items");
        Task<List<string>> SaveImagesAsync(List<IFormFile> files, string subfolder = "items");
        bool DeleteImage(string imagePath);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly string _rootPath;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long _maxFileSize = 10 * 1024 * 1024; // 10MB


        public FileUploadService(IWebHostEnvironment environment)
        {
            // Lưu ảnh vào root của server (ContentRootPath)
            _rootPath = environment.ContentRootPath;

            // Create uploads directory if it doesn't exist
            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
            }
        }

        public async Task<string> SaveImageAsync(IFormFile file, string subfolder = "items")
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

            
            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            // Lưu trực tiếp vào root của server
            var filePath = Path.Combine(_rootPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return filename (ảnh được lưu ở root)
            return fileName;
        }

        public async Task<List<string>> SaveImagesAsync(List<IFormFile> files, string subfolder = "items")
        {
            var savedPaths = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var path = await SaveImageAsync(file, subfolder);
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
                // Ảnh được lưu ở root, nên chỉ cần combine với root path
                var fullPath = Path.Combine(_rootPath, imagePath);

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