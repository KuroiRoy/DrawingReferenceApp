namespace ReferenceImages;

public partial class MainPage {
    public MainPage(MainPageViewModel viewModel) {
        InitializeComponent();
        BindingContext = viewModel;
        
        Loaded += (_, _) => {
            viewModel.Loaded();
        };
    }
}