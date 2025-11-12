using Microsoft.AspNetCore.Http;

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
            _rootPath = Path.Combine(environment.ContentRootPath, "uploads");

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

            // Create subfolder if it doesn't exist
            var folderPath = Path.Combine(_rootPath, subfolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path from root (e.g., "uploads/items/guid.jpg")
            // This path will be accessible via /uploads/items/guid.jpg
            return Path.Combine("uploads", subfolder, fileName).Replace("\\", "/");
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
                // If path is relative, combine with root path
                var fullPath = imagePath.StartsWith("uploads")
                    ? Path.Combine(_rootPath, "..", imagePath)
                    : imagePath;

                fullPath = Path.GetFullPath(fullPath);

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