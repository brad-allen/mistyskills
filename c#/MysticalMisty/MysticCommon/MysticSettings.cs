namespace MysticCommon
{
    public class MysticSettings
    {
        public string Voice { get; set; } = "en-us-x-sfg-local";
        public string Language { get; set; } = "en";
        public float VoicePitch { get; set; } = 1.0f;
        public float VoiceSpeed { get; set; } = 1.0f;
        public MysticMood Mood { get; set; } = MysticMood.Funny; //TODO
        public SpeechVerbosity SpeechVerbosity { get; set; } = SpeechVerbosity.Average;  //TODO
    }
}