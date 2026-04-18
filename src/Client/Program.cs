using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SharedUI.Services;

var Builder = WebAssemblyHostBuilder.CreateDefault(args);
Builder.RootComponents.Add<SharedUI.Components.Routes>("#app");
Builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(Builder.HostEnvironment.BaseAddress) });
Builder.Services.AddScoped<WolfsInteropService>();
Builder.Services.AddScoped<VoiceChatService>();
await Builder.Build().RunAsync();
