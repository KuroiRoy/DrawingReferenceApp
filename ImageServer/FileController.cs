using Common;
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
    
    [HttpGet("exist")]
    public IActionResult Exists([FromQuery] string path = "")
    {
        if (Path.IsPathRooted(path)) ValidationProblem("Only relative paths supported.");

        var fullPath = Path.Combine(settings.ImagesRootFolder, path);
        return Directory.Exists(fullPath) ? Ok() : NotFound();
    }

    [HttpGet("list")]
    public IActionResult ListFiles([FromQuery] string path = "", [FromQuery] bool folders = true, [FromQuery] bool files = true)
    {
        var fullPath = Path.Combine(settings.ImagesRootFolder, path);
        if (!Directory.Exists(fullPath)) return NotFound();

        var result = new FileListResponse
        {
            Path = path,
            Folders = !folders ? [] : Directory.GetDirectories(fullPath).Select(p => Path.GetRelativePath(settings.ImagesRootFolder, p)).ToList(),
            Files = !files ? [] : Directory.GetFiles(fullPath).Select(p => Path.GetRelativePath(settings.ImagesRootFolder, p)).ToList()
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