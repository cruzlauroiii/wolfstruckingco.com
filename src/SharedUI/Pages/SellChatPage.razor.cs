using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class SellChatPage
{
    private const string Q0 = "Hi! What car are you selling and where is it picked up?";
    private const string A0 = "A 2024 BYD Han EV. The factory is in Hefei, China.";
    private const string Q1 = "How much cash should the driver bring to the factory?";
    private const string A1 = "Eighteen thousand US dollars in full to BYD's export desk.";
    private const string Q2 = "Who is the buyer and where does the car go?";
    private const string A2 = "Sam, in Wilmington North Carolina, fourteen-eighteen Oak Street.";
    private const string Q3 = "What's the asking price and how does the buyer pay?";
    private const string A3 = "Forty-eight thousand five hundred. Cash on delivery, instant payment at the door.";
    private const string Q4 = "Got it. I'll set up four legs and pick the badges drivers need.";
    private const string A4 = "Yes please.";

    private const int MaxStep = 4;
    private const int Step1Reveal = 1;
    private const int Step2Reveal = 2;
    private const int Step3Reveal = 3;
    private const int Step4Reveal = 4;

    private List<Exchange> Pairs { get; } = [];
    private int Step { get; set; }

    private const int VisibleWindow = 3;

    private IEnumerable<Exchange> VisiblePairs =>
        Pairs.Count <= VisibleWindow ? Pairs : Pairs.Skip(Pairs.Count - VisibleWindow);

    protected override Task OnInitializedAsync()
    {
        Step = System.Math.Min(MaxStep, WolfsRenderContext.CurrentStep);
        Pairs.Add(new Exchange(Q0, A0));
        if (Step >= Step1Reveal) { Pairs.Add(new Exchange(Q1, A1)); }
        if (Step >= Step2Reveal) { Pairs.Add(new Exchange(Q2, A2)); }
        if (Step >= Step3Reveal) { Pairs.Add(new Exchange(Q3, A3)); }
        if (Step >= Step4Reveal) { Pairs.Add(new Exchange(Q4, A4)); }
        return Task.CompletedTask;
    }

    public sealed record Exchange(string Question, string Answer);
}
