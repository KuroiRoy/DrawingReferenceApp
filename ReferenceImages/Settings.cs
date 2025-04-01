using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReferenceImages;

public class Settings : INotifyPropertyChanged
{
    public static Settings Default { get; } = new();

    public bool AlwaysEnforceProhibitedWordsOnStartup
    {
        get;
        set => SetField(ref field, value);
    }

    public bool EnforceProhibitedWordsInPaths
    {
        get;
        set => SetField(ref field, value);
    }

    public string NetworkFileServerUrl {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public int SketchTimeInSeconds => SketchTimerMinutes * 60 + SketchTimerSeconds;

    public int SketchTimerMinutes
    {
        get;
        set => SetField(ref field, value);
    }

    public int SketchTimerSeconds
    {
        get;
        set => SetField(ref field, value);
    }

    public readonly ObservableRangeCollection<string> ProhibitedWordsInPaths = [];

    public void LoadSettings()
    {
        AlwaysEnforceProhibitedWordsOnStartup = Preferences.Get(nameof(AlwaysEnforceProhibitedWordsOnStartup), true);
        EnforceProhibitedWordsInPaths = Preferences.Get(nameof(EnforceProhibitedWordsInPaths), true);
        NetworkFileServerUrl = Preferences.Get(nameof(NetworkFileServerUrl), "[Server Ip/Url]:5083");
        ProhibitedWordsInPaths.AddRange(Preferences.Get(nameof(ProhibitedWordsInPaths), "nude naked topless").Split(' '));
        SketchTimerMinutes = Preferences.Get(nameof(SketchTimerMinutes), 5);
        SketchTimerSeconds = Preferences.Get(nameof(SketchTimerSeconds), 0);

        if (AlwaysEnforceProhibitedWordsOnStartup) EnforceProhibitedWordsInPaths = true;
    }

    public void SaveSettings()
    {
        Preferences.Set(nameof(AlwaysEnforceProhibitedWordsOnStartup), AlwaysEnforceProhibitedWordsOnStartup);
        Preferences.Set(nameof(EnforceProhibitedWordsInPaths), EnforceProhibitedWordsInPaths);
        Preferences.Set(nameof(NetworkFileServerUrl), NetworkFileServerUrl);
        Preferences.Set(nameof(ProhibitedWordsInPaths), string.Join(' ', ProhibitedWordsInPaths));
        Preferences.Set(nameof(SketchTimerMinutes), SketchTimerMinutes);
        Preferences.Set(nameof(SketchTimerSeconds), SketchTimerSeconds);
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