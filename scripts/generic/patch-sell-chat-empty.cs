#:property TargetFramework=net11.0

// patch-sell-chat-empty.cs - gate the Pairs seed in SellChatPage on
// WolfsRenderContext.CurrentStep > 0 so static prerender (Step=0, no auth)
// shows empty chat instead of leaking the BYD Han EV demo dialogue. (Item #13)
const string Path = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\SellChatPage.razor.cs";
var Text = await File.ReadAllTextAsync(Path);
var Nl = Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

var Old = "Step = System.Math.Min(MaxStep, WolfsRenderContext.CurrentStep);" + Nl +
          "        Pairs.Add(new Exchange(Q0, A0));" + Nl +
          "        if (Step >= Step1Reveal) { Pairs.Add(new Exchange(Q1, A1)); }" + Nl +
          "        if (Step >= Step2Reveal) { Pairs.Add(new Exchange(Q2, A2)); }" + Nl +
          "        if (Step >= Step3Reveal) { Pairs.Add(new Exchange(Q3, A3)); }" + Nl +
          "        if (Step >= Step4Reveal) { Pairs.Add(new Exchange(Q4, A4)); }";

var New = "Step = System.Math.Min(MaxStep, WolfsRenderContext.CurrentStep);" + Nl +
          "        if (Step <= 0 && WolfsRenderContext.ChatHistory.Count == 0) { return Task.CompletedTask; }" + Nl +
          "        Pairs.Add(new Exchange(Q0, A0));" + Nl +
          "        if (Step >= Step1Reveal) { Pairs.Add(new Exchange(Q1, A1)); }" + Nl +
          "        if (Step >= Step2Reveal) { Pairs.Add(new Exchange(Q2, A2)); }" + Nl +
          "        if (Step >= Step3Reveal) { Pairs.Add(new Exchange(Q3, A3)); }" + Nl +
          "        if (Step >= Step4Reveal) { Pairs.Add(new Exchange(Q4, A4)); }";

if (!Text.Contains(Old, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync("anchor missing"); return 1; }
await File.WriteAllTextAsync(Path, Text.Replace(Old, New));
await Console.Out.WriteLineAsync($"wrote {Path}");
return 0;
