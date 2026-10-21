using Azure;
using AzureOmsDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureOmsDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class ProductImagesController : ControllerBase
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly ProductImageStorageService _storage;
        private readonly ILogger<ProductImagesController> _logger;

        public ProductImagesController(
            ProductImageStorageService storage,
            ILogger<ProductImagesController> logger)
        {
            _storage = storage;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(
            MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            [FromForm] IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                return BadRequest("Please select an image.");

            if (file.Length > MaxFileSize)
                return BadRequest("Maximum file size is 5 MB.");

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            if (extension is not (".jpg" or ".jpeg" or ".png"))
                return BadRequest("Only JPG and PNG are allowed.");

            // Check the file signature, not only its extension.
            var header = new byte[8];

            await using (var input = file.OpenReadStream())
            {
                var read = await input.ReadAsync(
                    header.AsMemory(), cancellationToken);

                var isJpeg = read >= 3
                    && header[0] == 0xFF
                    && header[1] == 0xD8
                    && header[2] == 0xFF;

                var isPng = read == 8
                    && header.AsSpan().SequenceEqual(
                        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

                var valid = extension switch
                {
                    ".jpg" or ".jpeg" => isJpeg,
                    ".png" => isPng,
                    _ => false
                };

                if (!valid)
                    return BadRequest("Invalid image file.");
            }

            try
            {
                var blobName = await _storage.UploadAsync(
                    file, cancellationToken);

                return Ok(new
                {
                    blobName,
                    container = "product-images",
                    message = "Image uploaded successfully."
                });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(
                    ex, "Azure Blob upload failed.");

                return StatusCode(503,
                    "Blob Storage is currently unavailable.");
            }
        }


        [HttpGet("{blobName}")]
        public async Task<IActionResult> Download(
            string blobName,
            CancellationToken cancellationToken)
        {
            // Chỉ chấp nhận Blob name dạng GUID + phần mở rộng ảnh
            var extension = Path.GetExtension(blobName)
                .ToLowerInvariant();

            var nameWithoutExtension =
                Path.GetFileNameWithoutExtension(blobName);

            if (!Guid.TryParseExact(
                    nameWithoutExtension, "N", out _)
                || extension is not (".jpg" or ".jpeg" or ".png"))
            {
                return BadRequest("Invalid blob name.");
            }

            try
            {
                var result = await _storage.DownloadAsync(
                    blobName, cancellationToken);

                if (result is null)
                    return NotFound("Image not found.");

                var contentType = extension == ".png"
                    ? "image/png"
                    : "image/jpeg";

                return File(result.Content, contentType);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(
                    ex, "Azure Blob download failed.");

                return StatusCode(
                    503, "Blob Storage is currently unavailable.");
            }
        }
    }
}
