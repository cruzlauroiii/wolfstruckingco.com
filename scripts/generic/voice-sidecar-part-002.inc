    _ = Task.Run(async () =>
    {
        try
        {
            await Cors(ctx);
            if (ctx.Request.HttpMethod == "OPTIONS") { ctx.Response.StatusCode = 204; ctx.Response.OutputStream.Close(); return; }
            var path = ctx.Request.Url!.AbsolutePath.TrimEnd('/');
            if (path == "/health")
            {
                await WriteJson(ctx, 200, "{\"ok\":true,\"voice\":" + JsonStr(voice) + ",\"stt\":" + (!string.IsNullOrEmpty(accessToken) ? "true" : "false") + "}");
                return;
            }
            if (path == "/tts" && ctx.Request.HttpMethod == "POST")
            {
                using var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                var body = await sr.ReadToEndAsync();
                var doc = JsonNode.Parse(body)?.AsObject();
                var text = doc?["text"]?.GetValue<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text)) { await WriteJson(ctx, 400, "{\"error\":\"text required\"}"); return; }
                var mp3 = await Tts(text);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "audio/mpeg";
                await ctx.Response.OutputStream.WriteAsync(mp3);
                ctx.Response.OutputStream.Close();
                return;
            }
            if (path == "/stt" && ctx.Request.HttpMethod == "POST")
            {
                if (string.IsNullOrEmpty(accessToken)) { await WriteJson(ctx, 401, "{\"error\":\"OAuth token not configured\"}"); return; }
                using var ms = new MemoryStream();
                await ctx.Request.InputStream.CopyToAsync(ms);
                var audio = ms.ToArray();
                if (audio.Length == 0) { await WriteJson(ctx, 400, "{\"error\":\"empty body\"}"); return; }
                var text = await Stt(audio, accessToken);
                await WriteJson(ctx, 200, "{\"text\":" + JsonStr(text) + "}");
                return;
            }
            await WriteJson(ctx, 404, "{\"error\":\"not found\"}");
        }
        catch (Exception ex)
        {
            try { await WriteJson(ctx, 500, "{\"error\":" + JsonStr(ex.Message) + "}"); } catch { }
            Console.Error.WriteLine("req err: " + ex);
        }
    });
}
