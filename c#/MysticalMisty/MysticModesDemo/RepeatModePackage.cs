using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesDemo
{
    public class RepeatModePackage : BaseModePackage
    {
        private bool _repeatTime;
        public RepeatModePackage(IRobotMessenger misty) : base(misty) {}

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
            Misty.UnregisterEvent("Test-is-this-needed", null);
            //TODO make a disposable?
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("repeat");

            intent = new Intent
            {
                Name = "Repeat",
                Samples = samples
            };
            return true;
        }

        public override void ProcessEvent(MysticEvent mysticEvent)
        {
            if (_repeatTime && mysticEvent.RobotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.CompletedASR)
            {
                if (string.IsNullOrWhiteSpace(mysticEvent.RobotInteractionEvent.DialogState.Text))
                {
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "RepeatPhraseRetry");
                    _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                }
                else
                {
                    _ = Misty.SpeakAndListenAsync($"{mysticEvent.RobotInteractionEvent.DialogState.Text}. Okay. Say something else.", true, "RepeatPhrase", null);
                }
            }
        }
    }
}