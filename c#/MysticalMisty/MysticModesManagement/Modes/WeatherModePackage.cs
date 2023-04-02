using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesManagement
{
    public class WeatherModePackage : BaseAllModesPackage
    {
        private bool _repeatTime;
        private IList<string> _responses1 = new List<string>();
        private IList<string> _responses2 = new List<string>();
        private Random _random = new Random();
        public WeatherModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
      
           // _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops
            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            _ = Misty.SpeakAsync($"Say, Hey Misty, and talk to me!", true, "Weather");

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            //Do cleanup here...
            _repeatTime = false;
            Misty.UnregisterEvent("Test-is-this-needed", null);
            //TODO make a disposable?
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("weather");
            samples.Add("whether");
            samples.Add("temperature");
            samples.Add("cold outside");
            samples.Add("hot outside");

            intent = new Intent
            {
                Name = "Weather",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (_repeatTime && robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.FinalIntent)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "WeatherPhraseRetry");
                }
                else
                {
                    try
                    {
                        int random1 = _random.Next(0, _responses1.Count);
                        int random2 = _random.Next(0, _responses2.Count);
                        string response1 = _responses1[random1];
                        string response2 = _responses2[random2];
                        _ = Misty.SpeakAsync($"{response1}. {robotInteractionEvent.DialogState.Text}. {response2}", true, "WeatherPhrase");
                    }
                    catch
                    {
                        //should never happen
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "WeatherPhraseError");                        
                    }
                }

                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            }
        }
    }
}