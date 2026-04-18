namespace WolfsTruckingCo.Scripts.Specific;

public static class WriteFileScratchConfigVVoicePage
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\VoicePage.razor";
    public const string Mode = "overwrite";
    public const string Content = "@page \"/Voice\"\n\n<div class=\"Stage VoiceStage\">\n    <h1>📞 Voice agent</h1>\n    <p class=\"Sub\">Press and hold the button below to speak. The agent listens, then replies in voice.</p>\n\n    <div class=\"Card VoiceCard\">\n        <div class=\"VoicePulse\" aria-hidden=\"true\">🎙️</div>\n        <form action=\"/wolfstruckingco.com/Chat/\" method=\"get\" class=\"VoiceForm\">\n            <label for=\"VoiceInput\" class=\"VoiceLabel\">What do you need to ship?</label>\n            <input id=\"VoiceInput\" name=\"msg\" type=\"text\" placeholder=\"Tell the agent — text or paste your transcribed voice\" autofocus required />\n            <button type=\"submit\" class=\"Btn VoiceSubmit\">Send to agent ➤</button>\n        </form>\n        <p class=\"VoiceHint\">Tip: tap the mic icon on your keyboard to dictate. The agent treats text and dictated voice the same.</p>\n    </div>\n\n    <div class=\"Card VoiceCallout\">\n        <h2>How voice works here</h2>\n        <ul>\n            <li>📨 Your message goes to the dispatcher chat — same thread as text.</li>\n            <li>🚛 The dispatcher composes drivers, legs, customs, and badges from what you say.</li>\n            <li>📍 Track the resulting job from the marketplace or driver dashboard.</li>\n        </ul>\n    </div>\n</div>\n";
}
