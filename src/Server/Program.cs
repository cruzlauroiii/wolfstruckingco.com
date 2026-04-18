var Builder = WebApplication.CreateBuilder(args);
var App = Builder.Build();
App.UseStaticFiles();
App.Run();
