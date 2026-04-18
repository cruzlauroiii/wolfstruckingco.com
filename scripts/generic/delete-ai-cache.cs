#:property TargetFramework=net11.0

using System.IO;

if (args.Length < 1) { return 1; }
if (!File.Exists(args[0])) { return 2; }

var Cache = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\ai-chat-cache.jsonl";
if (File.Exists(Cache)) { File.Delete(Cache); }
return 0;
