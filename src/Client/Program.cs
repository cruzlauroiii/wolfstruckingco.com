using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SharedUI.Services;

var Builder = WebAssemblyHostBuilder.CreateDefault(args);
Builder.RootComponents.Add<SharedUI.Components.Routes>("#app");
Builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(Builder.HostEnvironment.BaseAddress) });
Builder.Services.AddScoped<WolfsJsBootstrap>();
Builder.Services.AddScoped<WolfsInteropService>();
Builder.Services.AddAuthorizationCore();
Builder.Services.AddScoped<AuthenticationStateProvider, LocalStorageAuthStateProvider>();
var Host = Builder.Build();
await Host.Services.GetRequiredService<WolfsJsBootstrap>().EnsureInstalledAsync();
await Host.RunAsync();
