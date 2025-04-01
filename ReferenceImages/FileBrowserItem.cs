namespace ReferenceImages;

public class FileBrowserItem
{
    public string Icon { get; set; }
    public bool IsDirectory { get; set; }
    public string Name { get; set; }
    public string RelativePath { get; set; }

    public FileBrowserItem(string icon, bool isDirectory, string relativePath, string? name = null) {
        Icon = icon;
        IsDirectory = isDirectory;
        RelativePath = Path.TrimEndingDirectorySeparator(relativePath);
        Name = name ?? Path.GetFileName(RelativePath);
    }
}