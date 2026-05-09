#:property TargetFramework=net11.0

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

if (args.Length < 1)
{
    return 1;
}

var Spec = await File.ReadAllLinesAsync(args[0]);

string Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0)
        {
            continue;
        }

        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@')
        {
            Tail = Tail[1..];
        }

        if (Tail.Length == 0 || Tail[0] != '\u0022')
        {
            continue;
        }

        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1)
        {
            continue;
        }

        return Tail[1..End];
    }

    return string.Empty;
}

var SecretsJsonPath = Get("SecretsJsonPath");
var Owner = Get("Owner");
var Repo = Get("Repo");
if (string.IsNullOrEmpty(SecretsJsonPath) || string.IsNullOrEmpty(Owner) || string.IsNullOrEmpty(Repo))
{
    return 2;
}

using var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(SecretsJsonPath));
var Pat = Doc.RootElement.TryGetProperty("GitHub:Pat", out var P) ? P.GetString() ?? string.Empty : string.Empty;
if (string.IsNullOrEmpty(Pat))
{
    return 3;
}

using var Http = new HttpClient();
Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Pat);
Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("wolfs-tools", "1.0"));
Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

var Url = new Uri($"https://api.github.com/repos/{Owner}/{Repo}/pages/builds/latest");
var Resp = await Http.GetAsync(Url);
var Body = await Resp.Content.ReadAsStringAsync();
Console.WriteLine("status=" + ((int)Resp.StatusCode).ToString(CultureInfo.InvariantCulture));
Console.WriteLine(Body);
return 0;
