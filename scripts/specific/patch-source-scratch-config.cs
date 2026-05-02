return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\worker\worker.js";
        public const string Find_01 = "{ name: 'db_list', description: 'List record keys in a collection (max 50).', input_schema: { type: 'object', properties: { collection: { type: 'string' } }, required: ['collection'] } },";
        public const string Replace_01 = "{ name: 'db_list', description: 'List record keys in a collection (max 50).', input_schema: { type: 'object', properties: { collection: { type: 'string' } }, required: ['collection'] } },\n          { name: 'db_get_blob', description: 'Read an uploaded file blob (chat-attach/* or uploads/*) from R2 as text. Returns first 4KB preview. Use this to verify documents the user attached.', input_schema: { type: 'object', properties: { key: { type: 'string' } }, required: ['key'] } },";
        public const string Find_02 = "} else if (tu.name === 'db_list') {";
        public const string Replace_02 = "} else if (tu.name === 'db_get_blob') {\n                const inp = tu.input || {};\n                const k = String(inp.key || '');\n                if (!k.startsWith('chat-attach/') && !k.startsWith('uploads/')) {\n                  result = JSON.stringify({ error: 'invalid key prefix' });\n                } else {\n                  const obj = await env.R2.get(k);\n                  if (!obj) { result = JSON.stringify({ found: false, key: k }); }\n                  else { const txt = await obj.text(); result = JSON.stringify({ key: k, size: txt.length, preview: txt.slice(0, 4096) }); }\n                }\n              } else if (tu.name === 'db_list') {";
    }
}
