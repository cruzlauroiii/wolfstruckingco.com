namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVWorkerChatAttach2
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\worker\worker.js";
    public const string Find_01 = "    // === File upload: stores binary blob in R2 under uploads";
    public const string Replace_01 = "    // === Anonymous chat attachment ===\n    if (url.pathname === '/api/chat-attach' && request.method === 'POST') {\n      const filename = (url.searchParams.get('filename') || 'attach').replace(/[^a-zA-Z0-9._-]/g, '_').slice(0, 80);\n      const contentType = request.headers.get('Content-Type') || 'application/octet-stream';\n      const body = await request.arrayBuffer();\n      if (body.byteLength === 0) return Response.json({ error: 'empty body' }, { status: 400, headers: h });\n      if (body.byteLength > 10 * 1024 * 1024) return Response.json({ error: 'too large', limit: '10MB' }, { status: 413, headers: h });\n      const key = 'chat-attach/' + Date.now() + '_' + rnd() + '_' + filename;\n      await env.R2.put(key, body, { httpMetadata: { contentType } });\n      return Response.json({ ok: true, key, size: body.byteLength, contentType, url: '/api/file/' + encodeURIComponent(key) }, { headers: h });\n    }\n\n    // === File upload: stores binary blob in R2 under uploads";
}
