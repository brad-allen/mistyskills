using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesManagement
{
    public class SettingsModePackage : BaseModePackage
    {
        private bool _repeatTime;
        private IList<string> _responses1 = new List<string>();
        private IList<string> _responses2 = new List<string>();
        private Random _random = new Random();
        public SettingsModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            _repeatTime = true;
            _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops
            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            _ = Misty.SpeakAsync($"Say, Hey Misty, and talk to me!", true, "Settings");

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
            samples.Add("settings");
            samples.Add("control panel");

            intent = new Intent
            {
                Name = "Settings",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        private void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (_repeatTime && robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.CompletedASR)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "SettingsPhraseRetry");
                }
                else
                {
                    try
                    {
                       
                    }
                    catch
                    {
                        //should never happen
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "SettingsPhraseError");                        
                    }
                }

                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            }
        }
    }
}