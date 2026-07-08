using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/upload")]
[Produces("application/json")]
public class UploadController : ControllerBase
{
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png"];
    private const long MaxFileSize = 10 * 1024 * 1024;

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Загрузить изображение для новости (только Admin)")]
    [SwaggerResponse(200, "Изображение загружено", typeof(Result<UploadResponse>))]
    [SwaggerResponse(400, "Некорректный файл", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<UploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<Result<UploadResponse>>> Upload(
        [FromForm] IFormFile file,
        CancellationToken ct
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest(Result<UploadResponse>.Fail("Файл не выбран", 400));

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return BadRequest(Result<UploadResponse>.Fail("Разрешены только JPEG и PNG", 400));

        var fileId = Guid.NewGuid();
        var uploadsDir = Path.Combine("uploads", "news");
        Directory.CreateDirectory(uploadsDir);

        var outputPath = Path.Combine(uploadsDir, $"{fileId}.jpg");

        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream, ct);

        if (image.Width > 1200)
        {
            var ratio = (double)1200 / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(1200, newHeight));
        }

        if (image.Height > 600)
        {
            var cropY = (image.Height - 600) / 2;
            image.Mutate(x => x.Crop(new Rectangle(0, cropY, image.Width, 600)));
        }
        else if (image.Height < 600)
        {
            image.Mutate(x =>
                x.Resize(new ResizeOptions
                {
                    Size = new Size(image.Width, 600),
                    Mode = ResizeMode.BoxPad,
                    PadColor = Color.White,
                })
            );
        }

        var encoder = new JpegEncoder { Quality = 85 };
        await image.SaveAsync(outputPath, encoder, ct);

        var url = $"/uploads/news/{fileId}.jpg";

        return Ok(Result<UploadResponse>.Ok(new UploadResponse { Url = url }));
    }
}
