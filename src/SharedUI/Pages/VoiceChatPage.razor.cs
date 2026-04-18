using System;
using System.Collections.Generic;
using Domain.Constants;

namespace SharedUI.Pages;

public partial class VoiceChatPage
{
    private const int Year = 2026;
    private const int Month = 4;
    private const int HourEarly = 5;
    private const int HourLate = 6;
    private const int Min02 = 2;
    private const int Min16 = 16;
    private const int Min20 = 20;
    private const int Min22 = 22;

    private const string Msg1 = "Good morning Jake. Your route is loaded for today. 4 deliveries, 287 miles. First pickup at Hickory warehouse.";
    private const string Msg2 = "Copy that dispatch. Pre-trip complete, heading to Hickory now.";
    private const string Msg3 = "10-4. Blue Ridge Furniture is expecting you at dock 7. BOL is in your tablet.";
    private const string Msg4 = "Loaded up at Hickory. 2,400 lbs furniture. Heading to Charlotte distribution center.";
    private const string Msg5 = "Roger. Traffic on I-77 is clear. ETA 7:45 AM. Charlotte receiving confirmed dock B.";

    private bool IsConnected { get; set; } = true;

    private List<ChatMessage> Messages { get; set; } = [];

    protected override void OnInitialized() =>
        Messages =
        [
            new() { Text = Msg1, IsUser = false, Timestamp = new DateTime(Year, Month, ScheduleConstants.PreTripInspectionMinutes, HourEarly, Min02, 0, DateTimeKind.Local) },
            new() { Text = Msg2, IsUser = true,  Timestamp = new DateTime(Year, Month, ScheduleConstants.PreTripInspectionMinutes, HourEarly, ScheduleConstants.PreTripInspectionMinutes, 0, DateTimeKind.Local) },
            new() { Text = Msg3, IsUser = false, Timestamp = new DateTime(Year, Month, ScheduleConstants.PreTripInspectionMinutes, HourEarly, Min16, 0, DateTimeKind.Local) },
            new() { Text = Msg4, IsUser = true,  Timestamp = new DateTime(Year, Month, ScheduleConstants.PreTripInspectionMinutes, HourLate,  Min20, 0, DateTimeKind.Local) },
            new() { Text = Msg5, IsUser = false, Timestamp = new DateTime(Year, Month, ScheduleConstants.PreTripInspectionMinutes, HourLate,  Min22, 0, DateTimeKind.Local) },
        ];

    private void ToggleConnection() => IsConnected = !IsConnected;

    private sealed class ChatMessage
    {
        public string Text { get; set; } = string.Empty;

        public bool IsUser { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
