#:property TargetFramework=net11.0

// patch-wolfs-call-js.cs - add window.WolfsCall to wolfs-interop.js so the
// 📞 chat-call button on Sell/Applicant/Dispatcher pages does something
// real: starts the existing WolfsChatVoice mic bridge, listens 8 seconds,
// drops the transcript into the nearest chat input, gracefully degrades
// when the voice sidecar isn't running. (Item #7)
const string Path = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\wwwroot\js\wolfs-interop.js";
var Text = await File.ReadAllTextAsync(Path);
const string Marker = "window.WolfsCall =";
if (Text.Contains(Marker, StringComparison.Ordinal)) { await Console.Out.WriteLineAsync("WolfsCall already present"); return 0; }

var Snippet =
    "\n  // ─── Chat call button (📞) ────────────────────────────────────────────\n" +
    "  // Used by Sell/Applicant/Dispatcher chat rows. Starts WolfsChatVoice if\n" +
    "  // available, listens 8s, drops transcript into the nearest chat input.\n" +
    "  w.WolfsCall = async function (btn) {\n" +
    "    var orig = btn ? btn.textContent : '';\n" +
    "    if (btn) { btn.textContent = '⏺'; btn.dataset.recording = '1'; btn.disabled = true; }\n" +
    "    var bridge = window.WolfsChatVoice;\n" +
    "    if (!bridge) { alert('🎤 Voice bridge not loaded — reload the page.'); reset(); return; }\n" +
    "    var ok = false;\n" +
    "    try { ok = await bridge.start(); } catch (e) { ok = false; }\n" +
    "    if (!ok) { alert('🎤 Voice sidecar not running. Start it locally (worker/voice-sidecar.cs) or wire the Cloudflare worker /voice endpoint for the cloud build.'); reset(); return; }\n" +
    "    setTimeout(async function () {\n" +
    "      var text = '';\n" +
    "      try { text = await bridge.stop(); } catch (e) {}\n" +
    "      reset();\n" +
    "      if (text) {\n" +
    "        var row = btn && btn.parentElement ? btn.parentElement : null;\n" +
    "        var inp = row ? row.querySelector('input[type=\"text\"]') : null;\n" +
    "        if (inp) { inp.value = text; inp.dispatchEvent(new Event('input', { bubbles: true })); }\n" +
    "        window.dispatchEvent(new CustomEvent('wolfs-call-transcript', { detail: { text: text } }));\n" +
    "      }\n" +
    "    }, 8000);\n" +
    "    function reset () { if (btn) { btn.textContent = orig; delete btn.dataset.recording; btn.disabled = false; } }\n" +
    "  };\n";

const string Anchor = "// ─── Cloudflare worker calls";
var I = Text.IndexOf(Anchor, StringComparison.Ordinal);
if (I < 0) { await Console.Error.WriteLineAsync($"anchor missing: {Anchor}"); return 1; }
var Updated = Text[..I] + Snippet + "  " + Text[I..];
await File.WriteAllTextAsync(Path, Updated);
await Console.Out.WriteLineAsync($"wrote {Path} ({Updated.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} chars)");
return 0;
