using Microsoft.Maui.Hosting;

namespace WolfsTruckingCo.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var Builder = MauiApp.CreateBuilder();
        Builder.UseMauiApp<App>();
        return Builder.Build();
    }
}
