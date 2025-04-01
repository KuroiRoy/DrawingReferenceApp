using System.Diagnostics;

namespace ReferenceImages;

public class Helper
{
    public static void DisplayAlert(string message, bool trace = true)
    {
        if (Application.Current?.Windows[0].Page is { IsLoaded: true } page) page.DisplayAlert("", message, "OK");
        if (trace) Trace.WriteLine(message);
    }
}