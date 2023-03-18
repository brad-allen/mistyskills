using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesDemo
{
    public class BackwardModePackage : BaseModePackage
    {
        private bool _repeatTime;
        public BackwardModePackage(IRobotMessenger misty) : base(misty) {}
        
        public override async Task<ResponsePacket> Start(IDictionary<string, object> parameters)
        {
            _repeatTime = true;
            _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops
            _ = Misty.SpeakAndListenAsync("Say something.", true, "saying", null);
            
            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            //Do cleanup here...
            _repeatTime = false;
            _ = Misty.StopKeyPhraseRecognitionAsync();
            Misty.UnregisterEvent("Test-is-this-needed", null);
            //TODO make a disposable?
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("backward");
            samples.Add("back ward");
            samples.Add("reverse");

            intent = new Intent
            {
                Name = "Backward",
                Samples = samples
            };
            return true;
        }

        public override void ProcessEvent(MysticEvent mysticEvent)
        {
            if (_repeatTime && mysticEvent.RobotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.CompletedASR)
            {
                if(string.IsNullOrWhiteSpace(mysticEvent.RobotInteractionEvent.DialogState.Text))
                {
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "BackwardPhraseRetry");
                    _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                }
                else
                {
                    char[] charArray = mysticEvent.RobotInteractionEvent.DialogState.Text.ToCharArray();
                    Array.Reverse(charArray);
                    _ = Misty.SpeakAndListenAsync($"{new string(charArray)}. Okay, say something else.", true, "BackwardPhrase", null);
                }
            }
        }
    }
}