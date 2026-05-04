using System.Text.Json.Nodes;
using System.IO;
using File = System.IO.File;
using Path = System.IO.Path;

namespace CdpTool;

public sealed partial class CdpCli
{
    private async Task ExecuteSceneOneScreenshotAsync(Dictionary<string, object> Args)
    {
        var url = Args.TryGetValue(CdpKey.Url, out var urlValue) ? urlValue.ToString()! : CdpProto.AboutBlank;
        var filePath = Args.TryGetValue(CdpArg.FilePath, out var fileValue) ? fileValue.ToString()! : Path.Combine(Environment.CurrentDirectory, "scene-one.png");
        await EnsurePageAttachedAsync();
        await SendCommandAsync(Cdp.PageNavigate, new JsonObject { [CdpKey.Url] = url });
        await Task.Delay(CdpTimeout.NavigationDelayMs);
        await SendCommandAsync(Cdp.RuntimeEvaluate, new JsonObject
        {
            [CdpKey.Expression] = """
                (() => {
                  localStorage.setItem('wolfs_role', 'buyer');
                  localStorage.setItem('wolfs_email', 'scene.one@example.com');
                  localStorage.setItem('wolfs_session', 'scene-one-' + Date.now());
                  localStorage.setItem('wolfs_sso', 'github');
                  location.reload();
                  return 'auth-seeded';
                })()
                """,
            [CdpKey.ReturnByValue] = true,
            [CdpKey.AwaitPromise] = true
        });
        await Task.Delay(CdpTimeout.NavigationDelayMs);
        var ready = "waiting";
        for (var i = 0; i < 20; i++)
        {
            var result = await SendCommandAsync(Cdp.RuntimeEvaluate, new JsonObject
            {
                [CdpKey.Expression] = "(() => /Log\\s*off/i.test(document.body?.innerText||'') ? 'ready' : 'waiting')()",
                [CdpKey.ReturnByValue] = true,
                [CdpKey.AwaitPromise] = true
            });
            ready = result?[CdpKey.Result]?[CdpKey.Value]?.ToString() ?? "waiting";
            if (ready == "ready") break;
            await Task.Delay(1000);
        }
        var click = await SendCommandAsync(Cdp.RuntimeEvaluate, new JsonObject
        {
            [CdpKey.Expression] = "(() => { const e=[...document.querySelectorAll('header button,header a,button,a')].find(x=>/Log\\s*off/i.test((x.textContent||x.ariaLabel||'').trim())); if(!e) return 'log-off-not-found'; e.click(); return 'clicked-log-off'; })()",
            [CdpKey.ReturnByValue] = true,
            [CdpKey.AwaitPromise] = true
        });
        var clicked = click?[CdpKey.Result]?[CdpKey.Value]?.ToString() ?? "unknown";
        await Task.Delay(CdpTimeout.PageLoadDelayMs);
        var shot = await SendCommandAsync(Cdp.PageCaptureScreenshot, new JsonObject { [CdpKey.Format] = CdpProto.PngFormat });
        var data = shot?[CdpKey.Data]?.ToString();
        if (string.IsNullOrEmpty(data)) throw new InvalidOperationException("Screenshot failed: no data");
        var fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(Environment.CurrentDirectory, filePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(fullPath, Convert.FromBase64String(data));
        Console.WriteLine("WASM/header ready result: " + ready);
        Console.WriteLine("Log Off click result: " + clicked);
        Console.WriteLine("Screenshot saved: " + fullPath);
    }
}
