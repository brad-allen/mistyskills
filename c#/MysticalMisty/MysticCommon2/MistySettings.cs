namespace MysticCommon
{
    public class MistySettings
    {
        public static string Voice { get; set; } = ""; //TODO
        public static double VoicePitch { get; set; } = 1.0;
        public static double VoiceSpeed { get; set; } = 1.0;
        public static MysticMood Mood { get; set; } = MysticMood.Funny;
        public static MysticMode Mode { get; set; } = MysticMode.Idle;
        public static SpeechVerbosity SpeechVerbosity { get; set; } = SpeechVerbosity.Average;
    }
}