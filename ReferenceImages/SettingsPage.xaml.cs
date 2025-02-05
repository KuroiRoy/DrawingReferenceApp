namespace ReferenceImages;

public partial class SettingsPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = Settings.Default;
        EntryProhibitedWords.Text = string.Join(' ', Settings.Default.ProhibitedWordsInPaths);
    }

    private void EntryProhibitedWords_OnUnfocused(object? sender, FocusEventArgs e)
    {
        var words = EntryProhibitedWords.Text.Split(' ').Where(word => !string.IsNullOrWhiteSpace(word));
        Settings.Default.ProhibitedWordsInPaths.ReplaceRange(words);
    }
}