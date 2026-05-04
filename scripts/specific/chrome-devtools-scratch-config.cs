return 0;

namespace Scripts
{
    internal static class CdpRun
    {
        public const string Command = "evaluate_script";
        public const string PageId = "1";
        public const string Function = "() => fetch('https://wolfstruckingco.nbth.workers.dev/ai', { method: 'POST', headers: {'Content-Type':'application/json','X-Wolfs-Session':'probe','X-Wolfs-Role':'admin'}, body: JSON.stringify({ messages: [{role:'user', content:'Reply with the literal JSON one-liner {\\\"score\\\":0.5,\\\"rewrite\\\":\\\"hello world\\\"} and nothing else.'}], system: 'Return only the JSON object.', max_tokens: 64 }) }).then(r => r.text()).then(t => t.slice(0, 800))";
        public const string OutputPath = @"C:\Users\user1\AppData\Local\Temp\worker-ai-probe.txt";
    }
}
