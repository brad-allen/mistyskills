using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesDemo
{
    public class IdleModePackage : BaseModePackage
    {
        private bool _repeatTime;
        public IdleModePackage(IRobotMessenger misty) : base(misty) { }

        public override async Task<ResponsePacket> Start(IDictionary<string, object> parameters)
        {
            _repeatTime = true;
            _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops

            //Set the intents of this modes
            //overlap contexts if want to add to the existing ones, filtered intents will remove those options from any current contexts after processing, retrain is experimental
            _ = Misty.SetContextAsync("AllModes", false, null, false);
            _ = Misty.SpeakAsync("Okay, when you are ready, say, Hey Misty, and tell me what you want to do.", true, "idleSpeech");

            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 2500);

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
            samples.Add("idle");
            samples.Add("be quiet");
            samples.Add("silence");
            samples.Add("silent");
            samples.Add("shut up");
            samples.Add("stop");

            intent = new Intent
            {
                Name = "Idle",
                Samples = samples
            };
            return true;
        }

        public override void ProcessEvent(MysticEvent mysticEvent)
        {

        }
    }
}