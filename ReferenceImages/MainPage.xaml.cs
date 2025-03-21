using System.ComponentModel;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Platform;

namespace ReferenceImages;

public partial class MainPage
{
    private const string preferenceKeyDirectoryPath = nameof(preferenceKeyDirectoryPath);
    private const string preferenceKeyImagePath = nameof(preferenceKeyImagePath);
    private static readonly string[] ValidImageFileExtensions = ["jpeg", "jpg", "png"];

    private FileInfo? imageFileInfo;
    private DirectoryInfo? directoryInfo;
    private readonly IFolderPicker folderPicker;
    private readonly List<string> imagePaths = [];
    private int imageIndex = -1;
    private IDispatcherTimer? timer;
    private int sketchTimeLeft;

    public MainPage(IFolderPicker folderPicker)
    {
        InitializeComponent();
        this.folderPicker = folderPicker;

        Loaded += (_, _) =>
        {
            LoadLastUsedDirectory();
            LoadLastUsedImage();

            Settings.Default.PropertyChanged += SettingsOnPropertyChanged;
            timer = Application.Current?.Dispatcher.CreateTimer();
            if (timer == null) return;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.IsRepeating = true;
            timer.Tick += TimerOnTick;
            sketchTimeLeft = Settings.Default.SketchTimeInSeconds;
            UpdateLabelSketchTimeLeft();
        };
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.EnforceProhibitedWordsInPaths) or nameof(Settings.ProhibitedWordsInPaths):
            {
                imagePaths.Clear();
                LoadImagePaths();
                if (!IsValidFile(imageFileInfo)) LoadRandomImage();
                break;
            }
            case nameof(Settings.SketchTimerMinutes) or nameof(Settings.SketchTimerSeconds):
            {
                sketchTimeLeft = Settings.Default.SketchTimeInSeconds;
                UpdateLabelSketchTimeLeft();
                break;
            }
        }
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
        if (directoryInfo is not { Exists: true }) return;

        if (!LoadImage(GetFirstImageFile()?.FullName)) Helper.DisplayAlert("No image found");
    }

    private FileInfo? GetFirstImageFile() => directoryInfo?
        .EnumerateFileSystemInfos("", SearchOption.AllDirectories)
        .OfType<FileInfo>()
        .FirstOrDefault(IsValidFile);

    private void LoadImagePaths()
    {
        if (directoryInfo is not { Exists:true } || imagePaths.Count > 0) return;
        imagePaths.AddRange(directoryInfo
            .EnumerateFileSystemInfos("", SearchOption.AllDirectories)
            .OfType<FileInfo>()
            .Where(IsValidFile)
            .Select(info => info.FullName)
        );
    }

    private void LoadRandomImage()
    {
        LoadImagePaths();
        LoadImage(imageIndex = Random.Shared.Next(0, imagePaths.Count));
    }

    private bool LoadImage(int index)
    {
        if (index < 0 || index >= imagePaths.Count) return false;
        return LoadImage(imagePaths[index]);
    }

    private bool LoadImage(string? path)
    {
        if (path is null) return false;

        imageFileInfo = new FileInfo(path);
        if (!IsValidFile(imageFileInfo)) return false;

        Image.Source = ImageSource.FromFile(imageFileInfo.FullName);
        LabelImage.Text = Path.GetRelativePath(directoryInfo?.FullName ?? string.Empty, imageFileInfo.FullName);
        Preferences.Set(preferenceKeyImagePath, imageFileInfo.FullName);
        sketchTimeLeft = Settings.Default.SketchTimeInSeconds;
        UpdateLabelSketchTimeLeft();
        return true;
    }

    private static bool IsValidFile(FileInfo? info)
    {
        if (info is null || !info.Exists) return false;
        if (!ValidImageFileExtensions.Contains(info.Extension.Trim('.'))) return false;
        if (Settings.Default.EnforceProhibitedWordsInPaths &&
            Settings.Default.ProhibitedWordsInPaths.Any(word => info.FullName.Contains(word, StringComparison.CurrentCultureIgnoreCase))) return false;
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
        LoadImage(imageIndex);
    }

    private void ButtonRandom_Clicked(object sender, EventArgs e) => LoadRandomImage();

    private async void ButtonSettings_OnClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync($"///{nameof(SettingsPage)}");

    private async Task PickFolderAsync(CancellationToken cancellationToken)
    {
        var result = await folderPicker.PickAsync(cancellationToken);
        if (result is { IsSuccessful: true, Folder.Path: not null })
        {
            LoadDirectory(result.Folder.Path);
            if (!IsSubdirectory(directoryInfo, imageFileInfo?.Directory)) LoadImage(GetFirstImageFile()?.FullName);
        }
    }

    private static bool IsSubdirectory(DirectoryInfo? parent, DirectoryInfo? child)
    {
        if (parent is null || child is null) return false;
        var parentPath = parent.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var childPath = child.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase);
    }

    private void ButtonTimerStart_OnClicked(object? sender, EventArgs e)
    {
        timer?.Start();
        ButtonTimerStart.IsVisible = false;
        ButtonTimerPause.IsVisible = true;
    }

    private void ButtonTimerPause_OnClicked(object? sender, EventArgs e)
    {
        timer?.Stop();
        ButtonTimerPause.IsVisible = false;
        ButtonTimerStart.IsVisible = true;
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        sketchTimeLeft--;
        UpdateLabelSketchTimeLeft();
        if (sketchTimeLeft > 0) return;
        
        LoadRandomImage();
    }

    private void UpdateLabelSketchTimeLeft()
    {
        var timeLeft = TimeSpan.FromSeconds(sketchTimeLeft);
        LabelSketchTimeLeft.Text = $"{timeLeft.Minutes}m {timeLeft.Seconds}s";
    }
}