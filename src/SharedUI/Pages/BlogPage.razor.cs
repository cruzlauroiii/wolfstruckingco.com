using System.Collections.Generic;

namespace SharedUI.Pages;

public partial class BlogPage
{
    private const string Slug1 = "/Blog/cdl-a-vs-cdl-b-pay";
    private const string Title1 = "CDL-A vs CDL-B pay in 2026";
    private const string Summary1 = "Real numbers from 1,200 driver paystubs across California — when CDL-A pays off and when local CDL-B beats it.";
    private const string Date1 = "Apr 2026";

    private const string Slug2 = "/Blog/port-la-drayage-guide";
    private const string Title2 = "Port of LA drayage guide";
    private const string Summary2 = "Yard congestion, chassis pools, TWIC checks — the full playbook for cracking LA-LB drayage as a new operator.";
    private const string Date2 = "Mar 2026";

    private const string Slug3 = "/Blog/reduce-freight-costs";
    private const string Title3 = "How to cut freight costs 18%";
    private const string Summary3 = "Three vendor consolidation moves a beverage shipper made to save $280K a year on drayage.";
    private const string Date3 = "Feb 2026";

    private static readonly List<Post> Posts =
    [
        new(Slug1, Title1, Summary1, Date1),
        new(Slug2, Title2, Summary2, Date2),
        new(Slug3, Title3, Summary3, Date3),
    ];

    private sealed record Post(string Slug, string Title, string Summary, string Date);
}
