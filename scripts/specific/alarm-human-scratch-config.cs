return 0;

namespace Scripts
{
    internal static class AlarmHumanScratchConfig
    {
        public const string Headline = "Wolfs pipeline needs you";
        public const string Body = "manual step required - see C:\\Users\\user1\\AppData\\Local\\Temp\\wolfs-human-needed.json";
        public const string AckPath = "C:\\Users\\user1\\AppData\\Local\\Temp\\wolfs-alarm-ack.txt";
        public const int BeepCount = 12;
        public const int BeepFreq = 880;
        public const int BeepMs = 600;
        public const int PollMs = 2000;
        public const int TimeoutSeconds = 1800;
    }
}
