return await Scripts.HttpHead.RunAsync(args).ConfigureAwait(false);

namespace Scripts
{
    internal static class HttpHead
    {
        public static async System.Threading.Tasks.Task<int> RunAsync(string[] Args)
        {
            if (Args is null || Args.Length < 1)
            {
                return 2;
            }
            var Text = await System.IO.File.ReadAllTextAsync(Args[0]).ConfigureAwait(false);
            var Url = Extract(Text, "Url");
            var Out = Extract(Text, "OutputFile");
            if (Url is null || Out is null)
            {
                return 3;
            }
            using var Client = new System.Net.Http.HttpClient();
            Client.Timeout = System.TimeSpan.FromSeconds(30);
            using var Req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, Url);
            using var Resp = await Client.SendAsync(Req).ConfigureAwait(false);
            var Status = ((int)Resp.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var Cl = Resp.Content.Headers.ContentLength?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            var Ct = Resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
            await System.IO.File.WriteAllTextAsync(Out, $"{Status}|{Cl}|{Ct}\n").ConfigureAwait(false);
            return Resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.MultipleChoices ? 0 : 4;
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
            if (Q2Rel < 0)
            {
                return null;
            }
            var Raw = Body[..Q2Rel].ToString();
            return Raw.Replace("\\\\", "\\", System.StringComparison.Ordinal);
        }
    }
}
