namespace ReferenceImages;

public class Helper
{
    public static void DisplayAlert(string message)
    {
        if (Application.Current?.Windows[0].Page is { IsLoaded: true } page) page.DisplayAlert("", message, "OK");
    }
}