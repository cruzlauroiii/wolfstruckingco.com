using System.Collections.Generic;

namespace SharedUI.Pages;

public partial class CaseStudiesPage
{
    private const string Industry1 = "Beverage distribution";
    private const string Customer1 = "BevCo (anonymized)";
    private const string Story1 = "BevCo had been losing 18% of margin to multiple drayage carriers and reefer no-shows. We consolidated their LA-LB drayage onto a single dedicated route with reefer-equipped trucks and live tracking.";
    private const string M1A = "$280K";
    private const string M1ALabel = "annual savings";
    private const string M1B = "99.1%";
    private const string M1BLabel = "on-time";
    private const string M1C = "3 days";
    private const string M1CLabel = "implementation";

    private const string Industry2 = "Construction supply";
    private const string Customer2 = "BuildCorp";
    private const string Story2 = "BuildCorp's flatbed needs spiked seasonally. Wolfs absorbed surge capacity and routed loads through our Burbank yard for cross-docking before final delivery.";
    private const string M2A = "47%";
    private const string M2ALabel = "faster cycle time";
    private const string M2B = "$95K";
    private const string M2BLabel = "saved Q3";
    private const string M2C = "0";
    private const string M2CLabel = "missed deliveries";

    private static readonly List<Case> Cases =
    [
        new(Industry1, Customer1, Story1,
            [new(M1A, M1ALabel), new(M1B, M1BLabel), new(M1C, M1CLabel)]),
        new(Industry2, Customer2, Story2,
            [new(M2A, M2ALabel), new(M2B, M2BLabel), new(M2C, M2CLabel)]),
    ];

    private sealed record Metric(string Value, string Label);

    private sealed record Case(string Industry, string Customer, string Story, List<Metric> Metrics);
}
