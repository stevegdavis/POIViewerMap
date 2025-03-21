﻿using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using POIViewerMap.Stores;
using POIViewerMap.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace POIViewerMap;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseMauiCommunityToolkit()
        .UseSkiaSharp(true)
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<IAppSettings, AppSettings>();
        builder.Services.AddSingleton<MapViewPage>();
        return builder.Build();
	}
}
