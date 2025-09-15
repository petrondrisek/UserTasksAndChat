using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserTasksAndChat.Dto.File;

namespace UserTasksAndChat.Services
{
    public interface IFilesService
    {
        Task<string[]> UploadMultipleFormFilesAsync(IFormFileCollection formFiles);
        Task<string> UploadFormFileAsync(IFormFile file);
        bool DeleteFile(string filePath);
        bool DeleteMultipleFiles(string[] filePaths);
        FileValidationResultDto ValidateFile(IFormFile file);
        Task<FileResultDto> GetFileAsync(string fileName);
        Stream GetFileStream(string fileName);
        FileInfoDto GetFileInfo(string fileName);
        string GetContentType(string fileName);
    }

    public class FileStorageOptions
    {
        public string BasePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Data", "Files");
        public long MaxFileSize { get; set; } = 50 * 1024 * 1024;
        public string[] AllowedExtensions { get; set; } = [".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".mp4"];
    }

    public class FilesService : IFilesService
    {
        private readonly IFileValidationService _fileValidationService;
        private readonly FileStorageOptions _storageOptions;
        private readonly ILogger<FilesService> _logger;

        private static readonly Dictionary<string, string> ContentTypes = new()
        {
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".txt", "text/plain" },
            { ".csv", "text/csv" },
            { ".json", "application/json" },
            { ".xml", "application/xml" },
            { ".zip", "application/zip" },
            { ".rar", "application/x-rar-compressed" },
            { ".7z", "application/x-7z-compressed" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".svg", "image/svg+xml" },
            { ".mp3", "audio/mpeg" },
            { ".mp4", "video/mp4" },
            { ".avi", "video/x-msvideo" }
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".mp4"
        };

        private const long MaxFileSize = 50 * 1024 * 1024;

        public FilesService(IOptions<FileStorageOptions> storageOptions, ILogger<FilesService> logger, IFileValidationService fileValidationService)
        {
            _storageOptions = storageOptions.Value;
            _logger = logger;
            EnsureUploadDirectoryExists();
            _fileValidationService = fileValidationService;
        }

        public async Task<string[]> UploadMultipleFormFilesAsync(IFormFileCollection formFiles)
        {
            var uploadTasks = formFiles
                .Select(async file =>
                {
                    try
                    {
                        return await UploadFormFileAsync(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to upload file: {FileName}", file.FileName);
                        return null;
                    }
                })
                .ToArray();

            var results = await Task.WhenAll(uploadTasks);
            var resultsSelect = results.Where(r => r is not null).Select(r => r!);
            return [..resultsSelect];
        }

        public async Task<string> UploadFormFileAsync(IFormFile file)
        {
            var validationResult = ValidateFile(file);
            if (!validationResult.IsValid)
                throw new ArgumentException(validationResult.ErrorMessage);

            var uniqueFileName = GenerateUniqueFileName(file.FileName);
            var filePath = Path.Combine(_storageOptions.BasePath, uniqueFileName);

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await file.CopyToAsync(fileStream);

            return Path.GetRelativePath(_storageOptions.BasePath, filePath).Replace('\\', '/');
        }

        public FileValidationResultDto ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return FileValidationResultDto.Invalid("File is empty or null");

            if (file.Length > MaxFileSize)
                return FileValidationResultDto.Invalid($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return FileValidationResultDto.Invalid($"File type '{extension}' is not allowed");

            return FileValidationResultDto.Valid();
        }

        public bool DeleteFile(string filePath)
        {
            try
            {
                var fullPath = GetSecureFilePath(filePath);
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("Attempted to delete non-existent file: {FilePath}", filePath);
                    return false;
                }

                File.Delete(fullPath);
                _logger.LogInformation("Successfully deleted file: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                throw new IOException($"Failed to delete file: {ex.Message}", ex);
            }
        }

        public bool DeleteMultipleFiles(string[] filePaths)
        {
            var successCount = 0;
            foreach (var filePath in filePaths)
            {
                try
                {
                    if (DeleteFile(filePath))
                        Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file: {FilePath}", filePath);
                }
            }

            _logger.LogInformation("Successfully deleted {SuccessCount} out of {TotalCount} files",
                successCount, filePaths.Length);

            return successCount > 0;
        }

        private void EnsureUploadDirectoryExists()
        {
            if (!Directory.Exists(_storageOptions.BasePath))
            {
                Directory.CreateDirectory(_storageOptions.BasePath);
                _logger.LogInformation("Created upload directory: {Path}", _storageOptions.BasePath);
            }
        }

        private static string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomSuffix = Random.Shared.Next(1000, 9999);

            return $"{nameWithoutExtension}_{timestamp}_{randomSuffix}{extension}";
        }

        private string GetSecureFilePath(string filePath)
        {
            string fullPath = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(_storageOptions.BasePath, filePath);

            var normalizedPath = Path.GetFullPath(fullPath);
            var normalizedAllowedDir = Path.GetFullPath(_storageOptions.BasePath);

            if (!normalizedPath.StartsWith(normalizedAllowedDir, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("File path must be within the allowed directory");

            return normalizedPath;
        }

        public async Task<FileResultDto> GetFileAsync(string fileName)
        {
            var filePath = GetValidatedFilePath(fileName);
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(fileName);

            return new FileResultDto(fileBytes, contentType);
        }

        public Stream GetFileStream(string fileName)
        {
            var filePath = GetValidatedFilePath(fileName);
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        }

        public FileInfoDto GetFileInfo(string fileName)
        {
            var filePath = GetValidatedFilePath(fileName);
            var fileInfo = new FileInfo(filePath);

            return new FileInfoDto
            {
                Name = fileInfo.Name,
                Size = fileInfo.Length,
                SizeFormatted = FormatFileSize(fileInfo.Length),
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                Extension = fileInfo.Extension,
                ContentType = GetContentType(fileName)
            };
        }

        public string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return ContentTypes.GetValueOrDefault(extension, "application/octet-stream");
        }

        private string GetValidatedFilePath(string fileName)
        {
            if (!_fileValidationService.IsValidFileName(fileName))
                throw new FileNotFoundException("File not found or invalid");

            return Path.Combine(_storageOptions.BasePath, fileName);
        }

        private static string FormatFileSize(long bytes)
        {
            var suffixes = new [] { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:N1} {suffixes[counter]}";
        }
    }
}
