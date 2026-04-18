using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class DispatcherPage
{
    private const string AuditStore = "audit";
    private const string WorkersStore = "workers";
    private const string FieldKind = "kind";
    private const string FieldId = "id";
    private const string FieldSubject = "subject";
    private const string FieldOnBehalfOf = "onBehalfOf";
    private const string FieldEmail = "email";
    private const string FieldName = "name";

    private const string KindDispatch = "dispatcher.action";

    private const string KwFactory = "factory";
    private const string KwCash = "cash payment";
    private const string KwGps = "gps";
    private const string KwLoaded = "loaded";
    private const string KwContainer = "container";
    private const string KwPortLa = "port of la";
    private const string KwLaDriver = "driver from los angeles";
    private const string KwShip = "ship";
    private const string KwPickedUp = "picked up";
    private const string KwCrash = "crash";
    private const string KwDelay = "delay";
    private const string KwReroute = "reroute";
    private const string KwMemphis = "memphis";
    private const string KwPhoenix = "phoenix";
    private const string KwBuyer = "buyer";
    private const string KwDoor = "door";
    private const string KwClose = "close";
    private const string KwComplete = "complete";
    private const string KwNewTime = "new time";
    private const string KwBuyerTime = "tells the buyer";

    private const string Q1 = "Are you at the factory?";
    private const string A1 = "Just pulled into the gate.";
    private const string AttachFactoryPhoto = "factory-gate.jpg";
    private const string Q2 = "Did the factory take the cash?";
    private const string A2 = "Yes — eighteen thousand, all in. They handed me the keys.";
    private const string AttachReceipt = "BYD-cash-receipt-18000USD.jpg";
    private const string Q3 = "GPS tracker placed?";
    private const string A3 = "Inside the passenger seat compartment.";
    private const string AttachGps = "gps-tracker-installed.jpg";
    private const string Q4 = "Loaded into the container?";
    private const string A4 = "Yes, sealed and photographed.";
    private const string AttachContainer = "container-sealed.jpg";
    private const string Q5 = "Ship is in. Ready for pickup at terminal four-zero-one?";
    private const string A5 = "On my way. Heading in now.";
    private const string Q6 = "Got the car?";
    private const string A6 = "Yes. Heading east on I-10.";
    private const string Q7 = "Heads up — GPS shows heavy congestion 2 miles ahead. We've already pushed your ETA by 90 minutes.";
    private const string A7 = "Got it. Sticking to the route.";
    private const string Q8 = "New route — take I-40 east through Knoxville.";
    private const string A8 = "Got it, switching now.";
    private const string Q9 = "Hand off at the next yard. Driver's waiting.";
    private const string A9 = "Almost there.";
    private const string Q10 = "At the buyer's door?";
    private const string A10 = "Yes. He's coming out now.";
    private const string Q11 = "Job's closed. Payouts going out.";
    private const string A11 = "Thanks!";
    private const string Q12 = "Heads up — your delivery is now landing at 10 PM. Crash on I-10 pushed everything 90 minutes.";
    private const string A12 = "Got it. I'll be home, just call when you turn onto Oak.";
    private const string Q0 = "How's the route?";
    private const string A0 = "Smooth so far.";

    private const string DriverFallback = "Driver";
    private const string Empty = "";
    private const string Question = "?";
    private const char AtSign = '@';
    private const char Space = ' ';

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private List<Bubble> Pairs { get; } = [];
    private string? Recipient { get; set; }

    private string Initials
    {
        get
        {
            var R = Recipient ?? Empty;
            if (R.Length == 0) { return Question; }
            var First = R.Split(AtSign)[0].Split(Space)[0];
            if (string.IsNullOrEmpty(First)) { First = Question; }
            return First.Length > 0 ? First[..1].ToUpperInvariant() : Question;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var Latest = (await Wolfs.DbAllAsync<JsonObject>(AuditStore))
            .Where(R => R is not null && R[FieldKind]?.GetValue<string>() == KindDispatch)
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty)
            .FirstOrDefault();
        var Subject = (Latest?[FieldSubject]?.ToString() ?? Empty).ToLowerInvariant();
        var Email = Latest?[FieldOnBehalfOf]?.ToString() ?? Empty;
        var WorkerName = (await Wolfs.DbAllAsync<JsonObject>(WorkersStore))
            .Where(W => W is not null && string.Equals(W[FieldEmail]?.GetValue<string>() ?? Empty, Email, StringComparison.OrdinalIgnoreCase))
            .Select(W => W?[FieldName]?.GetValue<string>() ?? Empty)
            .FirstOrDefault(N => !string.IsNullOrEmpty(N));
        Recipient = !string.IsNullOrEmpty(WorkerName) ? WorkerName : ShortName(Email);

        var (Q, A, Attachment) = Subject switch
        {
            var S when S.Contains(KwBuyerTime, StringComparison.Ordinal) || S.Contains(KwNewTime, StringComparison.Ordinal) => (Q12, A12, Empty),
            var S when S.Contains(KwLoaded, StringComparison.Ordinal) || S.Contains(KwContainer, StringComparison.Ordinal) => (Q4, A4, AttachContainer),
            var S when S.Contains(KwGps, StringComparison.Ordinal) => (Q3, A3, AttachGps),
            var S when S.Contains(KwCash, StringComparison.Ordinal) => (Q2, A2, AttachReceipt),
            var S when S.Contains(KwFactory, StringComparison.Ordinal) => (Q1, A1, AttachFactoryPhoto),
            var S when S.Contains(KwPortLa, StringComparison.Ordinal) || (S.Contains(KwLaDriver, StringComparison.Ordinal) && S.Contains(KwShip, StringComparison.Ordinal)) => (Q5, A5, Empty),
            var S when S.Contains(KwPickedUp, StringComparison.Ordinal) => (Q6, A6, Empty),
            var S when S.Contains(KwCrash, StringComparison.Ordinal) || S.Contains(KwDelay, StringComparison.Ordinal) => (Q7, A7, Empty),
            var S when S.Contains(KwReroute, StringComparison.Ordinal) => (Q8, A8, Empty),
            var S when S.Contains(KwMemphis, StringComparison.Ordinal) || S.Contains(KwPhoenix, StringComparison.Ordinal) => (Q9, A9, Empty),
            var S when S.Contains(KwBuyer, StringComparison.Ordinal) || S.Contains(KwDoor, StringComparison.Ordinal) => (Q10, A10, Empty),
            var S when S.Contains(KwClose, StringComparison.Ordinal) || S.Contains(KwComplete, StringComparison.Ordinal) => (Q11, A11, Empty),
            _ => (Q0, A0, Empty),
        };
        Pairs.Add(new Bubble(Q, A, Attachment));
    }

    private static string ShortName(string Email) =>
        string.IsNullOrEmpty(Email) ? DriverFallback : (Email.Split(AtSign).FirstOrDefault() ?? DriverFallback);

    public sealed record Bubble(string Q, string A, string Attachment);
}
