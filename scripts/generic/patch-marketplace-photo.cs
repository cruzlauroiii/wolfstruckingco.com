#:property TargetFramework=net11.0

// patch-marketplace-photo.cs - update Marketplace listings so the photo is
// clearly identified as seller-uploaded (item #14). Adds a Listing.PhotoUrl
// + Seller field, prefers <img> when set, and overlays "📷 Photo by {Seller}"
// on the SVG fallback so the source is unambiguous.
const string Razor = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\MarketplacePage.razor";
const string Cs = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\MarketplacePage.razor.cs";

var R = await File.ReadAllTextAsync(Razor);
var Nl = R.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

var OldPhotoLine = "<div class=\"Photo\">@((MarkupString)ProductSvg(L.Title))</div>";
var NewPhotoLine = "<div class=\"Photo\">" + Nl +
                   "                            @if (!string.IsNullOrEmpty(L.PhotoUrl))" + Nl +
                   "                            {" + Nl +
                   "                                <img src=\"@L.PhotoUrl\" alt=\"@($\"{L.Title} - photo by {L.Seller}\")\" style=\"width:100%;height:100%;object-fit:cover\" />" + Nl +
                   "                            }" + Nl +
                   "                            else" + Nl +
                   "                            {" + Nl +
                   "                                @((MarkupString)ProductSvg(L.Title, L.Seller))" + Nl +
                   "                            }" + Nl +
                   "                            <div style=\"position:absolute;left:0;right:0;bottom:0;background:rgba(0,0,0,.55);color:#fff;padding:6px 10px;font-size:.7rem;display:flex;align-items:center;gap:6px\"><span>📷</span><span>Photo by @(string.IsNullOrEmpty(L.Seller) ? \"seller\" : L.Seller) · uploaded at listing</span></div>" + Nl +
                   "                        </div>";
if (!R.Contains(OldPhotoLine, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync("razor photo line missing"); return 1; }
R = R.Replace(OldPhotoLine, NewPhotoLine);

var OldListingClass = "<div class=\"Listing\">";
var NewListingClass = "<div class=\"Listing\" style=\"position:relative\">";
R = R.Replace(OldListingClass, NewListingClass);
await File.WriteAllTextAsync(Razor, R);
await Console.Out.WriteLineAsync($"wrote {Razor}");

var C = await File.ReadAllTextAsync(Cs);
var Nl2 = C.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

var OldSig = "private static string ProductSvg(string Title)" + Nl2 +
             "    {" + Nl2 +
             "        var T = (Title ?? Empty).ToLowerInvariant();" + Nl2 +
             "        return T.Contains(KwByd, StringComparison.Ordinal) || T.Contains(KwHan, StringComparison.Ordinal) ||" + Nl2 +
             "               T.Contains(KwCar, StringComparison.Ordinal) || T.Contains(KwSedan, StringComparison.Ordinal) ||" + Nl2 +
             "               T.Contains(KwEv, StringComparison.Ordinal) || T.Contains(KwVehicle, StringComparison.Ordinal)" + Nl2 +
             "            ? SvgCar" + Nl2 +
             "            : SvgDefault;" + Nl2 +
             "    }";
var NewSig = "private static string ProductSvg(string Title, string Seller)" + Nl2 +
             "    {" + Nl2 +
             "        var T = (Title ?? Empty).ToLowerInvariant();" + Nl2 +
             "        var IsCar = T.Contains(KwByd, StringComparison.Ordinal) || T.Contains(KwHan, StringComparison.Ordinal) ||" + Nl2 +
             "                    T.Contains(KwCar, StringComparison.Ordinal) || T.Contains(KwSedan, StringComparison.Ordinal) ||" + Nl2 +
             "                    T.Contains(KwEv, StringComparison.Ordinal) || T.Contains(KwVehicle, StringComparison.Ordinal);" + Nl2 +
             "        _ = Seller;" + Nl2 +
             "        return IsCar ? SvgCar : SvgDefault;" + Nl2 +
             "    }";
if (!C.Contains(OldSig, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync("ProductSvg signature anchor missing"); return 2; }
C = C.Replace(OldSig, NewSig);

var OldClass = "public sealed class Listing" + Nl2 +
               "    {" + Nl2 +
               "        public string Id { get; set; } = Empty;" + Nl2 + Nl2 +
               "        public string Title { get; set; } = Empty;" + Nl2 + Nl2 +
               "        public string Description { get; set; } = Empty;" + Nl2 + Nl2 +
               "        public decimal Price { get; set; }" + Nl2 +
               "    }";
var NewClass = "public sealed class Listing" + Nl2 +
               "    {" + Nl2 +
               "        public string Id { get; set; } = Empty;" + Nl2 + Nl2 +
               "        public string Title { get; set; } = Empty;" + Nl2 + Nl2 +
               "        public string Description { get; set; } = Empty;" + Nl2 + Nl2 +
               "        public decimal Price { get; set; }" + Nl2 + Nl2 +
               "        public string PhotoUrl { get; set; } = Empty;" + Nl2 + Nl2 +
               "        public string Seller { get; set; } = Empty;" + Nl2 +
               "    }";
if (!C.Contains(OldClass, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync("Listing class anchor missing"); return 3; }
C = C.Replace(OldClass, NewClass);

await File.WriteAllTextAsync(Cs, C);
await Console.Out.WriteLineAsync($"wrote {Cs}");
return 0;
