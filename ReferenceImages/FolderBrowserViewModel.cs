using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;

namespace ReferenceImages;

public partial class FolderBrowserViewModel : BaseViewModel {
    private CancellationTokenSource cancellationTokenSource;
    private readonly FileService fileService;
    private TaskCompletionSource<FolderPickerResult>? taskCompletionSource;
    
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
        catch (TaskCanceledException) {
            // Ignored
            return new FolderPickerResult(null, null);
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
        taskCompletionSource?.SetResult(new FolderPickerResult(null, null));
    }

    private async Task LoadFoldersAsync(string path, CancellationToken cancellationToken) {
        var pathExists = await fileService.GetPathExists(path, cancellationToken);
        if (!pathExists) path = string.Empty;
        
        var folders = await fileService.GetFoldersAsync(path, cancellationToken);
        Items.ReplaceRange(folders.Select(folder => new FileBrowserItem { Icon = "", IsDirectory = true, Name = folder }));
    }
    
    private string GetFileIcon(string filename) => Path.GetExtension(filename) switch
    {
        ".pdf" => "pdf_icon.png",
        ".jpg" or ".png" => "image_icon.png",
        _ => "file_icon.png"
    };
}