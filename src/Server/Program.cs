#pragma warning disable MA0036
var Builder = WebApplication.CreateBuilder(args);
var App = Builder.Build();
App.UseStaticFiles();
await App.RunAsync().ConfigureAwait(false);
