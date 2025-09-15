using Microsoft.Extensions.Options;

namespace UserTasksAndChat.Services
{
    public interface IFileValidationService
    {
        bool IsValidFileName(string fileName);
        bool IsValidFilePath(string filePath);
    }

    public class FileValidationService : IFileValidationService
    {
        private readonly FileStorageOptions _storageOptions;
        private readonly ILogger<FileValidationService> _logger;

        public FileValidationService(IOptions<FileStorageOptions> storageOptions, ILogger<FileValidationService> logger)
        {
            _storageOptions = storageOptions.Value;
            _logger = logger;
        }

        public bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            char[] invalidChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                fileName.Contains(".."))
            {
                return false;
            }

            var filePath = Path.Combine(_storageOptions.BasePath, fileName);
            return IsValidFilePath(filePath);
        }

        public bool IsValidFilePath(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var fullFilePath = Path.GetFullPath(filePath);
                var fullBasePath = Path.GetFullPath(_storageOptions.BasePath);

                return fullFilePath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating file path: {FilePath}", filePath);
                return false;
            }
        }
    }
}
