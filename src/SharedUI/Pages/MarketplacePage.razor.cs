using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class MarketplacePage
{
    private const string ListingsStore = "listings";
    private const string LoginRoute = "Login";
    private const string BuyApi = "/api/buy";
    private const string PaymentModeCard = "card";
    private const string RoleEmployer = "employer";
    private const string RoleClient = "client";
    private const string RoleAdmin = "admin";
    private const string RoleStaff = "staff";
    private const string Empty = "";
    private const string KwByd = "byd";
    private const string KwHan = "han";
    private const string KwCar = "car";
    private const string KwSedan = "sedan";
    private const string KwEv = "ev";
    private const string KwVehicle = "vehicle";

    private const string SvgCar = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 110' width='100%' height='100%' role='img' aria-label='product' preserveAspectRatio='xMidYMid meet'><defs><linearGradient id='sky' x1='0' y1='0' x2='0' y2='1'><stop offset='0' stop-color='#dbeafe'/><stop offset='1' stop-color='#f3f6fa'/></linearGradient><linearGradient id='body' x1='0' y1='0' x2='0' y2='1'><stop offset='0' stop-color='#1f2937'/><stop offset='.55' stop-color='#0f172a'/><stop offset='1' stop-color='#000'/></linearGradient><linearGradient id='glass' x1='0' y1='0' x2='0' y2='1'><stop offset='0' stop-color='#cbd5e1'/><stop offset='1' stop-color='#475569'/></linearGradient></defs><rect width='200' height='110' fill='url(#sky)'/><line x1='0' y1='90' x2='200' y2='90' stroke='#94a3b8' stroke-width='.6' stroke-dasharray='2 3'/><ellipse cx='100' cy='90' rx='80' ry='3.5' fill='#000' opacity='.18'/><path d='M22 80 L28 70 L54 60 L72 50 L130 48 L160 56 L182 70 L186 80 Z' fill='url(#body)'/><path d='M58 60 Q70 38 110 38 Q146 38 156 58' fill='url(#body)' stroke='#0f172a' stroke-width='.5'/><path d='M64 58 Q74 44 106 44 Q138 44 150 58 L148 60 L66 60 Z' fill='url(#glass)' opacity='.95'/><path d='M104 44 L104 60' stroke='#0f172a' stroke-width='1.1'/><path d='M138 50 L150 60' stroke='#0f172a' stroke-width='1.1'/><path d='M80 64 L80 78' stroke='#1e293b' stroke-width='.5'/><path d='M114 64 L114 78' stroke='#1e293b' stroke-width='.5'/><rect x='90' y='66' width='8' height='1.4' fill='#475569'/><rect x='124' y='66' width='8' height='1.4' fill='#475569'/><path d='M178 70 q4 -2 6 4 q-1 4 -7 2 z' fill='#fef3c7' stroke='#9a3a10' stroke-width='.4'/><path d='M22 78 q-2 -1 -1 3 q3 1 3 -1 z' fill='#dc2626' stroke='#7f1d1d' stroke-width='.3'/><path d='M170 74 Q176 76 184 76' stroke='#9ca3af' stroke-width='.6' fill='none'/><rect x='150' y='66' width='4' height='2' fill='#3b82f6'/><circle cx='52' cy='86' r='10' fill='#0f172a' stroke='#374151' stroke-width='1.2'/><circle cx='52' cy='86' r='5' fill='#9ca3af'/><circle cx='52' cy='86' r='2.4' fill='#0f172a'/><circle cx='150' cy='86' r='10' fill='#0f172a' stroke='#374151' stroke-width='1.2'/><circle cx='150' cy='86' r='5' fill='#9ca3af'/><circle cx='150' cy='86' r='2.4' fill='#0f172a'/><g stroke='#0f172a' stroke-width='.7'><line x1='47' y1='86' x2='57' y2='86'/><line x1='52' y1='81' x2='52' y2='91'/></g><g stroke='#0f172a' stroke-width='.7'><line x1='145' y1='86' x2='155' y2='86'/><line x1='150' y1='81' x2='150' y2='91'/></g></svg>";
    private const string SvgDefault = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 110' width='100%' height='100%' role='img' aria-label='product' preserveAspectRatio='xMidYMid meet'><rect width='200' height='110' fill='#f3f6fa'/><g fill='none' stroke='#9a3a10' stroke-width='3'><rect x='55' y='30' width='90' height='50'/><line x1='55' y1='50' x2='145' y2='50'/></g></svg>";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private bool Authed { get; set; }

    private bool CanSell { get; set; }

    private List<Listing> Listings { get; set; } = [];

    private NewListing Draft { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        var Auth = await Wolfs.AuthGetAsync();
        Authed = !string.IsNullOrEmpty(Auth.Role);
        CanSell = Authed && (Auth.Role == RoleEmployer || Auth.Role == RoleClient || Auth.Role == RoleAdmin || Auth.Role == RoleStaff);
        try
        {
            Listings = [.. (await Wolfs.DbAllAsync<Listing>(ListingsStore))
                .OrderByDescending(L => L.Id ?? Empty)];
        }
        catch (System.Net.Http.HttpRequestException) { Listings = []; }
        catch (System.Text.Json.JsonException) { Listings = []; }
    }

    private async Task BuyAsync(string Id)
    {
        if (!Authed)
        {
            Nav.NavigateTo(LoginRoute, forceLoad: false);
            return;
        }

        try
        {
            await Wolfs.WorkerPostAsync(BuyApi, new { listingId = Id, qty = 1, paymentMode = PaymentModeCard });
        }
        catch (System.Net.Http.HttpRequestException) { }
    }

    private async Task PostListingAsync()
    {
        if (CanSell) { await Task.CompletedTask; }
    }

    private static string ProductSvg(string Title)
    {
        var T = (Title ?? Empty).ToLowerInvariant();
        return T.Contains(KwByd, StringComparison.Ordinal) || T.Contains(KwHan, StringComparison.Ordinal) ||
               T.Contains(KwCar, StringComparison.Ordinal) || T.Contains(KwSedan, StringComparison.Ordinal) ||
               T.Contains(KwEv, StringComparison.Ordinal) || T.Contains(KwVehicle, StringComparison.Ordinal)
            ? SvgCar
            : SvgDefault;
    }

    public sealed class Listing
    {
        public string Id { get; set; } = Empty;

        public string Title { get; set; } = Empty;

        public string Description { get; set; } = Empty;

        public decimal Price { get; set; }
    }

    public sealed class NewListing
    {
        public string Title { get; set; } = Empty;

        public string Description { get; set; } = Empty;

        public decimal Price { get; set; }
    }
}
