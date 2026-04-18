#:property TargetFramework=net11.0

// patch-chat-rows.cs - replace the single-mic chat input row in
// SellChatPage / ApplicantPage / DispatcherPage with a call+attach+send row
// (item #6). Buttons render side-by-side. Pure HTML onclick handlers so they
// work pre-WASM-hydration on static prerendered pages.
const string Repo = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\";

(string File, string Old, string New)[] Patches =
[
    (Repo + "SellChatPage.razor",
     "    <div style=\"display:flex;gap:8px;align-items:center;background:#fff;border:1px solid var(--border);border-radius:24px;padding:6px 10px\">\n        <input type=\"text\" style=\"flex:1;border:none;outline:none;padding:10px;font-size:.9rem\" placeholder=\"Type your reply…\" />\n        <button class=\"Btn\" style=\"border-radius:50%;width:44px;height:44px;padding:0;font-size:1rem;flex:none\">🎙️</button>\n    </div>",
     ChatRow("ChatAttach", "Type your reply…", "Call agent", "Attach photo / receipt")),

    (Repo + "ApplicantPage.razor",
     "    <div style=\"display:flex;gap:8px;align-items:center;background:#fff;border:1px solid var(--border);border-radius:24px;padding:6px 10px\">\n        <button class=\"Btn Ghost\" style=\"border-radius:50%;width:40px;height:40px;padding:0;font-size:1.1rem;flex:none\">📎</button>\n        <input type=\"text\" style=\"flex:1;border:none;outline:none;padding:10px;font-size:.9rem\" placeholder=\"Type your answer or attach a scan…\" />\n        <button class=\"Btn\" style=\"border-radius:50%;width:44px;height:44px;padding:0;font-size:1rem;flex:none\">🎙️</button>\n    </div>",
     ChatRow("ApplicantAttach", "Type your answer…", "Call agent", "Attach badge / cert scan")),

    (Repo + "DispatcherPage.razor",
     "    <div style=\"display:flex;gap:8px;align-items:center;background:#fff;border:1px solid var(--border);border-radius:24px;padding:6px 10px\">\n        <button class=\"Btn Ghost\" style=\"border-radius:50%;width:40px;height:40px;padding:0;font-size:1.1rem;flex:none\">📎</button>\n        <input type=\"text\" style=\"flex:1;border:none;outline:none;padding:10px;font-size:.9rem\" placeholder=\"Type or attach a photo…\" />\n        <button class=\"Btn\" style=\"border-radius:50%;width:44px;height:44px;padding:0;font-size:1rem;flex:none\">🎙️</button>\n    </div>",
     ChatRow("DispatcherAttach", "Type or attach a photo…", "Call driver", "Attach photo")),
];

var Total = 0;
foreach (var (Path, Old, New) in Patches)
{
    if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"missing: {Path}"); return 2; }
    var Text = await File.ReadAllTextAsync(Path);
    var Nl = Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    var OldNorm = Old.Replace("\n", Nl, StringComparison.Ordinal);
    var NewNorm = New.Replace("\n", Nl, StringComparison.Ordinal);
    if (!Text.Contains(OldNorm, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync($"anchor missing in {Path}"); return 3; }
    await File.WriteAllTextAsync(Path, Text.Replace(OldNorm, NewNorm));
    Total++;
    await Console.Out.WriteLineAsync($"patched {Path}");
}
await Console.Out.WriteLineAsync($"done — {Total.ToString(System.Globalization.CultureInfo.InvariantCulture)} files");
return 0;

static string ChatRow(string AttachId, string Placeholder, string CallTitle, string AttachTitle) =>
    "    <div style=\"display:flex;gap:6px;align-items:center;background:#fff;border:1px solid var(--border);border-radius:24px;padding:6px 8px\">\n" +
    "        <input type=\"text\" style=\"flex:1;border:none;outline:none;padding:10px;font-size:.9rem\" placeholder=\"" + Placeholder + "\" />\n" +
    "        <button class=\"Btn Ghost\" type=\"button\" title=\"" + CallTitle + "\" onclick=\"if(window.WolfsCall){window.WolfsCall(this)}else{alert('Voice call is wired in the live build via the Cloudflare worker — see worker/voice-sidecar.cs.')}\" style=\"border-radius:50%;width:40px;height:40px;padding:0;font-size:1rem;flex:none\">📞</button>\n" +
    "        <input type=\"file\" id=\"" + AttachId + "\" accept=\"image/*,application/pdf\" style=\"display:none\" />\n" +
    "        <button class=\"Btn Ghost\" type=\"button\" title=\"" + AttachTitle + "\" onclick=\"document.getElementById('" + AttachId + "').click()\" style=\"border-radius:50%;width:40px;height:40px;padding:0;font-size:1rem;flex:none\">📎</button>\n" +
    "        <button class=\"Btn\" type=\"button\" title=\"Send\" style=\"border-radius:50%;width:44px;height:44px;padding:0;font-size:1rem;flex:none\">➤</button>\n" +
    "    </div>";
