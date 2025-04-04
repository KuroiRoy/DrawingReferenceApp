﻿using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace ReferenceImages;

public static class MauiProgram {

    public static MauiApp CreateMauiApp () {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("tabler-icons.ttf", "Icons");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        
#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(lifecycleBuilder =>
            {
                lifecycleBuilder.OnWindowCreated(window =>
                {
                    var manager = WinUIEx.WindowManager.Get(window);
                    manager.PersistenceId = "MainWindowPersistenceId";
                    
                    Settings.Default.LoadSettings();
                });
                lifecycleBuilder.OnClosed((window, args) =>
                {
                    Settings.Default.SaveSettings();
                });
            });
        });
#endif

        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FileService.Default);
        builder.Services.AddSingleton(Settings.Default);
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<MainPageViewModel>();

        return builder.Build();
    }
    
}