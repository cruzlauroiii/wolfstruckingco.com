using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class DocumentsPage
{
    private const string AuditStore = "audit";
    private const string FieldKind = "kind";
    private const string FieldSubject = "subject";
    private const string KindUpload = "documents.upload";
    private const string Empty = "";

    private const string CertCdlA = "CDL · Class A";
    private const string CertCdlAMatch = "cdl";
    private const string CertCdlADetail = "Front + back. Required for tractor-trailer.";
    private const string CertCdlB = "CDL · Class B";
    private const string CertCdlBMatch = "class b";
    private const string CertCdlBDetail = "Required for straight trucks and buses.";
    private const string CertMed = "DOT medical card";
    private const string CertMedMatch = "medical";
    private const string CertMedDetail = "Long form, valid 24 months.";
    private const string CertHaz = "Hazmat endorsement";
    private const string CertHazMatch = "hazmat";
    private const string CertHazDetail = "Required for placarded loads.";
    private const string CertTanker = "Tanker endorsement";
    private const string CertTankerMatch = "tanker";
    private const string CertTankerDetail = "Required for liquid bulk.";
    private const string CertDoubles = "Doubles + triples";
    private const string CertDoublesMatch = "double";
    private const string CertDoublesDetail = "Required for multi-trailer.";
    private const string CertPassenger = "Passenger endorsement";
    private const string CertPassengerMatch = "passenger";
    private const string CertPassengerDetail = "Required for buses and vans.";
    private const string CertSchoolBus = "School bus";
    private const string CertSchoolBusMatch = "school bus";
    private const string CertSchoolBusDetail = "Required for student transport.";
    private const string CertAirBrake = "Air-brake cert";
    private const string CertAirBrakeMatch = "air brake";
    private const string CertAirBrakeDetail = "Required for full-size combo trucks.";
    private const string CertTwic = "Port pass · TWIC";
    private const string CertTwicMatch = "twic";
    private const string CertTwicDetail = "Federal port-of-entry credential.";
    private const string CertDrayage = "Drayage cert";
    private const string CertDrayageMatch = "drayage";
    private const string CertDrayageDetail = "Harbor commission yard access.";
    private const string CertInterstate = "Interstate authority";
    private const string CertInterstateMatch = "interstate";
    private const string CertInterstateDetail = "MC number for cross-state hauling.";
    private const string CertTeam = "Team-driver cert";
    private const string CertTeamMatch = "team";
    private const string CertTeamDetail = "Split-sleeper-berth qualification.";
    private const string CertAuto = "Auto-handling final";
    private const string CertAutoMatch = "auto-handling";
    private const string CertAutoDetail = "Sealed-vehicle delivery training.";
    private const string CertChina = "China export driver";
    private const string CertChinaMatch = "china export";
    private const string CertChinaDetail = "Customs broker letter for CN origin.";
    private const string CertInsurance = "Insurance binder";
    private const string CertInsuranceMatch = "insurance";
    private const string CertInsuranceDetail = "Liability + cargo coverage.";
    private const string CertVehicle = "Vehicle inspection";
    private const string CertVehicleMatch = "vehicle inspection";
    private const string CertVehicleDetail = "Annual DVIR sign-off.";
    private const string CertLicense = "Driver's license";
    private const string CertLicenseMatch = "license";
    private const string CertLicenseDetail = "State-issued ID, photo, current.";

    private static readonly (string Name, string Match, string Detail)[] Certs =
    [
        (CertCdlA, CertCdlAMatch, CertCdlADetail),
        (CertCdlB, CertCdlBMatch, CertCdlBDetail),
        (CertMed, CertMedMatch, CertMedDetail),
        (CertHaz, CertHazMatch, CertHazDetail),
        (CertTanker, CertTankerMatch, CertTankerDetail),
        (CertDoubles, CertDoublesMatch, CertDoublesDetail),
        (CertPassenger, CertPassengerMatch, CertPassengerDetail),
        (CertSchoolBus, CertSchoolBusMatch, CertSchoolBusDetail),
        (CertAirBrake, CertAirBrakeMatch, CertAirBrakeDetail),
        (CertTwic, CertTwicMatch, CertTwicDetail),
        (CertDrayage, CertDrayageMatch, CertDrayageDetail),
        (CertInterstate, CertInterstateMatch, CertInterstateDetail),
        (CertTeam, CertTeamMatch, CertTeamDetail),
        (CertAuto, CertAutoMatch, CertAutoDetail),
        (CertChina, CertChinaMatch, CertChinaDetail),
        (CertInsurance, CertInsuranceMatch, CertInsuranceDetail),
        (CertVehicle, CertVehicleMatch, CertVehicleDetail),
        (CertLicense, CertLicenseMatch, CertLicenseDetail),
    ];

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private List<string> UploadedSubjects { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        var Rows = await Wolfs.DbAllAsync<JsonObject>(AuditStore);
        UploadedSubjects = [.. Rows
            .Where(R => R is not null && R[FieldKind]?.GetValue<string>() == KindUpload)
            .Select(R => R?[FieldSubject]?.GetValue<string>() ?? Empty)];
    }
}
