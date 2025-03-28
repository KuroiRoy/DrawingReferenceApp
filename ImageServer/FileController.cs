using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ImageServer;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly FileServerSettings settings;
    private readonly ILogger<FileController> logger;

    public FileController(IOptions<FileServerSettings> settings, ILogger<FileController> logger)
    {
        this.settings = settings.Value;
        this.logger = logger;

        // Ensure directory exists or create it
        if (!Directory.Exists(this.settings.ImagesRootFolder)) logger.LogError("Image folder not found: {Folder}", this.settings.ImagesRootFolder);
    }

    [HttpGet("list")]
    public IActionResult ListFiles([FromQuery] string path = "")
    {
        var fullPath = Path.Combine(settings.ImagesRootFolder, path);

        if (!Directory.Exists(fullPath))
            return NotFound();

        var result = new
        {
            Directories = Directory.GetDirectories(fullPath).Select(p => Path.GetRelativePath(settings.ImagesRootFolder, p)),
            Files = Directory.GetFiles(fullPath).Select(p => Path.GetRelativePath(settings.ImagesRootFolder, p))
        };

        return Ok(result);
    }

    [HttpGet("download")]
    public IActionResult DownloadFile([FromQuery] string filePath)
    {
        var fullPath = Path.Combine(settings.ImagesRootFolder, filePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, "application/octet-stream", Path.GetFileName(fullPath));
    }
}