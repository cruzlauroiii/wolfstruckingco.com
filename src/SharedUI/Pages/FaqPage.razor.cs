using System.Collections.Generic;

namespace SharedUI.Pages;

public partial class FaqPage
{
    private const string Q1 = "How much does it cost to ship with Wolfs Trucking?";
    private const string A1 = "Our base rate is $1.45/mile with transparent pricing. Weight surcharges apply over 10,000 lbs. Multi-stop deliveries add $25/stop. Rush delivery adds 30%.";
    private const string Q2 = "What areas does Wolfs Trucking serve?";
    private const string A2 = "Headquartered in Burbank, CA. Primary service: Southern California. Regional routes throughout the Western US. Nationwide freight via carrier partnerships.";
    private const string Q3 = "What are the CDL driver pay rates?";
    private const string A3 = "CDL-A Regional: $65,000-$80,000/year. CDL-B Local: $55,000-$65,000/year. Plus $3,000 sign-on bonus, full health benefits, 401(k) match, home weekly guaranteed.";
    private const string Q4 = "What is your on-time delivery rate?";
    private const string A4 = "Wolfs Trucking Co. maintains a 97.3% on-time delivery rate, exceeding the industry standard of 95%.";
    private const string Q5 = "How can I book a shipment?";
    private const string A5 = "Book instantly through our client portal with instant quotes, multi-stop support, and secure payment. Or call (818) 624-0454 for dedicated account service.";

    private static readonly List<Faq> Questions =
    [
        new(Q1, A1),
        new(Q2, A2),
        new(Q3, A3),
        new(Q4, A4),
        new(Q5, A5),
    ];

    private sealed record Faq(string Q, string A);
}
