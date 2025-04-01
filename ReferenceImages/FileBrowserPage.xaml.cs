namespace ReferenceImages;

public partial class FileBrowserPage {
    public FileBrowserPage(FolderBrowserViewModel networkFolderPicker) {
        InitializeComponent();
        BindingContext = networkFolderPicker;
    }
    
    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (e.CurrentSelection?.FirstOrDefault() is not FileBrowserItem selectedItem) return;
        
        if (selectedItem.IsDirectory)
        {
            // Navigate into directory
            // await _viewModel.NavigateToDirectory(selectedItem.Name);
        }
        else
        {
            // File selected - return result
            // await Shell.Current.Navigation.PopModalAsync();
            // MessagingCenter.Send(this, "FileSelected", 
                // Path.Combine(_viewModel.CurrentPath, selectedItem.Name));
        }
    }
}