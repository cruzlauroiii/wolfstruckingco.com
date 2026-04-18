namespace Domain.Constants;

public static class ChatVariantConstants
{
    public const string DriverRolePrefix = "driver";
    public const string ApplicantRole = "applicant";
    public const string DispatcherRole = "dispatcher";

    public const string SellSystemPrompt = UnifiedSystemPrompt;
    public const string ApplicantSystemPrompt = UnifiedSystemPrompt;
    public const string DispatcherSystemPrompt = UnifiedSystemPrompt;
    public const string UnifiedSystemPrompt = "You are Wolfs Trucking Co.'s unified dispatcher. Figure out from the user's first message whether they are shipping freight, applying to drive, browsing the marketplace, coordinating an active driver, or reviewing applications, then continue the conversation in that mode. Reply in 1 to 2 short sentences. Ask ONE focused follow-up question per turn or confirm the next step. Every Wolfs flow is in scope: shipment posting, driver applications and badge intake including CDL-A, CDL-B, Hazmat, Tanker, Doubles, PortPass, Interstate, Auto, China-export, and Team, live call dispatching, marketplace purchases, and admin review. Write in plain natural English. Do not use markdown, asterisks, emoji, numbered placeholders, or template tokens of any kind.";

    public const string SellerLede = "Tell the agent what you're shipping. He'll write the job posting for you.";
    public const string SellerPlaceholder = "What are you shipping?";
    public const string SellerCallTitle = "Call agent";
    public const string SellerAttachTitle = "Attach photo / receipt";
    public const string SellerSubject = "seller-job-intake";

    public const string DriverLede = "Answer the agent's questions and attach scans of any badge you have.";
    public const string DriverPlaceholder = "Tell us about your driving experience";
    public const string DriverCallTitle = "Call agent";
    public const string DriverAttachTitle = "Attach badge / cert scan";
    public const string DriverSubject = "driver-application-intake";

    public const string DispatcherLede = "Live call. Talk to your driver hands-free.";
    public const string DispatcherPlaceholder = "Status update or directive";
    public const string DispatcherCallTitle = "Call driver";
    public const string DispatcherAttachTitle = "Attach photo";
    public const string DispatcherSubject = "dispatcher-call";
}
