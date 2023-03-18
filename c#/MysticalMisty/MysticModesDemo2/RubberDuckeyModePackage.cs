using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesDemo
{
    public class RubberDuckeyModePackage : BaseModePackage
    {
        private bool _repeatTime;
        private IList<string> _responses1 = new List<string>();
        private IList<string> _responses2 = new List<string>();
        private Random _random = new Random();
        public RubberDuckeyModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(IDictionary<string, object> parameters)
        {
            _responses1.Add("Hmmmm.");
            _responses1.Add(" ... ");
            _responses1.Add(" Okay, let me think.");
            _responses1.Add("So think it through.");
            _responses1.Add("Well, let me ponder that.");
            _responses1.Add("What you are saying is ");

            _responses2.Add("Are you sure about that?");
            _responses2.Add("Seems to make sense.");
            _responses2.Add("I'm not so sure.");
            _responses2.Add("Can that be true?");
            _responses2.Add("Seems clear to me?");
            _responses2.Add("Seems unclear to me? Can you say that a different way?");

            _repeatTime = true;
            _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops
            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            _ = Misty.SpeakAsync($"Say, Hey Misty, and talk to me!", true, "RubberDuckey");

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
            samples.Add("rubber duckey");
            samples.Add("duckey");
            samples.Add("duck");

            intent = new Intent
            {
                Name = "RubberDuckey",
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
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "RubberDuckeyPhraseRetry");
                }
                else
                {
                    try
                    {
                        int random1 = _random.Next(0, _responses1.Count);
                        int random2 = _random.Next(0, _responses2.Count);
                        string response1 = _responses1[random1];
                        string response2 = _responses2[random2];
                        _ = Misty.SpeakAsync($"{response1}. {mysticEvent.RobotInteractionEvent.DialogState.Text}. {response2}", true, "RubberDuckeyPhrase");
                    }
                    catch
                    {
                        //should never happen
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "RubberDuckeyPhraseError");                        
                    }
                }

                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            }
        }
    }
}