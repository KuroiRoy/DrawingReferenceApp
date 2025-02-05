using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReferenceImages;

public class Settings : INotifyPropertyChanged
{
    public static Settings Default { get; } = new();

    public bool EnforceProhibitedWordsInPaths
    {
        get;
        set => SetField(ref field, value);
    }

    public readonly ObservableRangeCollection<string> ProhibitedWordsInPaths = [];

    public void LoadSettings()
    {
        EnforceProhibitedWordsInPaths = Preferences.Get(nameof(EnforceProhibitedWordsInPaths), true);
        var words = Preferences.Get(nameof(ProhibitedWordsInPaths), "nude naked topless");
        ProhibitedWordsInPaths.AddRange(words.Split(' '));
    }

    public void SaveSettings()
    {
        Preferences.Set(nameof(EnforceProhibitedWordsInPaths), EnforceProhibitedWordsInPaths);
        Preferences.Set(nameof(ProhibitedWordsInPaths), string.Join(' ', ProhibitedWordsInPaths));
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}