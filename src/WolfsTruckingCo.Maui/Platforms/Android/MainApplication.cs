using Android.App;
using Android.Runtime;

namespace WolfsTruckingCo.Maui.Platforms.Android;

[Application]
public sealed class MainApplication(nint Handle, JniHandleOwnership Ownership) : MauiApplication(Handle, Ownership)
{
    protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
