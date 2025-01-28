using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Graphics.Platform;

namespace ReferenceImages;

public partial class MainPage {

    private const string preferenceKeyDirectoryPath = nameof(preferenceKeyDirectoryPath);
    private const string preferenceKeyImagePath = nameof(preferenceKeyImagePath);
    private static readonly string[] ValidImageFileExtensions = ["jpeg", "jpg", "png"];

    private FileInfo? imageFileInfo;
    private DirectoryInfo? directoryInfo;
    private readonly IFolderPicker folderPicker;
    private readonly List<string> imagePaths = [];
    private int imageIndex = -1;

    public MainPage (IFolderPicker folderPicker) {
        InitializeComponent();

        this.folderPicker = folderPicker;

        LoadLastUsedDirectory();
        LoadLastUsedImage();
    }

    private void LoadLastUsedDirectory()
    {
        var path = Preferences.Get(preferenceKeyDirectoryPath, null) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (LoadDirectory(path)) return;
        LoadDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    }

    private bool LoadDirectory(string path)
    {
        directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists) return false;
        LabelPath.Text = directoryInfo.FullName;
        Preferences.Set(preferenceKeyDirectoryPath, directoryInfo.FullName);
        imagePaths.Clear();
        imageIndex = -1;
        return true;
    }

    private void LoadLastUsedImage()
    {
        var path = Preferences.Get(preferenceKeyImagePath, string.Empty);
        if (path is not "" && LoadImage(path)) return;
        if (directoryInfo is null) return;

        if (!LoadImage(GetFirstImageFile()?.FullName)) DisplayAlert("Error", "No image found", "OK");
    }

    private FileInfo? GetFirstImageFile() => directoryInfo?.EnumerateFileSystemInfos("", SearchOption.AllDirectories).OfType<FileInfo>().FirstOrDefault(info => ValidImageFileExtensions.Contains(info.Extension.Trim('.')));

    private void LoadImagePaths()
    {
        if (directoryInfo == null || imagePaths.Count > 0) return;
        imagePaths.AddRange(directoryInfo.EnumerateFileSystemInfos("", SearchOption.AllDirectories).OfType<FileInfo>().Where(info => ValidImageFileExtensions.Contains(info.Extension.Trim('.'))).Select(info => info.FullName));
    }

    private bool LoadImage (string? path)
    {
        if (path is null) return false;
        
        imageFileInfo = new FileInfo(path);
        if (imageFileInfo is null || !ValidImageFileExtensions.Contains(imageFileInfo.Extension.Trim('.'))) return false;

        Image.Source = ImageSource.FromFile(imageFileInfo.FullName);
        Preferences.Set(preferenceKeyImagePath, imageFileInfo.FullName);
        return true;
    }

    private async void ButtonFolder_Clicked(object sender, EventArgs e)
    {
        await PickFolderAsync(CancellationToken.None);
    }

    private void ButtonPrevious_Clicked(object sender, EventArgs e)
    {
        LoadImagePaths();
        if (imagePaths.Count == 0) return;
        if (imageIndex == -1)
        {
            imageFileInfo ??= GetFirstImageFile();
            if (imageFileInfo is null) return;
            
            imageIndex = imagePaths.IndexOf(imageFileInfo.FullName);
        }

        imageIndex--;
        if (imageIndex < 0) imageIndex = imagePaths.Count - 1;
        LoadImage(imagePaths[imageIndex]);
    }

    private void ButtonNext_Clicked(object sender, EventArgs e)
    {
        LoadImagePaths();
        if (imagePaths.Count == 0) return;
        if (imageIndex == -1)
        {
            imageFileInfo ??= GetFirstImageFile();
            if (imageFileInfo is null) return;
            
            imageIndex = imagePaths.IndexOf(imageFileInfo.FullName);
        }

        imageIndex++;
        if (imageIndex > imagePaths.Count - 1) imageIndex = 0;
        LoadImage(imagePaths[imageIndex]);
    }

    private void ButtonRandom_Clicked(object sender, EventArgs e)
    {
        LoadImagePaths();
        imageIndex = Random.Shared.Next(0, imagePaths.Count);
        LoadImage(imagePaths[imageIndex]);
    }

    private async Task PickFolderAsync(CancellationToken cancellationToken)
    {
        var result = await folderPicker.PickAsync(cancellationToken);
        if (result is { IsSuccessful: true, Folder.Path: not null })
        {
            LoadDirectory(result.Folder.Path);
            if (!IsSubdirectory(directoryInfo, imageFileInfo?.Directory)) LoadImage(GetFirstImageFile()?.FullName); 
        }
        else await DisplayAlert("Error", result.Exception?.Message, "OK");
    }
    
    private static bool IsSubdirectory(DirectoryInfo? parent, DirectoryInfo? child)
    {
        if (parent is null || child is null) return false;
        var parentPath = parent.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var childPath = child.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase);
    }
}