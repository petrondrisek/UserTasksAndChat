using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserTasksAndChat.Services;

namespace UserTasksAndChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController(
        IFileValidationService _fileValidationService,
        IFilesService _fileService,
        ILogger<FileController> _logger
    ) : ControllerBase
    {
        [HttpGet("{fileName}")]
        [Authorize]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (!_fileValidationService.IsValidFileName(fileName))
            {
                _logger.LogWarning("Invalid file name requested: {FileName}", fileName);
                return NotFound(new { message = "File not found" });
            }

            try
            {
                var fileResult = await _fileService.GetFileAsync(fileName);
                return File(fileResult.Content, fileResult.ContentType, fileName);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("File not found: {FileName}", fileName);
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("stream/{fileName}")]
        [Authorize]
        public IActionResult DownloadFileStream(string fileName)
        {
            if (!_fileValidationService.IsValidFileName(fileName))
            {
                _logger.LogWarning("Invalid file name requested for streaming: {FileName}", fileName);
                return NotFound(new { message = "File not found" });
            }

            try
            {
                var fileStream = _fileService.GetFileStream(fileName);
                var contentType = _fileService.GetContentType(fileName);

                return File(fileStream, contentType, fileName, enableRangeProcessing: true);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("File not found for streaming: {FileName}", fileName);
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming file: {FileName}", fileName);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("info/{fileName}")]
        [Authorize]
        public IActionResult GetFileInfo(string fileName)
        {
            if (!_fileValidationService.IsValidFileName(fileName))
            {
                _logger.LogWarning("Invalid file name requested for info: {FileName}", fileName);
                return NotFound(new { message = "File not found" });
            }

            try
            {
                var fileInfo = _fileService.GetFileInfo(fileName);
                return Ok(fileInfo);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("File info not found: {FileName}", fileName);
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info: {FileName}", fileName);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}