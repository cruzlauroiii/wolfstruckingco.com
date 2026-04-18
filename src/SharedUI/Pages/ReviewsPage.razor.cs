using System.Collections.Generic;

namespace SharedUI.Pages;

public partial class ReviewsPage
{
    private const int Stars5 = 5;
    private const int Stars4 = 4;

    private const string Quote1 = "On-time every single load. Their drayage out of Long Beach saves us 2 hours per container.";
    private const string Author1 = "Maria S.";
    private const string Role1 = "Logistics Mgr · Beverage co.";
    private const string Quote2 = "Easy to book online. Driver was professional and the load arrived early.";
    private const string Author2 = "James T.";
    private const string Role2 = "Warehouse Operator";
    private const string Quote3 = "$3K bonus, home weekly, late-model truck. Best move I've made in 8 years.";
    private const string Author3 = "Jordan V.";
    private const string Role3 = "CDL-A Driver";
    private const string Quote4 = "Pricing transparent. Customer service prompt. Fleet always on-time.";
    private const string Author4 = "Anita K.";
    private const string Role4 = "Procurement";

    private static readonly List<Review> Items =
    [
        new(Stars5, Quote1, Author1, Role1),
        new(Stars5, Quote2, Author2, Role2),
        new(Stars5, Quote3, Author3, Role3),
        new(Stars4, Quote4, Author4, Role4),
    ];

    private sealed record Review(int Stars, string Quote, string Author, string Role);
}
