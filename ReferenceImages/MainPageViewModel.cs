using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReferenceImages;

public partial class MainPageViewModel : BaseViewModel {
    private const string preferenceKeyDirectoryPath = nameof(preferenceKeyDirectoryPath);
    private const string preferenceKeyImagePath = nameof(preferenceKeyImagePath);
    private static readonly string[] ValidImageFileExtensions = ["jpeg", "jpg", "png"];

    private FileInfo? imageFileInfo;
    private DirectoryInfo? directoryInfo;
    private readonly FileService fileService;
    private readonly List<string> imagePaths = [];
    private int imageIndex = -1;
    private IDispatcherTimer? timer;
    private int sketchTimeLeft;

    public ImageSource? Image {
        get;
        set => SetField(ref field, value);
    } = null;

    public string ImagePath {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public string FolderPath {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public bool IsTimerRunning {
        get;
        set => SetField(ref field, value);
    }

    public string SketchTimeLeftString {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public MainPageViewModel(FileService fileService) {
        this.fileService = fileService;
    }

    public void Loaded() {
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
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(Settings.EnforceProhibitedWordsInPaths) or nameof(Settings.ProhibitedWordsInPaths): {
                imagePaths.Clear();
                LoadImagePaths();
                if (!IsValidFile(imageFileInfo)) LoadRandomImage();
                break;
            }
            case nameof(Settings.SketchTimerMinutes) or nameof(Settings.SketchTimerSeconds): {
                sketchTimeLeft = Settings.Default.SketchTimeInSeconds;
                UpdateLabelSketchTimeLeft();
                break;
            }
        }
    }

    private void LoadLastUsedDirectory() {
        var path = Preferences.Get(preferenceKeyDirectoryPath, null) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (LoadDirectory(path)) return;
        LoadDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    }

    private bool LoadDirectory(string path) {
        directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists) return false;
        FolderPath = directoryInfo.FullName;
        Preferences.Set(preferenceKeyDirectoryPath, directoryInfo.FullName);
        imagePaths.Clear();
        imageIndex = -1;
        return true;
    }

    private void LoadLastUsedImage() {
        var path = Preferences.Get(preferenceKeyImagePath, string.Empty);
        if (path is not "" && LoadImage(path)) return;
        if (directoryInfo is not { Exists: true }) return;

        if (!LoadImage(GetFirstImageFile()?.FullName)) Helper.DisplayAlert("No image found");
    }

    private FileInfo? GetFirstImageFile() => directoryInfo?
        .EnumerateFileSystemInfos("", SearchOption.AllDirectories)
        .OfType<FileInfo>()
        .FirstOrDefault(IsValidFile);

    private void LoadImagePaths() {
        if (directoryInfo is not { Exists: true } || imagePaths.Count > 0) return;
        imagePaths.AddRange(directoryInfo
            .EnumerateFileSystemInfos("", SearchOption.AllDirectories)
            .OfType<FileInfo>()
            .Where(IsValidFile)
            .Select(info => info.FullName)
        );
    }

    [RelayCommand]
    private void LoadRandomImage() {
        LoadImagePaths();
        LoadImage(imageIndex = Random.Shared.Next(0, imagePaths.Count));
    }

    private void LoadImage(int index) {
        if (index < 0 || index >= imagePaths.Count) return;
        LoadImage(imagePaths[index]);
    }

    private bool LoadImage(string? path) {
        if (path is null) return false;

        imageFileInfo = new FileInfo(path);
        if (!IsValidFile(imageFileInfo)) return false;

        Image = ImageSource.FromFile(imageFileInfo.FullName);
        ImagePath = Path.GetRelativePath(directoryInfo?.FullName ?? string.Empty, imageFileInfo.FullName);
        Preferences.Set(preferenceKeyImagePath, imageFileInfo.FullName);
        sketchTimeLeft = Settings.Default.SketchTimeInSeconds;
        UpdateLabelSketchTimeLeft();
        return true;
    }

    private static bool IsValidFile(FileInfo? info) {
        if (info is null || !info.Exists) return false;
        if (!ValidImageFileExtensions.Contains(info.Extension.Trim('.'))) return false;
        if (Settings.Default.EnforceProhibitedWordsInPaths &&
            Settings.Default.ProhibitedWordsInPaths.Any(word => info.FullName.Contains(word, StringComparison.CurrentCultureIgnoreCase))) return false;
        return true;
    }

    [RelayCommand]
    private async Task OpenFolderPicker() {
        await PickFolderAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void LoadPreviousImage() {
        LoadImagePaths();
        if (imagePaths.Count == 0) return;
        if (imageIndex == -1) {
            imageFileInfo ??= GetFirstImageFile();
            if (imageFileInfo is null) return;

            imageIndex = imagePaths.IndexOf(imageFileInfo.FullName);
        }

        imageIndex--;
        if (imageIndex < 0) imageIndex = imagePaths.Count - 1;
        LoadImage(imagePaths[imageIndex]);
    }

    [RelayCommand]
    private void LoadNextImage() {
        LoadImagePaths();
        if (imagePaths.Count == 0) return;
        if (imageIndex == -1) {
            imageFileInfo ??= GetFirstImageFile();
            if (imageFileInfo is null) return;

            imageIndex = imagePaths.IndexOf(imageFileInfo.FullName);
        }

        imageIndex++;
        if (imageIndex > imagePaths.Count - 1) imageIndex = 0;
        LoadImage(imageIndex);
    }

    [RelayCommand]
    private async Task OpenSettingsPage() => await Shell.Current.GoToAsync($"///{nameof(SettingsPage)}");

    private async Task PickFolderAsync(CancellationToken cancellationToken) {
        var initialPath = directoryInfo is { Exists: true } directory ? directory.FullName : string.Empty;
        var result = await fileService.PickFolderAsync(initialPath, cancellationToken);
        if (result is { IsSuccessful: true, Folder.Path: not null }) {
            LoadDirectory(result.Folder.Path);
            if (!IsSubdirectory(directoryInfo, imageFileInfo?.Directory)) LoadImage(GetFirstImageFile()?.FullName);
        }
    }

    private static bool IsSubdirectory(DirectoryInfo? parent, DirectoryInfo? child) {
        if (parent is null || child is null) return false;
        var parentPath = parent.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var childPath = child.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void StartTimer() {
        timer?.Start();
        IsTimerRunning = true;
    }

    [RelayCommand]
    private void StopTimer() {
        timer?.Stop();
        IsTimerRunning = false;
    }

    private void TimerOnTick(object? sender, EventArgs eventArgs) {
        sketchTimeLeft--;
        UpdateLabelSketchTimeLeft();
        if (sketchTimeLeft > 0) return;

        LoadRandomImage();
    }

    private void UpdateLabelSketchTimeLeft() {
        var timeLeft = TimeSpan.FromSeconds(sketchTimeLeft);
        SketchTimeLeftString = $"{timeLeft.Minutes}m {timeLeft.Seconds}s";
    }
}