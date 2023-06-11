using System.Collections;
using System.Collections.Generic;

namespace MysticCommon
{
    public class MysticLanguages
    {
        /// <summary>
        /// Misty's onboard languages?
        /// </summary>
        public static IDictionary AllLanguages { get; private set; } = new Dictionary<string, string>()
        {
            { "ar", "Arabic" },
            { "bn", "Bengali" },
            { "cmn", "Chinese, Mandarin" },
            { "cs", "Czech" },
            { "da", "Danish" },
            { "de", "German" },
            { "el", "Greek" },
            { "en", "English" },
            { "es", "Spanish" },
            { "et", "Estonian" },
            { "fi", "Finish" },
            { "fil", "Filipino" },
            { "fr", "French" },
            { "gu", "Gujarati" },
            { "hi", "Hindi" },
            { "hu", "Hunagrian" },
            { "id", "Indonesion" },
            { "it", "Italian" },
            { "ja", "Japanese" },
            { "jv", "Javanese" },
            { "km", "Khmer" },
            { "kn", "Kannada" },
            { "ko", "Korean" },
            { "ml", "Malayalam" },
            { "mr", "Marathi" },
            { "nb", "Norweigan" },
            { "ne", "Nepali" },
            { "nl", "Dutch" },
            { "pl", "Polish" },
            { "pt", "Portuguese" },
            { "ro", "Romanian" },
            { "ru", "Russian" },
            { "si", "Sinhala" },
            { "sk", "Slovak" },
            { "su", "Sundanese" },
            { "sv", "Swedish" },
            { "ta", "Tamil" },
            { "te", "Telugu" },
            { "th", "Thai" },
            { "uk", "Ukrainian" },
            { "ur", "Urdu" },
            { "vi", "Vietnamese" },
            { "yue", "Chinese, Cantonese" }
        };
    }
}