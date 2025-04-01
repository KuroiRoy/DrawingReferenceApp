using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Common;
using CommunityToolkit.Maui.Storage;

namespace ReferenceImages;

public class FileService {
    public static FileService Default { get; } = new(FolderPicker.Default);
    
    private readonly HttpClient httpClient = new();
    private readonly IFolderPicker localFolderPicker;
    private FileBrowserPage? networkFilePickerView;
    private readonly FolderBrowserViewModel networkFolderPicker;
    
    public FileService(IFolderPicker localFolderPicker) {
        this.localFolderPicker = localFolderPicker;
        networkFolderPicker = new FolderBrowserViewModel(this);
    }

    public async Task<FolderPickerResult> PickFolderAsync(string initialPath, CancellationToken cancellationToken) {
        var source = await Shell.Current.DisplayActionSheet(
            "Select file from:", "Cancel", null, 
            "Local", "Network");
        
        return source switch {
            "Local" => await localFolderPicker.PickAsync(initialPath, cancellationToken),
            "Network" => await PickNetworkFolderAsync(initialPath, cancellationToken),
            _ => new FolderPickerResult(null, null)
        };
    }

    public async Task<bool> GetPathExists(string path, CancellationToken cancellationToken) {
        var url = $"{Settings.Default.NetworkFileServerUrl}/api/files/exist?path={WebUtility.UrlEncode(path)}";
        try {
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            if (response.StatusCode != HttpStatusCode.BadRequest) return response.StatusCode == HttpStatusCode.OK;
            
            Helper.DisplayAlert($"Failed to get if path exists:\n{response.Content}\nUrl: {url}");
            return false;
        }
        catch (HttpRequestException httpRequestException) {
            Helper.DisplayAlert($"Failed to get if path exists:\n{httpRequestException.Message}\nUrl: {url}");
            return false;
        }
        catch (InvalidOperationException invalidOperationException) {
            Helper.DisplayAlert($"Failed to get if path exists:\n{invalidOperationException.Message}\nUrl: {url}");
            return false;
        }
        catch (OperationCanceledException) {
            return false;
        }
    }

    public async Task<FileListResponse?> GetFoldersAsync(string path, CancellationToken cancellationToken) {
        var url = $"{Settings.Default.NetworkFileServerUrl}/api/files/list?path={WebUtility.UrlEncode(path)}&folders=true&files=false";
        try {
            return await httpClient.GetFromJsonAsync<FileListResponse>(url, cancellationToken);
        }
        catch (HttpRequestException httpRequestException) {
            Helper.DisplayAlert($"Failed to pick folder:\n{httpRequestException}\nUrl: {url}");
            return null;
        }
        catch (InvalidOperationException invalidOperationException) {
            Helper.DisplayAlert($"Failed to pick folder:\n{invalidOperationException.Message}\nUrl: {url}");
            return null;
        }
        catch (OperationCanceledException) {
            return null;
        }
    }

    public async Task DownloadFileAsync(string serverUrl, string filePath, string localPath)
    {
        var url = $"{serverUrl}/api/files/download?filePath={WebUtility.UrlEncode(filePath)}";
        var response = await httpClient.GetAsync(url);

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(localPath);
        await stream.CopyToAsync(fileStream);
    }

    private async Task<FolderPickerResult> PickNetworkFolderAsync(string initialPath, CancellationToken cancellationToken) {
        var modalTask = Shell.Current.Navigation.PushModalAsync(networkFilePickerView ??= new FileBrowserPage(networkFolderPicker));
        var pickTask = networkFolderPicker.PickAsync(initialPath, cancellationToken);
        
        await modalTask;
        return await pickTask;
    }
}