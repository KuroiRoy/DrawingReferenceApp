using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;

namespace ReferenceImages;

public partial class FolderBrowserViewModel : BaseViewModel {
    private CancellationTokenSource cancellationTokenSource;
    private readonly FileService fileService;
    private TaskCompletionSource<FolderPickerResult>? taskCompletionSource;

    public bool IsLoading {
        get;
        set => SetField(ref field, value);
    }

    public string FolderPath {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public ObservableRangeCollection<FileBrowserItem> Items { get; } = [];

    public FolderBrowserViewModel(FileService fileService)
    {
        this.fileService = fileService;
        cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task<FolderPickerResult> PickAsync(string initialPath, CancellationToken cancellationToken) {
        taskCompletionSource = new TaskCompletionSource<FolderPickerResult>();
        using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);

        try {
            await LoadFoldersAsync(initialPath, linkedCancellationToken.Token);
            return await taskCompletionSource.Task.WaitAsync(linkedCancellationToken.Token);
        }
        catch (TaskCanceledException taskCanceledException) {
            return new FolderPickerResult(null, taskCanceledException);
        }
        finally {
            taskCompletionSource = null;
            await Shell.Current.Navigation.PopModalAsync();
        }
    }

    [RelayCommand]
    private void Cancel() {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        cancellationTokenSource = new CancellationTokenSource();
    }

    [RelayCommand]
    private void SelectFolder() {
        taskCompletionSource?.SetResult(new FolderPickerResult(new Folder($"network://{FolderPath}", Path.GetDirectoryName(FolderPath) ?? string.Empty), null));
    }

    [RelayCommand]
    private async Task OpenFolder(FileBrowserItem item) => await LoadFoldersAsync(item.RelativePath, cancellationTokenSource.Token);

    private async Task LoadFoldersAsync(string path, CancellationToken cancellationToken) {
        IsLoading = true;

        var pathExists = await fileService.GetPathExists(path, cancellationToken);
        if (!pathExists) path = string.Empty;
        
        var response = await fileService.GetFoldersAsync(path, cancellationToken);
        IsLoading = false;
        if (response is null) return;

        Items.ReplaceRange(response.Folders.Select(folder => new FileBrowserItem("\ueaad", true, folder)));
        if (!string.IsNullOrWhiteSpace(response.Path)) Items.Insert(0, new FileBrowserItem("\ueaad", true, Directory.GetParent(response.Path)?.Name ?? response.Path, ".."));
        FolderPath = response.Path;
    }
    
    private string GetFileIcon(string filename) => Path.GetExtension(filename) switch
    {
        ".pdf" => "pdf_icon.png",
        ".jpg" or ".png" => "image_icon.png",
        _ => "file_icon.png"
    };
}