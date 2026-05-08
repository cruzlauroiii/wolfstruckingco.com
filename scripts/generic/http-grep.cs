return await Scripts.HttpGrep.RunAsync(args).ConfigureAwait(false);

namespace Scripts
{
    internal static class HttpGrep
    {
        public static async System.Threading.Tasks.Task<int> RunAsync(string[] Args)
        {
            if (Args is null || Args.Length < 1)
            {
                return 2;
            }
            var Text = await System.IO.File.ReadAllTextAsync(Args[0]).ConfigureAwait(false);
            var Url = Extract(Text, "Url");
            var Expected = Extract(Text, "Expected");
            if (Url is null || Expected is null)
            {
                return 3;
            }
            using var Client = new System.Net.Http.HttpClient();
            Client.Timeout = System.TimeSpan.FromSeconds(30);
            using var Resp = await Client.GetAsync(new System.Uri(Url)).ConfigureAwait(false);
            if (!Resp.IsSuccessStatusCode)
            {
                return 4;
            }
            var Body = await Resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Body.Contains(Expected, System.StringComparison.Ordinal) ? 0 : 5;
        }

        private static string? Extract(string Text, string Key)
        {
            var Marker = "const string " + Key;
            var I = Text.IndexOf(Marker, System.StringComparison.Ordinal);
            if (I < 0)
            {
                return null;
            }
            var Span = Text.AsSpan(I);
            var EqRel = Span.IndexOf('=');
            if (EqRel < 0)
            {
                return null;
            }
            var Span2 = Span[(EqRel + 1)..];
            var Q1Rel = Span2.IndexOf('\u0022');
            if (Q1Rel < 0)
            {
                return null;
            }
            var After = Span2[(Q1Rel + 1)..];
            var EndRel = After.IndexOf(';');
            if (EndRel < 0)
            {
                return null;
            }
            var Body = After[..EndRel];
            var Q2Rel = Body.LastIndexOf('\u0022');
            return Q2Rel < 0 ? null : Body[..Q2Rel].ToString();
        }
    }
}
