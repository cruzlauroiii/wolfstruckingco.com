using System;
using Domain.Constants;

namespace SharedUI.Pages;

public partial class ChatPage
{
    private static readonly ChatVariant SellerVariant = new(
        ChatVariantConstants.SellSystemPrompt,
        ChatVariantConstants.SellerLede,
        ChatVariantConstants.SellerPlaceholder,
        ChatVariantConstants.SellerCallTitle,
        ChatVariantConstants.SellerAttachTitle,
        ChatVariantConstants.SellerSubject);

    private static readonly ChatVariant DriverVariant = new(
        ChatVariantConstants.ApplicantSystemPrompt,
        ChatVariantConstants.DriverLede,
        ChatVariantConstants.DriverPlaceholder,
        ChatVariantConstants.DriverCallTitle,
        ChatVariantConstants.DriverAttachTitle,
        ChatVariantConstants.DriverSubject);

    private static readonly ChatVariant DispatcherChatVariant = new(
        ChatVariantConstants.DispatcherSystemPrompt,
        ChatVariantConstants.DispatcherLede,
        ChatVariantConstants.DispatcherPlaceholder,
        ChatVariantConstants.DispatcherCallTitle,
        ChatVariantConstants.DispatcherAttachTitle,
        ChatVariantConstants.DispatcherSubject);

    private bool ShowSignInPrompt { get; set; }

    private ChatVariant CurrentVariant { get; set; } = DriverVariant;

    private static ChatVariant ResolveVariantByRole(string Role)
    {
        return Role switch
        {
            ChatVariantConstants.ApplicantRole => DriverVariant,
            ChatVariantConstants.DispatcherRole => DispatcherChatVariant,
            var R when R.StartsWith(ChatVariantConstants.DriverRolePrefix, StringComparison.Ordinal) => DriverVariant,
            _ => SellerVariant,
        };
    }

    private sealed record ChatVariant(string SystemPrompt, string Lede, string Placeholder, string CallTitle, string AttachTitle, string Subject);
}
